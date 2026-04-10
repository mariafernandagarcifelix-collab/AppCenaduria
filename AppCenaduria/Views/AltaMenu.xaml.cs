using Supabase;
using AppCenaduria.Models;

namespace AppCenaduria.Views;

public partial class AltaMenu : ContentPage
{
    private Supabase.Client _supabase; 
    private FileResult _fotoSeleccionada; // Variable global para guardar la foto en memoria

    public AltaMenu()
	{
		InitializeComponent();
	}

    protected override void OnAppearing()
    {
        base.OnAppearing();
        if (_supabase == null)
        {
            _supabase = Application.Current.Handler.MauiContext.Services.GetService<Supabase.Client>();
        }
    }

    // --- FUNCIÓN PARA ABRIR LA GALERÍA DEL CELULAR ---
    private async void OnSeleccionarFotoClicked(object sender, EventArgs e)
    {
        try
        {
            _fotoSeleccionada = await MediaPicker.Default.PickPhotoAsync();

            if (_fotoSeleccionada != null)
            {
                // Mostramos la foto en la pantalla para que el administrador la vea
                var stream = await _fotoSeleccionada.OpenReadAsync();
                imgPreview.Source = ImageSource.FromStream(() => stream);
                imgPreview.IsVisible = true;

                // Limpiamos la URL manual porque usaremos el archivo
                txtFotoUrl.Text = string.Empty;
            }
        }
        catch (Exception ex)
        {
            await DisplayAlert("Error", "No se pudo abrir la galería: " + ex.Message, "OK");
        }
    }

    // --- FUNCIÓN PARA GUARDAR EN LA BASE DE DATOS ---
    private async void OnGuardarClicked(object sender, EventArgs e)
    {
        if (string.IsNullOrWhiteSpace(txtNombre.Text) || string.IsNullOrWhiteSpace(txtPrecio.Text))
        {
            await DisplayAlert("Atención", "El nombre y el precio son obligatorios.", "OK");
            return;
        }

        try
        {
            string urlFinal = txtFotoUrl.Text?.Trim() ?? "";

            // Si el usuario seleccionó una foto de la galería, la subimos a Supabase Storage primero
            if (_fotoSeleccionada != null)
            {
                using var stream = await _fotoSeleccionada.OpenReadAsync();
                using var memoryStream = new MemoryStream();
                await stream.CopyToAsync(memoryStream);
                var bytesObtenidos = memoryStream.ToArray();

                // Creamos un nombre único para que no choquen las fotos (ej. 1234-5678_foto.jpg)
                string nombreArchivoUnico = $"{Guid.NewGuid()}_{_fotoSeleccionada.FileName}";

                // Subimos los bytes al bucket "platillos"
                await _supabase.Storage.From("platillos").Upload(bytesObtenidos, nombreArchivoUnico);

                // Recuperamos la URL pública generada por Supabase
                urlFinal = _supabase.Storage.From("platillos").GetPublicUrl(nombreArchivoUnico);
            }

            var nuevoPlatillo = new Platillo
            {
                Nombre = txtNombre.Text.Trim(),
                Descripcion = txtDescripcion.Text?.Trim() ?? "",
                Precio = Convert.ToDecimal(txtPrecio.Text),
                FotografiaUrl = urlFinal, // Guardamos la URL pública o la que escribieron a mano
                Disponible = swDisponible.IsToggled
            };

            await _supabase.From<Platillo>().Insert(nuevoPlatillo);

            await DisplayAlert("Éxito", "¡El platillo se guardó correctamente!", "Genial");

            // Limpiamos la pantalla
            txtNombre.Text = string.Empty;
            txtDescripcion.Text = string.Empty;
            txtPrecio.Text = string.Empty;
            txtFotoUrl.Text = string.Empty;
            imgPreview.IsVisible = false;
            _fotoSeleccionada = null;
            swDisponible.IsToggled = true;
        }
        catch (Exception ex)
        {
            await DisplayAlert("Error", "No se pudo guardar: " + ex.Message, "OK");
        }
    }
}