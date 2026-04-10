using AppCenaduria.Models;
using AppCenaduria.Controllers;

namespace AppCenaduria.Views;

public partial class HistorialPedidos : ContentPage
{
    private HistorialClienteController _controller;

    public HistorialPedidos()
    {
        InitializeComponent();
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        if (_controller == null)
        {
            var supabase = Application.Current.Handler.MauiContext.Services.GetService<Supabase.Client>();
            _controller = new HistorialClienteController(supabase);
        }

        await CargarMiHistorial();
    }

    private async Task CargarMiHistorial()
    {
        try
        {
            var pedidos = await _controller.ObtenerMisPedidosAsync();
            listaHistorial.ItemsSource = pedidos;
        }
        catch (Exception ex)
        {
            await DisplayAlert("Error", "No pudimos cargar tu historial: " + ex.Message, "OK");
        }
    }
}