using BlazorPeliculas.Shared.Entidades;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Threading.Tasks;

namespace BlazorPeliculas.Server.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class VotosController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<IdentityUser> _userManager;

        public VotosController(ApplicationDbContext context,
            UserManager<IdentityUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        [HttpPost]
        [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
        public async Task<ActionResult> Votar(VotoPelicula votoPelicula)
        {
            var user = await _userManager.FindByEmailAsync(HttpContext.User.Identity.Name);
            var userId = user.Id;
            var votoActual = await _context.VotosPeliculas
                .FirstOrDefaultAsync(x => x.PeliculaId == votoPelicula.PeliculaId && x.UserId == userId);

            if (votoActual == null)
            {
                votoPelicula.UserId = userId;
                votoPelicula.FechaVoto = DateTime.Today;
                _context.Add(votoPelicula);
                await _context.SaveChangesAsync();
            }
            else
            {
                votoActual.Voto = votoPelicula.Voto;
                await _context.SaveChangesAsync();
            }

            return NoContent();
        }
    }
}
