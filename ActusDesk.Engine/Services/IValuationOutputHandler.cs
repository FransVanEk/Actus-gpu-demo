namespace ActusDesk.Engine.Services;

/// <summary>
/// Interface for handling valuation output
/// Follows Open/Closed Principle - new output formats can be added without modifying existing code
/// </summary>
public interface IValuationOutputHandler
{
    /// <summary>
    /// Write valuation results to output
    /// </summary>
    /// <param name="results">Valuation results to output</param>
    /// <param name="outputPath">Path to write output to</param>
    /// <param name="ct">Cancellation token</param>
    Task WriteAsync(ValuationResults results, string outputPath, CancellationToken ct = default);

    /// <summary>
    /// Get the file extension for this output format (e.g., ".csv", ".json")
    /// </summary>
    string FileExtension { get; }

    /// <summary>
    /// Get the description of this output format
    /// </summary>
    string Description { get; }
}
