using System;
using System.Configuration;
using System.Data;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using Microsoft.Data.SqlClient;

namespace SistemaTransportes
{
    /// <summary>
    /// Lógica de interacción para MainWindow.xaml (Login del Sistema)
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            // Precalienta el socket TLS y el Connection Pool hacia Azure SQL en segundo plano
            _ = Task.Run(() =>
            {
                try
                {
                    using var conn = new SqlConnection(ObtenerCadenaConexion());
                    conn.Open();
                }
                catch
                {
                    // Silencioso: si no hay red inmediata, el login regular gestionará la excepción
                }
            });
        }

        private void BtnAccesoRapido_Click(object sender, RoutedEventArgs e)
        {
            txtUsuario.Text = "OpControl";
            txtPassword.Password = "1598753";
        }

        private void BtnAccesoRapido2_Click(object sender, RoutedEventArgs e)
        {
            txtUsuario.Text = "OpVenta2";
            txtPassword.Password = "1598753";
        }

        private async void BtnIngresar_Click(object sender, RoutedEventArgs e)
        {
            string username = txtUsuario.Text.Trim();
            string password = txtPassword.Password;

            // 1. Validación de campos obligatorios
            if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
            {
                MessageBox.Show("Por favor, ingrese tanto el usuario como la contraseña.", 
                                "Campos Requeridos", 
                                MessageBoxButton.OK, 
                                MessageBoxImage.Warning);
                return;
            }

            // 2. Cálculo del hash SHA-256 en mayúsculas (equivalente a HASHBYTES('SHA2_256', ...) en SQL)
            string passwordHash = CalcularSha256(password);

            btnIngresar.IsEnabled = false;
            Mouse.OverrideCursor = Cursors.Wait;

            try
            {
                string connectionString = ObtenerCadenaConexion();

                await using var connection = new SqlConnection(connectionString);
                await connection.OpenAsync();

                // 3. Consulta unificada de autenticación y turno de caja en un único roundtrip de red
                const string queryAuth = @"
                    SELECT 
                        u.UsuarioID, 
                        ISNULL(u.Nombres, u.Username) AS Nombres,
                        (
                            SELECT TOP 1 c.CajaTurnoID 
                            FROM dbo.CajasTurno c 
                            WHERE c.UsuarioID = u.UsuarioID 
                              AND c.Estado IN ('Abierto', 'Abierta')
                            ORDER BY c.CajaTurnoID DESC
                        ) AS CajaTurnoID
                    FROM dbo.Usuarios u
                    WHERE u.Username = @Username 
                      AND u.PasswordHash = @PasswordHash 
                      AND u.Activo = 1;";

                int usuarioId = 0;
                string nombres = string.Empty;
                int cajaTurnoId = 0;

                using (var cmd = new SqlCommand(queryAuth, connection))
                {
                    cmd.Parameters.Add("@Username", SqlDbType.NVarChar, 40).Value = username;
                    cmd.Parameters.Add("@PasswordHash", SqlDbType.NVarChar, 256).Value = passwordHash;

                    using var reader = await cmd.ExecuteReaderAsync();
                    if (await reader.ReadAsync())
                    {
                        usuarioId = reader.GetInt32(0);
                        nombres = reader.GetString(1);
                        cajaTurnoId = reader.IsDBNull(2) ? 0 : reader.GetInt32(2);
                    }
                }

                // 4. Validación de existencia del usuario
                if (usuarioId == 0)
                {
                    Mouse.OverrideCursor = null;
                    MessageBox.Show("Usuario o contraseña incorrectos.", 
                                    "Error de Autenticación", 
                                    MessageBoxButton.OK, 
                                    MessageBoxImage.Error);
                    return;
                }

                // 5. Mensaje de bienvenida con autorización
                Mouse.OverrideCursor = null;
                MessageBox.Show($"¡Bienvenido al sistema, {nombres}! Turno de caja #{cajaTurnoId} activo.", 
                                "Acceso Autorizado", 
                                MessageBoxButton.OK, 
                                MessageBoxImage.Information);

                // 6. Navegación a la ventana de venta de boletos
                try
                {
                    var session = new UsuarioSessionModel
                    {
                        UsuarioID = usuarioId,
                        Username = username,
                        Nombres = nombres,
                        Rol = "Operador",
                        CajaTurnoID = cajaTurnoId
                    };
                    var ventanaVenta = new VentaBoletosWindow(session);
                    Application.Current.MainWindow = ventanaVenta;
                    ventanaVenta.Show();
                    this.Close();
                }
                catch (Exception ex)
                {
                    string detalle = ex.InnerException != null ? $"\nDetalle: {ex.InnerException.Message}" : "";
                    MessageBox.Show($"Error al cargar la ventana de ventas: {ex.Message}{detalle}", "Error de Interfaz", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            catch (SqlException ex)
            {
                Mouse.OverrideCursor = null;
                MessageBox.Show($"Error de conexión con la base de datos SQL Server:\n{ex.Message}", 
                                "Error de Conexión", 
                                MessageBoxButton.OK, 
                                MessageBoxImage.Error);
            }
            catch (Exception ex)
            {
                Mouse.OverrideCursor = null;
                MessageBox.Show($"Ocurrió un error inesperado al procesar la autenticación:\n{ex.Message}", 
                                "Error", 
                                MessageBoxButton.OK, 
                                MessageBoxImage.Error);
            }
            finally
            {
                btnIngresar.IsEnabled = true;
                Mouse.OverrideCursor = null;
            }
        }

        private static string ObtenerCadenaConexion()
        {
            return ConfigurationManager.ConnectionStrings["BD_Transportes"]?.ConnectionString
                ?? "Server=tcp:sistema-transportes-2026.database.windows.net,1433;Initial Catalog=BD_Transportes;Persist Security Info=False;User ID=admin_st;Password=1425PA31%;MultipleActiveResultSets=False;Encrypt=True;TrustServerCertificate=False;Connection Timeout=30;";
        }

        private static string CalcularSha256(string input)
        {
            byte[] bytes = SHA256.HashData(Encoding.UTF8.GetBytes(input));
            return Convert.ToHexString(bytes);
        }
    }
}