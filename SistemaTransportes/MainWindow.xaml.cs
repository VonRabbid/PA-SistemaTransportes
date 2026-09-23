using System;
using System.Configuration;
using System.Data;
using System.Security.Cryptography;
using System.Text;
using System.Windows;
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
        }

        private void BtnAccesoRapido_Click(object sender, RoutedEventArgs e)
        {
            txtUsuario.Text = "OpControl";
            txtPassword.Password = "1598753";
        }

        private void BtnIngresar_Click(object sender, RoutedEventArgs e)
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

            try
            {
                string connectionString = ObtenerCadenaConexion();

                using var connection = new SqlConnection(connectionString);
                connection.Open();

                // 3. Consulta parametrizada a dbo.Usuarios
                const string queryUsuario = @"
                    SELECT UsuarioID, ISNULL(Nombres, Username) AS Nombres
                    FROM dbo.Usuarios
                    WHERE Username = @Username 
                      AND PasswordHash = @PasswordHash 
                      AND Activo = 1;";

                int usuarioId = 0;
                string nombres = string.Empty;

                using (var cmdUsuario = new SqlCommand(queryUsuario, connection))
                {
                    cmdUsuario.Parameters.Add("@Username", SqlDbType.NVarChar, 40).Value = username;
                    cmdUsuario.Parameters.Add("@PasswordHash", SqlDbType.NVarChar, 256).Value = passwordHash;

                    using var reader = cmdUsuario.ExecuteReader();
                    if (reader.Read())
                    {
                        usuarioId = reader.GetInt32(0);
                        nombres = reader.GetString(1);
                    }
                }

                // 4. Validación de existencia del usuario
                if (usuarioId == 0)
                {
                    MessageBox.Show("Usuario o contraseña incorrectos.", 
                                    "Error de Autenticación", 
                                    MessageBoxButton.OK, 
                                    MessageBoxImage.Error);
                    return;
                }

                // 5. Consulta del turno de caja activo en dbo.CajasTurno
                const string queryCaja = @"
                    SELECT TOP 1 CajaTurnoID
                    FROM dbo.CajasTurno
                    WHERE UsuarioID = @UsuarioID 
                      AND Estado IN ('Abierto', 'Abierta')
                    ORDER BY CajaTurnoID DESC;";

                int cajaTurnoId = 0;
                using (var cmdCaja = new SqlCommand(queryCaja, connection))
                {
                    cmdCaja.Parameters.Add("@UsuarioID", SqlDbType.Int).Value = usuarioId;

                    var resultadoCaja = cmdCaja.ExecuteScalar();
                    if (resultadoCaja != null && resultadoCaja != DBNull.Value)
                    {
                        cajaTurnoId = Convert.ToInt32(resultadoCaja);
                    }
                }

                // 6. Mensaje de bienvenida con autorización
                MessageBox.Show($"¡Bienvenido al sistema, {nombres}! Turno de caja #{cajaTurnoId} activo.", 
                                "Acceso Autorizado", 
                                MessageBoxButton.OK, 
                                MessageBoxImage.Information);

                // 7. Navegación a la ventana de venta de boletos
                var ventanaVenta = new VentaBoletosWindow();
                ventanaVenta.Show();
                this.Close();
            }
            catch (SqlException ex)
            {
                MessageBox.Show($"Error de conexión con la base de datos SQL Server:\n{ex.Message}", 
                                "Error de Conexión", 
                                MessageBoxButton.OK, 
                                MessageBoxImage.Error);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ocurrió un error inesperado al procesar la autenticación:\n{ex.Message}", 
                                "Error", 
                                MessageBoxButton.OK, 
                                MessageBoxImage.Error);
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