using BlazorPeliculas.Client.Helpers;
using BlazorPeliculas.Client.Repositorios;
using BlazorPeliculas.Shared.DTOs;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.JSInterop;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Text.Json;
using System.Threading.Tasks;

namespace BlazorPeliculas.Client.Auth
{
    public class ProveedorAutenticacionJWT : AuthenticationStateProvider, ILoginService
    {
        public static readonly string TOKENKEY = "TOKENKEY";
        public static readonly string EXPIRATIONTOKENKEY = "EXPIRATIONTOKENKEY";
        private readonly IJSRuntime _js;
        private readonly HttpClient _httpClient;
        private readonly IRepositorio _repositorio;

        private AuthenticationState Anonimo => 
            new AuthenticationState(new ClaimsPrincipal(new ClaimsIdentity()));

        public ProveedorAutenticacionJWT(IJSRuntime js, HttpClient httpClient, IRepositorio repositorio)
        {
            _js = js;
            _httpClient = httpClient;
            _repositorio = repositorio;
        }
        public async override Task<AuthenticationState> GetAuthenticationStateAsync()
        {
            var token = await _js.GetFromLocalStorage(TOKENKEY);
            if (string.IsNullOrEmpty(token)) { return Anonimo; }

            var tiempoExpiracionString = await _js.GetFromLocalStorage(EXPIRATIONTOKENKEY);
            DateTime tiempoExpiracion;

            if (DateTime.TryParse(tiempoExpiracionString, out tiempoExpiracion))
            {
                if (TokenExpirado(tiempoExpiracion)) 
                {
                    await Limpiar();
                    return Anonimo;
                }
                if (DebeRenovarToken(tiempoExpiracion))
                {
                    token = await RenovarToken(token);
                }

            }

            return ConstruirAuthenticationState(token);
        }
        public async Task ManejarRenovacionToken()
        {
            var tiempoExpiracionString = await _js.GetFromLocalStorage(EXPIRATIONTOKENKEY);
            DateTime tiempoExpiracion;

            if (DateTime.TryParse(tiempoExpiracionString, out tiempoExpiracion))
            {
                if (TokenExpirado(tiempoExpiracion))
                {
                    await Logout();
                }
                if (DebeRenovarToken(tiempoExpiracion))
                {
                    var token = await _js.GetFromLocalStorage(TOKENKEY);
                    var nuevoToken = await RenovarToken(token);
                    var authState = ConstruirAuthenticationState(nuevoToken);
                    NotifyAuthenticationStateChanged(Task.FromResult(authState));
                }
            }
        }
        private async Task<string> RenovarToken(string token)
        {
            Console.WriteLine("Renovando el token...");
            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("bearer", token);
            var nuevoTokenResponse = await _repositorio.Get<UserToken>("api/cuentas/RenovarToken");
            var nuevoToken = nuevoTokenResponse.Response;
            await _js.SetInLocalStorage(TOKENKEY, nuevoToken.Token);
            await _js.SetInLocalStorage(EXPIRATIONTOKENKEY, nuevoToken.Expiration.ToString());
            return nuevoToken.Token;
        }
        private bool DebeRenovarToken(DateTime tiempoExpiracion)
        {
            return tiempoExpiracion.Subtract(DateTime.Now) < TimeSpan.FromMinutes(5);
        }
        private bool TokenExpirado(DateTime tiempoExpiracion)
        {
            return tiempoExpiracion <= DateTime.Now;
        }
        private AuthenticationState ConstruirAuthenticationState(string token)
        {
            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("bearer", token);
            return new AuthenticationState(new ClaimsPrincipal(new ClaimsIdentity(ParseClaimsFromJwt(token), "jwt")));
        }
        private IEnumerable<Claim> ParseClaimsFromJwt(string jwt)
        {
            var claims = new List<Claim>();
            var payload = jwt.Split('.')[1];
            var jsonBytes = ParseBase64WithoutPadding(payload);
            var keyValuePairs = JsonSerializer.Deserialize<Dictionary<string, object>>(jsonBytes);

            keyValuePairs.TryGetValue(ClaimTypes.Role, out object roles);

            if (roles != null)
            {
                if (roles.ToString().Trim().StartsWith("["))
                {
                    var parsedRoles = JsonSerializer.Deserialize<string[]>(roles.ToString());

                    foreach (var parsedRole in parsedRoles)
                    {
                        claims.Add(new Claim(ClaimTypes.Role, parsedRole));
                    }
                }
                else
                {
                    claims.Add(new Claim(ClaimTypes.Role, roles.ToString()));
                }

                keyValuePairs.Remove(ClaimTypes.Role);
            }

            claims.AddRange(keyValuePairs.Select(kvp => new Claim(kvp.Key, kvp.Value.ToString())));
            return claims;
        }

        private byte[] ParseBase64WithoutPadding(string base64)
        {
            switch (base64.Length % 4)
            {
                case 2: base64 += "=="; break;
                case 3: base64 += "="; break;
            }
            return Convert.FromBase64String(base64);
        }

        public async Task Login(UserToken userToken)
        {
            await _js.SetInLocalStorage(TOKENKEY, userToken.Token);
            await _js.SetInLocalStorage(EXPIRATIONTOKENKEY, userToken.Expiration.ToString());
            var authState = ConstruirAuthenticationState(userToken.Token);
            NotifyAuthenticationStateChanged(Task.FromResult(authState));
        }

        public async Task Logout()
        {
            await Limpiar();
            NotifyAuthenticationStateChanged(Task.FromResult(Anonimo));
        }
        private async Task Limpiar()
        {
            await _js.RemoveItem(TOKENKEY);
            await _js.RemoveItem(EXPIRATIONTOKENKEY);
            _httpClient.DefaultRequestHeaders.Authorization = null;
        }
    }
}
