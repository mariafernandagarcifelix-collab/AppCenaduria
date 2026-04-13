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

            // 🔥 Control manual de la vista vacía para Syncfusion
            if (pedidos == null || pedidos.Count == 0)
            {
                listaHistorial.IsVisible = false;
                vistaVacia.IsVisible = true;
            }
            else
            {
                listaHistorial.IsVisible = true;
                vistaVacia.IsVisible = false;
            }
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
            lblTicketFolio.Text = $"Folio: #{pedidoSeleccionado.Folio}";
            lblTicketCliente.Text = $"{pedidoSeleccionado.NombreCliente ?? App.UsuarioActual.NombreCompleto}";
            lblTicketFecha.Text = $"{pedidoSeleccionado.FechaPedido:dd/MM/yyyy HH:mm}";
            lblTicketTipo.Text = $" {pedidoSeleccionado.TipoEntrega ?? "N/A"} - {pedidoSeleccionado.TipoPago ?? "N/A"}";
            lblTicketTotal.Text = $"${pedidoSeleccionado.Total:F2}";

            if (pedidoSeleccionado.Estado == "En preparación")
            {
                btnCancelarPedido.IsVisible = true;
                btnCancelarPedido.CommandParameter = pedidoSeleccionado;
            }
            else
            {
                btnCancelarPedido.IsVisible = false;
            }

            try
            {
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

    private async void OnCancelarPedidoClienteClicked(object sender, EventArgs e)
    {
        var boton = sender as Button;
        var pedido = boton.CommandParameter as Pedido;

        bool confirmar = await DisplayAlert("Cancelar Pedido", "¿Estás seguro de que deseas cancelar la orden #" + pedido.Folio + "?", "Sí, cancelar", "No");

        if (confirmar)
        {
            pedido.Estado = "Cancelado";
            await _controller.ActualizarEstadoPedidoAsync(pedido);
            await DisplayAlert("Cancelado", "Tu pedido ha sido cancelado exitosamente.", "OK");
            modalTicket.IsVisible = false;
            await CargarMiHistorial();
        }
    }
}
