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

    private async void OnPlatilloTapped(object sender, TappedEventArgs e)
    {
        var platilloSeleccionado = e.Parameter as Platillo;
        if (platilloSeleccionado != null)
        {
            await Navigation.PushAsync(new PersonalizarPlatillo(platilloSeleccionado));
        }
    }

    private async void OnVerCarritoClicked(object sender, EventArgs e)
    {
        await Navigation.PushAsync(new MiCarrito());
    }

    private void OnFiltroClicked(object sender, EventArgs e)
    {
        if (_platillosTodos == null) return;

        var boton = sender as Button;
        string categoria = boton.Text;

        // Reset
        btnFiltroTodos.BackgroundColor = Color.FromArgb("#1A1A1A");
        btnFiltroTodos.TextColor = Color.FromArgb("#AAAAAA");
        
        btnFiltroComida.BackgroundColor = Color.FromArgb("#1A1A1A");
        btnFiltroComida.TextColor = Color.FromArgb("#AAAAAA");

        btnFiltroBebidas.BackgroundColor = Color.FromArgb("#1A1A1A");
        btnFiltroBebidas.TextColor = Color.FromArgb("#AAAAAA");

        btnFiltroPostres.BackgroundColor = Color.FromArgb("#1A1A1A");
        btnFiltroPostres.TextColor = Color.FromArgb("#AAAAAA");

        // Highlight
        boton.BackgroundColor = Color.FromArgb("#fc4b08");
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