using Microsoft.Extensions.Logging;
using Microsoft.Maui.LifecycleEvents;
using Plugin.Firebase.CloudMessaging;
using Plugin.Firebase.Core;
using Supabase;

// 1. AQUÍ ESTÁ LA LIBRERÍA DE ANDROID QUE FALTABA
#if ANDROID
using Plugin.Firebase.Core.Platforms.Android;
#endif

namespace AppCenaduria
{
    public static class MauiProgram
    {
        public static MauiApp CreateMauiApp()
        {
            var builder = MauiApp.CreateBuilder();
            builder
                .UseMauiApp<App>()
                .RegisterFirebaseServices()
                .ConfigureFonts(fonts =>
                {
                    fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                    fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
                });

            // --- INICIO DE CONEXIÓN A SUPABASE ---
            string supabaseUrl = "https://lliiyuxmrswelexktuxh.supabase.co";
            string supabaseKey = "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJpc3MiOiJzdXBhYmFzZSIsInJlZiI6ImxsaWl5dXhtcnN3ZWxleGt0dXhoIiwicm9sZSI6ImFub24iLCJpYXQiOjE3NzQ4NDc1NDIsImV4cCI6MjA5MDQyMzU0Mn0.3SA0cQG4X77bwiAegniM1ZO8iJpmGWa2lNlRjKQYpTI";

            var options = new SupabaseOptions
            {
                AutoRefreshToken = true
            };

            // Creamos el cliente de base de datos
            var supabaseClient = new Supabase.Client(supabaseUrl, supabaseKey, options);

            // Lo registramos como Singleton para usarlo en cualquier pantalla
            builder.Services.AddSingleton(supabaseClient);
            // --- FIN DE CONEXIÓN ---

#if DEBUG
            builder.Logging.AddDebug();
#endif

            return builder.Build();
        }

        // Método especial para despertar Firebase
        private static MauiAppBuilder RegisterFirebaseServices(this MauiAppBuilder builder)
        {
            builder.ConfigureLifecycleEvents(events => {
#if ANDROID
                // 2. AQUÍ ESTÁ LA VERSIÓN NUEVA DEL INITIALIZE CON LA BRÚJULA
                events.AddAndroid(android => android.OnCreate((activity, state) =>
                    CrossFirebase.Initialize(activity, () => Microsoft.Maui.ApplicationModel.Platform.CurrentActivity)));
#endif
            });

            // Registramos el servicio de mensajería (Notificaciones)
            builder.Services.AddSingleton(_ => CrossFirebaseCloudMessaging.Current);
            return builder;
        }
    }
}