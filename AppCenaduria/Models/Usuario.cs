using Postgrest.Attributes;
using Postgrest.Models;
using Postgrest.Attributes;
using Postgrest.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace AppCenaduria.Models
{
    // Le decimos a Supabase que esta clase se conecta exactamente con la tabla "Usuarios"
    [Table("usuarios")]
    public class Usuario : BaseModel
    {
        // Llave primaria
        [PrimaryKey("idusuario", false)]
        public string IdUsuario { get; set; }

        [Column("correogoogle")]
        public string CorreoGoogle { get; set; }

        [Column("nombrecompleto")]
        public string NombreCompleto { get; set; }

        [Column("telefono")]
        public string Telefono { get; set; }

        [Column("domicilio")]
        public string Domicilio { get; set; }

        [Column("rol")]
        public string Rol { get; set; } // "Administrador" o "Cliente"

        [Column("token_notificacion")]
        public string TokenNotificacion { get; set; }
    }
}
