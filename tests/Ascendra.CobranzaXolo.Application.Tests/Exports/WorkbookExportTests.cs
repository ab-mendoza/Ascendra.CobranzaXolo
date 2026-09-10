using System.IO.Compression;

using Ascendra.CobranzaXolo.Api.Exports;
using Ascendra.CobranzaXolo.Application.Dashboard;
using Ascendra.CobranzaXolo.Application.Recurrence;

namespace Ascendra.CobranzaXolo.Application.Tests.Exports;

public sealed class WorkbookExportTests
{
    [Fact]
    public void Promises_export_creates_an_openxml_workbook()
    {
        var bytes = PromisesWorkbookExporter.Create(
        [
            new PromiseListItem(
                "101", "1-1-123", "Agente prueba", new DateTime(2026, 9, 9),
                1500m, 500m, "Plan de Pago", 12,
                new DateTime(2026, 9, 1, 9, 30, 0), "Vigente")
        ]);

        AssertOpenXmlWorkbook(bytes, "xl/workbook.xml");
    }

    [Fact]
    public void Recurrence_export_creates_an_openxml_workbook()
    {
        var response = new RecurrenceResponse(
            [new RecurrencePeriod("week_2026_36", "Semana 36 · 2026", 2026, 36)],
            [new RecurrenceRow("1-1-123", "Activo", "Agente prueba", "Vigente",
                new Dictionary<string, decimal?> { ["week_2026_36"] = 500m })],
            500m);

        AssertOpenXmlWorkbook(RecurrenceWorkbookExporter.Create(response), "xl/workbook.xml");
    }

    private static void AssertOpenXmlWorkbook(byte[] bytes, string requiredEntry)
    {
        Assert.True(bytes.Length > 100);
        using var archive = new ZipArchive(new MemoryStream(bytes), ZipArchiveMode.Read);
        Assert.Contains(archive.Entries, entry => entry.FullName == requiredEntry);
    }
}