using Supabase;
using AppCenaduria.Models;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;

namespace AppCenaduria.Controllers
{
    public class ComandaItem
    {
        public string NombrePlatillo { get; set; }
        public int Cantidad { get; set; }
        public decimal PrecioUnitario { get; set; }
        public decimal Subtotal { get; set; }
        public string Comentarios { get; set; }
        public bool TieneComentarios => !string.IsNullOrWhiteSpace(Comentarios);
    }

    public class ComandaPedidoController
    {
        private readonly Supabase.Client _supabase;

        public ComandaPedidoController(Supabase.Client supabase)
        {
            _supabase = supabase;
        }

        public async Task<List<ComandaItem>> ObtenerDetallesDelPedidoAsync(string idPedido)
        {
            // 1. Traer los detalles del pedido
            var resDetalles = await _supabase.From<DetallePedido>()
                                             .Where(d => d.IdPedido == idPedido)
                                             .Get();
            var detalles = resDetalles.Models;

            if (detalles == null || detalles.Count == 0) return new List<ComandaItem>();

            // 2. Traer todos los platillos para cruzar la información (o solo los requeridos)
            var idsPlatillos = detalles.Select(d => d.IdPlatillo).Distinct().ToList();

            // Usamos Filter con Operator.In para evitar errores de parseo de LINQ con Contains
            var resPlatillos = await _supabase.From<Platillo>()
                                              .Filter("idplatillo", Postgrest.Constants.Operator.In, idsPlatillos)
                                              .Get();
            var platillosInfo = resPlatillos.Models.ToDictionary(p => p.IdPlatillo, p => p.Nombre);

            // 3. Mapear al objeto DTO
            var resultado = new List<ComandaItem>();
            foreach(var d in detalles)
            {
                resultado.Add(new ComandaItem
                {
                    NombrePlatillo = platillosInfo.ContainsKey(d.IdPlatillo) ? platillosInfo[d.IdPlatillo] : "Platillo Desconocido",
                    Cantidad = d.Cantidad,
                    PrecioUnitario = d.PrecioUnitario,
                    Subtotal = d.Subtotal,
                    Comentarios = d.Comentarios
                });
            }

            return resultado;
        }

        public async Task<Usuario> ObtenerUsuarioDelPedidoAsync(string idUsuario)
        {
            var res = await _supabase.From<Usuario>()
                                     .Where(u => u.IdUsuario == idUsuario)
                                     .Get();
            return res.Models.FirstOrDefault();
        }
    }
}
