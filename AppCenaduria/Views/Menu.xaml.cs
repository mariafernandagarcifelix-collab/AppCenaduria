using System.Threading.Tasks;
using AppCenaduria.Models;

namespace AppCenaduria.Views;

public partial class Menu : FlyoutPage
{
    public Menu(string rolUsuario)
    {
        InitializeComponent();

        // Escondemos todo por defecto
        seccionCliente.IsVisible = false;
        seccionMesero.IsVisible = false;
        seccionAdmin.IsVisible = false;

        // Mostramos solo la sección que le corresponde a su rol
        if (rolUsuario == "Administrador")
        {
            seccionAdmin.IsVisible = true;
            Detail = new NavigationPage(new GestionPedidos());
        }
        else if (rolUsuario == "Mesero")
        {
            seccionMesero.IsVisible = true;
            Detail = new NavigationPage(new VerMenuMesero());
        }
        else // Si es "Cliente"
        {
            seccionCliente.IsVisible = true;
            Detail = new NavigationPage(new VerMenuCliente());
        }
    }

    // ¡AQUÍ ESTÁ EL MÉTODO QUE FALTABA!
    public void ActualizarBadgeCarrito()
    {
        int cantidadPlatillos = Carrito.CarritoGlobal.Articulos.Count;
        if (cantidadPlatillos > 0)
        {
            badgeCarrito.IsVisible = true;
            lblCantidadCarrito.Text = cantidadPlatillos.ToString();
        }
        else
        {
            badgeCarrito.IsVisible = false;
        }
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();

        ActualizarBadgeCarrito();

        // Animación en cascada solo para la sección que esté visible
        foreach (var item in menuStack.Children)
        {
            if (item is VerticalStackLayout stack && stack.IsVisible)
            {
                foreach (var subItem in stack.Children)
                {
                    if (subItem is Border controlBorder)
                    {
                        controlBorder.Opacity = 0;
                        await controlBorder.FadeTo(1, 150);
                        await Task.Delay(30);
                    }
                }
            }
        }
    }

    private async Task AnimarBotonClic(object sender)
    {
        if (sender is Border border)
        {
            await border.ScaleTo(0.95, 50);
            await border.ScaleTo(1, 50);
        }
        else if (sender is Button btn)
        {
            await btn.ScaleTo(0.90, 50);
            await btn.ScaleTo(1, 50);
        }
    }

    // --- BOTONES CLIENTE ---
    private async void OnPerfilClicked(object sender, TappedEventArgs e)
    {
        await AnimarBotonClic(sender);
        Detail = new NavigationPage(new Perfil());
        IsPresented = false;
    }

    private async void OnVerMenuClicked(object sender, TappedEventArgs e)
    {
        await AnimarBotonClic(sender);
        Detail = new NavigationPage(new VerMenuCliente());
        IsPresented = false;
    }

    private async void OnMiCarritoClicked(object sender, TappedEventArgs e)
    {
        await AnimarBotonClic(sender);
        Detail = new NavigationPage(new MiCarrito());
        IsPresented = false;
    }

    private async void OnHistorialClicked(object sender, TappedEventArgs e)
    {
        await AnimarBotonClic(sender);
        Detail = new NavigationPage(new HistorialPedidos());
        IsPresented = false;
    }

    // --- BOTONES MESERO ---
    private async void OnVerMenuMeseroClicked(object sender, TappedEventArgs e)
    {
        await AnimarBotonClic(sender);
        Detail = new NavigationPage(new VerMenuMesero());
        IsPresented = false;
    }

    private async void OnComandaMeseroClicked(object sender, TappedEventArgs e)
    {
        await AnimarBotonClic(sender);
        Detail = new NavigationPage(new ComandaMesero());
        IsPresented = false;
    }

    private async void OnEstatusCocinaMeseroClicked(object sender, TappedEventArgs e)
    {
        await AnimarBotonClic(sender);
        Detail = new NavigationPage(new EstatusCocinaMesero());
        IsPresented = false;
    }

    // --- BOTONES ADMIN ---
    private async void OnAltaMenuClicked(object sender, TappedEventArgs e)
    {
        await AnimarBotonClic(sender);
        Detail = new NavigationPage(new AltaMenu());
        IsPresented = false;
    }

    private async void OnGestionPedidosClicked(object sender, TappedEventArgs e)
    {
        await AnimarBotonClic(sender);
        Detail = new NavigationPage(new GestionPedidos());
        IsPresented = false;
    }

    private async void OnReportesClicked(object sender, TappedEventArgs e)
    {
        await AnimarBotonClic(sender);
        Detail = new NavigationPage(new Reportes());
        IsPresented = false;
    }

    private async void OnHistorialVentasClicked(object sender, TappedEventArgs e)
    {
        await AnimarBotonClic(sender);
        Detail = new NavigationPage(new HistorialVentas());
        IsPresented = false;
    }

    // --- CERRAR SESIÓN ---
    private async void OnCerrarSesionClicked(object sender, TappedEventArgs e)
    {
        await AnimarBotonClic(sender);

        bool respuesta = await DisplayAlert("Cerrar Sesión", "¿Estás seguro de que quieres salir?", "Sí, salir", "Cancelar");
        if (respuesta)
        {
            Preferences.Remove("UserRole");
            Application.Current.MainPage = new Login();
        }
    }
}