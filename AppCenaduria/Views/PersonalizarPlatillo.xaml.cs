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

        // Preguntamos al usuario qué quiere hacer
        bool irACarrito = await DisplayAlert("¡Añadido!", $"{_cantidad}x {_platilloActual.Nombre} en tu carrito.", "Ir a Mi Carrito", "Seguir pidiendo");

        if (irACarrito)
        {
            // Si elige ir al carrito, cambiamos la pantalla principal usando el menú lateral
            if (Application.Current.MainPage is FlyoutPage menu)
            {
                menu.Detail = new NavigationPage(new MiCarrito());
            }
        }
        else
        {
            // Si elige seguir pidiendo, lo regresamos al catálogo
            await Navigation.PopAsync();
        }
    }
}