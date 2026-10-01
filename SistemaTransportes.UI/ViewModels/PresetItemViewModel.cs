using SistemaTransportes.UI.MVVM;

namespace SistemaTransportes.UI.ViewModels;

public class PresetItemViewModel : ViewModelBase
{
    private decimal _tarifa;
    private decimal _tarifaPorKgActual = 3.00m;

    public string Titulo { get; set; } = string.Empty;
    public string Nombre { get; set; } = string.Empty;
    public string Descripcion { get; set; } = string.Empty;
    public decimal TarifaPasajero { get; set; }
    public decimal TarifaSoloEncomienda { get; set; }

    public decimal Tarifa
    {
        get => _tarifa;
        set
        {
            if (SetProperty(ref _tarifa, value))
            {
                OnPropertyChanged(nameof(TarifaDisplay));
                OnPropertyChanged(nameof(DisplayTexto));
            }
        }
    }

    public decimal PesoRef { get; set; }
    public bool EsPersonalizado { get; set; }
    public string Icono { get; set; } = "📦";

    public decimal TarifaPorKgActual
    {
        get => _tarifaPorKgActual;
        set => SetProperty(ref _tarifaPorKgActual, value);
    }

    public string TarifaDisplay => EsPersonalizado
        ? $"S/. {TarifaPorKgActual:N2} / Kg"
        : $"S/. {Tarifa:N2}";

    public string PesoRefDisplay => EsPersonalizado
        ? "Balanza manual (máx. 50 kg)"
        : $"{PesoRef:0.#} kg ref.";

    public string DisplayTexto => EsPersonalizado
        ? $"{Icono} {Nombre} — S/. {TarifaPorKgActual:N2} / Kg"
        : $"{Icono} {Nombre} — S/. {Tarifa:N2} ({PesoRef:0.#} kg ref.)";

    public void ActualizarModo(bool esSoloEncomienda)
    {
        TarifaPorKgActual = esSoloEncomienda ? 4.00m : 3.00m;
        Tarifa = esSoloEncomienda ? TarifaSoloEncomienda : TarifaPasajero;
        OnPropertyChanged(nameof(TarifaDisplay));
        OnPropertyChanged(nameof(DisplayTexto));
    }

    public bool CoincideCon(string texto)
    {
        if (string.IsNullOrWhiteSpace(texto))
        {
            return false;
        }

        string t = texto.Trim();

        return string.Equals(Nombre, t, StringComparison.OrdinalIgnoreCase)
            || string.Equals(Titulo, t, StringComparison.OrdinalIgnoreCase)
            || Nombre.StartsWith(t, StringComparison.OrdinalIgnoreCase)
            || Titulo.StartsWith(t, StringComparison.OrdinalIgnoreCase)
            || Nombre.Contains(t, StringComparison.OrdinalIgnoreCase)
            || Titulo.Contains(t, StringComparison.OrdinalIgnoreCase);
    }

    public override string ToString() => DisplayTexto;
}
