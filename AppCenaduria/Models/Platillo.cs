using System;
using System.Collections.Generic;
using System.Text;
using Postgrest.Attributes;
using Postgrest.Models;

namespace AppCenaduria.Models
{
    // Buscamos la tabla exactamente en minúsculas
    [Table("platillos")]
    public class Platillo : BaseModel
    {
        [PrimaryKey("idplatillo", false)]
        public string IdPlatillo { get; set; }

        [Column("nombre")]
        public string Nombre { get; set; }

        [Column("descripcion")]
        public string Descripcion { get; set; }

        [Column("precio")]
        public decimal Precio { get; set; }

        [Column("fotografiaurl")]
        public string FotografiaUrl { get; set; }

        [Column("disponible")]
        public bool Disponible { get; set; }
    }
}
