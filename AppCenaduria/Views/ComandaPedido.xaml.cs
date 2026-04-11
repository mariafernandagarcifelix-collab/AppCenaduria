using System;
using System.Collections.Generic;
using Microsoft.Maui.Controls;
using AppCenaduria.Models;
using AppCenaduria.Controllers;
using System.Threading.Tasks;

namespace AppCenaduria.Views
{
    public partial class ComandaPedido : ContentPage
    {
        private ComandaPedidoController _controller;
        private Pedido _pedidoGlobal;

        public ComandaPedido(Pedido pedido)
        {
            InitializeComponent();
            _pedidoGlobal = pedido;
            
            // Set basic info immediately
            lblFolio.Text = $"ORDEN #{pedido.Folio}";
            lblFecha.Text = pedido.FechaPedido.ToString("dd/MM/yyyy hh:mm tt");
            lblTipoEntrega.Text = pedido.TipoEntrega;
            lblTipoPago.Text = pedido.TipoPago;
            lblMontoTotal.Text = $"${pedido.Total:F2}";
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();

            if (_controller == null)
            {
                var supabase = Application.Current.Handler.MauiContext.Services.GetService<Supabase.Client>();
                _controller = new ComandaPedidoController(supabase);
            }

            await CargarComanda();
        }

        private async Task CargarComanda()
        {
            try
            {
                // Cargar ítems
                var items = await _controller.ObtenerDetallesDelPedidoAsync(_pedidoGlobal.IdPedido);
                listaPlatillos.ItemsSource = items;

                // Cargar info del creador (para saber rol)
                var creador = await _controller.ObtenerUsuarioDelPedidoAsync(_pedidoGlobal.IdUsuario);

                if (creador != null)
                {
                    if (creador.Rol == "Administrador")
                    {
                        // Si lo levantó un admin, el valor complejo que formamos en MiCarrito se guardó en _pedidoGlobal.NombreCliente
                        lblDatosCliente.Text = $"PEDIDO POR MESERO: {creador.NombreCompleto}\n" +
                                               $"Para: {_pedidoGlobal.NombreCliente}";
                    }
                    else
                    {
                        // Si lo levantó el cliente desde su casa, usamos los datos de su perfil
                        string datosMuestra = $"Cliente: {creador.NombreCompleto}\n" +
                                              $"Tel: {creador.Telefono}";

                        if (_pedidoGlobal.TipoEntrega == "Domicilio")
                        {
                            datosMuestra += $"\nDom: {creador.Domicilio}";
                        }

                        lblDatosCliente.Text = datosMuestra;
                    }
                }
                else
                {
                    lblDatosCliente.Text = $"Cliente: {_pedidoGlobal.NombreCliente}\n(No se pudo cargar el perfil más a detalle)";
                }
            }
            catch (Exception ex)
            {
                await DisplayAlert("Error", $"No se pudo cargar la comanda al 100%: {ex.Message}", "OK");
            }
        }

        private async void OnCerrarClicked(object sender, EventArgs e)
        {
            await Navigation.PopAsync();
        }
    }
}
