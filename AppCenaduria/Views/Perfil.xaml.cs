using Supabase;
using AppCenaduria.Models;

namespace AppCenaduria.Views;

public partial class Perfil : ContentPage
{
    private Supabase.Client _supabase;
    private Usuario _usuarioActual;

    public Perfil()
	{
		InitializeComponent();
	}

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        _supabase = Application.Current.Handler.MauiContext.Services.GetService<Supabase.Client>();
        await CargarDatosUsuario();
    }

    private async Task CargarDatosUsuario()
    {
        try
        {
            string correo = _supabase.Auth.CurrentUser.Email;
            var res = await _supabase.From<Usuario>().Where(x => x.CorreoGoogle == correo).Get();
            _usuarioActual = res.Models.FirstOrDefault();

            if (_usuarioActual != null)
            {
                // Si ya tiene datos (de Google o guardados antes), los ponemos en las cajas
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

        try
        {
            _usuarioActual.NombreCompleto = txtNombre.Text.Trim();
            _usuarioActual.Telefono = txtTelefono.Text?.Trim();
            _usuarioActual.Domicilio = txtDomicilio.Text?.Trim();

            await _supabase.From<Usuario>().Update(_usuarioActual);

            await DisplayAlert("Éxito", "Perfil actualizado correctamente.", "OK");
        }
        catch (Exception ex)
        {
            await DisplayAlert("Error", "No se pudo actualizar: " + ex.Message, "OK");
        }
    }
}