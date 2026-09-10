namespace Ascendra.CobranzaXolo.Application.Collections;

public sealed class CollectionHeatmapQuery
{
    /// <summary>agent-day, agent-product or weekly-comparison.</summary>
    public string View { get; init; } = "agent-day";
    public int? Week { get; init; }
}