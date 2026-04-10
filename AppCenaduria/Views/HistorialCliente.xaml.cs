using AppCenaduria.Models;
using AppCenaduria.Controllers;

namespace AppCenaduria.Views;

public partial class HistorialCliente : ContentPage
{
    private HistorialClienteController _controller;

    public HistorialCliente()
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

        await CargarMisPedidos();
    }

    private async Task CargarMisPedidos()
    {
        try
        {
            var pedidos = await _controller.ObtenerMisPedidosAsync();
            listaMisPedidos.ItemsSource = pedidos;
        }
        catch (Exception ex)
        {
            await DisplayAlert("Error", "No pudimos cargar tus pedidos: " + ex.Message, "OK");
        }
    }

    private async void OnPedidoTapped(object sender, TappedEventArgs e)
    {
        var pedidoSeleccionado = e.Parameter as Pedido;
        if (pedidoSeleccionado == null) return;

        lblTicketFolio.Text = $"Orden #{pedidoSeleccionado.Folio}";
        lblTicketEstado.Text = $"Estado: {pedidoSeleccionado.Estado}";
        lblTicketTotal.Text = $"${pedidoSeleccionado.Total:F2}";

        try
        {
            var detalles = await _controller.ObtenerDetallesPedidoAsync(pedidoSeleccionado.IdPedido);
            var catalogo = await _controller.ObtenerCatalogoPlatillosAsync();

            var listaVisualTicket = new List<ItemTicketClienteUI>();

            foreach (var detalle in detalles)
            {
                var platilloReal = catalogo.FirstOrDefault(p => p.IdPlatillo == detalle.IdPlatillo);

                listaVisualTicket.Add(new ItemTicketClienteUI
                {
                    Cantidad = detalle.Cantidad,
                    NombrePlatillo = platilloReal != null ? platilloReal.Nombre : "Platillo borrado del menú",
                    Subtotal = detalle.Subtotal
                });
            }

            listaDetallesTicket.ItemsSource = listaVisualTicket;
        }
        catch
        {
            listaDetallesTicket.ItemsSource = null;
        }

        modalTicketCliente.IsVisible = true;
    }

    private void OnCerrarTicketClicked(object sender, EventArgs e)
    {
        modalTicketCliente.IsVisible = false;
    }
}

public class ItemTicketClienteUI
{
    public int Cantidad { get; set; }
    public string NombrePlatillo { get; set; }
    public decimal Subtotal { get; set; }
}