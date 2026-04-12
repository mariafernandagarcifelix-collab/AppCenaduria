using AppCenaduria.Controllers;
using AppCenaduria.Models;
using System.Collections.ObjectModel;
using System.Linq;

namespace AppCenaduria.Views;

public partial class EditarPedidoEnCocina : ContentPage
{
    private Pedido _pedidoActual;
    private EditarPedidoController _controller;
    private ObservableCollection<ItemEdicion> _itemsPedido;
    private List<Platillo> _menuDisponible;

    public EditarPedidoEnCocina(Pedido pedido)
    {
        InitializeComponent();
        _pedidoActual = pedido;
        lblFolio.Text = $"Editando Orden #{pedido.Folio}";

        var supabase = Application.Current.Handler.MauiContext.Services.GetService<Supabase.Client>();
        _controller = new EditarPedidoController(supabase);
        _itemsPedido = new ObservableCollection<ItemEdicion>();
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await CargarDatosAsync();
    }

    private async Task CargarDatosAsync()
    {
        try
        {
            // Cargamos todos los platillos para el Picker de agregar
            _menuDisponible = await _controller.ObtenerPlatillosAsync();
            pckPlatillos.ItemsSource = _menuDisponible;

            // Traemos lo que la gente pidió originalmente
            var detalles = await _controller.ObtenerDetallesAsync(_pedidoActual.IdPedido);

            _itemsPedido.Clear();
            foreach (var det in detalles)
            {
                var platillo = _menuDisponible.FirstOrDefault(p => p.IdPlatillo == det.IdPlatillo);
                if (platillo != null)
                {
                    _itemsPedido.Add(new ItemEdicion
                    {
                        IdPlatillo = det.IdPlatillo,
                        NombrePlatillo = platillo.Nombre,
                        PrecioUnitario = det.PrecioUnitario,
                        Cantidad = det.Cantidad,
                        Comentarios = det.Comentarios
                    });
                }
            }

            RefrescarLista();
        }
        catch (Exception ex)
        {
            await DisplayAlert("Error", $"No se pudieron cargar los datos: {ex.Message}", "OK");
        }
    }

    private void ActualizarTotal()
    {
        decimal total = _itemsPedido.Sum(i => i.Subtotal);
        lblTotal.Text = $"Nuevo Total: ${total:F2}";
    }

    private void RefrescarLista()
    {
        // Un truco visual para que la lista reaccione a los cambios
        listaEdicion.ItemsSource = null;
        listaEdicion.ItemsSource = _itemsPedido;
        ActualizarTotal();
    }

    private void OnAgregarPlatilloClicked(object sender, EventArgs e)
    {
        var platilloSeleccionado = pckPlatillos.SelectedItem as Platillo;
        if (platilloSeleccionado == null)
        {
            DisplayAlert("Atención", "Selecciona un platillo de la lista primero.", "OK");
            return;
        }

        // Si ya lo habían pedido, le sumamos 1 a la cantidad. Si no, lo creamos de cero.
        var itemExistente = _itemsPedido.FirstOrDefault(i => i.IdPlatillo == platilloSeleccionado.IdPlatillo);
        if (itemExistente != null)
        {
            itemExistente.Cantidad++;
        }
        else
        {
            _itemsPedido.Add(new ItemEdicion
            {
                IdPlatillo = platilloSeleccionado.IdPlatillo,
                NombrePlatillo = platilloSeleccionado.Nombre,
                PrecioUnitario = platilloSeleccionado.Precio,
                Cantidad = 1,
                Comentarios = ""
            });
        }

        RefrescarLista();
        pckPlatillos.SelectedIndex = -1; // Limpiar el combo box
    }

    private void OnAumentarClicked(object sender, EventArgs e)
    {
        var boton = sender as Button;
        var item = boton.CommandParameter as ItemEdicion;
        item.Cantidad++;
        RefrescarLista();
    }

    private async void OnDisminuirClicked(object sender, EventArgs e)
    {
        var boton = sender as Button;
        var item = boton.CommandParameter as ItemEdicion;

        if (item.Cantidad > 1)
        {
            item.Cantidad--;
            RefrescarLista();
        }
        else
        {
            bool resp = await DisplayAlert("Eliminar", $"¿Quieres quitar {item.NombrePlatillo} de la orden?", "Sí", "No");
            if (resp)
            {
                _itemsPedido.Remove(item);
                RefrescarLista();
            }
        }
    }

    private async void OnEliminarClicked(object sender, EventArgs e)
    {
        var boton = sender as Button;
        var item = boton.CommandParameter as ItemEdicion;

        bool resp = await DisplayAlert("Eliminar", $"¿Estás seguro de quitar {item.NombrePlatillo} por completo?", "Sí, quitar", "Cancelar");
        if (resp)
        {
            _itemsPedido.Remove(item);
            RefrescarLista();
        }
    }

    private void OnComentarioChanged(object sender, TextChangedEventArgs e)
    {
        var entry = sender as Entry;
        var item = entry.BindingContext as ItemEdicion;
        if (item != null)
        {
            item.Comentarios = e.NewTextValue;
        }
    }

    private async void OnGuardarCambiosClicked(object sender, EventArgs e)
    {
        if (_itemsPedido.Count == 0)
        {
            await DisplayAlert("Error", "El pedido no puede quedar sin platillos. Si quieres cancelarlo, regresa y presiona 'Cancelado'.", "OK");
            return;
        }

        try
        {
            decimal nuevoTotal = _itemsPedido.Sum(i => i.Subtotal);
            var detallesNuevos = new List<DetallePedido>();

            foreach (var item in _itemsPedido)
            {
                detallesNuevos.Add(new DetallePedido
                {
                    IdPedido = _pedidoActual.IdPedido,
                    IdPlatillo = item.IdPlatillo,
                    Cantidad = item.Cantidad,
                    PrecioUnitario = item.PrecioUnitario,
                    Subtotal = item.Subtotal,
                    Comentarios = item.Comentarios ?? ""
                });
            }

            // Enviamos el trabajo pesado al Controlador
            await _controller.GuardarCambiosAsync(_pedidoActual.IdPedido, nuevoTotal, detallesNuevos);

            // Avisamos por notificación que su pedido fue modificado
            var supabase = Application.Current.Handler.MauiContext.Services.GetService<Supabase.Client>();
            await AppCenaduria.Services.NotificationService.NotificarCambioEstadoAClienteAsync(supabase, _pedidoActual.IdUsuario, "Actualizado en Cocina");

            await DisplayAlert("Éxito", "El pedido ha sido modificado.", "OK");
            await Navigation.PopAsync(); // Regresamos a la pantalla de la cocina
        }
        catch (Exception ex)
        {
            await DisplayAlert("Error", $"Ocurrió un error al guardar: {ex.Message}", "OK");
        }
    }
}

// Clase "Molde" temporal exclusiva para que la pantalla pueda calcular y mostrar datos
public class ItemEdicion
{
    public string IdPlatillo { get; set; }
    public string NombrePlatillo { get; set; }
    public decimal PrecioUnitario { get; set; }
    public int Cantidad { get; set; }
    public decimal Subtotal => Cantidad * PrecioUnitario;
    public string Comentarios { get; set; }
}