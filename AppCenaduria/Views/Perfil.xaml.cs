using AppCenaduria.Models;
using AppCenaduria.Controllers;

namespace AppCenaduria.Views;

public partial class Perfil : ContentPage
{
    private PerfilController _controller;
    private Usuario _usuarioActual;

    public Perfil()
    {
        InitializeComponent();
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        
        if (_controller == null)
        {
            var supabase = Application.Current.Handler.MauiContext.Services.GetService<Supabase.Client>();
            _controller = new PerfilController(supabase);
        }

        await CargarDatosUsuario();
    }

    private async Task CargarDatosUsuario()
    {
        try
        {
            _usuarioActual = await _controller.ObtenerPerfilAsync();

            if (_usuarioActual != null)
            {
                txtNombre.Text = _usuarioActual.NombreCompleto;
                txtTelefono.Text = _usuarioActual.Telefono;
                txtDomicilio.Text = _usuarioActual.Domicilio;
            }
        }
        catch (Exception ex)
        {
            await DisplayAlert("Error", "No pudimos cargar tu perfil: " + ex.Message, "OK");
        }
    }

    private async void OnGuardarPerfilClicked(object sender, EventArgs e)
    {
        if (string.IsNullOrWhiteSpace(txtNombre.Text))
        {
            await DisplayAlert("Atención", "El nombre es obligatorio.", "OK");
            return;
        }
        if (string.IsNullOrWhiteSpace(txtTelefono.Text) || txtTelefono.Text.Trim().Length != 10)
        {
            await DisplayAlert("Teléfono Inválido", "Por favor ingresa un número de teléfono válido de exactamente 10 dígitos.", "OK");
            return;
        }

        try
        {
            _usuarioActual.NombreCompleto = txtNombre.Text.Trim();
            _usuarioActual.Telefono = txtTelefono.Text?.Trim();
            _usuarioActual.Domicilio = txtDomicilio.Text?.Trim();

            await _controller.ActualizarPerfilAsync(_usuarioActual);

            await DisplayAlert("Éxito", "Perfil actualizado correctamente.", "OK");
        }
        catch (Exception ex)
        {
            await DisplayAlert("Error", "No se pudo actualizar: " + ex.Message, "OK");
        }
    }
}