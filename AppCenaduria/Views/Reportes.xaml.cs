using AppCenaduria.Models;
using Supabase;

namespace AppCenaduria.Views;

public partial class Reportes : ContentPage
{
    private Supabase.Client _supabase;
    // NUEVO: Variable para guardar todos los pedidos y no saturar la base de datos
    private List<Pedido> _todosLosPedidos = new List<Pedido>();

    public Reportes()
    {
        InitializeComponent();
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        _supabase = Application.Current.Handler.MauiContext.Services.GetService<Supabase.Client>();

        // Configuramos las fechas por defecto (por ejemplo, los últimos 7 días)
        dpInicio.Date = DateTime.Now.AddDays(-7);
        dpFin.Date = DateTime.Now;

        await GenerarReportes();
    }

    private async Task GenerarReportes()
    {
        try
        {
            // Descargamos TODOS los pedidos completados una sola vez
            var respuesta = await _supabase.From<Pedido>()
                                           .Where(x => x.Estado == "Entregado")
                                           .Get();

            _todosLosPedidos = respuesta.Models;

            // Variables de tiempo para las tarjetas superiores
            var fechaHoy = DateTime.Now.Date;
            var mesActual = DateTime.Now.Month;
            var añoActual = DateTime.Now.Year;

            // 1. Cálculos fijos de HOY
            var pedidosHoy = _todosLosPedidos.Where(p => p.FechaPedido.Date == fechaHoy).ToList();
            lblVentasHoy.Text = $"${pedidosHoy.Sum(p => p.Total):F2}";
            lblPedidosHoy.Text = $"{pedidosHoy.Count} pedidos";

            // 2. Cálculos fijos del MES
            var pedidosMes = _todosLosPedidos.Where(p => p.FechaPedido.Month == mesActual && p.FechaPedido.Year == añoActual).ToList();
            lblVentasMes.Text = $"${pedidosMes.Sum(p => p.Total):F2}";
            lblPedidosMes.Text = $"{pedidosMes.Count} pedidos";

            // 3. Aplicamos el filtro visual por primera vez
            AplicarFiltro();
        }
        catch (Exception ex)
        {
            await DisplayAlert("Error", "No se pudieron calcular los reportes: " + ex.Message, "OK");
        }
    }

    // Evento del botón Buscar
    private void OnFiltrarClicked(object sender, EventArgs e)
    {
        AplicarFiltro();
    }

    private void AplicarFiltro()
    {
        if (_todosLosPedidos == null || !_todosLosPedidos.Any()) return;

        // SOLUCIÓN AL ERROR DE DATETIME?: Convertimos explícitamente el valor a una fecha segura
        DateTime fechaInicioObj = Convert.ToDateTime(dpInicio.Date);
        DateTime fechaFinObj = Convert.ToDateTime(dpFin.Date);

        // Tomamos la fecha de inicio desde las 00:00 hrs
        var fechaInicio = fechaInicioObj.Date;

        // Tomamos la fecha fin y le agregamos 23 horas con 59 min para abarcar todo el día
        var fechaFin = fechaFinObj.Date.AddDays(1).AddTicks(-1);

        // Filtramos la lista en memoria
        var pedidosFiltrados = _todosLosPedidos
            .Where(p => p.FechaPedido >= fechaInicio && p.FechaPedido <= fechaFin)
            .OrderByDescending(p => p.FechaPedido)
            .ToList();

        // Actualizamos la pantalla
        listaResumen.ItemsSource = pedidosFiltrados;
        lblTotalFiltrado.Text = $"${pedidosFiltrados.Sum(p => p.Total):F2}";
    }
}