using System;
using System.Collections.Generic;
using System.Text;
using Postgrest.Attributes;
using Postgrest.Models;

namespace AppCenaduria.Models
{
    [Table("detallepedidos")] // Nombre exacto de tu tabla
    public class DetallePedido : BaseModel
    {
        [PrimaryKey("iddetalle", false)]
        public string IdDetalle { get; set; }

        [Column("idpedido")] // Llave foránea en singular
        public string IdPedido { get; set; }

        [Column("idplatillo")]
        public string IdPlatillo { get; set; }

        [Column("cantidad")]
        public int Cantidad { get; set; }

        [Column("preciounitario")]
        public decimal PrecioUnitario { get; set; }

        [Column("subtotal")]
        public decimal Subtotal { get; set; }

        [Column("comentarios")]
        public string Comentarios { get; set; }
    }
}
