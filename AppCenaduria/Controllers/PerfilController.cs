using Supabase;
using AppCenaduria.Models;

namespace AppCenaduria.Controllers
{
    public class PerfilController
    {
        private readonly Supabase.Client _supabase;

        public PerfilController(Supabase.Client supabase)
        {
            _supabase = supabase;
        }

        public async Task<Usuario> ObtenerPerfilAsync()
        {
            string correo = _supabase.Auth.CurrentUser?.Email;
            if (string.IsNullOrEmpty(correo)) return null;

            var res = await _supabase.From<Usuario>().Where(x => x.CorreoGoogle == correo).Get();
            return res.Models.FirstOrDefault();
        }

        public async Task ActualizarPerfilAsync(Usuario perfilModificado)
        {
            await _supabase.From<Usuario>().Update(perfilModificado);
        }
    }
}
