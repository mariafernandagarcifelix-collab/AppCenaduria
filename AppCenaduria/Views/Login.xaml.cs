using Supabase;
using AppCenaduria.Controllers;
using Microsoft.Maui.Authentication;
using System.Threading.Tasks;

namespace AppCenaduria.Views;

public partial class Login : ContentPage
{
    private LoginController _controller;

    public Login()
    {
        InitializeComponent();
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        if (_controller == null)
        {
            var supabase = Application.Current.Handler.MauiContext.Services.GetService<Supabase.Client>();
            _controller = new LoginController(supabase);
        }

        // Disparamos la animación del logo al abrir la pantalla
        _ = AnimarLogo();
    }

    private async void OnGoogleAuthClicked(object sender, EventArgs e)
    {
        if (loaderGoogle.IsRunning) return; // Prevenir doble click

        // 1. FORZAMOS el cambio visual en el hilo principal para que SÍ diga "Procesando..."
        MainThread.BeginInvokeOnMainThread(() =>
        {
            loaderGoogle.IsRunning = true;
            loaderGoogle.IsVisible = true;
            lblGoogleBtn.Text = "Procesando...";
            btnGoogle.Opacity = 0.7;
        });

        // 2. Damos tiempo suficiente para que la pantalla se dibuje antes de abrir el navegador
        await Task.Delay(300);

        try
        {
            string loginUrl = "https://lliiyuxmrswelexktuxh.supabase.co/auth/v1/authorize?provider=google&redirect_to=cenaduriaapp://";
            var authResult = await WebAuthenticator.Default.AuthenticateAsync(new Uri(loginUrl), new Uri("cenaduriaapp://"));

            string accessToken = authResult.Properties["access_token"];
            string refreshToken = authResult.Properties["refresh_token"];

            var usuarioDb = await _controller.IniciarSesionGoogleAsync(accessToken, refreshToken);

            // 🔥 3. RECUPERAMOS EL FIX DEL CARRITO QUE SE HABÍA BORRADO 🔥
            App.UsuarioActual = usuarioDb;

            Preferences.Set("UserRole", usuarioDb.Rol);

            MainThread.BeginInvokeOnMainThread(() =>
            {
                Application.Current.MainPage = new Menu(usuarioDb.Rol);
            });
        }
        catch (TaskCanceledException)
        {
            RestaurarBotonLogin();
            MainThread.BeginInvokeOnMainThread(async () =>
            {
                await DisplayAlert("Cancelado", "Se canceló el inicio de sesión con Google.", "OK");
            });
        }
        catch (Exception ex)
        {
            RestaurarBotonLogin();
            MainThread.BeginInvokeOnMainThread(async () =>
            {
                await DisplayAlert("Error de Conexión", "Ocurrió un problema: " + ex.Message, "OK");
            });
        }
    }

    private void RestaurarBotonLogin()
    {
        MainThread.BeginInvokeOnMainThread(() =>
        {
            loaderGoogle.IsRunning = false;
            loaderGoogle.IsVisible = false;
            lblGoogleBtn.Text = "Continuar con Google";
            btnGoogle.Opacity = 1.0;
        });
    }

    // --- Animación tipo brillo infinito ---
    private async Task AnimarLogo()
    {
        while (true)
        {
            // Reinicia posición
            shine.TranslationX = -120;
            shine.Opacity = 0.8;

            // Movimiento diagonal tipo brillo
            await shine.TranslateTo(180, 0, 1200, Easing.CubicInOut);

            // Desaparece
            shine.Opacity = 0;

            await Task.Delay(1500);
        }
    }
}