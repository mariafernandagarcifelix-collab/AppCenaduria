using AppCenaduria.Models;
using AppCenaduria.Controllers;

namespace AppCenaduria.Views;

public partial class Reportes : ContentPage
{
    private ReportesController _controller;
    private List<Pedido> _todosLosPedidos = new List<Pedido>();

    public Reportes()
    {
        InitializeComponent();
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();

        if (_controller == null)
        {
            var supabase = Application.Current.Handler.MauiContext.Services.GetService<Supabase.Client>();
            _controller = new ReportesController(supabase);
        }

        dpInicio.Date = DateTime.Now.AddDays(-7);
        dpFin.Date = DateTime.Now;

        await GenerarReportes();
    }

    private async Task GenerarReportes()
    {
        try
        {
            _todosLosPedidos = await _controller.ObtenerReportesEntregadosAsync();

            var fechaHoy = DateTime.Now.Date;
            var mesActual = DateTime.Now.Month;
            var añoActual = DateTime.Now.Year;

            var pedidosHoy = _todosLosPedidos.Where(p => p.FechaPedido.Date == fechaHoy).ToList();
            lblVentasHoy.Text = $"${pedidosHoy.Sum(p => p.Total):F2}";
            lblPedidosHoy.Text = $"{pedidosHoy.Count} pedidos";

            var pedidosMes = _todosLosPedidos.Where(p => p.FechaPedido.Month == mesActual && p.FechaPedido.Year == añoActual).ToList();
            lblVentasMes.Text = $"${pedidosMes.Sum(p => p.Total):F2}";
            lblPedidosMes.Text = $"{pedidosMes.Count} pedidos";

            AplicarFiltro();
        }
        catch (Exception ex)
        {
            await DisplayAlert("Error", "No se pudieron calcular los reportes: " + ex.Message, "OK");
        }
    }

    private void OnFiltrarClicked(object sender, EventArgs e)
    {
        AplicarFiltro();
    }

    private void AplicarFiltro()
    {
        if (_todosLosPedidos == null || !_todosLosPedidos.Any()) return;

        DateTime fechaInicioObj = Convert.ToDateTime(dpInicio.Date);
        DateTime fechaFinObj = Convert.ToDateTime(dpFin.Date);

        var fechaInicio = fechaInicioObj.Date;
        var fechaFin = fechaFinObj.Date.AddDays(1).AddTicks(-1);

        var pedidosFiltrados = _todosLosPedidos
            .Where(p => p.FechaPedido >= fechaInicio && p.FechaPedido <= fechaFin)
            .OrderByDescending(p => p.FechaPedido)
            .ToList();

        listaResumen.ItemsSource = pedidosFiltrados;
        lblTotalFiltrado.Text = $"${pedidosFiltrados.Sum(p => p.Total):F2}";
    }
}