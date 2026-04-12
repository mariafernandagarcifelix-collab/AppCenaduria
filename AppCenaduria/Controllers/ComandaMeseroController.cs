using AppCenaduria.Models;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace AppCenaduria.Controllers
{
    public class ComandaMeseroController
    {
        private readonly Supabase.Client _supabase;

        public ComandaMeseroController(Supabase.Client supabase)
        {
            _supabase = supabase;
        }

        public decimal CalcularTotal(List<Carrito.ItemCarrito> items)
        {
            decimal total = 0;
            foreach (var item in items)
            {
                total += item.Subtotal;
            }
            return total;
        }

        // 🔥 AQUÍ ESTÁ EL CAMBIO: Se agregó 'string tipoPago' para que sean 6 argumentos exactos
        public async Task<NuevoPedido> CrearPedidoMeseroAsync(string idMesero, string nombreClienteOMesa, string tipoEntrega, string tipoPago, decimal total, List<Carrito.ItemCarrito> items)
        {
            // Creamos el encabezado del pedido
            var nuevoPedido = new NuevoPedido
            {
                IdUsuario = idMesero, // Guardamos quién tomó la orden (el Mesero)
                NombreCliente = nombreClienteOMesa, // Aquí guardamos "Mesa 4" o "Juan Perez"
                Total = total,
                Estado = "En preparación",
                TipoPago = tipoPago, // 🔥 Y AQUÍ GUARDAMOS EL TIPO DE PAGO SELECCIONADO
                TipoEntrega = tipoEntrega
            };

            var resultadoPedido = await _supabase.From<NuevoPedido>().Insert(nuevoPedido);
            var pedidoGuardado = resultadoPedido.Models.First();

            // Guardamos el detalle de los platillos
            foreach (var item in items)
            {
                await _supabase.From<DetallePedido>().Insert(new DetallePedido
                {
                    IdPedido = pedidoGuardado.IdPedido,
                    IdPlatillo = item.IdPlatillo,
                    Cantidad = item.Cantidad,
                    PrecioUnitario = item.PrecioUnitario,
                    Subtotal = item.Subtotal,
                    Comentarios = item.Comentarios
                });
            }

            return pedidoGuardado;
        }
    }
}