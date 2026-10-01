using System.Windows.Controls;
using System.Windows.Input;
using SistemaTransportes.Application.Interfaces;
using SistemaTransportes.UI.MVVM;

namespace SistemaTransportes.UI.ViewModels;

public class LoginViewModel : ViewModelBase
{
    private readonly IAuthService _authService;

    public event Action<UsuarioSessionModel>? LoginExitoso;
    public event Action<string, string, bool>? NotificarMensaje;

    private string _username = string.Empty;
    public string Username
    {
        get => _username;
        set => SetProperty(ref _username, value);
    }

    private string _password = string.Empty;
    public string Password
    {
        get => _password;
        set => SetProperty(ref _password, value);
    }

    private bool _estaCargando;
    public bool EstaCargando
    {
        get => _estaCargando;
        set => SetProperty(ref _estaCargando, value);
    }

    private string? _mensajeError;
    public string? MensajeError
    {
        get => _mensajeError;
        set => SetProperty(ref _mensajeError, value);
    }

    public ICommand IngresarCommand { get; }
    public ICommand AccesoRapido1Command { get; }
    public ICommand AccesoRapido2Command { get; }

    public LoginViewModel(IAuthService authService)
    {
        _authService = authService ?? throw new ArgumentNullException(nameof(authService));

        IngresarCommand = new RelayCommand(async param => await EjecutarIngresoAsync(param));
        AccesoRapido1Command = new RelayCommand(AccesoRapidoOpControl);
        AccesoRapido2Command = new RelayCommand(AccesoRapidoOpVenta2);
    }

    public void AccesoRapidoOpControl()
    {
        Username = "OpControl";
        Password = "1598753";
    }

    public void AccesoRapidoOpVenta2()
    {
        Username = "OpVenta2";
        Password = "1598753";
    }

    public async Task EjecutarIngresoAsync(object? parameter = null)
    {
        string pass = Password;
        if (parameter is PasswordBox pbox && !string.IsNullOrEmpty(pbox.Password))
        {
            pass = pbox.Password;
        }

        if (string.IsNullOrWhiteSpace(Username) || string.IsNullOrWhiteSpace(pass))
        {
            MensajeError = "Por favor, ingrese tanto el usuario como la contraseña.";
            NotificarMensaje?.Invoke(MensajeError, "Campos Requeridos", false);
            return;
        }

        EstaCargando = true;
        MensajeError = null;

        try
        {
            var resultado = await _authService.IniciarSesionAsync(Username.Trim(), pass);

            if (resultado == null)
            {
                MensajeError = "Usuario o contraseña incorrectos.";
                NotificarMensaje?.Invoke(MensajeError, "Error de Autenticación", false);
                return;
            }

            var (usuario, turno) = resultado.Value;
            var session = new UsuarioSessionModel
            {
                UsuarioID = usuario.UsuarioID,
                Username = usuario.Username,
                Nombres = usuario.Nombres ?? usuario.Username,
                Rol = usuario.Rol,
                CajaTurnoID = turno?.CajaTurnoID ?? 0
            };

            string msg = $"¡Bienvenido al sistema, {session.Nombres}! Turno de caja #{session.CajaTurnoID} activo.";
            NotificarMensaje?.Invoke(msg, "Acceso Autorizado", true);

            LoginExitoso?.Invoke(session);
        }
        catch (Exception ex)
        {
            MensajeError = $"Error al procesar la autenticación: {ex.Message}";
            NotificarMensaje?.Invoke(MensajeError, "Error de Conexión", false);
        }
        finally
        {
            EstaCargando = false;
        }
    }
}
