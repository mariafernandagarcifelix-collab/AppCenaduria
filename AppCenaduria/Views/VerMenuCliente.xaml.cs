using Supabase;
using AppCenaduria.Models;

namespace AppCenaduria.Views;

public partial class VerMenuCliente : ContentPage
{
    private Supabase.Client _supabase;

    public VerMenuCliente()
	{
		InitializeComponent();
	}

    protected override async void OnAppearing()
    {
        base.OnAppearing();

        if (_supabase == null)
        {
            _supabase = Application.Current.Handler.MauiContext.Services.GetService<Supabase.Client>();
        }

        await CargarMenuAsync();
    }

    private async Task CargarMenuAsync()
    {
        try
        {
            // Descargamos de PostgreSQL solo los platillos disponibles
            var respuesta = await _supabase.From<Platillo>()
                                           .Where(x => x.Disponible == true)
                                           .Get();

            // Llenamos la interfaz gráfica con los datos
            listaPlatillos.ItemsSource = respuesta.Models;
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

        // En lugar de mostrar un mensaje, abrimos la pantalla de personalización
        // pasándole el platillo que el cliente tocó
        await Navigation.PushAsync(new PersonalizarPlatillo(platilloSeleccionado));
    }
}