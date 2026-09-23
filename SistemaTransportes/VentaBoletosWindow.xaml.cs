using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace SistemaTransportes
{
    public partial class VentaBoletosWindow : Window
    {
        private class AsientoModel
        {
            public int NroAsiento { get; set; }
            public int Piso { get; set; }
            public string Estado { get; set; } = "Libre"; // Libre, Ocupado, Seleccionado
        }

        private class ViajeModel
        {
            public string Hora { get; set; } = "";
            public string Bus { get; set; } = "";
            public string Servicio { get; set; } = "";
            public decimal Tarifa { get; set; }
            public string SalidaTexto { get; set; } = "";
        }

        private readonly List<AsientoModel> _asientos = new();
        private readonly List<ViajeModel> _viajes = new();
        private int _pisoActual = 1;
        private int? _asientoSeleccionado = null;
        private ViajeModel _viajeSeleccionado = null!;

        public VentaBoletosWindow()
        {
            InitializeComponent();
            InicializarRutas();
            InicializarViajes();
            InicializarAsientos();
            RenderizarPiso(_pisoActual);
            dpFechaViaje.SelectedDate = DateTime.Today;
        }

        private void InicializarRutas()
        {
            string[] origenes = { "Lima", "Huancayo", "Arequipa", "Trujillo", "Cusco" };
            string[] destinos = { "Huancayo", "Lima", "Arequipa", "Ayacucho", "Chiclayo" };

            cmbOrigen.ItemsSource = origenes;
            cmbOrigen.SelectedIndex = 0; // Lima

            cmbDestino.ItemsSource = destinos;
            cmbDestino.SelectedIndex = 0; // Huancayo
        }

        private void InicializarViajes()
        {
            _viajes.Clear();
            _viajes.Add(new ViajeModel
            {
                Hora = "08:00 AM",
                Bus = "Mercedes-Benz 120 (Placa ABC-123)",
                Servicio = "VIP (2 Pisos)",
                Tarifa = 65.00m,
                SalidaTexto = "Salida: Hoy 08:00 AM | Servicio VIP"
            });
            _viajes.Add(new ViajeModel
            {
                Hora = "01:30 PM",
                Bus = "Scania K410 (Placa XYZ-789)",
                Servicio = "Ejecutivo",
                Tarifa = 55.00m,
                SalidaTexto = "Salida: Hoy 01:30 PM | Servicio Ejecutivo"
            });
            _viajes.Add(new ViajeModel
            {
                Hora = "09:00 PM",
                Bus = "Volvo B430R (Placa PER-456)",
                Servicio = "Premium Suite",
                Tarifa = 80.00m,
                SalidaTexto = "Salida: Hoy 09:00 PM | Servicio Premium Suite"
            });

            _viajeSeleccionado = _viajes[0];
            RenderizarListaViajes();
            ActualizarResumenViaje();
        }

        private void InicializarAsientos()
        {
            _asientos.Clear();

            // Piso 1: Asientos 1 al 20 (5 filas de 4)
            for (int i = 1; i <= 20; i++)
            {
                _asientos.Add(new AsientoModel
                {
                    NroAsiento = i,
                    Piso = 1,
                    // Ocupamos algunos asientos como ejemplo
                    Estado = (i == 3 || i == 4 || i == 11 || i == 12) ? "Ocupado" : "Libre"
                });
            }

            // Piso 2: Asientos 21 al 48 (7 filas de 4)
            for (int i = 21; i <= 48; i++)
            {
                _asientos.Add(new AsientoModel
                {
                    NroAsiento = i,
                    Piso = 2,
                    Estado = (i == 23 || i == 24 || i == 35 || i == 36 || i == 42) ? "Ocupado" : "Libre"
                });
            }
        }

        private void RenderizarPiso(int piso)
        {
            _pisoActual = piso;
            panelFilasAsientos.Children.Clear();

            var asientosPiso = _asientos.Where(a => a.Piso == piso).OrderBy(a => a.NroAsiento).ToList();

            // Renderizamos filas de 4 asientos con pasillo central
            for (int i = 0; i < asientosPiso.Count; i += 4)
            {
                var gridFila = new Grid { Margin = new Thickness(0, 3, 0, 3) };
                gridFila.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) }); // Ventana Izq
                gridFila.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) }); // Pasillo Izq
                gridFila.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(38, GridUnitType.Pixel) }); // Pasillo
                gridFila.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) }); // Pasillo Der
                gridFila.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) }); // Ventana Der

                // Asiento 1
                if (i < asientosPiso.Count)
                    gridFila.Children.Add(CrearBotonAsiento(asientosPiso[i], 0));

                // Asiento 2
                if (i + 1 < asientosPiso.Count)
                    gridFila.Children.Add(CrearBotonAsiento(asientosPiso[i + 1], 1));

                // Pasillo
                var borderPasillo = new Border
                {
                    Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#E2E8F0")),
                    CornerRadius = new CornerRadius(4),
                    Margin = new Thickness(3, 2, 3, 2),
                    Height = 40,
                    Child = new TextBlock
                    {
                        Text = "||",
                        FontFamily = new FontFamily("Segoe UI"),
                        FontWeight = FontWeights.Bold,
                        FontSize = 10,
                        Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#94A3B8")),
                        HorizontalAlignment = HorizontalAlignment.Center,
                        VerticalAlignment = VerticalAlignment.Center
                    }
                };
                Grid.SetColumn(borderPasillo, 2);
                gridFila.Children.Add(borderPasillo);

                // Asiento 3
                if (i + 2 < asientosPiso.Count)
                    gridFila.Children.Add(CrearBotonAsiento(asientosPiso[i + 2], 3));

                // Asiento 4
                if (i + 3 < asientosPiso.Count)
                    gridFila.Children.Add(CrearBotonAsiento(asientosPiso[i + 3], 4));

                panelFilasAsientos.Children.Add(gridFila);
            }
        }

        private Button CrearBotonAsiento(AsientoModel asiento, int columna)
        {
            var btn = new Button
            {
                Style = (Style)FindResource("BotonAsientoItemStyle"),
                Tag = asiento
            };

            // Contenido con número de asiento y estado
            var sp = new StackPanel { VerticalAlignment = VerticalAlignment.Center };
            var txtNum = new TextBlock
            {
                Text = $"N° {asiento.NroAsiento:00}",
                FontFamily = new FontFamily("Segoe UI"),
                FontWeight = FontWeights.Bold,
                FontSize = 11,
                Foreground = Brushes.White
            };
            var txtEstado = new TextBlock
            {
                Text = asiento.Estado,
                FontFamily = new FontFamily("Segoe UI"),
                FontSize = 8.5,
                Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#F8FAFC"))
            };
            sp.Children.Add(txtNum);
            sp.Children.Add(txtEstado);
            btn.Content = sp;

            // Colores según estado
            ActualizarColorBotonAsiento(btn, asiento.Estado);

            if (asiento.Estado != "Ocupado")
            {
                btn.Click += Asiento_Click;
            }
            else
            {
                btn.Cursor = System.Windows.Input.Cursors.No;
            }

            Grid.SetColumn(btn, columna);
            return btn;
        }

        private void ActualizarColorBotonAsiento(Button btn, string estado)
        {
            if (estado == "Libre")
            {
                btn.Background = (Brush)FindResource("AsientoLibreBrush");
            }
            else if (estado == "Ocupado")
            {
                btn.Background = (Brush)FindResource("AsientoOcupadoBrush");
            }
            else if (estado == "Seleccionado")
            {
                btn.Background = (Brush)FindResource("AsientoSeleccionadoBrush");
                btn.BorderBrush = (Brush)FindResource("ContrastOrangeBrush");
                btn.BorderThickness = new Thickness(2);
            }
        }

        private void Asiento_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is AsientoModel asiento)
            {
                if (asiento.Estado == "Ocupado") return;

                // Deseleccionar el anterior
                foreach (var a in _asientos.Where(x => x.Estado == "Seleccionado"))
                {
                    a.Estado = "Libre";
                }

                if (_asientoSeleccionado == asiento.NroAsiento)
                {
                    _asientoSeleccionado = null;
                    lblAsientoSeleccionadoTexto.Text = "Ninguno seleccionado";
                    lblResumenAsiento.Text = "N° --";
                }
                else
                {
                    asiento.Estado = "Seleccionado";
                    _asientoSeleccionado = asiento.NroAsiento;
                    lblAsientoSeleccionadoTexto.Text = $"N° {asiento.NroAsiento:00} (Piso {asiento.Piso})";
                    lblResumenAsiento.Text = $"N° {asiento.NroAsiento:00} (Piso {asiento.Piso})";
                }

                RenderizarPiso(_pisoActual);
            }
        }

        private void BtnPiso1_Click(object sender, RoutedEventArgs e)
        {
            btnPiso1.Background = (Brush)FindResource("PrimaryAccentBrush");
            btnPiso1.Foreground = Brushes.White;
            btnPiso2.Background = Brushes.White;
            btnPiso2.Foreground = (Brush)FindResource("PrimaryBrush");
            RenderizarPiso(1);
        }

        private void BtnPiso2_Click(object sender, RoutedEventArgs e)
        {
            btnPiso2.Background = (Brush)FindResource("PrimaryAccentBrush");
            btnPiso2.Foreground = Brushes.White;
            btnPiso1.Background = Brushes.White;
            btnPiso1.Foreground = (Brush)FindResource("PrimaryBrush");
            RenderizarPiso(2);
        }

        private void RenderizarListaViajes()
        {
            panelViajesDisponibles.Children.Clear();

            foreach (var viaje in _viajes)
            {
                bool esSeleccionado = viaje == _viajeSeleccionado;

                var border = new Border
                {
                    Background = esSeleccionado ? new SolidColorBrush((Color)ColorConverter.ConvertFromString("#EBF8FF")) : Brushes.White,
                    BorderBrush = esSeleccionado ? (Brush)FindResource("PrimaryAccentBrush") : (Brush)FindResource("BorderLightBrush"),
                    BorderThickness = new Thickness(esSeleccionado ? 2 : 1),
                    CornerRadius = new CornerRadius(8),
                    Padding = new Thickness(14, 12, 14, 12),
                    Margin = new Thickness(0, 0, 0, 10),
                    Cursor = System.Windows.Input.Cursors.Hand,
                    Tag = viaje
                };

                var grid = new Grid();
                grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(85, GridUnitType.Pixel) }); // Hora
                grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) }); // Info Bus & Servicio
                grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(110, GridUnitType.Pixel) }); // Tarifa y Acción

                // Columna 1: Hora
                var spHora = new StackPanel { VerticalAlignment = VerticalAlignment.Center };
                spHora.Children.Add(new TextBlock
                {
                    Text = viaje.Hora,
                    FontFamily = new FontFamily("Segoe UI"),
                    FontWeight = FontWeights.Bold,
                    FontSize = 15,
                    Foreground = (Brush)FindResource("PrimaryBrush")
                });
                spHora.Children.Add(new TextBlock
                {
                    Text = "Salida",
                    FontFamily = new FontFamily("Segoe UI"),
                    FontSize = 11,
                    Foreground = (Brush)FindResource("TextSecondaryBrush")
                });
                Grid.SetColumn(spHora, 0);
                grid.Children.Add(spHora);

                // Columna 2: Detalles
                var spDetalle = new StackPanel { VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(10, 0, 10, 0) };
                var spServicio = new StackPanel { Orientation = Orientation.Horizontal };
                spServicio.Children.Add(new Border
                {
                    Background = (Brush)FindResource("TealLightBrush"),
                    CornerRadius = new CornerRadius(4),
                    Padding = new Thickness(6, 2, 6, 2),
                    Margin = new Thickness(0, 0, 6, 2),
                    Child = new TextBlock
                    {
                        Text = viaje.Servicio,
                        FontFamily = new FontFamily("Segoe UI"),
                        FontWeight = FontWeights.Bold,
                        FontSize = 10.5,
                        Foreground = (Brush)FindResource("TealBrush")
                    }
                });
                spDetalle.Children.Add(spServicio);
                spDetalle.Children.Add(new TextBlock
                {
                    Text = viaje.Bus,
                    FontFamily = new FontFamily("Segoe UI"),
                    FontSize = 12,
                    Foreground = (Brush)FindResource("TextPrimaryBrush")
                });
                Grid.SetColumn(spDetalle, 1);
                grid.Children.Add(spDetalle);

                // Columna 3: Tarifa y Botón Seleccionar
                var spPrecio = new StackPanel { VerticalAlignment = VerticalAlignment.Center, HorizontalAlignment = HorizontalAlignment.Right };
                spPrecio.Children.Add(new TextBlock
                {
                    Text = $"S/. {viaje.Tarifa:N2}",
                    FontFamily = new FontFamily("Segoe UI"),
                    FontWeight = FontWeights.Bold,
                    FontSize = 17,
                    Foreground = (Brush)FindResource("PrimaryBrush"),
                    HorizontalAlignment = HorizontalAlignment.Right
                });
                spPrecio.Children.Add(new TextBlock
                {
                    Text = esSeleccionado ? "✓ Seleccionado" : "Elegir viaje",
                    FontFamily = new FontFamily("Segoe UI"),
                    FontWeight = FontWeights.SemiBold,
                    FontSize = 11,
                    Foreground = esSeleccionado ? (Brush)FindResource("PrimaryAccentBrush") : (Brush)FindResource("TextSecondaryBrush"),
                    HorizontalAlignment = HorizontalAlignment.Right
                });
                Grid.SetColumn(spPrecio, 2);
                grid.Children.Add(spPrecio);

                border.Child = grid;
                border.MouseLeftButtonUp += (s, e) =>
                {
                    _viajeSeleccionado = viaje;
                    RenderizarListaViajes();
                    ActualizarResumenViaje();
                };

                panelViajesDisponibles.Children.Add(border);
            }
        }

        private void ActualizarResumenViaje()
        {
            if (_viajeSeleccionado != null)
            {
                lblResumenSalida.Text = _viajeSeleccionado.SalidaTexto;
                lblResumenTarifa.Text = $"S/. {_viajeSeleccionado.Tarifa:N2}";
                lblTotalPagar.Text = $"S/. {_viajeSeleccionado.Tarifa:N2}";
                lblBusInfo.Text = $" ({_viajeSeleccionado.Servicio})";
                CalcularVuelto();
            }
        }

        private void CmbRuta_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (cmbOrigen?.SelectedItem != null && cmbDestino?.SelectedItem != null)
            {
                string ruta = $"{cmbOrigen.SelectedItem} ➔ {cmbDestino.SelectedItem}";
                if (lblRutaSeleccionada != null) lblRutaSeleccionada.Text = $" ({ruta})";
                if (lblResumenRuta != null) lblResumenRuta.Text = ruta;
            }
        }

        private void BtnIntercambiar_Click(object sender, RoutedEventArgs e)
        {
            int idxOrigen = cmbOrigen.SelectedIndex;
            cmbOrigen.SelectedIndex = cmbDestino.SelectedIndex;
            cmbDestino.SelectedIndex = idxOrigen;
        }

        private void RbMetodoPago_Checked(object sender, RoutedEventArgs e)
        {
            if (panelEfectivo != null)
            {
                panelEfectivo.Visibility = (rbEfectivo?.IsChecked == true) ? Visibility.Visible : Visibility.Collapsed;
            }
        }

        private void TxtMontoRecibido_TextChanged(object sender, TextChangedEventArgs e)
        {
            CalcularVuelto();
        }

        private void CalcularVuelto()
        {
            if (lblVuelto == null || _viajeSeleccionado == null) return;

            if (decimal.TryParse(txtMontoRecibido?.Text, NumberStyles.Any, CultureInfo.InvariantCulture, out decimal recibido))
            {
                decimal total = _viajeSeleccionado.Tarifa;
                decimal vuelto = recibido - total;
                if (vuelto >= 0)
                {
                    lblVuelto.Text = $"S/. {vuelto:N2}";
                    lblVuelto.Foreground = (Brush)FindResource("CajaSuccessBrush");
                }
                else
                {
                    lblVuelto.Text = $"Faltan S/. {Math.Abs(vuelto):N2}";
                    lblVuelto.Foreground = (Brush)FindResource("AsientoOcupadoBrush");
                }
            }
            else
            {
                lblVuelto.Text = "Monto inválido";
                lblVuelto.Foreground = (Brush)FindResource("AsientoOcupadoBrush");
            }
        }

        private void BtnEmitirBoleto_Click(object sender, RoutedEventArgs e)
        {
            // 1. Validar asiento seleccionado
            if (!_asientoSeleccionado.HasValue)
            {
                MessageBox.Show("Por favor, seleccione un asiento disponible en el croquis del bus.", 
                                "Asiento Requerido", 
                                MessageBoxButton.OK, 
                                MessageBoxImage.Warning);
                return;
            }

            // 2. Validar datos del pasajero
            string dni = txtDni.Text.Trim();
            string nombres = txtNombres.Text.Trim();

            if (string.IsNullOrWhiteSpace(dni) || dni.Length != 8 || !dni.All(char.IsDigit))
            {
                MessageBox.Show("Ingrese un número de DNI válido de 8 dígitos.", 
                                "DNI Inválido", 
                                MessageBoxButton.OK, 
                                MessageBoxImage.Warning);
                txtDni.Focus();
                return;
            }

            if (string.IsNullOrWhiteSpace(nombres))
            {
                MessageBox.Show("Ingrese los nombres y apellidos del pasajero.", 
                                "Nombre Requerido", 
                                MessageBoxButton.OK, 
                                MessageBoxImage.Warning);
                txtNombres.Focus();
                return;
            }

            // 3. Confirmar emisión y marcar asiento como Ocupado
            int asientoOcupadoNum = _asientoSeleccionado.Value;
            var asientoObj = _asientos.FirstOrDefault(a => a.NroAsiento == asientoOcupadoNum);
            if (asientoObj != null)
            {
                asientoObj.Estado = "Ocupado";
            }

            string metodoPago = rbEfectivo.IsChecked == true ? "Efectivo" : (rbYape.IsChecked == true ? "Yape/Plin" : "Tarjeta");

            MessageBox.Show(
                $"¡Boleto emitido con éxito!\n\n" +
                $"• Pasajero: {nombres}\n" +
                $"• DNI: {dni}\n" +
                $"• Ruta: {cmbOrigen.SelectedItem} ➔ {cmbDestino.SelectedItem}\n" +
                $"• Salida: {_viajeSeleccionado.Hora} ({_viajeSeleccionado.Servicio})\n" +
                $"• Asiento Asignado: N° {asientoOcupadoNum:00} (Piso {asientoObj?.Piso})\n" +
                $"• Método de Pago: {metodoPago}\n" +
                $"• Total Cobrado: S/. {_viajeSeleccionado.Tarifa:N2}\n\n" +
                $"Imprimiendo comprobante de viaje...",
                "Venta Registrada Exitosamente",
                MessageBoxButton.OK,
                MessageBoxImage.Information);

            // 4. Limpiar selección y campos
            _asientoSeleccionado = null;
            lblAsientoSeleccionadoTexto.Text = "Ninguno seleccionado";
            lblResumenAsiento.Text = "N° --";
            txtDni.Clear();
            txtNombres.Clear();
            RenderizarPiso(_pisoActual);
        }

        private void BtnCerrarSesion_Click(object sender, RoutedEventArgs e)
        {
            var login = new MainWindow();
            login.Show();
            this.Close();
        }
    }
}
