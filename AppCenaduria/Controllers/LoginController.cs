using Supabase;
using AppCenaduria.Models;
using Microsoft.Maui.Authentication;
using System.Threading.Tasks;
using Microsoft.Maui.Storage;

namespace AppCenaduria.Controllers
{
    public class LoginController
    {
        private readonly Supabase.Client _supabase;

        public LoginController(Supabase.Client supabase)
        {
            _supabase = supabase;
        }

        public async Task<Usuario> IniciarSesionGoogleAsync(string accessToken, string refreshToken)
        {

            var sesionGoogle = await _supabase.Auth.SetSession(accessToken, refreshToken);

            string correoFiltro = sesionGoogle.User.Email;

            var respuesta = await _supabase.From<Usuario>()
                                           .Where(x => x.CorreoGoogle == correoFiltro)
                                           .Get();

            var usuarioDb = respuesta.Models.FirstOrDefault();

            string tokenCelular = Preferences.Get("FCMTokenGuardado", "");

            if (usuarioDb == null)
            {
                usuarioDb = new Usuario
                {
                    CorreoGoogle = correoFiltro,
                    NombreCompleto = "Cliente Nuevo",
                    Rol = "Cliente",
                    TokenNotificacion = tokenCelular 
                };

                await _supabase.From<Usuario>().Insert(usuarioDb);
            }
            else
            {
                if (!string.IsNullOrEmpty(tokenCelular) && usuarioDb.TokenNotificacion != tokenCelular)
                {
                    usuarioDb.TokenNotificacion = tokenCelular;
                    await _supabase.From<Usuario>().Update(usuarioDb);
                }
            }

            return usuarioDb;
        }
    }
}
