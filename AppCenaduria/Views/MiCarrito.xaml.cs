using AppCenaduria.Models;
using AppCenaduria.Controllers;

namespace AppCenaduria.Views;

public partial class MiCarrito : ContentPage
{
    private MiCarritoController _controller;

    public MiCarrito()
    {
        InitializeComponent();
    }

    private Usuario _usuarioActual;

    protected override async void OnAppearing()
    {
        base.OnAppearing();

        if (_controller == null)
        {
            var supabase = Application.Current.Handler.MauiContext.Services.GetService<Supabase.Client>();
            _controller = new MiCarritoController(supabase);
        }

        // Cargar el usuario real una sola vez
        _usuarioActual = await _controller.ObtenerUsuarioActualAsync();

        ActualizarTotalInterfaz();
    }

    private void ActualizarTotalInterfaz()
    {
        decimal totalPedido = _controller.RecalcularTotal(Carrito.CarritoGlobal.Articulos);
        lblTotal.Text = $"${totalPedido:F2}";
        
        // Actualizar el List Source para forzar re-render de subtotales
        listaCarrito.ItemsSource = null;
        listaCarrito.ItemsSource = Carrito.CarritoGlobal.Articulos;
        
        // Actualizar el numerito del menú (opcional)
        if (Application.Current.MainPage is AppCenaduria.Views.Menu menuApp)
        {
            menuApp.ActualizarBadgeCarrito();
        }
    }

    private void OnAumentarCantidad(object sender, EventArgs e)
    {
        var button = sender as Button;
        var item = button?.CommandParameter as Carrito.ItemCarrito;
        if (item != null)
        {
            item.Cantidad++;
            ActualizarTotalInterfaz();
        }
    }

    private void OnDisminuirCantidad(object sender, EventArgs e)
    {
        var button = sender as Button;
        var item = button?.CommandParameter as Carrito.ItemCarrito;
        if (item != null)
        {
            if (item.Cantidad > 1) 
            {
                item.Cantidad--;
                ActualizarTotalInterfaz();
            }
        }
    }

    private void OnEliminarPlatillo(object sender, EventArgs e)
    {
        var button = sender as Button;
        var item = button?.CommandParameter as Carrito.ItemCarrito;
        if (item != null)
        {
            Carrito.CarritoGlobal.Articulos.Remove(item);
            ActualizarTotalInterfaz();
        }
    }

    private void OnPickerEntregaSelectedIndexChanged(object sender, EventArgs e)
    {
        if (_usuarioActual == null || _usuarioActual.Rol != "Administrador")
            return; // No mostramos nada si es cliente

        string tipoEntrega = pickerEntrega.SelectedItem?.ToString();
        stackDatosAdmin.IsVisible = true;

        borderAdminNombre.IsVisible = false;
        borderAdminTelefono.IsVisible = false;
        borderAdminDomicilio.IsVisible = false;
        borderAdminMesa.IsVisible = false;

        if (tipoEntrega == "Domicilio")
        {
            borderAdminNombre.IsVisible = true;
            borderAdminTelefono.IsVisible = true;
            borderAdminDomicilio.IsVisible = true;
        }
        else if (tipoEntrega == "Recoger")
        {
            borderAdminNombre.IsVisible = true;
            borderAdminTelefono.IsVisible = true;
        }
        else if (tipoEntrega == "Comer ahí")
        {
            borderAdminMesa.IsVisible = true;
        }
    }

