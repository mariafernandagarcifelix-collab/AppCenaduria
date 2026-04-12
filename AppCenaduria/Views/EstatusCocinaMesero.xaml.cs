using AppCenaduria.Models;
using AppCenaduria.Controllers;

namespace AppCenaduria.Views;

public partial class EstatusCocinaMesero : ContentPage
{
    private GestionPedidosController _controller;
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
            var pedidos = await _controller.ObtenerPedidosPendientesAsync();

            listaPedidos.ItemsSource = null;
            listaPedidos.ItemsSource = pedidos;
        }
        catch (Exception ex)
        {
            await DisplayAlert("Error", "No pudimos cargar las órdenes: " + ex.Message, "OK");
        }
    }

    // Borra el OnCambiarEstadoClicked viejo y pega este nuevo:

    private async void OnEditarPedidoClicked(object sender, EventArgs e)
    {
        var boton = sender as Button;
        var pedidoSeleccionado = boton.CommandParameter as Pedido;

        // 1. Validamos que el pedido siga en la cocina
        if (pedidoSeleccionado.Estado != "En preparación")
        {
            await DisplayAlert("Acción no permitida", "Solo se pueden editar los pedidos que siguen 'En preparación'.", "OK");
            return;
        }

        // 2. Pedimos el PIN de seguridad al administrador
        string pin = await DisplayPromptAsync("Seguridad Requerida", "Pide a un Administrador que ingrese su PIN para autorizar el cambio:", keyboard: Keyboard.Numeric);

        // Aquí defines el PIN secreto de tu local
        if (pin != "1234")
        {
            await DisplayAlert("Acceso Denegado", "PIN incorrecto o cancelado. No se puede editar la orden.", "OK");
            return;
        }

        // 3. Si llegamos aquí, el Admin puso el PIN correcto
        await DisplayAlert("Modo Edición", "Acceso concedido. Puede editar la orden #" + pedidoSeleccionado.Folio, "OK");

        // En el futuro, aquí pondremos la navegación a tu nueva pantalla de edición:
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