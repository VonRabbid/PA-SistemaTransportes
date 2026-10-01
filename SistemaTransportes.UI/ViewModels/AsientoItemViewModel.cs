using System.Windows.Input;
using SistemaTransportes.UI.MVVM;

namespace SistemaTransportes.UI.ViewModels;

public class AsientoItemViewModel : ViewModelBase
{
    public int NroAsiento { get; set; }
    public int Piso { get; set; } = 1;

    private string _estado = "Libre";
    public string Estado
    {
        get => _estado;
        set
        {
            if (SetProperty(ref _estado, value))
            {
                OnPropertyChanged(nameof(EstadoVisual));
                OnPropertyChanged(nameof(EsSeleccionado));
                OnPropertyChanged(nameof(EsClickeable));
            }
        }
    }

    public string EstadoVisual => Estado;
    public bool EsSeleccionado => Estado == "Seleccionado";
    public bool EsClickeable => Estado != "Ocupado";

    public ICommand? ClickCommand { get; set; }
}

public class FilaAsientoItemViewModel
{
    public AsientoItemViewModel? AsientoVentanaIzq { get; set; }
    public AsientoItemViewModel? AsientoPasilloIzq { get; set; }
    public AsientoItemViewModel? AsientoPasilloDer { get; set; }
    public AsientoItemViewModel? AsientoVentanaDer { get; set; }
}
