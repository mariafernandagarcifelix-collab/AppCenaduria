using AppCenaduria.Models;
using AppCenaduria.Controllers;

namespace AppCenaduria.Views;

public partial class HistorialVentas : ContentPage
{
    private HistorialVentasController _controller;
    private List<Pedido> _todosLosPedidos = new List<Pedido>();
    private string _filtroActivo = "Todos";

    public HistorialVentas()
    {
        InitializeComponent();
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        if (_controller == null)
        {
            var supabase = Application.Current.Handler.MauiContext.Services.GetService<Supabase.Client>();
            _controller = new HistorialVentasController(supabase);
        }

        dpInicio.Date = DateTime.Now.Date;
        dpFin.Date = DateTime.Now.Date;

        await CargarHistorial();
    }

    private async Task CargarHistorial()
    {
        try
        {
            _todosLosPedidos = await _controller.ObtenerVentasEntregadasAsync();
            AplicarFiltros();
        }
        catch (Exception ex)
        {
            await DisplayAlert("Error", "No se pudo cargar la bitácora: " + ex.Message, "OK");
        }
    }

    private void OnFiltrosCambiados(object sender, EventArgs e)
    {
        AplicarFiltros();
    }

    private void OnFiltroChipClicked(object sender, EventArgs e)
    {
        var botonPresionado = sender as Button;
        _filtroActivo = botonPresionado.StyleId;

        var contenedor = botonPresionado.Parent as HorizontalStackLayout;
        foreach (Button btn in contenedor.Children)
        {
            btn.BackgroundColor = Color.FromArgb("#333333");
        }
        botonPresionado.BackgroundColor = Color.FromArgb("#fc4b08");

        AplicarFiltros();
    }

    private void AplicarFiltros()
    {
        if (_todosLosPedidos == null) return;

        var textoBusqueda = sbBuscador.Text?.ToLower() ?? "";

        DateTime fechaInicio = Convert.ToDateTime(dpInicio.Date).Date;
        DateTime fechaFin = Convert.ToDateTime(dpFin.Date).Date.AddDays(1).AddTicks(-1);

        bool usarFechas = swUsarFechas.IsToggled; 

        var filtrados = _todosLosPedidos.Where(p =>
            (!usarFechas || (p.FechaPedido >= fechaInicio && p.FechaPedido <= fechaFin)) &&
            (
                (p.NombreCliente != null && p.NombreCliente.ToLower().Contains(textoBusqueda)) ||
                p.Folio.ToString().Contains(textoBusqueda)
            ) &&
            (
                _filtroActivo == "Todos" ||
                p.TipoPago == _filtroActivo ||
                p.TipoEntrega == _filtroActivo
            )
        ).ToList();

        listaHistorial.ItemsSource = filtrados;
    }

    private async void OnPedidoTapped(object sender, TappedEventArgs e)
    {
        var pedidoSeleccionado = e.Parameter as Pedido;

        if (pedidoSeleccionado != null)
        {
            lblTicketFolio.Text = $"Folio: #{pedidoSeleccionado.Folio}";
            lblTicketCliente.Text = $"Cliente/Mesa: {pedidoSeleccionado.NombreCliente ?? "Sin Nombre"}";
            lblTicketFecha.Text = $"Fecha: {pedidoSeleccionado.FechaPedido.ToString("dd/MM/yyyy HH:mm")}";
            lblTicketTipo.Text = $"Tipo: {pedidoSeleccionado.TipoEntrega ?? "N/A"} - {pedidoSeleccionado.TipoPago ?? "N/A"}";
            lblTicketTotal.Text = $"${pedidoSeleccionado.Total:F2}";

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

    private async void OnDotsTapped(object sender, TappedEventArgs e)
    {
        var pedidoSeleccionado = e.Parameter as Pedido;

        if (pedidoSeleccionado == null) return;

        string accion = await DisplayActionSheet($"Opciones de Orden #{pedidoSeleccionado.Folio}", "Cerrar", null, "Imprimir Ticket", "Compartir Venta", "Eliminar Pedido");

        if (accion == "Imprimir Ticket")
        {
            await DisplayAlert("Impresora", "Buscando impresora térmica por Bluetooth...", "OK");
        }
        else if (accion == "Compartir Venta")
        {
            await DisplayAlert("Preparando", "Estamos generando el resumen de venta con detalles...", "OK");

            string stringDetallesTicket = "";

            try
            {
                var detalles = await _controller.ObtenerDetallesPedidoAsync(pedidoSeleccionado.IdPedido);
                var catalogo = await _controller.ObtenerCatalogoPlatillosAsync();

                foreach (var detalle in detalles)
                {
                    var platilloReal = catalogo.FirstOrDefault(p => p.IdPlatillo == detalle.IdPlatillo);
                    string nombrePlat = platilloReal != null ? platilloReal.Nombre : "Platillo N/A";

                    stringDetallesTicket += $"👉 {detalle.Cantidad}x  {nombrePlat} ..... ${detalle.Subtotal:F2}\n";
                }
            }
            catch
            {
                stringDetallesTicket = "(No se pudo cargar el desglose de platillos)\n";
            }

            string mensajeWhatsapp = $"*🍽️ CENADURÍA EL PARQUESITO*\n" +
                                     $"----------------------------\n" +
                                     $"*Folio:* #{pedidoSeleccionado.Folio}\n" +
                                     $"*Cliente:* {pedidoSeleccionado.NombreCliente ?? "Mesa"}\n" +
                                     $"*Fecha:* {pedidoSeleccionado.FechaPedido:dd/MM/yyyy HH:mm}\n" +
                                     $"----------------------------\n" +
                                     $"*RESUMEN DE ORDEN:*\n" +
                                     $"{stringDetallesTicket}" +
                                     $"----------------------------\n" +
                                     $"*TOTAL A PAGAR:* ${pedidoSeleccionado.Total:F2}\n" +
                                     $"----------------------------\n" +
                                     $"*Pago:* {pedidoSeleccionado.TipoPago ?? "N/A"} - {pedidoSeleccionado.TipoEntrega ?? "N/A"}\n\n" +
                                     $"¡Gracias por su preferencia! 🌮🌮";

            await Share.Default.RequestAsync(new ShareTextRequest
            {
                Text = mensajeWhatsapp,
                Title = "Enviar Ticket de Venta"
            });
        }
        else if (accion == "Eliminar Pedido")
        {
            bool confirmar = await DisplayAlert("⚠️ Eliminar", $"¿Estás seguro de eliminar permanentemente la orden #{pedidoSeleccionado.Folio} de {pedidoSeleccionado.NombreCliente ?? "Mesa"}? Se restará de tus ganancias.", "Sí, Eliminar", "No");

            if (confirmar)
            {
                try
                {
                    pedidoSeleccionado.Estado = "Eliminado";
                    await _controller.ActualizarPedidoAsync(pedidoSeleccionado);

                    await DisplayAlert("Éxito", "El pedido ha sido eliminado.", "OK");
                    await CargarHistorial();
                }
                catch (Exception ex)
                {
                    await DisplayAlert("Error", "No se pudo eliminar: " + ex.Message, "OK");
                }
            }
        }
    }

    private void OnSwitchFechasToggled(object sender, ToggledEventArgs e)
    {
        contenedorFechas.IsEnabled = e.Value;
        contenedorFechas.Opacity = e.Value ? 1.0 : 0.5;
        AplicarFiltros();
    }
}

public class ItemTicketUI
{
    public int Cantidad { get; set; }
    public string NombrePlatillo { get; set; }
    public decimal Subtotal { get; set; }
}