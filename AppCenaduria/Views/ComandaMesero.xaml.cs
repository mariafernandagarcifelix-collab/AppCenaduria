using AppCenaduria.Controllers;
using AppCenaduria.Models;
using System;

namespace AppCenaduria.Views;

public partial class ComandaMesero : ContentPage
{
    private ComandaMeseroController _controller;

    public ComandaMesero()
    {
        InitializeComponent();
        var supabase = Application.Current.Handler.MauiContext.Services.GetService<Supabase.Client>();
        _controller = new ComandaMeseroController(supabase);
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        CargarComanda();
    }

    private void CargarComanda()
    {
        decimal total = _controller.CalcularTotal(Carrito.CarritoGlobal.Articulos);
        lblTotal.Text = $"${total:F2}";
        
        listaComanda.ItemsSource = null;
        listaComanda.ItemsSource = Carrito.CarritoGlobal.Articulos;
    }

    // --- LÓGICA DE LA LISTA INTERACTIVA ---
    private void OnAumentarCantidad(object sender, EventArgs e)
    {
        var button = sender as Button;
        if (button?.CommandParameter is Carrito.ItemCarrito item)
        {
            item.Cantidad++;
            CargarComanda();
        }
    }

    private void OnDisminuirCantidad(object sender, EventArgs e)
    {
        var button = sender as Button;
        if (button?.CommandParameter is Carrito.ItemCarrito item && item.Cantidad > 1)
        {
            item.Cantidad--;
            CargarComanda();
        }
    }

    private void OnEliminarPlatillo(object sender, EventArgs e)
    {
        var button = sender as Button;
        if (button?.CommandParameter is Carrito.ItemCarrito item)
        {
            Carrito.CarritoGlobal.Articulos.Remove(item);
            CargarComanda();
        }
    }

    // --- LÓGICA DE VISTA DE CAMPOS DINÁMICOS ---
    private void OnPickerEntregaSelectedIndexChanged(object sender, EventArgs e)
    {
        string tipoEntrega = pickerEntrega.SelectedItem?.ToString();
        stackDatosMesero.IsVisible = true;

        // Escondemos todos por defecto
        borderMeseroNombre.IsVisible = false;
        borderMeseroTelefono.IsVisible = false;
        borderMeseroDomicilio.IsVisible = false;
        borderMeseroMesa.IsVisible = false;

        // Mostramos según la opción
        if (tipoEntrega == "Domicilio")
        {
            borderMeseroNombre.IsVisible = true;
            borderMeseroTelefono.IsVisible = true;
            borderMeseroDomicilio.IsVisible = true;
        }
        else if (tipoEntrega == "Para recoger")
        {
            borderMeseroNombre.IsVisible = true;
            borderMeseroTelefono.IsVisible = true;
        }
        else if (tipoEntrega == "Comer ahí (Mesa)")
        {
            borderMeseroMesa.IsVisible = true;
        }
    }

    // --- CONFIRMAR ORDEN ---
    private async void OnConfirmarPedidoClicked(object sender, EventArgs e)
    {
        if (Carrito.CarritoGlobal.Articulos == null || Carrito.CarritoGlobal.Articulos.Count == 0)
        {
            await DisplayAlert("Comanda vacía", "Agrega al menos un platillo para continuar.", "OK");
            return;
        }

        if (pickerEntrega.SelectedItem == null || pickerPago.SelectedItem == null)
        {
            await DisplayAlert("Faltan Opciones", "Por favor selecciona el tipo de entrega y el método de pago.", "OK");
            return;
        }

        string tipoEntrega = pickerEntrega.SelectedItem.ToString();
        string tipoPago = pickerPago.SelectedItem.ToString();
        string infoClienteFinal = "";

        // Validaciones de los campos que aparecieron
        // Validaciones de los campos que aparecieron
        if (tipoEntrega == "Domicilio")
        {
            if (string.IsNullOrWhiteSpace(entryMeseroNombre.Text) || string.IsNullOrWhiteSpace(entryMeseroTelefono.Text) || string.IsNullOrWhiteSpace(entryMeseroDomicilio.Text))
            {
                await DisplayAlert("Faltan Datos", "Si es a Domicilio debes ingresar el nombre, teléfono y domicilio del cliente.", "OK");
                return;
            }

            // 🔥 NUEVO CANDADO PARA TELÉFONO 🔥
            if (entryMeseroTelefono.Text.Trim().Length != 10)
            {
                await DisplayAlert("Teléfono Inválido", "El número de teléfono debe tener exactamente 10 dígitos.", "OK");
                return;
            }

            infoClienteFinal = $"{entryMeseroNombre.Text.Trim()} | Tel: {entryMeseroTelefono.Text.Trim()} | Dom: {entryMeseroDomicilio.Text.Trim()}";
        }
        else if (tipoEntrega == "Para recoger")
        {
            if (string.IsNullOrWhiteSpace(entryMeseroNombre.Text) || string.IsNullOrWhiteSpace(entryMeseroTelefono.Text))
            {
                await DisplayAlert("Faltan Datos", "Si es para Recoger debes ingresar el nombre y teléfono del cliente.", "OK");
                return;
            }

            // 🔥 NUEVO CANDADO PARA TELÉFONO 🔥
            if (entryMeseroTelefono.Text.Trim().Length != 10)
            {
                await DisplayAlert("Teléfono Inválido", "El número de teléfono debe tener exactamente 10 dígitos.", "OK");
                return;
            }

            infoClienteFinal = $"{entryMeseroNombre.Text.Trim()} | Tel: {entryMeseroTelefono.Text.Trim()}";
        }
        else if (tipoEntrega == "Comer ahí (Mesa)")
        {
            if (string.IsNullOrWhiteSpace(entryMeseroMesa.Text))
            {
                await DisplayAlert("Faltan Datos", "Si es para Comer ahí debes ingresar el número de mesa.", "OK");
                return;
            }
            infoClienteFinal = entryMeseroMesa.Text.Trim();
        }

        try
        {
            decimal totalReal = _controller.CalcularTotal(Carrito.CarritoGlobal.Articulos);

            // Mandamos todo al controlador
            await _controller.CrearPedidoMeseroAsync(App.UsuarioActual.IdUsuario, infoClienteFinal, tipoEntrega, tipoPago, totalReal, Carrito.CarritoGlobal.Articulos);

            // Limpiamos todo el formulario
            Carrito.CarritoGlobal.Articulos.Clear();
            entryMeseroNombre.Text = "";
            entryMeseroTelefono.Text = "";
            entryMeseroDomicilio.Text = "";
            entryMeseroMesa.Text = "";
            pickerEntrega.SelectedIndex = -1;
            pickerPago.SelectedIndex = -1;
            stackDatosMesero.IsVisible = false;

            CargarComanda();

            // Notificamos a la cocina/administradores
            var supabase = Application.Current.Handler.MauiContext.Services.GetService<Supabase.Client>();
            await AppCenaduria.Services.NotificationService.NotificarNuevoPedidoAStaffAsync(supabase);

            await DisplayAlert("¡Éxito!", "La comanda ha sido enviada a cocina.", "OK");
        }
        catch (Exception ex)
        {
            await DisplayAlert("Error", "Hubo un problema: " + ex.Message, "OK");
        }
    }
}