using AppCenaduria.Models;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace AppCenaduria.Controllers
{
    public class EditarPedidoController
    {
        private readonly Supabase.Client _supabase;

        public EditarPedidoController(Supabase.Client supabase)
        {
            _supabase = supabase;
        }

        public async Task<List<DetallePedido>> ObtenerDetallesAsync(string idPedido)
        {
            var response = await _supabase.From<DetallePedido>()
                .Where(x => x.IdPedido == idPedido)
                .Get();
            return response.Models;
        }

        public async Task<List<Platillo>> ObtenerPlatillosAsync()
        {
            var response = await _supabase.From<Platillo>()
                .Where(x => x.Disponible == true)
                .Get();
            return response.Models;
        }

        public async Task GuardarCambiosAsync(string idPedido, decimal nuevoTotal, List<DetallePedido> nuevosDetalles)
        {
            // 1. Borramos el "ticket" viejo de detalles
            await _supabase.From<DetallePedido>()
                .Where(x => x.IdPedido == idPedido)
                .Delete();

            // 2. Insertamos el "ticket" nuevo con las modificaciones
            foreach (var det in nuevosDetalles)
            {
                var insertDetalle = new DetallePedido
                {
                    IdPedido = idPedido,
                    IdPlatillo = det.IdPlatillo,
                    Cantidad = det.Cantidad,
                    PrecioUnitario = det.PrecioUnitario,
                    Subtotal = det.Subtotal,
                    Comentarios = det.Comentarios
                };
                await _supabase.From<DetallePedido>().Insert(insertDetalle);
            }

            // 3. Actualizamos el precio final en el Pedido principal
            var pedidoUpdate = await _supabase.From<Pedido>()
                .Where(p => p.IdPedido == idPedido)
                .Get();

            var pedido = pedidoUpdate.Models.FirstOrDefault();
            if (pedido != null)
            {
                pedido.Total = nuevoTotal;
                await _supabase.From<Pedido>().Update(pedido);
            }
        }
    }
}