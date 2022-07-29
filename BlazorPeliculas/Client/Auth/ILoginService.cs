using BlazorPeliculas.Shared.DTOs;
using System.Threading.Tasks;

namespace BlazorPeliculas.Client.Auth
{
    public interface ILoginService
    {
        Task Login(UserToken userToken);
        Task Logout();
        Task ManejarRenovacionToken();
    }
}
