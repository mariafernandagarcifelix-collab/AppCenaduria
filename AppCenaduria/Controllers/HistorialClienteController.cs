using Supabase;
using AppCenaduria.Models;

namespace AppCenaduria.Controllers
{
    public class HistorialClienteController
    {
        private readonly Supabase.Client _supabase;

        public HistorialClienteController(Supabase.Client supabase)
        {
            _supabase = supabase;
        }

        public async Task<List<Pedido>> ObtenerMisPedidosAsync()
        {
            string correoActual = _supabase.Auth.CurrentUser?.Email;
            if (string.IsNullOrEmpty(correoActual)) return new List<Pedido>();

            var respuestaUsuario = await _supabase.From<Usuario>().Where(u => u.CorreoGoogle == correoActual).Get();
            var miUsuario = respuestaUsuario.Models.FirstOrDefault();

            if (miUsuario != null)
            {
                var respuestaPedidos = await _supabase.From<Pedido>()
                                                      .Where(p => p.IdUsuario == miUsuario.IdUsuario)
                                                      .Order(p => p.FechaPedido, Postgrest.Constants.Ordering.Descending)
                                                      .Get();

                return respuestaPedidos.Models;
            }

            return new List<Pedido>();
        }

        public async Task<List<DetallePedido>> ObtenerDetallesPedidoAsync(string idPedido)
        {
            var respuestaDetalles = await _supabase.From<DetallePedido>()
                                                   .Where(d => d.IdPedido == idPedido)
                                                   .Get();
            return respuestaDetalles.Models;
        }

        public async Task<List<Platillo>> ObtenerCatalogoPlatillosAsync()
        {
            var respuestaPlatillos = await _supabase.From<Platillo>().Get();
            return respuestaPlatillos.Models;
        }
    }
}
