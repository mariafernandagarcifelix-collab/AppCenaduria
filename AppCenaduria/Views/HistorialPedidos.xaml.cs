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

    private async void OnPedidoTapped(object sender, TappedEventArgs e)
    {
        var pedidoSeleccionado = e.Parameter as Pedido;

        if (pedidoSeleccionado != null)
        {
            // Llenar el encabezado del ticket
            lblTicketFolio.Text = $"Folio: #{pedidoSeleccionado.Folio}";
            lblTicketCliente.Text = $"{pedidoSeleccionado.NombreCliente ?? App.UsuarioActual.NombreCompleto}";
            lblTicketFecha.Text = $"{pedidoSeleccionado.FechaPedido:dd/MM/yyyy HH:mm}";
            lblTicketTipo.Text = $" {pedidoSeleccionado.TipoEntrega ?? "N/A"} - {pedidoSeleccionado.TipoPago ?? "N/A"}";
            lblTicketTotal.Text = $"${pedidoSeleccionado.Total:F2}";

            try
            {
                // OJO: Asegúrate de tener estos dos métodos en tu Controlador de Historial de Cliente
                // Si no los tienes, cópialos de HistorialVentasController a tu HistorialClienteController
                var detalles = await _controller.ObtenerDetallesPedidoAsync(pedidoSeleccionado.IdPedido);
                var catalogo = await _controller.ObtenerCatalogoPlatillosAsync();

                var listaVisualTicket = new List<ItemTicketUI>();

                foreach (var detalle in detalles)
                {
                    var platilloReal = catalogo.FirstOrDefault(p => p.IdPlatillo == detalle.IdPlatillo);
                    listaVisualTicket.Add(new ItemTicketUI
                    {
                        Cantidad = detalle.Cantidad,
                        NombrePlatillo = platilloReal != null ? platilloReal.Nombre : "Platillo Borrado",
                        Subtotal = detalle.Subtotal
                    });
                }
                listaDetallesTicket.ItemsSource = listaVisualTicket;
            }
            catch
            {
                listaDetallesTicket.ItemsSource = null;
            }

            modalTicket.IsVisible = true;
        }
    }

    private void OnCerrarTicketClicked(object sender, EventArgs e)
    {
        modalTicket.IsVisible = false;
    }
}