namespace Ddts.Domain.Clients;

/// <summary>
/// Danışanın alerji/kısıt listesindeki bir besin (Şekil 4.1, FR-B2).
/// Tablo: client_food_restrictions; birincil anahtar (client_id, food_item_id), yani bir besin listede bir kez.
/// </summary>
public sealed class FoodRestriction
{
    private FoodRestriction()
    {
    }

    public FoodRestriction(Guid foodItemId, RestrictionType type)
    {
        FoodItemId = foodItemId;
        Type = type;
    }

    public Guid FoodItemId { get; private set; }

    public RestrictionType Type { get; private set; }
}
