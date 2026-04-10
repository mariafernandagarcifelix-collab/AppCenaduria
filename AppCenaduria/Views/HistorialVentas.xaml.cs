using AppCenaduria.Models;
using Supabase;

namespace AppCenaduria.Views;

public partial class HistorialVentas : ContentPage
{
    private Supabase.Client _supabase;
    private List<Pedido> _todosLosPedidos = new List<Pedido>();
    private string _filtroActivo = "Todos";

    public HistorialVentas()
    {
        InitializeComponent();
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        _supabase = Application.Current.Handler.MauiContext.Services.GetService<Supabase.Client>();

        // Ponemos el rango de fechas al día de hoy por defecto
        dpInicio.Date = DateTime.Now.Date;
        dpFin.Date = DateTime.Now.Date;

        await CargarHistorial();
    }

    private async Task CargarHistorial()
    {
        try
        {
            var respuesta = await _supabase.From<Pedido>()
                                           .Where(x => x.Estado == "Entregado")
                                           .Order(x => x.FechaPedido, Postgrest.Constants.Ordering.Descending)
                                           .Get();

            _todosLosPedidos = respuesta.Models;
            AplicarFiltros();
        }
        catch (Exception ex)
        {
            await DisplayAlert("Error", "No se pudo cargar la bitácora: " + ex.Message, "OK");
        }
    }

    // EVENTO ÚNICO PARA AMBOS BUSCADORES (Texto y Calendarios)
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

        // Fechas de inicio y fin (Aseguramos que abarque todo el día final)
        DateTime fechaInicio = Convert.ToDateTime(dpInicio.Date).Date;
        DateTime fechaFin = Convert.ToDateTime(dpFin.Date).Date.AddDays(1).AddTicks(-1);

        //var filtrados = _todosLosPedidos.Where(p =>
        //    // Filtro 1: Rango de Fechas
        //    (p.FechaPedido >= fechaInicio && p.FechaPedido <= fechaFin) &&
        //    // Filtro 2: Texto (Nombre o Folio)
        //    (
        //        (p.NombreCliente != null && p.NombreCliente.ToLower().Contains(textoBusqueda)) ||
        //        p.Folio.ToString().Contains(textoBusqueda)
        //    ) &&
        //    // Filtro 3: Chips
        //    (
        //        _filtroActivo == "Todos" ||
        //        p.TipoPago == _filtroActivo ||
        //        p.TipoEntrega == _filtroActivo
        //    )
        //).ToList();

        bool usarFechas = swUsarFechas.IsToggled; // Revisamos si el switch está prendido

        var filtrados = _todosLosPedidos.Where(p =>
            // Filtro 1: Rango de Fechas (¡SOLO SI EL SWITCH ESTÁ PRENDIDO!)
            (!usarFechas || (p.FechaPedido >= fechaInicio && p.FechaPedido <= fechaFin)) &&
            // Filtro 2: Texto (Nombre o Folio)
            (
                (p.NombreCliente != null && p.NombreCliente.ToLower().Contains(textoBusqueda)) ||
                p.Folio.ToString().Contains(textoBusqueda)
            ) &&
            // Filtro 3: Chips
            (
                _filtroActivo == "Todos" ||
                p.TipoPago == _filtroActivo ||
                p.TipoEntrega == _filtroActivo
            )
        ).ToList();

