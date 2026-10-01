using SistemaTransportes.UI.MVVM;

namespace SistemaTransportes.UI.ViewModels;

public class PasajeroItemViewModel : ViewModelBase
{
    public int NroAsiento { get; set; }
    public int Piso { get; set; } = 1;
    public decimal Precio { get; set; }

    private string _dni = "";
    public string Dni
    {
        get => _dni;
        set => SetProperty(ref _dni, value);
    }

    private string _nombres = "";
    public string Nombres
    {
        get => _nombres;
        set => SetProperty(ref _nombres, value);
    }
}
