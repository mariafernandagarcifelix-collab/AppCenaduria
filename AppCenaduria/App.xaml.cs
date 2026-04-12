using AppCenaduria.Models;
using AppCenaduria.Views;
using Microsoft.Extensions.DependencyInjection;
using Plugin.Firebase.CloudMessaging;

namespace AppCenaduria
{
    public partial class App : Application
    {
        public static Usuario UsuarioActual { get; set; }

        public App()
        {
            InitializeComponent();

            MainPage = new NavigationPage(new ContentPage { Content = new ActivityIndicator { IsRunning = true, VerticalOptions = LayoutOptions.Center } });
        }

        protected override async void OnStart()
        {
            base.OnStart();

            try
            {
                var supabase = Application.Current.Handler.MauiContext.Services.GetService<Supabase.Client>();
                await supabase.InitializeAsync();

                var sesionActual = supabase.Auth.CurrentSession;

                if (sesionActual != null && sesionActual.User != null)
                {
                    var respuesta = await supabase.From<Usuario>()
                                                  .Where(x => x.CorreoGoogle == sesionActual.User.Email)
                                                  .Get();

                    UsuarioActual = respuesta.Models.FirstOrDefault();

                    if (UsuarioActual != null)
                    {
                        MainThread.BeginInvokeOnMainThread(() => {
                            MainPage = new Menu(UsuarioActual.Rol);
                        });
                    }
                    else
                    {
                        IrAlLogin();
                    }
                }
                else
                {
                    IrAlLogin();
                }

                await ConfigurarNotificaciones();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error en OnStart: {ex.Message}");
                IrAlLogin();
            }
        }

        private void IrAlLogin()
        {
            Preferences.Remove("UserRole");
            MainThread.BeginInvokeOnMainThread(() => {
                MainPage = new Login();
            });
        }

        private async Task ConfigurarNotificaciones()
        {
            try
            {
                if (DeviceInfo.Platform == DevicePlatform.Android)
                {
                    await Permissions.RequestAsync<Permissions.PostNotifications>();
                }
                await CrossFirebaseCloudMessaging.Current.CheckIfValidAsync();

                // Obtenemos el token del celular
                var token = await CrossFirebaseCloudMessaging.Current.GetTokenAsync();
                Preferences.Set("FCMTokenGuardado", token);

                // 🔥 EL PASO QUE FALTABA: Subir el token a tu tabla de Supabase 🔥
                if (UsuarioActual != null && UsuarioActual.TokenNotificacion != token)
                {
                    UsuarioActual.TokenNotificacion = token;
                    var supabase = Application.Current.Handler.MauiContext.Services.GetService<Supabase.Client>();
                    await supabase.From<Usuario>().Update(UsuarioActual);
                    System.Diagnostics.Debug.WriteLine($"✅ Token de {UsuarioActual.NombreCompleto} actualizado en la BD.");
                }

                CrossFirebaseCloudMessaging.Current.NotificationReceived += (sender, args) => {
                    if (args.Notification != null)
                    {
                        MainThread.BeginInvokeOnMainThread(() => {
#if ANDROID
                            MostrarNotificacionNativaAndroid(args.Notification.Title, args.Notification.Body);
#else
                            Application.Current.MainPage.DisplayAlert(args.Notification.Title, args.Notification.Body, "OK");
#endif
                        });
                    }
                };
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error configurando notificaciones: {ex.Message}");
            }
        }

        // =========================================================================
        // MÉTODO EXCLUSIVO DE ANDROID PARA DIBUJAR EL BANNER SUPERIOR
        // =========================================================================
#if ANDROID
        private void MostrarNotificacionNativaAndroid(string titulo, string cuerpo)
        {
            var context = global::Android.App.Application.Context;
            var channelId = "cenaduria_notifications";

            // 1. Crear el canal (Obligatorio para versiones modernas de Android)
            if (global::Android.OS.Build.VERSION.SdkInt >= global::Android.OS.BuildVersionCodes.O)
            {
                var channel = new global::Android.App.NotificationChannel(
                    channelId,
                    "Notificaciones de El Parquesito",
                    global::Android.App.NotificationImportance.High);

                var notificationManager = (global::Android.App.NotificationManager)context.GetSystemService(global::Android.Content.Context.NotificationService);
                notificationManager?.CreateNotificationChannel(channel);
            }

            // 2. Construir la tarjeta visual (El globito que baja)
            var builder = new global::Android.App.Notification.Builder(context, channelId)
                .SetContentTitle(titulo ?? "Cenaduría El Parquesito")
                .SetContentText(cuerpo ?? "Tienes una nueva notificación")
                .SetSmallIcon(global::Android.Resource.Drawable.IcDialogInfo) // Ícono por defecto
                .SetAutoCancel(true);

            // 3. Disparar la notificación para que suene y vibre
            var notifManager = (global::Android.App.NotificationManager)context.GetSystemService(global::Android.Content.Context.NotificationService);
            notifManager?.Notify(new System.Random().Next(1000, 9999), builder.Build());
        }
#endif
    }
}