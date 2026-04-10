using Supabase;
using AppCenaduria.Views;
using Microsoft.Maui.Authentication;

namespace AppCenaduria.Views;

public partial class Login : ContentPage
{
    private Supabase.Client _supabase;
    public Login()
    {
        InitializeComponent();
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();

        // Para este momento, la pantalla ya se dibujó y el Handler ya no es nulo
        if (_supabase == null)
        {
            _supabase = Application.Current.Handler.MauiContext.Services.GetService<Supabase.Client>();
        }
    }



    // --- LA MAGIA DE GOOGLE COMPLETA ---
    private async void OnGoogleAuthClicked(object sender, EventArgs e)
    {
        try
        {
            string loginUrl = "https://lliiyuxmrswelexktuxh.supabase.co/auth/v1/authorize?provider=google&redirect_to=cenaduriaapp://";

            var authResult = await WebAuthenticator.Default.AuthenticateAsync(new Uri(loginUrl), new Uri("cenaduriaapp://"));

            string accessToken = authResult.Properties["access_token"];
            string refreshToken = authResult.Properties["refresh_token"];

            // 4. Le damos esos Tokens a nuestra app y GUARDAMOS LA SESIÓN en una variable
            var sesionGoogle = await _supabase.Auth.SetSession(accessToken, refreshToken);

            // --- LÓGICA DE ROLES Y BASE DE DATOS ---
            // Extraemos el correo directamente del objeto de sesión para evitar que llegue nulo
            string correoFiltro = sesionGoogle.User.Email;

            // Busca al usuario en PostgreSQL
            var respuesta = await _supabase.From<Models.Usuario>()
                                           .Where(x => x.CorreoGoogle == correoFiltro)
                                           .Get();

            var usuarioDb = respuesta.Models.FirstOrDefault();

            // --- ¡NUEVO!: EXTRAEMOS EL TOKEN FCM DE LA MEMORIA DEL CELULAR ---
            string tokenCelular = Preferences.Get("FCMTokenGuardado", "");

            // Si no existe, lo creamos como Cliente nuevo y le pegamos el token
            if (usuarioDb == null)
            {
                usuarioDb = new Models.Usuario
                {
                    CorreoGoogle = correoFiltro,
                    NombreCompleto = "Cliente Nuevo",
                    Rol = "Cliente",
                    TokenNotificacion = tokenCelular // Guardamos el token en la creación
                };

                await _supabase.From<Models.Usuario>().Insert(usuarioDb);
            }
            else
            {
                // Si el cliente ya existía, le actualizamos el token (por si cambió de celular o reinstaló la app)
                if (!string.IsNullOrEmpty(tokenCelular))
                {
                    usuarioDb.TokenNotificacion = tokenCelular;
                    await _supabase.From<Models.Usuario>().Update(usuarioDb);
                }
            }

            // 5. --- NAVEGACIÓN CORREGIDA (FORZANDO EL HILO PRINCIPAL) ---
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