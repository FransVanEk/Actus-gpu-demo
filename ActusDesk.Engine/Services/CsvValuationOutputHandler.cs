using Microsoft.Extensions.Logging;
using System.Globalization;
using System.Text;

namespace ActusDesk.Engine.Services;

/// <summary>
/// CSV output handler for valuation results
/// Writes all events with scenario name, date, and event details as columns
/// </summary>
public class CsvValuationOutputHandler : IValuationOutputHandler
{
    private readonly ILogger<CsvValuationOutputHandler> _logger;

    public string FileExtension => ".csv";
    public string Description => "CSV (Comma Separated Values)";

    public CsvValuationOutputHandler(ILogger<CsvValuationOutputHandler> logger)
    {
        _logger = logger;
    }

    public async Task WriteAsync(ValuationResults results, string outputPath, CancellationToken ct = default)
    {
        try
        {
            _logger.LogInformation("Writing CSV output to: {Path}", outputPath);

            using var writer = new StreamWriter(outputPath, false, Encoding.UTF8);

            // Write header
            await writer.WriteLineAsync("Scenario,Date,ContractId,ContractType,EventType,Payoff,PresentValue,Currency");

            // Write all events grouped by day
            int eventCount = 0;
            foreach (var dayValue in results.DayEventValues.OrderBy(d => d.Date))
            {
                foreach (var evt in dayValue.Events.OrderBy(e => e.ScenarioName).ThenBy(e => e.ContractId))
                {
                    ct.ThrowIfCancellationRequested();

                    // Escape scenario name and contract ID in case they contain commas or quotes
                    var scenario = EscapeCsvField(evt.ScenarioName);
                    var date = evt.EventDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
                    var contractId = EscapeCsvField(evt.ContractId);
                    var contractType = evt.ContractType;
                    var eventType = evt.EventType;
                    var payoff = evt.Payoff.ToString(CultureInfo.InvariantCulture);
                    var pv = evt.PresentValue.ToString(CultureInfo.InvariantCulture);
                    var currency = evt.Currency;

                    await writer.WriteLineAsync($"{scenario},{date},{contractId},{contractType},{eventType},{payoff},{pv},{currency}");
                    eventCount++;
                }
            }

            _logger.LogInformation("Successfully wrote {Count} events to CSV file", eventCount);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error writing CSV output to {Path}", outputPath);
            throw;
        }
    }

    private string EscapeCsvField(string field)
    {
        if (string.IsNullOrEmpty(field))
            return field;

        // If field contains comma, quote, or newline, wrap in quotes and escape internal quotes
        if (field.Contains(',') || field.Contains('"') || field.Contains('\n') || field.Contains('\r'))
        {
            return $"\"{field.Replace("\"", "\"\"")}\"";
        }

        return field;
    }
}
