using System.Net.Http.Headers;
using System.Text.Json;
using AppCenaduria.Models;
using Google.Apis.Auth.OAuth2;
using Supabase;

namespace AppCenaduria.Controllers
{
    public class MiCarritoController
    {
        private readonly Supabase.Client _supabase;

        public MiCarritoController(Supabase.Client supabase)
        {
            _supabase = supabase;
        }

        public async Task<Usuario> ObtenerUsuarioActualAsync()
        {
            if (_supabase.Auth.CurrentUser == null) return null;

            string correoActual = _supabase.Auth.CurrentUser.Email;
            var respuestaUsuario = await _supabase.From<Usuario>().Where(x => x.CorreoGoogle == correoActual).Get();
            return respuestaUsuario.Models.FirstOrDefault();
        }

        public async Task<NuevoPedido> CrearPedidoAsync(Usuario usuario, decimal total, List<Carrito.ItemCarrito> items, string tipoEntrega = "Local", string tipoPago = "Efectivo")
        {
            var nuevoPedido = new NuevoPedido
            {
                IdUsuario = usuario.IdUsuario,
                NombreCliente = usuario.NombreCompleto,
                Total = total,
                Estado = "En preparación",
                TipoPago = tipoPago,
                TipoEntrega = tipoEntrega
            };

            var resultadoPedido = await _supabase.From<NuevoPedido>().Insert(nuevoPedido);
            var pedidoGuardado = resultadoPedido.Models.First();

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

        public async Task NotificarAdminAsync(NuevoPedido pedidoGuardado)
        {
            try
            {
                var resAdmin = await _supabase.From<Usuario>().Where(u => u.Rol == "Administrador").Get();
                var administrador = resAdmin.Models.FirstOrDefault();

                if (administrador != null && !string.IsNullOrEmpty(administrador.TokenNotificacion))
                {
                    await EnviarNotificacionPushV1(
                        administrador.TokenNotificacion,
                        "¡Nuevo Pedido Recibido! 🔥",
                        $"Nueva orden a nombre de {pedidoGuardado.NombreCliente} por ${pedidoGuardado.Total:F2} 🌮"
                    );
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Error notificando al admin: " + ex.Message);
            }
        }

        private async Task EnviarNotificacionPushV1(string tokenDestino, string titulo, string mensajeCuerpo)
        {
            try
            {
                string jsonKey = "";
                using (var stream = await FileSystem.OpenAppPackageFileAsync("firebase-key.json"))
                using (var reader = new System.IO.StreamReader(stream))
                {
                    jsonKey = await reader.ReadToEndAsync();
                }

                string projectId = "cenaduriaapp";

                var credential = GoogleCredential.FromJson(jsonKey)
                                                 .CreateScoped("https://www.googleapis.com/auth/firebase.messaging");

                string accessToken = await ((ITokenAccess)credential).GetAccessTokenForRequestAsync();

                using (var client = new HttpClient())
                {
                    client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
                    string url = $"https://fcm.googleapis.com/v1/projects/{projectId}/messages:send";

                    var payload = new
                    {
                        message = new
                        {
                            token = tokenDestino,
                            notification = new
                            {
                                title = titulo,
                                body = mensajeCuerpo
                            }
                        }
                    };

                    string jsonPayload = JsonSerializer.Serialize(payload);
                    var content = new StringContent(jsonPayload, System.Text.Encoding.UTF8, "application/json");

                    var response = await client.PostAsync(url, content);

                    if (response.IsSuccessStatusCode)
                    {
                        System.Diagnostics.Debug.WriteLine("Notificación al admin enviada con éxito");
                    }
                    else
                    {
                        string error = await response.Content.ReadAsStringAsync();
                        System.Diagnostics.Debug.WriteLine("Error de Google Push: " + error);
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error crítico en push al admin: {ex.Message}");
            }
        }
    }
}
