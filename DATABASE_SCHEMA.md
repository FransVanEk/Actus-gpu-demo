# Database Schema for Contract Sources

This document describes the expected database schema for loading contracts from a database.

## Overview

ActusDesk supports loading contracts from any ADO.NET-compatible database (SQL Server, PostgreSQL, MySQL, SQLite, etc.). The database sources use streaming reads to efficiently handle large datasets.

## PAM Contracts Table

### Table Name
Default: `PamContracts` (configurable)

### Required Columns

| Column Name | Data Type | Nullable | Description |
|------------|-----------|----------|-------------|
| `ContractId` | VARCHAR/TEXT | No | Unique contract identifier |
| `Currency` | VARCHAR(3) | No | ISO currency code (e.g., USD, EUR) |
| `StatusDate` | DATE/DATETIME | No | Status/observation date |
| `InitialExchangeDate` | DATE/DATETIME | Yes | Initial exchange date (nullable) |
| `MaturityDate` | DATE/DATETIME | No | Maturity/end date |
| `NotionalPrincipal` | DECIMAL/DOUBLE | No | Notional principal amount |
| `NominalInterestRate` | DECIMAL/DOUBLE | Yes | Annual interest rate (as decimal, e.g., 0.05 for 5%) |
| `ContractRole` | VARCHAR(10) | No | Contract role: "RPA" (receiver) or "RPL" (payer) |
| `DayCountConvention` | VARCHAR(20) | No | Day count convention (e.g., "30E/360", "ACT/360") |
| `NotionalScalingMultiplier` | DECIMAL/DOUBLE | Yes | Notional scaling multiplier (default 1.0) |
| `InterestScalingMultiplier` | DECIMAL/DOUBLE | Yes | Interest scaling multiplier (default 1.0) |
| `ContractPerformance` | VARCHAR(10) | Yes | Performance status (default "PF") |

### Example SQL (SQL Server)

```sql
CREATE TABLE PamContracts (
    ContractId VARCHAR(50) PRIMARY KEY,
    Currency VARCHAR(3) NOT NULL,
    StatusDate DATE NOT NULL,
    InitialExchangeDate DATE NULL,
    MaturityDate DATE NOT NULL,
    NotionalPrincipal DECIMAL(18, 2) NOT NULL,
    NominalInterestRate DECIMAL(10, 6) NULL,
    ContractRole VARCHAR(10) NOT NULL,
    DayCountConvention VARCHAR(20) NOT NULL,
    NotionalScalingMultiplier DECIMAL(10, 6) NULL DEFAULT 1.0,
    InterestScalingMultiplier DECIMAL(10, 6) NULL DEFAULT 1.0,
    ContractPerformance VARCHAR(10) NULL DEFAULT 'PF',
    INDEX IX_StatusDate (StatusDate),
    INDEX IX_Currency (Currency)
);
```

### Example SQL (PostgreSQL)

```sql
CREATE TABLE PamContracts (
    ContractId VARCHAR(50) PRIMARY KEY,
    Currency VARCHAR(3) NOT NULL,
    StatusDate DATE NOT NULL,
    InitialExchangeDate DATE,
    MaturityDate DATE NOT NULL,
    NotionalPrincipal NUMERIC(18, 2) NOT NULL,
    NominalInterestRate NUMERIC(10, 6),
    ContractRole VARCHAR(10) NOT NULL,
    DayCountConvention VARCHAR(20) NOT NULL,
    NotionalScalingMultiplier NUMERIC(10, 6) DEFAULT 1.0,
    InterestScalingMultiplier NUMERIC(10, 6) DEFAULT 1.0,
    ContractPerformance VARCHAR(10) DEFAULT 'PF'
);

CREATE INDEX ix_pamcontracts_statusdate ON PamContracts(StatusDate);
CREATE INDEX ix_pamcontracts_currency ON PamContracts(Currency);
```

## ANN Contracts Table

### Table Name
Default: `AnnContracts` (configurable)

### Required Columns

All columns from PAM Contracts table, plus:

| Column Name | Data Type | Nullable | Description |
|------------|-----------|----------|-------------|
| `NextPrincipalRedemptionPayment` | DECIMAL/DOUBLE | Yes | Next principal redemption payment amount |

### Example SQL (SQL Server)

