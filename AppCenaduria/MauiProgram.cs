// Importación de los espacios de nombres necesarios para el funcionamiento de la aplicación
using Microsoft.Extensions.Logging;
using Microsoft.Maui.LifecycleEvents;
using Plugin.Firebase.CloudMessaging;
using Plugin.Firebase.Core;
using Supabase;
using Syncfusion.Maui.Core.Hosting;

// Directiva del preprocesador que importa librerías específicas solo si se compila para Android
#if ANDROID
using Plugin.Firebase.Core.Platforms.Android;
#endif

namespace AppCenaduria // Espacio de nombres principal del proyecto
{
    public static class MauiProgram
    {
        // Método principal que inicializa, configura y construye la aplicación MAUI
        public static MauiApp CreateMauiApp()
        {
            var builder = MauiApp.CreateBuilder(); // Crea el constructor de la aplicación

            builder
                .UseMauiApp<App>() // Define que la clase 'App' es la raíz visual y lógica de la aplicación
                .ConfigureSyncfusionCore() // Inicializa el núcleo de los controles de Syncfusion
                .RegisterFirebaseServices() // Llama al método de extensión local (definido más abajo) para configurar Firebase
                .ConfigureFonts(fonts => // Configura las fuentes personalizadas para toda la interfaz
                {
                    // Registra la fuente OpenSans regular y le asigna el alias "OpenSansRegular"
                    fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                    // Registra la fuente OpenSans semibold y le asigna el alias "OpenSansSemibold"
                    fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
                });

            // URL base de conexión a tu proyecto en Supabase
            string supabaseUrl = "https://lliiyuxmrswelexktuxh.supabase.co";
            // Clave pública (anon key) de Supabase para la autenticación y peticiones seguras
            string supabaseKey = "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJpc3MiOiJzdXBhYmFzZSIsInJlZiI6ImxsaWl5dXhtcnN3ZWxleGt0dXhoIiwicm9sZSI6ImFub24iLCJpYXQiOjE3NzQ4NDc1NDIsImV4cCI6MjA5MDQyMzU0Mn0.3SA0cQG4X77bwiAegniM1ZO8iJpmGWa2lNlRjKQYpTI";

            // Configuración de las opciones del cliente de Supabase
            var options = new SupabaseOptions
            {
                AutoRefreshToken = true // Habilita la renovación automática del token de sesión del usuario
            };

            // Se instancia el cliente de Supabase con la URL, la clave y las opciones de sesión
            var supabaseClient = new Supabase.Client(supabaseUrl, supabaseKey, options);

            // Se registra el cliente de Supabase como un servicio Singleton (una única instancia compartida en toda la app) mediante inyección de dependencias
            builder.Services.AddSingleton(supabaseClient);

            // Directiva para agregar logs de depuración solo cuando se está compilando en modo DEBUG
#if DEBUG
            builder.Logging.AddDebug();
#endif

            // Construye y devuelve la instancia de la aplicación MAUI ya configurada
            return builder.Build();
        }

        // Método de extensión privado que encapsula la configuración e inicialización de Firebase
        private static MauiAppBuilder RegisterFirebaseServices(this MauiAppBuilder builder)
        {
            builder.ConfigureLifecycleEvents(events => {
                // Configuración específica de eventos del ciclo de vida para la plataforma Android
#if ANDROID
                // Se suscribe al evento OnCreate de la actividad principal de Android
                events.AddAndroid(android => android.OnCreate((activity, state) =>
                    // Inicializa el núcleo de Firebase pasándole la actividad actual
                    CrossFirebase.Initialize(activity, () => Microsoft.Maui.ApplicationModel.Platform.CurrentActivity)));
#endif
            });

            // Registramos el servicio de mensajería en la nube (Notificaciones Push de Firebase) como Singleton en el contenedor de dependencias
            builder.Services.AddSingleton(_ => CrossFirebaseCloudMessaging.Current);

            return builder; // Devuelve el builder para permitir que se sigan encadenando métodos
        }
    }
}