    private async void OnConfirmarPedidoClicked(object sender, EventArgs e)
    {
        if (Carrito.CarritoGlobal.Articulos == null || Carrito.CarritoGlobal.Articulos.Count == 0)
        {
            await DisplayAlert("Carrito de Compras", "Carrito vacío. Agrega al menos un platillo para continuar", "OK");
            return;
        }

        if (pickerEntrega.SelectedItem == null || pickerPago.SelectedItem == null)
        {
            await DisplayAlert("Faltan Opciones", "Por favor selecciona el tipo de entrega y el método de pago.", "OK");
            return;
        }

        string tipoEntrega = pickerEntrega.SelectedItem.ToString();
        string tipoPago = pickerPago.SelectedItem.ToString();

        try
        {
            if (_usuarioActual == null)
            {
                await DisplayAlert("Sesión Caducada", "No pudimos recuperar tu perfil. Por favor, cierra sesión y vuelve a ingresar con Google.", "Entendido");
                return;
            }

            bool esAdmin = _usuarioActual.Rol == "Administrador";
            string datosClienteFinal = _usuarioActual.NombreCompleto; // Por defecto o vacío

            if (!esAdmin)
            {
                bool faltanDatosBasicos = string.IsNullOrWhiteSpace(_usuarioActual.NombreCompleto) ||
                                          string.IsNullOrWhiteSpace(_usuarioActual.Telefono);
                                          
                bool faltaDomicilio = tipoEntrega == "Domicilio" && string.IsNullOrWhiteSpace(_usuarioActual.Domicilio);

                if (faltanDatosBasicos || faltaDomicilio)
                {
                    string mensajeAlerta = tipoEntrega == "Domicilio" 
                        ? "Para realizar un pedido a domicilio, necesitamos tu nombre, teléfono y dirección de entrega."
                        : "Para procesar tu pedido, necesitamos al menos tu nombre y teléfono.";

                    await DisplayAlert("Datos Incompletos", mensajeAlerta, "Ir a Mi Perfil");

                    if (Application.Current.MainPage is FlyoutPage menu)
                    {
                        menu.Detail = new NavigationPage(new Perfil());
                    }
                    return;
                }
            }
            else
            {
                // Es Admin: Validar los campos extras
                if (tipoEntrega == "Domicilio")
                {
                    if (string.IsNullOrWhiteSpace(entryAdminNombre.Text) || string.IsNullOrWhiteSpace(entryAdminTelefono.Text) || string.IsNullOrWhiteSpace(entryAdminDomicilio.Text))
                    {
                        await DisplayAlert("Faltan Datos", "Si es a Domicilio debes ingresar el nombre, teléfono y domicilio del cliente.", "OK");
                        return;
                    }
                    datosClienteFinal = $"{entryAdminNombre.Text} | Tel: {entryAdminTelefono.Text} | Dom: {entryAdminDomicilio.Text}";
                }
                else if (tipoEntrega == "Recoger")
                {
                    if (string.IsNullOrWhiteSpace(entryAdminNombre.Text) || string.IsNullOrWhiteSpace(entryAdminTelefono.Text))
                    {
                        await DisplayAlert("Faltan Datos", "Si es para Recoger debes ingresar el nombre y teléfono del cliente.", "OK");
                        return;
                    }
                    datosClienteFinal = $"{entryAdminNombre.Text} | Tel: {entryAdminTelefono.Text}";
                }
                else if (tipoEntrega == "Comer ahí")
                {
                    if (string.IsNullOrWhiteSpace(entryAdminMesa.Text))
                    {
                        await DisplayAlert("Faltan Datos", "Si es para Comer ahí debes ingresar el número de mesa.", "OK");
                        return;
                    }
                    datosClienteFinal = entryAdminMesa.Text;
                }
            }

            decimal totalReal = Carrito.CarritoGlobal.Articulos.Sum(x => x.Subtotal);

            // IMPORTANTE: Modificamos _usuarioActual.NombreCompleto para que se guarde el dato final correcto.
            var clonUsuarioParams = new Usuario
            {
                IdUsuario = _usuarioActual.IdUsuario,
                NombreCompleto = datosClienteFinal,
                Telefono = _usuarioActual.Telefono,
                Domicilio = _usuarioActual.Domicilio,
                CorreoGoogle = _usuarioActual.CorreoGoogle,
                Rol = _usuarioActual.Rol,
                TokenNotificacion = _usuarioActual.TokenNotificacion
            };

            var pedidoGuardado = await _controller.CrearPedidoAsync(clonUsuarioParams, totalReal, Carrito.CarritoGlobal.Articulos, tipoEntrega, tipoPago);

            await DisplayAlert("¡Éxito!", "Tu pedido ha sido enviado. ¡Gracias por tu compra!", "OK");

            Carrito.CarritoGlobal.Articulos.Clear();
            listaCarrito.ItemsSource = null;
            lblTotal.Text = "$0.00";
            
            // Limpiamos los campos
            entryAdminNombre.Text = "";
            entryAdminTelefono.Text = "";
            entryAdminDomicilio.Text = "";
            entryAdminMesa.Text = "";

            await _controller.NotificarAdminAsync(pedidoGuardado);

            if (Application.Current.MainPage is AppCenaduria.Views.Menu menuApp)
            {
                menuApp.ActualizarBadgeCarrito();
                menuApp.Detail = new NavigationPage(new VerMenuCliente());
            }
            else if (Application.Current.MainPage is FlyoutPage flyout)
            {
                flyout.Detail = new NavigationPage(new VerMenuCliente());
            }
        }
        catch (Exception ex)
        {
            await DisplayAlert("Error", "Hubo un problema: " + ex.Message, "OK");
        }
    }
}