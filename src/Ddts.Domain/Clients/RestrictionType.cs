namespace Ddts.Domain.Clients;

/// <summary>
/// Besin kısıtı türü (BR-05): Allergy → besin plana eklenemez, Dislike → yalnızca uyarı.
/// client_food_restrictions.type
/// </summary>
public enum RestrictionType
{
    Allergy,
    Dislike,
}
