using System.Threading.Tasks;
using AppCenaduria.Models;

namespace AppCenaduria.Views;

public partial class Menu : FlyoutPage
{
    public Menu(string rolUsuario)
    {
        InitializeComponent();

        if (rolUsuario != "Administrador")
        {
            seccionAdmin.IsVisible = false;
        }
        else
        {
            seccionPerfil.IsVisible = false;
        }
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();

        // 1. ACTUALIZAR EL CONTADOR DEL CARRITO
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

        // 2. ANIMACIÓN EN CASCADA DEL MENÚ
        foreach (var item in menuStack.Children)
        {
            if (item is Border b)
            {
                b.Opacity = 0;
                await b.FadeTo(1, 150);
                await Task.Delay(30);
            }
            else if (item is VerticalStackLayout adminStack)
            {
                foreach (var adminItem in adminStack.Children)
                {
                    if (adminItem is Border adminB)
                    {
                        adminB.Opacity = 0;
                        await adminB.FadeTo(1, 150);
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

    // --- BOTONES DEL MENÚ (AHORA USAN TappedEventArgs) ---
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

    // --- MÉTODO PARA EL BOTÓN FLOTANTE DE LA PANTALLA DE INICIO ---
    private async void OnFabInicioClicked(object sender, EventArgs e)
    {
        await AnimarBotonClic(sender);
        Detail = new NavigationPage(new MiCarrito());
        IsPresented = false;
    }
}