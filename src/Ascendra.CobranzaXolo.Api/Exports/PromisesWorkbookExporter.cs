using Ascendra.CobranzaXolo.Application.Dashboard;

using ClosedXML.Excel;

namespace Ascendra.CobranzaXolo.Api.Exports;

public static class PromisesWorkbookExporter
{
    private static readonly string[] Headers =
    [
        "Id de promesa", "Cliente único", "Agente", "Fecha de promesa",
        "Monto inicial", "Monto semanal", "Tipo de promesa", "Número de semanas",
        "Fecha de creación", "Estatus"
    ];

    public static byte[] Create(IReadOnlyList<PromiseListItem> promises)
    {
        using var workbook = new XLWorkbook();
        var sheet = workbook.Worksheets.Add("Promesas");

        for (var column = 0; column < Headers.Length; column++)
        {
            sheet.Cell(1, column + 1).Value = Headers[column];
        }

        var rowNumber = 2;
        foreach (var promise in promises)
        {
            sheet.Cell(rowNumber, 1).Value = promise.IdPromesa;
            sheet.Cell(rowNumber, 2).Value = promise.ClienteUnico;
            sheet.Cell(rowNumber, 3).Value = promise.Agente;
            SetDate(sheet.Cell(rowNumber, 4), promise.FechaPromesa);
            SetCurrency(sheet.Cell(rowNumber, 5), promise.MontoInicial);
            SetCurrency(sheet.Cell(rowNumber, 6), promise.MontoSemanal);
            sheet.Cell(rowNumber, 7).Value = promise.TipoPromesa;
            sheet.Cell(rowNumber, 8).Value = promise.NumeroSemanas;
            SetDate(sheet.Cell(rowNumber, 9), promise.FechaCreacion);
            sheet.Cell(rowNumber, 10).Value = promise.Estatus;
            rowNumber++;
        }

        var table = sheet.Range(1, 1, Math.Max(rowNumber - 1, 1), Headers.Length)
            .CreateTable("Promesas");
        table.Theme = XLTableTheme.TableStyleMedium2;
        table.ShowAutoFilter = true;
        sheet.SheetView.FreezeRows(1);
        sheet.Columns().AdjustToContents();
        sheet.Column(2).Width = Math.Max(sheet.Column(2).Width, 20);
        sheet.Column(3).Width = Math.Max(sheet.Column(3).Width, 22);
        sheet.Row(1).Height = 24;

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        return stream.ToArray();
    }

    private static void SetDate(IXLCell cell, DateTime? value)
    {
        if (value is null) return;
        cell.Value = value.Value;
        cell.Style.NumberFormat.Format = "dd/MM/yyyy HH:mm";
    }

    private static void SetCurrency(IXLCell cell, decimal? value)
    {
        if (value is null) return;
        cell.Value = value.Value;
        cell.Style.NumberFormat.Format = "$#,##0.00";
    }
}