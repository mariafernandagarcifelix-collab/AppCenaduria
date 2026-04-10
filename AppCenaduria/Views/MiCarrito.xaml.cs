using AppCenaduria.Models;
using AppCenaduria.Controllers;

namespace AppCenaduria.Views;

public partial class MiCarrito : ContentPage
{
    private MiCarritoController _controller;

    public MiCarrito()
    {
        InitializeComponent();
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();

        if (_controller == null)
        {
            var supabase = Application.Current.Handler.MauiContext.Services.GetService<Supabase.Client>();
            _controller = new MiCarritoController(supabase);
        }

        listaCarrito.ItemsSource = null;
        listaCarrito.ItemsSource = Carrito.CarritoGlobal.Articulos;

        decimal totalPedido = 0;
        foreach (var item in Carrito.CarritoGlobal.Articulos)
        {
            totalPedido += item.Subtotal;
        }

        lblTotal.Text = $"${totalPedido:F2}";
    }

    private async void OnConfirmarPedidoClicked(object sender, EventArgs e)
    {
        if (Carrito.CarritoGlobal.Articulos.Count == 0) return;

        try
        {
            var miUsuarioReal = await _controller.ObtenerUsuarioActualAsync();

            if (string.IsNullOrWhiteSpace(miUsuarioReal?.NombreCompleto) ||
                string.IsNullOrWhiteSpace(miUsuarioReal?.Telefono) ||
                string.IsNullOrWhiteSpace(miUsuarioReal?.Domicilio))
            {
                await DisplayAlert("Datos Incompletos", "Para realizar un pedido, necesitamos tu nombre, teléfono y dirección de entrega.", "Ir a Mi Perfil");

                if (Application.Current.MainPage is FlyoutPage menu)
                {
                    menu.Detail = new NavigationPage(new Perfil());
                }
                return;
            }

            decimal totalReal = Carrito.CarritoGlobal.Articulos.Sum(x => x.Subtotal);

            var pedidoGuardado = await _controller.CrearPedidoAsync(miUsuarioReal, totalReal, Carrito.CarritoGlobal.Articulos);

            await DisplayAlert("¡Éxito!", "Tu pedido ha sido enviado. ¡Gracias por tu compra!", "OK");

            Carrito.CarritoGlobal.Articulos.Clear();
            listaCarrito.ItemsSource = null;
            lblTotal.Text = "$0.00";

            await _controller.NotificarAdminAsync(pedidoGuardado);

            if (Application.Current.MainPage is FlyoutPage flyout)
            {
                flyout.Detail = new NavigationPage(new VerMenuCliente());
            }
        }
        catch (Exception ex)
        {
            await DisplayAlert("Error", "Hubo un problema: " + ex.Message, "OK");
        }
    }
}