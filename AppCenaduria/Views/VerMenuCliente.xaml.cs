using Supabase;
using AppCenaduria.Models;
using AppCenaduria.Controllers;

namespace AppCenaduria.Views;

public partial class VerMenuCliente : ContentPage
{
    private VerMenuClienteController _controller;

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
            var platillos = await _controller.ObtenerMenuDisponibleAsync();
            listaPlatillos.ItemsSource = platillos;
        }
        catch (Exception ex)
        {
            await DisplayAlert("Error", "No se pudo cargar el menú: " + ex.Message, "OK");
        }
    }

    private async void OnAgregarClicked(object sender, EventArgs e)
    {
        var boton = sender as Button;
        var platilloSeleccionado = boton.CommandParameter as Platillo;

        await Navigation.PushAsync(new PersonalizarPlatillo(platilloSeleccionado));
    }
}