        listaHistorial.ItemsSource = filtrados;
    }

    // EL NUEVO CLIC DE LA TARJETA (Trae los platillos de Supabase)
    // EL NUEVO CLIC DE LA TARJETA (Trae los platillos y cruza los nombres)
    private async void OnPedidoTapped(object sender, TappedEventArgs e)
    {
        var pedidoSeleccionado = e.Parameter as Pedido;

        if (pedidoSeleccionado != null)
        {
            // Llenamos la cabecera
            lblTicketFolio.Text = $"Folio: #{pedidoSeleccionado.Folio}";
            lblTicketCliente.Text = $"Cliente/Mesa: {pedidoSeleccionado.NombreCliente ?? "Sin Nombre"}";
            lblTicketFecha.Text = $"Fecha: {pedidoSeleccionado.FechaPedido.ToString("dd/MM/yyyy HH:mm")}";
            lblTicketTipo.Text = $"Tipo: {pedidoSeleccionado.TipoEntrega ?? "N/A"} - {pedidoSeleccionado.TipoPago ?? "N/A"}";
            lblTicketTotal.Text = $"${pedidoSeleccionado.Total:F2}";

            // BUSCAMOS LOS PLATILLOS Y CRUZAMOS LA INFORMACIÓN
            try
            {
                // 1. Traemos los códigos y cantidades del pedido
                var respuestaDetalles = await _supabase.From<DetallePedido>()
                                                       .Where(d => d.IdPedido == pedidoSeleccionado.IdPedido)
                                                       .Get();
                var detalles = respuestaDetalles.Models;

                // 2. Traemos el menú para saber cómo se llaman (Catálogo)
                var respuestaPlatillos = await _supabase.From<Models.Platillo>().Get();
                var catalogo = respuestaPlatillos.Models;

                // 3. Armamos la lista visual para el XAML
                var listaVisualTicket = new List<ItemTicketUI>();

                foreach (var detalle in detalles)
                {
                    // Buscamos el platillo que coincide con el ID guardado
                    // (Nota: Asumo que en tu modelo Platillo la llave se llama IdPlatillo y el texto Nombre)
                    var platilloReal = catalogo.FirstOrDefault(p => p.IdPlatillo == detalle.IdPlatillo);

                    listaVisualTicket.Add(new ItemTicketUI
                    {
                        Cantidad = detalle.Cantidad,
                        NombrePlatillo = platilloReal != null ? platilloReal.Nombre : "Platillo Borrado",
                        Subtotal = detalle.Subtotal
                    });
                }

                // Le damos la lista ya traducida a la pantalla
                listaDetallesTicket.ItemsSource = listaVisualTicket;
            }
            catch
            {
                // Si falla por internet, la lista quedará en blanco pero el ticket sí abre
                listaDetallesTicket.ItemsSource = null;
            }

            modalTicket.IsVisible = true;
        }
    }

    private void OnCerrarTicketClicked(object sender, EventArgs e)
    {
        modalTicket.IsVisible = false;
    }

    private async void OnCancelarInvoked(object sender, EventArgs e)
    {
        var swipeItem = sender as SwipeItem;
        var pedidoSeleccionado = swipeItem.CommandParameter as Pedido;

        bool confirmar = await DisplayAlert("⚠️ Cancelar Venta", $"¿Estás seguro de cancelar la orden #{pedidoSeleccionado.Folio}? Esta acción restará el dinero de los reportes.", "Sí, Cancelar", "No");

        if (confirmar)
        {
            try
            {
                pedidoSeleccionado.Estado = "Cancelado";
                await _supabase.From<Pedido>().Update(pedidoSeleccionado);

                await DisplayAlert("Éxito", "El pedido ha sido cancelado.", "OK");
                await CargarHistorial();
            }
            catch (Exception ex)
            {
                await DisplayAlert("Error", "No se pudo cancelar: " + ex.Message, "OK");
            }
        }
    }

    // MENÚ NATIVO DE TRES PUNTOS (ActionSheet)
    // ¡ACTUALIZADO!: AHORA SÍ CON LA OPCIÓN DE IMPRIMIR INCLUIDA
    private async void OnDotsTapped(object sender, EventArgs e)
    {
        // 1. Identificamos el pedido que se tocó
        var label = sender as Label;
        var pedidoSeleccionado = label.BindingContext as Pedido; // Obtenemos el pedido ligado a este Frame

        if (pedidoSeleccionado == null) return;

        // 2. Lanzamos el menú nativo ActionSheet (¡YA CON IMPRIMIR TICKET REGRESADO!)
        string accion = await DisplayActionSheet($"Opciones de Orden #{pedidoSeleccionado.Folio}", "Cerrar", null, "Imprimir Ticket", "Compartir Venta", "Eliminar Pedido");

        // --- LÓGICA DE IMPRIMIR TICKET ---
        if (accion == "Imprimir Ticket")
        {
            await DisplayAlert("Impresora", "Buscando impresora térmica por Bluetooth...", "OK");
            // Aquí iría la lógica o librería de tu impresora térmica en el futuro
        }
        // --- LÓGICA DE COMPARTIR VENTA ---
        else if (accion == "Compartir Venta")
        {
            await DisplayAlert("Preparando", "Estamos generando el resumen de venta con detalles...", "OK");

            string stringDetallesTicket = "";

            try
            {
                var respuestaDetalles = await _supabase.From<DetallePedido>()
                                                       .Where(d => d.IdPedido == pedidoSeleccionado.IdPedido)
                                                       .Get();
                var detalles = respuestaDetalles.Models;

                var respuestaPlatillos = await _supabase.From<Models.Platillo>().Get();
                var catalogo = respuestaPlatillos.Models;

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
        // --- LÓGICA DE ELIMINAR PEDIDO ---
        else if (accion == "Eliminar Pedido")
        {
            bool confirmar = await DisplayAlert("⚠️ Eliminar", $"¿Estás seguro de eliminar permanentemente la orden #{pedidoSeleccionado.Folio} de {pedidoSeleccionado.NombreCliente ?? "Mesa"}? Se restará de tus ganancias.", "Sí, Eliminar", "No");

            if (confirmar)
            {
                try
                {
                    pedidoSeleccionado.Estado = "Eliminado";
                    await _supabase.From<Pedido>().Update(pedidoSeleccionado);

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
        // Si el switch se prende, habilitamos los calendarios y les damos color normal
        contenedorFechas.IsEnabled = e.Value;
        contenedorFechas.Opacity = e.Value ? 1.0 : 0.5;
        AplicarFiltros();
    }
}

// Pon esto hasta abajo de tu archivo HistorialVentas.xaml.cs
public class ItemTicketUI
{
    public int Cantidad { get; set; }
    public string NombrePlatillo { get; set; }
    public decimal Subtotal { get; set; }
}