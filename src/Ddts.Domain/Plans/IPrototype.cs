namespace Ddts.Domain.Plans;

/// <summary>
/// Prototype deseni (Şekil 4.8): nesne kendisinin bağımsız (derin) bir kopyasını üretir.
/// Meal ve MealItem uygular; şablondan plan oluştururken kullanılır (BR-07).
/// </summary>
public interface IPrototype<out T>
{
    T Clone();
}
