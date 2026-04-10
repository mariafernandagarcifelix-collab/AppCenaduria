using AppCenaduria.Models;
using Supabase;

namespace AppCenaduria.Views;

public partial class HistorialCliente : ContentPage
{
    private Supabase.Client _supabase;

    public HistorialCliente()
	{
		InitializeComponent();
	}

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        if (_supabase == null)
            _supabase = Application.Current.Handler.MauiContext.Services.GetService<Supabase.Client>();

        await CargarMisPedidos();
    }

    private async Task CargarMisPedidos()
    {
        try
        {
            // 1. Identificamos qué cliente tiene la sesión abierta
            string correoActual = _supabase.Auth.CurrentUser?.Email;
            if (string.IsNullOrEmpty(correoActual)) return;

            var respuestaUsuario = await _supabase.From<Usuario>().Where(u => u.CorreoGoogle == correoActual).Get();
            var miUsuario = respuestaUsuario.Models.FirstOrDefault();

            if (miUsuario != null)
            {
                // 2. Traemos SOLO los pedidos cuyo "IdUsuario" coincida con el cliente actual
                var respuestaPedidos = await _supabase.From<Pedido>()
                                                      .Where(p => p.IdUsuario == miUsuario.IdUsuario)
                                                      .Order(p => p.FechaPedido, Postgrest.Constants.Ordering.Descending) // Del más nuevo al más viejo
                                                      .Get();

                listaMisPedidos.ItemsSource = respuestaPedidos.Models;
            }
        }
        catch (Exception ex)
        {
            await DisplayAlert("Error", "No pudimos cargar tus pedidos: " + ex.Message, "OK");
        }
    }

    // EVENTO: Mostrar el ticket desglosado al tocar la tarjeta
    private async void OnPedidoTapped(object sender, TappedEventArgs e)
    {
        var pedidoSeleccionado = e.Parameter as Pedido;
        if (pedidoSeleccionado == null) return;

        // Llenamos la cabecera visual
        lblTicketFolio.Text = $"Orden #{pedidoSeleccionado.Folio}";
        lblTicketEstado.Text = $"Estado: {pedidoSeleccionado.Estado}";
        lblTicketTotal.Text = $"${pedidoSeleccionado.Total:F2}";

        try
        {
            // Traemos los códigos y cantidades de los platillos
            var respuestaDetalles = await _supabase.From<DetallePedido>()
                                                   .Where(d => d.IdPedido == pedidoSeleccionado.IdPedido)
                                                   .Get();
            var detalles = respuestaDetalles.Models;

            // Traemos el menú para saber los nombres de los platillos
            var respuestaPlatillos = await _supabase.From<Platillo>().Get();
            var catalogo = respuestaPlatillos.Models;

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

        // Mostramos el pop-up negro
        modalTicketCliente.IsVisible = true;
    }

    private void OnCerrarTicketClicked(object sender, EventArgs e)
    {
        modalTicketCliente.IsVisible = false;
    }
}

// Clase de apoyo exclusiva para el formato de este ticket visual (Pegar hasta el fondo)
public class ItemTicketClienteUI
{
    public int Cantidad { get; set; }
    public string NombrePlatillo { get; set; }
    public decimal Subtotal { get; set; }
}