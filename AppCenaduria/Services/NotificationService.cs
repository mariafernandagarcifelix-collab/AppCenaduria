using System;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;
using Google.Apis.Auth.OAuth2;
using Microsoft.Maui.Storage;
using AppCenaduria.Models;

namespace AppCenaduria.Services
{
    public static class NotificationService
    {
        // 🔥 TU ID REAL PUESTO DIRECTAMENTE
        private const string FirebaseProjectId = "cenaduriaapp";

        private static readonly HttpClient _httpClient = new HttpClient();
        private static GoogleCredential _credential;

        private static async Task<string> GetAccessTokenAsync()
        {
            try
            {
                System.Diagnostics.Debug.WriteLine("🔍 [Paso 1] Buscando llave de Google en Resources/Raw...");

                // Abre el archivo de seguridad
                using var stream = await FileSystem.OpenAppPackageFileAsync("firebase-auth.json");

                System.Diagnostics.Debug.WriteLine("🔍 [Paso 2] Archivo encontrado. Generando pase de acceso...");
                _credential = GoogleCredential.FromStream(stream).CreateScoped("https://www.googleapis.com/auth/firebase.messaging");

                string token = await _credential.UnderlyingCredential.GetAccessTokenForRequestAsync();
                System.Diagnostics.Debug.WriteLine("✅ [Paso 3] Pase de acceso generado correctamente.");

                return token;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ ERROR CRÍTICO al leer archivo JSON: {ex.Message}");
                return null;
            }
        }

        public static async Task EnviarNotificacionAsync(string token, string titulo, string cuerpo)
        {
            System.Diagnostics.Debug.WriteLine($"🚀 Iniciando envío a token (Inicia con): {token.Substring(0, Math.Min(10, token.Length))}...");

            string accessToken = await GetAccessTokenAsync();
            if (string.IsNullOrEmpty(accessToken))
            {
                System.Diagnostics.Debug.WriteLine("❌ ERROR: El pase de acceso está vacío, se cancela el envío.");
                return;
            }

            string fcmEndpoint = $"https://fcm.googleapis.com/v1/projects/{FirebaseProjectId}/messages:send";
            System.Diagnostics.Debug.WriteLine($"📡 Enviando paquete a: {fcmEndpoint}");

            var payload = new
            {
                message = new
                {
                    token = token,
                    notification = new { title = titulo, body = cuerpo },
                    android = new { notification = new { sound = "default" } }
                }
            };

            var request = new HttpRequestMessage(HttpMethod.Post, fcmEndpoint);
            request.Headers.TryAddWithoutValidation("Authorization", $"Bearer {accessToken}");
            request.Content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");

            try
            {
                var response = await _httpClient.SendAsync(request);
                var responseBody = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    System.Diagnostics.Debug.WriteLine($"✅ FCM ÉXITO TOTAL: La notificación ya debe estar sonando en el celular.");
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine($"❌ FCM ERROR GOOGLE RECHAZÓ EL PAQUETE: {response.StatusCode} - {responseBody}");
                }
            }
            catch (Exception e)
            {
                System.Diagnostics.Debug.WriteLine($"❌ ERROR DE INTERNET/HTTP: {e.Message}");
            }
        }

        // ==============================================================================
        // 1. CUANDO EL CLIENTE PIDE: Le avisamos a los Administradores y Meseros
        // ==============================================================================
        public static async Task NotificarNuevoPedidoAStaffAsync(Supabase.Client supabase)
        {
            try
            {
                var respuesta = await supabase.From<Usuario>()
                                              .Where(u => u.Rol == "Administrador" || u.Rol == "Mesero")
                                              .Get();

                foreach (var staff in respuesta.Models)
                {
                    if (!string.IsNullOrEmpty(staff.TokenNotificacion))
                    {
                        _ = EnviarNotificacionAsync(staff.TokenNotificacion, "🌮 ¡Nuevo Pedido!", "Ha entrado una nueva orden. Revisa la bandeja de cocina.");
                    }
                }
            }
            catch (Exception ex) { System.Diagnostics.Debug.WriteLine($"Error buscando staff: {ex.Message}"); }
        }

        // ==============================================================================
        // 2. CUANDO LA COCINA ACTUALIZA: Le avisamos al Cliente dueño de ese pedido
        // ==============================================================================
        public static async Task NotificarCambioEstadoAClienteAsync(Supabase.Client supabase, string clienteId, string nuevoEstado)
        {
            try
            {
                var respuesta = await supabase.From<Usuario>()
                                              .Where(u => u.IdUsuario == clienteId)
                                              .Get();

                var cliente = respuesta.Models.FirstOrDefault();

                if (cliente != null && !string.IsNullOrEmpty(cliente.TokenNotificacion))
                {
                    string emoji = nuevoEstado switch
                    {
                        "En preparación" => "👨‍🍳",
                        "Listo" => "✅",
                        "En camino" => "🛵",
                        "Cancelado" => "❌",
                        _ => "📋"
                    };

                    _ = EnviarNotificacionAsync(cliente.TokenNotificacion, $"{emoji} Actualización de tu pedido", $"Tu comida ahora está: {nuevoEstado}");
                }
            }
            catch (Exception ex) { System.Diagnostics.Debug.WriteLine($"Error buscando cliente: {ex.Message}"); }
        }
    }
}