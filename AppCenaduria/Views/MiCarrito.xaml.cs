using System.IO;
using AppCenaduria.Models;
using static AppCenaduria.Models.Carrito;
using Supabase;
using System.Net.Http.Headers;
using System.Text.Json;
using Google.Apis.Auth.OAuth2;

namespace AppCenaduria.Views;

public partial class MiCarrito : ContentPage
{
    private Supabase.Client _supabase;
    private decimal _totalFinal = 0;

    public MiCarrito()
    {
        InitializeComponent();
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();

        // 1. Refrescamos la lista en pantalla con lo que haya en la memoria RAM
        listaCarrito.ItemsSource = null; // Truco para obligar a la lista a recargarse
        listaCarrito.ItemsSource = CarritoGlobal.Articulos;

        // 2. Calculamos el Total sumando los subtotales de todos los artículos
        decimal totalPedido = 0;
        foreach (var item in CarritoGlobal.Articulos)
        {
            totalPedido += item.Subtotal;
        }

        // 3. Mostramos el total en la etiqueta naranja
        lblTotal.Text = $"${totalPedido:F2}";
    }

    private async void OnConfirmarPedidoClicked(object sender, EventArgs e)
    {
        if (CarritoGlobal.Articulos.Count == 0) return;

        if (_supabase == null)
            _supabase = Application.Current.Handler.MauiContext.Services.GetService<Supabase.Client>();

        try
        {
            // 1. Obtener los datos del usuario actual de la BD
            string correoActual = _supabase.Auth.CurrentUser.Email;
            var respuestaUsuario = await _supabase.From<Usuario>().Where(x => x.CorreoGoogle == correoActual).Get();
            var miUsuarioReal = respuestaUsuario.Models.FirstOrDefault();

            // 2. VALIDACIÓN CRÍTICA: ¿Tiene nombre, teléfono y domicilio?
            if (string.IsNullOrWhiteSpace(miUsuarioReal?.NombreCompleto) ||
                string.IsNullOrWhiteSpace(miUsuarioReal?.Telefono) ||
                string.IsNullOrWhiteSpace(miUsuarioReal?.Domicilio))
            {
                await DisplayAlert("Datos Incompletos", "Para realizar un pedido, necesitamos tu nombre, teléfono y dirección de entrega.", "Ir a Mi Perfil");

                // Lo mandamos a la pantalla de Perfil
                if (Application.Current.MainPage is FlyoutPage menu)
                {
                    menu.Detail = new NavigationPage(new Perfil());
                }
                return;
            }

            // 3. Si todo está bien, calculamos el total y guardamos
            decimal totalReal = CarritoGlobal.Articulos.Sum(x => x.Subtotal);

            var nuevoPedido = new NuevoPedido
            {
                IdUsuario = miUsuarioReal.IdUsuario,
                NombreCliente = miUsuarioReal.NombreCompleto,
                Total = totalReal,
                Estado = "En preparación",
                TipoPago = "Efectivo",
                TipoEntrega = "Local"
            };

            var resultadoPedido = await _supabase.From<NuevoPedido>().Insert(nuevoPedido);
            var pedidoGuardado = resultadoPedido.Models.First();

            foreach (var item in CarritoGlobal.Articulos)
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

            // 4. LIMPIEZA DEL CARRITO
            await DisplayAlert("¡Éxito!", "Tu pedido ha sido enviado. ¡Gracias por tu compra!", "OK");

            CarritoGlobal.Articulos.Clear(); // Vaciamos la lista en memoria
            listaCarrito.ItemsSource = null; // Limpiamos visualmente la pantalla
            lblTotal.Text = "$0.00";         // Reseteamos el total visual

            // --- 5. NUEVO: NOTIFICAR AL ADMINISTRADOR ---
            try
            {
                // Buscamos a alguien que tenga el rol de Administrador
                var resAdmin = await _supabase.From<Usuario>().Where(u => u.Rol == "Administrador").Get();
                var administrador = resAdmin.Models.FirstOrDefault();

                // Si encontramos al admin y tiene su antena (Token) conectada, le disparamos el mensaje
                if (administrador != null && !string.IsNullOrEmpty(administrador.TokenNotificacion))
                {
                    await EnviarNotificacionPushV1(
                        administrador.TokenNotificacion,
                        "¡Nuevo Pedido Recibido! 🔥",
                        $"Nueva orden a nombre de {pedidoGuardado.NombreCliente} por ${totalReal:F2} 🌮"
                    );
                }
            }
            catch (Exception ex)
            {
                // Registramos el error de la notificación pero no bloqueamos la app del cliente
                System.Diagnostics.Debug.WriteLine("Error notificando al admin: " + ex.Message);
            }
            // --- FIN DE LA NOTIFICACIÓN ---

            // Opcional: Regresarlo al menú de platillos
            if (Application.Current.MainPage is FlyoutPage flyout)
            {
                flyout.Detail = new NavigationPage(new VerMenuCliente());
            }

        }
        catch (Exception ex)
        {
            await DisplayAlert("Error", "Hubo un problema: " + ex.Message, "OK");
        }
    }

    // --- MOTOR DE NOTIFICACIONES PUSH PARA GOOGLE V1 ---
    private async Task EnviarNotificacionPushV1(string tokenDestino, string titulo, string mensajeCuerpo)
    {
        try
        {
            // Tu llave maestra de Firebase
            // 1. Leemos la llave en secreto desde el archivo interno del teléfono
            string jsonKey = "";
            using (var stream = await FileSystem.OpenAppPackageFileAsync("firebase-key.json"))
            using (var reader = new System.IO.StreamReader(stream))
            {
                jsonKey = await reader.ReadToEndAsync();
            }


            string projectId = "cenaduriaapp";

            // Generamos el Token Maestro de Google
            var credential = GoogleCredential.FromJson(jsonKey)
                                             .CreateScoped("https://www.googleapis.com/auth/firebase.messaging");

            string accessToken = await ((ITokenAccess)credential).GetAccessTokenForRequestAsync();

            // Preparamos el cliente HTTP para disparar el mensaje
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