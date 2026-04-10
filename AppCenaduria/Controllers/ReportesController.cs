using Supabase;
using AppCenaduria.Models;

namespace AppCenaduria.Controllers
{
    public class ReportesController
    {
        private readonly Supabase.Client _supabase;

        public ReportesController(Supabase.Client supabase)
        {
            _supabase = supabase;
        }

        public async Task<List<Pedido>> ObtenerReportesEntregadosAsync()
        {
            var respuesta = await _supabase.From<Pedido>()
                                           .Where(x => x.Estado == "Entregado")
                                           .Get();
            return respuesta.Models;
        }
    }
}
