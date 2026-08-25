namespace GiftExchange.Library.Entities;

/// <summary>
/// Reference data listing the valid hat statuses. DSQL has no foreign keys, so this cannot
/// constrain <see cref="HatEntity.Status"/>; the relationship is configured in EF for joins
/// and for the shape it gives the model, not for enforcement.
/// </summary>
public class HatStatusEntity
{
    public required string Status { get; set; }

    public ICollection<HatEntity> Hats { get; set; } = [];
}
