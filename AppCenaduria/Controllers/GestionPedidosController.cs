using System.Net.Http.Headers;
using System.Text.Json;
using AppCenaduria.Models;
using Google.Apis.Auth.OAuth2;
using Supabase;

namespace AppCenaduria.Controllers
{
    public class GestionPedidosController
    {
        private readonly Supabase.Client _supabase;

        public GestionPedidosController(Supabase.Client supabase)
        {
            _supabase = supabase;
        }

        public async Task<List<Pedido>> ObtenerPedidosPendientesAsync()
        {
            var respuesta = await _supabase.From<Pedido>()
                                           .Filter("estado", Postgrest.Constants.Operator.NotEqual, "Entregado")
                                           .Filter("estado", Postgrest.Constants.Operator.NotEqual, "Cancelado")
                                           .Order(x => x.Folio, Postgrest.Constants.Ordering.Ascending)
                                           .Get();
            return respuesta.Models;
        }

        public async Task ActualizarEstadoPedidoAsync(Pedido pedido)
        {
            await _supabase.From<Pedido>().Update(pedido);
        }

        public async Task NotificarClienteAsync(Pedido pedidoSeleccionado, string nuevoEstado)
        {
            var resCliente = await _supabase.From<Usuario>().Where(u => u.IdUsuario == pedidoSeleccionado.IdUsuario).Get();
            var cliente = resCliente.Models.FirstOrDefault();

            if (cliente != null && !string.IsNullOrEmpty(cliente.TokenNotificacion))
            {
                string titulo = "Actualización de tu pedido 🌮";
                string mensaje = $"Tu orden #{pedidoSeleccionado.Folio} ahora está: {nuevoEstado}";

                if (nuevoEstado == "En proceso de entrega")
                    mensaje = $"¡Buenas noticias! Tu pedido #{pedidoSeleccionado.Folio} va en camino a tu domicilio. 🛵";
                else if (nuevoEstado == "Entregado")
                    mensaje = $"Tu pedido #{pedidoSeleccionado.Folio} ha sido entregado. ¡Que lo disfrutes! 😊";

                await Task.Delay(2500);
                await EnviarNotificacionPushV1(cliente.TokenNotificacion, titulo, mensaje);
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
                var credential = GoogleCredential.FromJson(jsonKey).CreateScoped("https://www.googleapis.com/auth/firebase.messaging");
                string accessToken = await ((ITokenAccess)credential).GetAccessTokenForRequestAsync();

                using (var client = new HttpClient())
                {
                    client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
                    string url = $"https://fcm.googleapis.com/v1/projects/{projectId}/messages:send";

                    var payload = new { message = new { token = tokenDestino, notification = new { title = titulo, body = mensajeCuerpo } } };
                    var content = new StringContent(JsonSerializer.Serialize(payload), System.Text.Encoding.UTF8, "application/json");

                    await client.PostAsync(url, content);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error en push: {ex.Message}");
            }
        }
    }
}
