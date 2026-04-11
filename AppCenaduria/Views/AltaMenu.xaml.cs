using Supabase;
using AppCenaduria.Models;
using AppCenaduria.Controllers;
using System.Globalization;

namespace AppCenaduria.Views;

public partial class AltaMenu : ContentPage
{
    private AltaMenuController _controller;
    private FileResult _fotoSeleccionada;
    private Platillo _platilloEnEdicion;

    public AltaMenu()
    {
        InitializeComponent();
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        if (_controller == null)
        {
            var supabase = Application.Current.Handler.MauiContext.Services.GetService<Supabase.Client>();
            _controller = new AltaMenuController(supabase);
        }

        await CargarMenu();
    }

    private async Task CargarMenu()
    {
        try
        {
            var platillos = await _controller.ObtenerPlatillosAsync();
            listaMenu.ItemsSource = platillos;
        }
        catch (Exception ex)
        {
            await DisplayAlert("Error", "No se pudo cargar el menú: " + ex.Message, "OK");
        }
    }

    private async void OnSeleccionarFotoClicked(object sender, EventArgs e)
    {
        try
        {
            _fotoSeleccionada = await MediaPicker.Default.PickPhotoAsync();
            if (_fotoSeleccionada != null)
            {
                var stream = await _fotoSeleccionada.OpenReadAsync();
                imgPreview.Source = ImageSource.FromStream(() => stream);
                borderPreview.IsVisible = true;
                txtFotoUrl.Text = string.Empty;
            }
        }
        catch (Exception ex)
        {
            await DisplayAlert("Error", "No se pudo abrir la galería: " + ex.Message, "OK");
        }
    }

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

            if (_fotoSeleccionada != null)
            {
                urlFinal = await _controller.SubirFotoAsync(_fotoSeleccionada);
            }

            if (_platilloEnEdicion == null)
            {
                // MODO: CREAR
                var nuevoPlatillo = new Platillo
                {
                    Nombre = txtNombre.Text.Trim(),
                    Descripcion = txtDescripcion.Text?.Trim() ?? "",
                    Categoria = pickerCategoria.SelectedItem?.ToString() ?? "Todos",
                    Precio = Convert.ToDecimal(txtPrecio.Text),
                    FotografiaUrl = urlFinal,
                    Disponible = swDisponible.IsToggled
                };

                await _controller.GuardarPlatilloAsync(nuevoPlatillo);
                await DisplayAlert("Éxito", "El platillo se guardó correctamente.", "OK");
            }
            else
            {
                // MODO: EDITAR
                _platilloEnEdicion.Nombre = txtNombre.Text.Trim();
                _platilloEnEdicion.Descripcion = txtDescripcion.Text?.Trim() ?? "";
                _platilloEnEdicion.Categoria = pickerCategoria.SelectedItem?.ToString() ?? "Todos";
                _platilloEnEdicion.Precio = Convert.ToDecimal(txtPrecio.Text);
                _platilloEnEdicion.Disponible = swDisponible.IsToggled;
                
                if (!string.IsNullOrEmpty(urlFinal))
                {
                    _platilloEnEdicion.FotografiaUrl = urlFinal;
                }

                await _controller.ActualizarPlatilloAsync(_platilloEnEdicion);
                await DisplayAlert("Éxito", "El platillo se actualizó correctamente.", "OK");
            }

            RestaurarFormulario();
            await CargarMenu();
        }
        catch (Exception ex)
        {
            await DisplayAlert("Error", "No se pudo guardar: " + ex.Message, "OK");
        }
    }

    private void OnEditarClicked(object sender, TappedEventArgs e)
    {
        var platillo = e.Parameter as Platillo;
        if (platillo == null) return;

        _platilloEnEdicion = platillo;

        lblTituloFormulario.Text = "Editar Platillo";
        btnGuardar.Text = "Actualizar Platillo";
        btnCancelar.IsVisible = true;
        Grid.SetColumnSpan(btnGuardar, 1);

        txtNombre.Text = platillo.Nombre;
        txtDescripcion.Text = platillo.Descripcion;
        pickerCategoria.SelectedItem = platillo.Categoria;
        txtPrecio.Text = platillo.Precio.ToString();
        swDisponible.IsToggled = platillo.Disponible;
        txtFotoUrl.Text = platillo.FotografiaUrl;

        if (!string.IsNullOrEmpty(platillo.FotografiaUrl))
        {
            imgPreview.Source = platillo.FotografiaUrl;
            borderPreview.IsVisible = true;
        }
        else
        {
            borderPreview.IsVisible = false;
        }
        
        _fotoSeleccionada = null;

        // Desplaza la vista arriba
        _ = mainScroll.ScrollToAsync(0, 0, true);
    }

    private async void OnEliminarClicked(object sender, TappedEventArgs e)
    {
        var platillo = e.Parameter as Platillo;
        if (platillo == null) return;

        bool confirm = await DisplayAlert("Eliminar", $"¿Estás seguro de eliminar '{platillo.Nombre}'?", "Sí, eliminar", "Cancelar");
        if (!confirm) return;

        try
        {
            await _controller.EliminarPlatilloAsync(platillo);
            await DisplayAlert("Eliminado", "Platillo eliminado correctamente.", "OK");
            RestaurarFormulario();
            await CargarMenu();
        }
        catch (Exception ex)
        {
            await DisplayAlert("Error", "No se pudo eliminar: " + ex.Message, "OK");
        }
    }

    private void OnCancelarClicked(object sender, EventArgs e)
    {
        RestaurarFormulario();
    }

    private void RestaurarFormulario()
    {
        _platilloEnEdicion = null;
        lblTituloFormulario.Text = "Nuevo Platillo";
        btnGuardar.Text = "Guardar Platillo";
        btnCancelar.IsVisible = false;
        Grid.SetColumnSpan(btnGuardar, 2);

        txtNombre.Text = string.Empty;
        txtDescripcion.Text = string.Empty;
        pickerCategoria.SelectedItem = null;
        txtPrecio.Text = string.Empty;
        txtFotoUrl.Text = string.Empty;
        borderPreview.IsVisible = false;
        _fotoSeleccionada = null;
        swDisponible.IsToggled = true;
    }
}

// Converters
public class BoolToDisponibilidadConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is bool isAvailable && isAvailable)
            return "● Disponible";
        return "● Agotado";
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => throw new NotImplementedException();
}

public class BoolToColorConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is bool isAvailable && isAvailable)
            return Color.FromArgb("#4CAF50");
        return Color.FromArgb("#F44336");
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => throw new NotImplementedException();
}