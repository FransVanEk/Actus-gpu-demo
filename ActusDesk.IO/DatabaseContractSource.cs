using ActusDesk.Domain.Pam;
using ActusDesk.Domain.Ann;
using System.Data;
using System.Data.Common;

namespace ActusDesk.IO;

/// <summary>
/// Interface for database connection configuration
/// </summary>
public interface IContractDatabase
{
    /// <summary>
    /// Get a database connection
    /// </summary>
    Task<DbConnection> GetConnectionAsync(CancellationToken ct = default);
}

/// <summary>
/// Generic database connection provider
/// Supports any ADO.NET provider (SQL Server, PostgreSQL, MySQL, etc.)
/// </summary>
public class ContractDatabase : IContractDatabase
{
    private readonly string _connectionString;
    private readonly Func<string, DbConnection> _connectionFactory;

    public ContractDatabase(string connectionString, Func<string, DbConnection> connectionFactory)
    {
        _connectionString = connectionString;
        _connectionFactory = connectionFactory;
    }

    public async Task<DbConnection> GetConnectionAsync(CancellationToken ct = default)
    {
        var connection = _connectionFactory(_connectionString);
        await connection.OpenAsync(ct);
        return connection;
    }
}

/// <summary>
/// Database-backed PAM contract source with streaming support
/// Loads contracts in batches for efficient memory usage
/// </summary>
public class PamDatabaseSource : IPamContractSource
{
    private readonly IContractDatabase _database;
    private readonly string _tableName;
    private readonly string _whereClause;
    private readonly int _batchSize;

    public PamDatabaseSource(
        IContractDatabase database,
        string tableName = "PamContracts",
        string whereClause = "",
        int batchSize = 10000)
    {
        _database = database;
        _tableName = tableName;
        _whereClause = whereClause;
        _batchSize = batchSize;
    }

    public async Task<IEnumerable<PamContractModel>> GetContractsAsync(CancellationToken ct = default)
    {
        var contracts = new List<PamContractModel>();

        await using var connection = await _database.GetConnectionAsync(ct);
        await using var command = connection.CreateCommand();

        // Build query
        var whereFilter = string.IsNullOrWhiteSpace(_whereClause) ? "" : $" WHERE {_whereClause}";
        command.CommandText = $@"
            SELECT 
                ContractId,
                Currency,
                StatusDate,
                InitialExchangeDate,
                MaturityDate,
                NotionalPrincipal,
                NominalInterestRate,
                ContractRole,
                DayCountConvention,
                NotionalScalingMultiplier,
                InterestScalingMultiplier,
                ContractPerformance
            FROM {_tableName}
            {whereFilter}
            ORDER BY ContractId";

        await using var reader = await command.ExecuteReaderAsync(ct);

        // Read contracts in batches
        while (await reader.ReadAsync(ct))
        {
            var contract = new PamContractModel
            {
                ContractId = reader.GetString(0),
                Currency = reader.GetString(1),
                StatusDate = reader.GetDateTime(2),
                InitialExchangeDate = reader.IsDBNull(3) ? null : reader.GetDateTime(3),
                MaturityDate = reader.GetDateTime(4),
                NotionalPrincipal = reader.GetDouble(5),
                NominalInterestRate = reader.IsDBNull(6) ? null : reader.GetDouble(6),
                ContractRole = reader.GetString(7),
                DayCountConvention = reader.GetString(8),
                NotionalScalingMultiplier = reader.IsDBNull(9) ? 1.0 : reader.GetDouble(9),
                InterestScalingMultiplier = reader.IsDBNull(10) ? 1.0 : reader.GetDouble(10),
                ContractPerformance = reader.IsDBNull(11) ? "PF" : reader.GetString(11)
            };

            contracts.Add(contract);

            // Allow cancellation checks periodically
            if (contracts.Count % _batchSize == 0)
            {
                ct.ThrowIfCancellationRequested();
            }
        }

        return contracts;
    }
}

/// <summary>
/// Database-backed ANN contract source with streaming support
/// Loads contracts in batches for efficient memory usage
/// </summary>
public class AnnDatabaseSource : IAnnContractSource
{
    private readonly IContractDatabase _database;
    private readonly string _tableName;
    private readonly string _whereClause;
    private readonly int _batchSize;

    public AnnDatabaseSource(
        IContractDatabase database,
        string tableName = "AnnContracts",
        string whereClause = "",
        int batchSize = 10000)
    {
        _database = database;
        _tableName = tableName;
        _whereClause = whereClause;
        _batchSize = batchSize;
    }

    public async Task<IEnumerable<AnnContractModel>> GetContractsAsync(CancellationToken ct = default)
    {
        var contracts = new List<AnnContractModel>();

        await using var connection = await _database.GetConnectionAsync(ct);
        await using var command = connection.CreateCommand();

        // Build query
        var whereFilter = string.IsNullOrWhiteSpace(_whereClause) ? "" : $" WHERE {_whereClause}";
        command.CommandText = $@"
            SELECT 
                ContractId,
                Currency,
                StatusDate,
                InitialExchangeDate,
                MaturityDate,
                NotionalPrincipal,
                NominalInterestRate,
                ContractRole,
                DayCountConvention,
                NotionalScalingMultiplier,
                InterestScalingMultiplier,
                ContractPerformance,
                NextPrincipalRedemptionPayment
            FROM {_tableName}
            {whereFilter}
            ORDER BY ContractId";

        await using var reader = await command.ExecuteReaderAsync(ct);

        // Read contracts in batches
        while (await reader.ReadAsync(ct))
        {
            var contract = new AnnContractModel
            {
                ContractId = reader.GetString(0),
                Currency = reader.GetString(1),
                StatusDate = reader.GetDateTime(2),
                InitialExchangeDate = reader.IsDBNull(3) ? null : reader.GetDateTime(3),
                MaturityDate = reader.GetDateTime(4),
                NotionalPrincipal = reader.GetDouble(5),
                NominalInterestRate = reader.IsDBNull(6) ? null : reader.GetDouble(6),
                ContractRole = reader.GetString(7),
                DayCountConvention = reader.GetString(8),
                NotionalScalingMultiplier = reader.IsDBNull(9) ? 1.0 : reader.GetDouble(9),
                InterestScalingMultiplier = reader.IsDBNull(10) ? 1.0 : reader.GetDouble(10),
                ContractPerformance = reader.IsDBNull(11) ? "PF" : reader.GetString(11),
                NextPrincipalRedemptionPayment = reader.IsDBNull(12) ? null : reader.GetDouble(12)
            };

            contracts.Add(contract);

            // Allow cancellation checks periodically
            if (contracts.Count % _batchSize == 0)
            {
                ct.ThrowIfCancellationRequested();
            }
        }

        return contracts;
    }
}
