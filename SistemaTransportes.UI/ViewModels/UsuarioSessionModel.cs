namespace SistemaTransportes.UI.ViewModels;

public class UsuarioSessionModel
{
    public int UsuarioID { get; set; }
    public string Username { get; set; } = "OpControl";
    public string Rol { get; set; } = "Operador";
    public int CajaTurnoID { get; set; } = 1;
    public string Nombres { get; set; } = "Operador de Control";
}
