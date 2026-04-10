using Supabase;
using AppCenaduria.Controllers;
using Microsoft.Maui.Authentication;

namespace AppCenaduria.Views;

public partial class Login : ContentPage
{
    private LoginController _controller;

    public Login()
    {
        InitializeComponent();
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        if (_controller == null)
        {
            var supabase = Application.Current.Handler.MauiContext.Services.GetService<Supabase.Client>();
            _controller = new LoginController(supabase);
        }
    }

    private async void OnGoogleAuthClicked(object sender, EventArgs e)
    {
        try
        {
            var usuarioDb = await _controller.IniciarSesionGoogleAsync();

            MainThread.BeginInvokeOnMainThread(() =>
            {
                Application.Current.MainPage = new Menu(usuarioDb.Rol);
            });
        }
        catch (TaskCanceledException)
        {
            MainThread.BeginInvokeOnMainThread(async () =>
            {
                await DisplayAlert("Cancelado", "Se canceló el inicio de sesión con Google.", "OK");
            });
        }
        catch (Exception ex)
        {
            MainThread.BeginInvokeOnMainThread(async () =>
            {
                await DisplayAlert("Error de Conexión", "Ocurrió un problema: " + ex.Message, "OK");
            });
        }
    }
}