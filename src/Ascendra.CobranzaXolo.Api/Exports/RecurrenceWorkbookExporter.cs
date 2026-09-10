using Ascendra.CobranzaXolo.Application.Recurrence;

using ClosedXML.Excel;

namespace Ascendra.CobranzaXolo.Api.Exports;

public static class RecurrenceWorkbookExporter
{
    public static byte[] Create(RecurrenceResponse recurrence)
    {
        using var workbook = new XLWorkbook();
        var sheet = workbook.Worksheets.Add("Recurrencia");
        var headers = new[] { "Cliente único", "Estado del plan", "Agente", "Estatus despacho" }
            .Concat(recurrence.Periods.Select(period => period.Label))
            .ToArray();

        for (var column = 0; column < headers.Length; column++)
        {
            sheet.Cell(1, column + 1).Value = headers[column];
        }

        var rowNumber = 2;
        foreach (var row in recurrence.Rows)
        {
            sheet.Cell(rowNumber, 1).Value = row.ClienteUnico;
            sheet.Cell(rowNumber, 2).Value = row.EstadoPlan;
            sheet.Cell(rowNumber, 3).Value = row.Agente;
            sheet.Cell(rowNumber, 4).Value = row.EstatusDespacho;

            for (var periodIndex = 0; periodIndex < recurrence.Periods.Count; periodIndex++)
            {
                var cell = sheet.Cell(rowNumber, periodIndex + 5);
                var amount = row.Payments.GetValueOrDefault(recurrence.Periods[periodIndex].Id);
                if (amount is not null)
                {
                    cell.Value = amount.Value;
                    cell.Style.NumberFormat.Format = "$#,##0.00";
                }
            }

            rowNumber++;
        }

        var lastColumn = headers.Length;
        var tableRange = sheet.Range(1, 1, Math.Max(rowNumber - 1, 1), lastColumn);
        var table = tableRange.CreateTable("Recurrencia");
        table.Theme = XLTableTheme.TableStyleMedium2;
        table.ShowAutoFilter = true;
        sheet.SheetView.FreezeRows(1);
        sheet.Columns(1, 4).AdjustToContents();
        sheet.Columns(5, lastColumn).Width = 17;
        sheet.Row(1).Height = 24;

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        return stream.ToArray();
    }
}