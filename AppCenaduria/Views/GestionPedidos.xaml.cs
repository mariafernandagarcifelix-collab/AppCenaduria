using System.IO;
using Supabase;
using AppCenaduria.Models;
using System.Net.Http.Headers;
using System.Text.Json;
using Google.Apis.Auth.OAuth2;

namespace AppCenaduria.Views;

public partial class GestionPedidos : ContentPage
{
    private Supabase.Client _supabase;

    public GestionPedidos()
    {
        InitializeComponent();
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        if (_supabase == null)
            _supabase = Application.Current.Handler.MauiContext.Services.GetService<Supabase.Client>();

        await CargarPedidosPendientes();
    }

    private async Task CargarPedidosPendientes()
    {
        try
        {
            // Traemos los pedidos que NO están entregados ni cancelados
            var respuesta = await _supabase.From<Pedido>()
                                           .Filter("estado", Postgrest.Constants.Operator.NotEqual, "Entregado")
                                           .Filter("estado", Postgrest.Constants.Operator.NotEqual, "Cancelado")
                                           .Order(x => x.Folio, Postgrest.Constants.Ordering.Ascending)
                                           .Get();

            listaPedidos.ItemsSource = null;
            listaPedidos.ItemsSource = respuesta.Models;
        }
        catch (Exception ex)
        {
            await DisplayAlert("Error", "No pudimos cargar las órdenes: " + ex.Message, "OK");
        }
    }

    private async void OnCambiarEstadoClicked(object sender, EventArgs e)
    {
        var boton = sender as Button;
        var pedidoSeleccionado = boton.CommandParameter as Pedido;

        // 1. Lanzamos el menú de estados
        string nuevoEstado = await DisplayActionSheet(
            $"Orden #{pedidoSeleccionado.Folio}",
            "Cerrar", null,
            "En preparación",
            "En proceso de entrega",
            "Entregado",
            "Cancelado"
        );

        // 2. Verificamos que sí seleccionó uno y que sea diferente al actual
        if (nuevoEstado != "Cerrar" && nuevoEstado != null && nuevoEstado != pedidoSeleccionado.Estado)
        {
            try
            {
                // Actualizamos Supabase
                pedidoSeleccionado.Estado = nuevoEstado;
                await _supabase.From<Pedido>().Update(pedidoSeleccionado);

                // 3. Notificamos al Cliente
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

                    // Disparamos la notificación
                    await EnviarNotificacionPushV1(cliente.TokenNotificacion, titulo, mensaje);
                }

                await CargarPedidosPendientes();
            }
            catch (Exception ex)
            {
                await DisplayAlert("Error", "No se pudo actualizar: " + ex.Message, "OK");
            }
        }
    }

    // --- PEGA AQUÍ TU MÉTODO EnviarNotificacionPushV1 CON TU LLAVE JSON ---
    private async Task EnviarNotificacionPushV1(string tokenDestino, string titulo, string mensajeCuerpo)
    {
        try
        {
            // Pega tu JSON real aquí adentro:
            // 1. Leemos la llave en secreto desde el archivo interno del teléfono
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