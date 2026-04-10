using AppCenaduria.Views;
using Microsoft.Extensions.DependencyInjection;
using Plugin.Firebase.CloudMessaging;

namespace AppCenaduria
{
    public partial class App : Application
    {
        public App()
        {
            InitializeComponent();
            MainPage = new Login();
        }

        protected override async void OnStart()
        {
            base.OnStart();

            try
            {
                // 1. FORZAR LA VENTANA DE PERMISO (Para Android 13+)
                if (DeviceInfo.Platform == DevicePlatform.Android)
                {
                    var status = await Permissions.CheckStatusAsync<Permissions.PostNotifications>();
                    if (status != PermissionStatus.Granted)
                    {
                        await Permissions.RequestAsync<Permissions.PostNotifications>();
                    }
                }

                // 2. Le decimos a Firebase que despierte
                await CrossFirebaseCloudMessaging.Current.CheckIfValidAsync();

                // 3. Obtenemos el "Número de Teléfono" de este celular (FCM Token)
                var token = await CrossFirebaseCloudMessaging.Current.GetTokenAsync();
                // Guardamos el token en la memoria interna del celular
                Preferences.Set("FCMTokenGuardado", token);
                // Lo imprimimos en la consola
                System.Diagnostics.Debug.WriteLine("======================================");
                System.Diagnostics.Debug.WriteLine($"FCM TOKEN DE ESTE CELULAR: {token}");
                System.Diagnostics.Debug.WriteLine("======================================");

                // 4. Programamos la "Antena"
                // 4. Programamos la "Antena"
                CrossFirebaseCloudMessaging.Current.NotificationReceived += (sender, args) =>
                {
                    // Lo dejamos en consola por si acaso
                    System.Diagnostics.Debug.WriteLine($"¡LLEGÓ UN MENSAJE!: {args.Notification.Title}");

                    // --- NUEVO: SI LA APP ESTÁ ABIERTA, MOSTRAMOS UN POP-UP VISUAL ---
                    if (args.Notification != null)
                    {
                        MainThread.BeginInvokeOnMainThread(async () =>
                        {
                            // Lanzamos una alerta nativa en medio de la pantalla
                            await Application.Current.MainPage.DisplayAlert(
                                args.Notification.Title,
                                args.Notification.Body,
                                "¡Genial!"
                            );
                        });
                    }
                };
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error al iniciar notificaciones: {ex.Message}");
            }
        }

        //protected override Window CreateWindow(IActivationState? activationState)
        //{
        //    return new Window(new AppShell());
        //}
    }
}