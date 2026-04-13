using AppCenaduria.Models;
using AppCenaduria.Controllers;

namespace AppCenaduria.Views;

public partial class EstatusCocinaMesero : ContentPage
{
    private GestionPedidosController _controller;
    private List<Pedido> _todosLosPedidosPendientes = new List<Pedido>();
    private string _filtroActivo = "Todos";

    public EstatusCocinaMesero()
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
            _todosLosPedidosPendientes = await _controller.ObtenerPedidosPendientesAsync();
            AplicarFiltros();
        }
        catch (Exception ex)
        {
            await DisplayAlert("Error", "No pudimos cargar las órdenes: " + ex.Message, "OK");
        }
    }

    private void OnFiltrosCambiados(object sender, EventArgs e) => AplicarFiltros();

    private void OnFiltroChipClicked(object sender, EventArgs e)
    {
        var botonPresionado = sender as Button;
        _filtroActivo = botonPresionado.StyleId;

        // Limpiar colores
        var contenedor = botonPresionado.Parent as HorizontalStackLayout;
        foreach (Button btn in contenedor.Children)
        {
            btn.BackgroundColor = Color.FromArgb("#141414");
            btn.TextColor = Color.FromArgb("#888888");
        }
        // Iluminar
        botonPresionado.BackgroundColor = Color.FromArgb("#fc4b08");
        botonPresionado.TextColor = Colors.White;

        AplicarFiltros();
    }

    private void AplicarFiltros()
    {
        if (_todosLosPedidosPendientes == null)
        {
            listaPedidos.IsVisible = false;
            vistaVacia.IsVisible = true;
            return;
        }

        var textoBusqueda = sbBuscador.Text?.ToLower() ?? "";

        var filtrados = _todosLosPedidosPendientes.Where(p =>
            ((p.NombreCliente != null && p.NombreCliente.ToLower().Contains(textoBusqueda)) ||
             (p.Folio.ToString().Contains(textoBusqueda))) &&
            (_filtroActivo == "Todos" || p.TipoEntrega == _filtroActivo)
        ).ToList();

        listaPedidos.ItemsSource = filtrados;

        // 🔥 Control manual de la vista vacía
        if (filtrados.Count == 0)
        {
            listaPedidos.IsVisible = false;
            vistaVacia.IsVisible = true;
        }
        else
        {
            listaPedidos.IsVisible = true;
            vistaVacia.IsVisible = false;
        }
    }

    private async void OnEditarPedidoClicked(object sender, EventArgs e)
    {
        var boton = sender as Button;
        var pedidoSeleccionado = boton.CommandParameter as Pedido;

        if (pedidoSeleccionado.Estado != "En preparación")
        {
            await DisplayAlert("Acción no permitida", "Solo se pueden editar los pedidos que siguen 'En preparación'.", "OK");
            return;
        }

        string pin = await DisplayPromptAsync("Seguridad Requerida", "Pide a un Administrador que ingrese su PIN para autorizar el cambio:", keyboard: Keyboard.Numeric);

        if (pin != "1234")
        {
            await DisplayAlert("Acceso Denegado", "PIN incorrecto o cancelado. No se puede editar la orden.", "OK");
            return;
        }

        await Navigation.PushAsync(new EditarPedidoEnCocina(pedidoSeleccionado));
    }

    private async void OnVerPedidoClicked(object sender, EventArgs e)
    {
        var boton = sender as Button;
        var pedidoSeleccionado = boton.CommandParameter as Pedido;

        if (pedidoSeleccionado != null)
        {
            await Navigation.PushAsync(new ComandaPedido(pedidoSeleccionado));
        }
    }
}