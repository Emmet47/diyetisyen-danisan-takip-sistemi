namespace Ddts.Domain.Energy;

/// <summary>
/// Strategy deseni (Şekil 4.7): bazal metabolizma hızını (BMH) hesaplayan formül. Yeni formül = bu arayüzü
/// uygulayan yeni bir sınıf + Program.cs'te kayıt; EnergyCalculator ve servisler değişmez (NFR-B1).
/// </summary>
public interface IEnergyStrategy
{
    /// <summary>İstekte seçilen formül adı (ör. "MifflinStJeor").</summary>
    string Name { get; }

    decimal CalculateBmr(EnergyInput input);
}
