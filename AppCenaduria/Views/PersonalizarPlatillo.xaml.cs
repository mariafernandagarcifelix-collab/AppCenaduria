using AppCenaduria.Models;
using static AppCenaduria.Models.Carrito;

namespace AppCenaduria.Views;

public partial class PersonalizarPlatillo : ContentPage
{
    private Platillo _platilloActual;
    private int _cantidad = 1;

    // Modificamos el constructor para que exija saber qué platillo eligieron
    public PersonalizarPlatillo(Platillo platilloElegido)
    {
        InitializeComponent();

        _platilloActual = platilloElegido;

        // Llenamos la pantalla con los datos del platillo
        lblNombre.Text = _platilloActual.Nombre;
        lblDescripcion.Text = _platilloActual.Descripcion;
        lblPrecio.Text = $"${_platilloActual.Precio:F2}";
        imgPlatillo.Source = _platilloActual.FotografiaUrl;
    }

    private void OnMenosClicked(object sender, EventArgs e)
    {
        if (_cantidad > 1)
        {
            _cantidad--;
            lblCantidad.Text = _cantidad.ToString();
        }
    }

    private void OnMasClicked(object sender, EventArgs e)
    {
        // Límite de 20 platillos iguales por seguridad
        if (_cantidad < 20)
        {
            _cantidad++;
            lblCantidad.Text = _cantidad.ToString();
        }
    }

    private async void OnAñadirCarritoClicked(object sender, EventArgs e)
    {
        var nuevoItem = new ItemCarrito
        {
            IdPlatillo = _platilloActual.IdPlatillo,
            Nombre = _platilloActual.Nombre,
            FotografiaUrl = _platilloActual.FotografiaUrl,
            Cantidad = _cantidad,
            PrecioUnitario = _platilloActual.Precio,
            Comentarios = txtComentarios.Text?.Trim() ?? "Ninguno"
        };

        CarritoGlobal.Articulos.Add(nuevoItem);

        if (Application.Current.MainPage is AppCenaduria.Views.Menu m)
        {
            m.ActualizarBadgeCarrito();
        }

        // Cambiamos un poco el texto para que suene bien para ambos roles
        bool irACarrito = await DisplayAlert("¡Añadido!", $"{_cantidad}x {_platilloActual.Nombre} agregado a la cuenta.", "Ir a Pagar/Mandar", "Seguir pidiendo");

        if (irACarrito)
        {
            if (Application.Current.MainPage is FlyoutPage menu)
            {
                // 🔥 AQUÍ ESTÁ LA MAGIA: Redirigimos según el rol
                if (App.UsuarioActual != null && App.UsuarioActual.Rol == "Mesero")
                {
                    menu.Detail = new NavigationPage(new ComandaMesero());
                }
                else
                {
                    menu.Detail = new NavigationPage(new MiCarrito());
                }
            }
        }
        else
        {
            await Navigation.PopAsync();
        }
    }
}