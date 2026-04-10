using System;
using System.Collections.Generic;
using System.Text;
using Supabase;
using Postgrest.Attributes;
using Postgrest.Models;

namespace AppCenaduria.Models
{
    [Table("pedidos")] // Nombre de tu tabla en minúsculas
    public class Pedido : BaseModel
    {
        [PrimaryKey("idpedido", false)] // Cambiamos idpedidos -> idpedido (singular)
        public string IdPedido { get; set; }

        [Column("folio")] // EL NÚMERO SECUENCIAL (1, 2, 3...)
        public int Folio { get; set; }

        [Column("idusuario")]
        public string IdUsuario { get; set; }

        [Column("nombrecliente")] // EL NOMBRE DEL CLIENTE (María Fernanda)
        public string NombreCliente { get; set; }

        [Column("total")]
        public decimal Total { get; set; }

        [Column("estado")]
        public string Estado { get; set; }

        // Agregamos estos dos campos que tienes en tu SQL
        [Column("tipopago")]
        public string TipoPago { get; set; }

        [Column("tipoentrega")]
        public string TipoEntrega { get; set; }

        [Column("fechapedido")]
        public DateTime FechaPedido { get; set; }
    }
}
