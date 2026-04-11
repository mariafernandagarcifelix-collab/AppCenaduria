using Supabase;
using AppCenaduria.Models;
using AppCenaduria.Controllers;

namespace AppCenaduria.Views;

public partial class VerMenuCliente : ContentPage
{
    private VerMenuClienteController _controller;
    private List<Platillo> _platillosTodos;
    private string _categoriaActual = "Todos";
    private string _textoBusqueda = "";

    public VerMenuCliente()
    {
        InitializeComponent();
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();

        if (_controller == null)
        {
            var supabase = Application.Current.Handler.MauiContext.Services.GetService<Supabase.Client>();
            _controller = new VerMenuClienteController(supabase);
        }

        // 🔥 ANIMACIÓN DEL BOTÓN FLOTANTE
        _ = AnimarFAB();

        // 🔥 ACTUALIZAR EL GLOBO ROJO DEL CARRITO (BADGE)
        int cantidadPlatillos = Carrito.CarritoGlobal.Articulos.Count;
        if (cantidadPlatillos > 0)
        {
            badgeCarritoMenu.IsVisible = true;
            lblCantidadCarrito.Text = cantidadPlatillos.ToString();
        }
        else
        {
            badgeCarritoMenu.IsVisible = false;
        }

        await CargarMenuAsync();
    }

    private async Task CargarMenuAsync()
    {
        try
        {
            _platillosTodos = await _controller.ObtenerMenuDisponibleAsync();
            listaMenu.ItemsSource = _platillosTodos;
        }
        catch (Exception ex)
        {
            await DisplayAlert("Error", "No se pudo cargar el menú: " + ex.Message, "OK");
        }
    }

    private async Task AnimarFAB()
    {
        if (fabCarrito != null)
        {
            while (true)
            {
                await fabCarrito.ScaleTo(1.1, 800, Easing.SinInOut);
                await fabCarrito.ScaleTo(1, 800, Easing.SinInOut);
            }
        }
    }

    // Efecto visual de rebote (Touch)
    private async Task AnimarBotonClic(object sender)
    {
        if (sender is Button boton)
        {
            await boton.ScaleTo(0.90, 50);
            await boton.ScaleTo(1, 50);
        }
    }

    // Cuando tocan la tarjeta completa
    private async void OnPlatilloTapped(object sender, TappedEventArgs e)
    {
        var platilloSeleccionado = e.Parameter as Platillo;
        if (platilloSeleccionado != null)
        {
            await Navigation.PushAsync(new PersonalizarPlatillo(platilloSeleccionado));
        }
    }

    // Cuando tocan específicamente el botón naranja "+"
    private async void OnAgregarClicked(object sender, EventArgs e)
    {
        if (sender is Button btn)
        {
            await AnimarBotonClic(btn);
            var platilloSeleccionado = btn.CommandParameter as Platillo;
            if (platilloSeleccionado != null)
            {
                await Navigation.PushAsync(new PersonalizarPlatillo(platilloSeleccionado));
            }
        }
    }

    private async void OnVerCarritoClicked(object sender, EventArgs e)
    {
        // NO animamos el botón aquí porque ya está siendo animado en un bucle infinito (AnimarFAB).
        // Evita que la pantalla se congele por choques en el hilo principal de MAUI.
        await Navigation.PushAsync(new MiCarrito());
    }

    private void OnFiltroClicked(object sender, EventArgs e)
    {
        if (_platillosTodos == null) return;

        var boton = sender as Button;
        string categoria = boton.Text;

        // Reset Visual
        btnFiltroTodos.BackgroundColor = Color.FromArgb("#1A1A1A");
        btnFiltroTodos.TextColor = Color.FromArgb("#AAAAAA");

        btnFiltroComida.BackgroundColor = Color.FromArgb("#1A1A1A");
        btnFiltroComida.TextColor = Color.FromArgb("#AAAAAA");

        btnFiltroBebidas.BackgroundColor = Color.FromArgb("#1A1A1A");
        btnFiltroBebidas.TextColor = Color.FromArgb("#AAAAAA");

        btnFiltroPostres.BackgroundColor = Color.FromArgb("#1A1A1A");
        btnFiltroPostres.TextColor = Color.FromArgb("#AAAAAA");

        // Highlight
        boton.BackgroundColor = Color.FromArgb("#FF6B00");
        boton.TextColor = Colors.White;

        _categoriaActual = categoria;
        AplicarFiltros();
    }

    private void AplicarFiltros()
    {
        if (_platillosTodos == null) return;

        var filtrados = _platillosTodos.AsEnumerable();

        if (_categoriaActual != "Todos")
        {
            filtrados = filtrados.Where(x => x.Categoria != null && x.Categoria.Equals(_categoriaActual, StringComparison.OrdinalIgnoreCase));
        }

        if (!string.IsNullOrWhiteSpace(_textoBusqueda))
        {
            filtrados = filtrados.Where(x => x.Nombre != null && x.Nombre.Contains(_textoBusqueda, StringComparison.OrdinalIgnoreCase));
        }

        listaMenu.ItemsSource = filtrados.ToList();
    }

    private void OnSearchTextChanged(object sender, TextChangedEventArgs e)
    {
        _textoBusqueda = e.NewTextValue ?? string.Empty;
        AplicarFiltros();
    }
}