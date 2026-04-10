using Supabase;
using AppCenaduria.Models;

namespace AppCenaduria.Views;

public partial class HistorialPedidos : ContentPage
{
    private Supabase.Client _supabase;

    public HistorialPedidos()
	{
		InitializeComponent();
	}

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        _supabase = Application.Current.Handler.MauiContext.Services.GetService<Supabase.Client>();

        await CargarMiHistorial();
    }

    private async Task CargarMiHistorial()
    {
        try
        {
            // 1. Verificamos la sesión
            if (_supabase.Auth.CurrentUser == null) return;

            // 2. Buscamos tu ID real en la tabla de Usuarios usando tu correo de Google
            string correoActual = _supabase.Auth.CurrentUser.Email;
            var respuestaUsuario = await _supabase.From<Usuario>().Where(x => x.CorreoGoogle == correoActual).Get();
            var miUsuarioReal = respuestaUsuario.Models.FirstOrDefault();

            if (miUsuarioReal != null)
            {
                // 3. Descargamos SOLAMENTE los pedidos donde tú eres el cliente, ordenados del más nuevo al más viejo
                var respuestaPedidos = await _supabase.From<Pedido>()
                                                      .Where(x => x.IdUsuario == miUsuarioReal.IdUsuario)
                                                      .Order(x => x.Folio, Postgrest.Constants.Ordering.Descending)
                                                      .Get();

                // 4. Llenamos la lista en la pantalla
                listaHistorial.ItemsSource = respuestaPedidos.Models;
            }
        }
        catch (Exception ex)
        {
            await DisplayAlert("Error", "No pudimos cargar tu historial: " + ex.Message, "OK");
        }
    }
}