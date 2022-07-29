using System;
using System.Timers;

namespace BlazorPeliculas.Client.Auth
{
    public class RenovadorToken : IDisposable
    {
        Timer timer;
        private readonly ILoginService _loginService;
        public RenovadorToken(ILoginService loginService)
        {
            _loginService = loginService;
        }

        public void Iniciar()
        {
            timer = new Timer();
            timer.Interval = 5000;//1000 * 60 * 4; // 4 minutos.
            timer.Elapsed += Timer_Elapsed;
            timer.Start();
        }
        private void Timer_Elapsed(object sender, ElapsedEventArgs args)
        {
            _loginService.ManejarRenovacionToken();
        }
        public void Dispose()
        {
            timer?.Dispose();
        }
    }
}
