namespace Ascendra.CobranzaXolo.Application.Collections;

public sealed record CollectionHeatmapResponse(
    string View,
    int? Week,
    IReadOnlyList<string> Rows,
    IReadOnlyList<string> Columns,
    IReadOnlyList<CollectionHeatmapCell> Cells,
    decimal TotalRecovery);

public sealed record CollectionHeatmapCell(string Row, string Column, decimal Amount);