﻿using Microsoft.JSInterop;
using System.Threading.Tasks;

namespace BlazorPeliculas.Client.Helpers
{
    public class MostrarMensajes : IMostrarMensajes
    {
        private readonly IJSRuntime _js;

        public MostrarMensajes(IJSRuntime js)
        {
            _js = js;
        }
        public async Task MostrarMensajeError(string mensaje)
        {
            await MostrarMensaje("Error", mensaje, "error");
        }

        public async Task MostrarMensajeExitoso(string mensaje)
        {
            await MostrarMensaje("Exitoso", mensaje, "success");
        }
        private async ValueTask MostrarMensaje(string titulo, string mensaje, string tipoMensaje)
        {
            //(Swal.fire
            //  'The Internet?',
            //  'That thing is still around?',
            //  'question')
            await _js.InvokeVoidAsync("Swal.fire", titulo, mensaje, tipoMensaje);
        }
    }
}
