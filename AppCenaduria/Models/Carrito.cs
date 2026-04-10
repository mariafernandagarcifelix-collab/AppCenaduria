using System;
using System.Collections.Generic;
using System.Text;

namespace AppCenaduria.Models
{
    public class Carrito
    {
        // Esta clase estática será nuestra "bolsa" temporal
        public static class CarritoGlobal
        {
            public static List<ItemCarrito> Articulos { get; set; } = new List<ItemCarrito>();
        }

        // Este es el molde de lo que echaremos a la bolsa
        public class ItemCarrito
        {
            public string IdPlatillo { get; set; }
            public string Nombre { get; set; }
            public string FotografiaUrl { get; set; }
            public int Cantidad { get; set; }
            public decimal PrecioUnitario { get; set; }
            public decimal Subtotal => Cantidad * PrecioUnitario; // Se calcula solo
            public string Comentarios { get; set; } // Aquí va el "Sin cebolla"
        }
    }
}