```sql
CREATE TABLE AnnContracts (
    ContractId VARCHAR(50) PRIMARY KEY,
    Currency VARCHAR(3) NOT NULL,
    StatusDate DATE NOT NULL,
    InitialExchangeDate DATE NULL,
    MaturityDate DATE NOT NULL,
    NotionalPrincipal DECIMAL(18, 2) NOT NULL,
    NominalInterestRate DECIMAL(10, 6) NULL,
    ContractRole VARCHAR(10) NOT NULL,
    DayCountConvention VARCHAR(20) NOT NULL,
    NotionalScalingMultiplier DECIMAL(10, 6) NULL DEFAULT 1.0,
    InterestScalingMultiplier DECIMAL(10, 6) NULL DEFAULT 1.0,
    ContractPerformance VARCHAR(10) NULL DEFAULT 'PF',
    NextPrincipalRedemptionPayment DECIMAL(18, 2) NULL,
    INDEX IX_StatusDate (StatusDate),
    INDEX IX_Currency (Currency)
);
```

## Usage Examples

### SQL Server

```csharp
using System.Data.SqlClient;
using ActusDesk.IO;

// Create database connection factory
var connectionString = "Server=localhost;Database=Contracts;Integrated Security=true;";
var db = new ContractDatabase(
    connectionString,
    cs => new SqlConnection(cs)
);

// Create PAM source with filtering
var pamSource = new PamDatabaseSource(
    db,
    tableName: "PamContracts",
    whereClause: "StatusDate >= '2024-01-01' AND Currency = 'USD'",
    batchSize: 10000
);

// Load contracts
var contracts = await pamSource.GetContractsAsync();
```

### PostgreSQL

```csharp
using Npgsql;
using ActusDesk.IO;

// Create database connection factory
var connectionString = "Host=localhost;Database=contracts;Username=user;Password=pass;";
var db = new ContractDatabase(
    connectionString,
    cs => new NpgsqlConnection(cs)
);

// Create ANN source
var annSource = new AnnDatabaseSource(
    db,
    tableName: "AnnContracts",
    whereClause: "",  // Load all
    batchSize: 5000
);

// Load contracts
var contracts = await annSource.GetContractsAsync();
```

### With GPU Provider

```csharp
// Load from database and transfer to GPU
var pamDeviceContracts = await pamGpuProvider.LoadToGpuAsync(
    pamSource,
    gpuContext,
    cancellationToken
);
```

## Performance Recommendations

1. **Indexing**: Create indexes on frequently filtered columns (StatusDate, Currency, MaturityDate)
2. **Batch Size**: Adjust `batchSize` parameter based on available memory (default: 10,000)
3. **WHERE Clauses**: Use efficient WHERE clauses to filter at database level
4. **Connection Pooling**: Ensure connection pooling is enabled in your connection string
5. **Read-Only**: Use read-only connection options for better performance

## Sample Data

### Insert Sample PAM Contracts (SQL Server)

```sql
INSERT INTO PamContracts VALUES
('PAM-001', 'USD', '2024-01-01', '2024-01-15', '2029-01-15', 1000000, 0.05, 'RPL', '30E/360', 1.0, 1.0, 'PF'),
('PAM-002', 'EUR', '2024-01-01', '2024-02-01', '2028-02-01', 500000, 0.03, 'RPA', 'ACT/360', 1.0, 1.0, 'PF'),
('PAM-003', 'GBP', '2024-01-01', '2024-03-01', '2030-03-01', 750000, 0.04, 'RPL', 'ACT/365', 1.0, 1.0, 'PF');
```

### Insert Sample ANN Contracts (SQL Server)

```sql
INSERT INTO AnnContracts VALUES
('ANN-001', 'USD', '2024-01-01', '2024-01-15', '2029-01-15', 1000000, 0.05, 'RPL', '30E/360', 1.0, 1.0, 'PF', 5000),
('ANN-002', 'EUR', '2024-01-01', '2024-02-01', '2028-02-01', 500000, 0.03, 'RPA', 'ACT/360', 1.0, 1.0, 'PF', 3000);
```

## Integration with ContractsService

The database sources integrate seamlessly with the existing ContractsService:

```csharp
// Load PAM contracts from database
await contractsService.LoadFromSourceAsync(pamDatabaseSource, cancellationToken);

// Load ANN contracts from database
await contractsService.LoadAnnFromSourceAsync(annDatabaseSource, cancellationToken);

// Or use composite source to load from multiple sources
var compositeSource = new PamCompositeSource(
    new PamFileSource("data/contracts.json"),
    pamDatabaseSource
);
await contractsService.LoadFromSourceAsync(compositeSource, cancellationToken);
```

## Troubleshooting

### Connection Errors
- Verify connection string is correct
- Ensure database server is accessible
- Check firewall rules and network connectivity

### Performance Issues
- Add indexes on filtered columns
- Reduce batch size if memory is limited
- Use WHERE clauses to filter at database level
- Enable connection pooling

### Schema Mismatch
- Verify column names match exactly (case-sensitive in some databases)
- Check data types are compatible
- Ensure nullable columns are properly marked
