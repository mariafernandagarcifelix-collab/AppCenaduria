using AppCenaduria.Models;
using AppCenaduria.Controllers;

namespace AppCenaduria.Views;

public partial class GestionPedidos : ContentPage
{
    private GestionPedidosController _controller;

    public GestionPedidos()
    {
        InitializeComponent();
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        if (_controller == null)
        {
            var supabase = Application.Current.Handler.MauiContext.Services.GetService<Supabase.Client>();
            _controller = new GestionPedidosController(supabase);
        }

        await CargarPedidosPendientes();
    }

    private async Task CargarPedidosPendientes()
    {
        try
        {
            var pedidos = await _controller.ObtenerPedidosPendientesAsync();

            listaPedidos.ItemsSource = null;
            listaPedidos.ItemsSource = pedidos;
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

        string nuevoEstado = await DisplayActionSheet(
            $"Orden #{pedidoSeleccionado.Folio}",
            "Cerrar", null,
            "En preparación",
            "En proceso de entrega",
            "Entregado",
            "Cancelado"
        );

        if (nuevoEstado != "Cerrar" && nuevoEstado != null && nuevoEstado != pedidoSeleccionado.Estado)
        {
            try
            {
                pedidoSeleccionado.Estado = nuevoEstado;
                await _controller.ActualizarEstadoPedidoAsync(pedidoSeleccionado);
                await _controller.NotificarClienteAsync(pedidoSeleccionado, nuevoEstado);

                await CargarPedidosPendientes();
            }
            catch (Exception ex)
            {
                await DisplayAlert("Error", "No se pudo actualizar: " + ex.Message, "OK");
            }
        }
    }
}