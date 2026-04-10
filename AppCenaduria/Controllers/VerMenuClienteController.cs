using Supabase;
using AppCenaduria.Models;

namespace AppCenaduria.Controllers
{
    public class VerMenuClienteController
    {
        private readonly Supabase.Client _supabase;

        public VerMenuClienteController(Supabase.Client supabase)
        {
            _supabase = supabase;
        }

        public async Task<List<Platillo>> ObtenerMenuDisponibleAsync()
        {
            var respuesta = await _supabase.From<Platillo>()
                                           .Where(x => x.Disponible == true)
                                           .Get();

            return respuesta.Models.OrderBy(p => p.Nombre).ToList();
        }
    }
}
