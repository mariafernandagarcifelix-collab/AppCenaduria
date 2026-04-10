namespace AppCenaduria.Views;

public partial class Menu : FlyoutPage
{
	public Menu(string rolUsuario)
	{
		InitializeComponent();

        // Regla de Seguridad: Si NO es administrador, desaparecemos los botones
        if (rolUsuario != "Administrador")
        {
            seccionAdmin.IsVisible = false;
        }
    }

    private void OnPerfilClicked(object sender, EventArgs e)
    {
        Detail = new NavigationPage(new Perfil());
        IsPresented = false;
    }

    private void OnAltaMenuClicked(object sender, EventArgs e)
    {
        // Cambiamos la vista principal (Detail) por nuestra nueva página
        Detail = new NavigationPage(new AltaMenu());
        // Escondemos el menú lateral automáticamente
        IsPresented = false;
    }

    private void OnVerMenuClicked(object sender, EventArgs e)
    {
        // Cambiamos la vista principal por el catálogo del cliente
        Detail = new NavigationPage(new VerMenuCliente());
        IsPresented = false; // Cerramos el menú lateral
    }

    private void OnMiCarritoClicked(object sender, EventArgs e)
    {
        // Cambiamos la vista principal a la pantalla del carrito
        Detail = new NavigationPage(new MiCarrito());
        IsPresented = false; // Cerramos el menú lateral
    }

    private void OnGestionPedidosClicked(object sender, EventArgs e)
    {
        Detail = new NavigationPage(new GestionPedidos());
        IsPresented = false;
    }

    private void OnHistorialClicked(object sender, EventArgs e)
    {
        Detail = new NavigationPage(new HistorialPedidos());
        IsPresented = false;
    }

    private void OnReportesClicked(object sender, EventArgs e)
    {
        Detail = new NavigationPage(new Reportes());
        IsPresented = false;

    }

    private void OnHistorialVentasClicked(object sender, EventArgs e)
    {
        Detail = new NavigationPage(new HistorialVentas());
        IsPresented = false;
    } 

    private async void OnCerrarSesionClicked(object sender, EventArgs e)
    {
        bool confirmacion = await DisplayAlert("Cerrar Sesión", "¿Estás seguro de que deseas salir?", "Sí", "Cancelar");
        if (confirmacion)
        {
            try
            {
                var supabase = Application.Current.Handler.MauiContext.Services.GetService<Supabase.Client>();
                await supabase.Auth.SignOut();
            }
            catch { } // Ignore errors if already signed out
            
            Preferences.Remove("UserRole");
            Application.Current.MainPage = new Login();
        }
    }
}