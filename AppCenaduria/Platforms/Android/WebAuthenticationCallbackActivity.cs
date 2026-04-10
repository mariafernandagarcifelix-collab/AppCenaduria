using System;
using System.Collections.Generic;
using System.Text;
using Android.App;
using Android.Content;
using Android.Content.PM;

namespace AppCenaduria.Platforms.Android
{
    // Este decorador le dice al sistema operativo Android que esta clase está escuchando la URL mágica
    [Activity(NoHistory = true, LaunchMode = LaunchMode.SingleTop, Exported = true)]
    [IntentFilter(new[] { Intent.ActionView },
        Categories = new[] { Intent.CategoryDefault, Intent.CategoryBrowsable },
        DataScheme = "cenaduriaapp")]
    public class WebAuthenticationCallbackActivity : Microsoft.Maui.Authentication.WebAuthenticatorCallbackActivity
    {
        // No necesitas escribir nada aquí adentro, MAUI hace la magia en el fondo.
    }
}
