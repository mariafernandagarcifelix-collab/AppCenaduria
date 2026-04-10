using Postgrest.Attributes;
using Postgrest.Models;

namespace AppCenaduria.Models
{
    [Table("pedidos")]
    public class NuevoPedido : BaseModel
    {
        [PrimaryKey("idpedido", false)]
        public string IdPedido { get; set; }

        // ¡ELIMINAMOS EL FOLIO DE AQUÍ! PostgreSQL lo generará automáticamente.

        [Column("idusuario")]
        public string IdUsuario { get; set; }

        [Column("nombrecliente")]
        public string NombreCliente { get; set; }

        [Column("total")]
        public decimal Total { get; set; }

        [Column("estado")]
        public string Estado { get; set; }

        [Column("tipopago")]
        public string TipoPago { get; set; }

        [Column("tipoentrega")]
        public string TipoEntrega { get; set; }
    }
}