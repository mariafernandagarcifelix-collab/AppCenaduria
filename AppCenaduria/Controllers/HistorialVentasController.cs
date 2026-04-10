using Supabase;
using AppCenaduria.Models;

namespace AppCenaduria.Controllers
{
    public class HistorialVentasController
    {
        private readonly Supabase.Client _supabase;

        public HistorialVentasController(Supabase.Client supabase)
        {
            _supabase = supabase;
        }

        public async Task<List<Pedido>> ObtenerVentasEntregadasAsync()
        {
            var respuesta = await _supabase.From<Pedido>()
                                           .Where(x => x.Estado == "Entregado")
                                           .Order(x => x.FechaPedido, Postgrest.Constants.Ordering.Descending)
                                           .Get();
            return respuesta.Models;
        }

        public async Task ActualizarPedidoAsync(Pedido pedido)
        {
            await _supabase.From<Pedido>().Update(pedido);
        }

        public async Task<List<DetallePedido>> ObtenerDetallesPedidoAsync(string idPedido)
        {
            var respuesta = await _supabase.From<DetallePedido>()
                                           .Where(d => d.IdPedido == idPedido)
                                           .Get();
            return respuesta.Models;
        }

        public async Task<List<Platillo>> ObtenerCatalogoPlatillosAsync()
        {
            var respuesta = await _supabase.From<Platillo>().Get();
            return respuesta.Models;
        }
    }
}
