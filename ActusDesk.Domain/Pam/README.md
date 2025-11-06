# ACTUS PAM Implementation

This directory contains a complete implementation of the ACTUS PAM (Principal At Maturity) contract type, based on the Java reference implementation from `org.actus.contracts.PrincipalAtMaturity`.

## Overview

The implementation is designed to be:
- **GPU-friendly**: Flat data structures with value types
- **Minimal**: Only PAM behavior, no generic infrastructure
- **Accurate**: Mirrors Java ACTUS semantics
- **Testable**: Comprehensive unit and integration tests

## Components

### 1. PamContractModel
A flat, serializable class containing all fields needed for PAM scheduling and application:
- Contract identification (ID, currency)
- Dates (status, IED, maturity, purchase, termination, capitalization)
- Cycle definitions for interest, rate resets, fees, scaling
- Financial terms (notional, rates, multipliers)
- Conventions (day count, business day, EOM)

### 2. PamEvent
GPU-friendly event structure with:
- Event date and type
- Contract ID and currency
- Business day convention code

Supports 11 event types:
- **AD**: Analysis Date
- **IED**: Initial Exchange Date
- **MD**: Maturity Date
- **PRD**: Purchase Date
- **IP**: Interest Payment
- **IPCI**: Interest Capitalization
- **RR**: Rate Reset
- **RRF**: Rate Reset Fixed
- **FP**: Fee Payment
- **SC**: Scaling Index
- **TD**: Termination Date

### 3. ScheduleFactory
Generates date schedules from cycle strings:
- Supports: "1M", "3M", "6M", "1Y", "10D", "2W", etc.
- Handles ISO 8601 duration format (with "P" prefix)
- End-of-month convention support

### 4. PamScheduler
Core scheduling logic that generates events based on contract terms:

1. Creates IED and MD events
2. Adds PRD (purchase) if present
3. Generates interest payment schedule:
   - Creates IP events based on cycle
   - Handles capitalization (converts IP → IPCI)
   - Fallback to annual schedule if no cycle specified
4. Generates rate reset events:
   - Creates RR events on schedule
   - Converts first RR to RRF if NextResetRate present
5. Generates fee payment events
6. Generates scaling index events
7. Handles termination (adds TD, removes events after termination)
8. Filters events by status date and "to" date
9. Sorts events by date and type

### 5. PamState
Contract state tracking:
- Notional principal
- Nominal interest rate
- Accrued interest
- Fee accrued
- Scaling multipliers
- Status date
- Contract performance

Includes `InitFrom()` method for proper state initialization from model.

### 6. PamEventApplier
Applies events to state in chronological order:

Event behaviors:
- **IED**: Set notional and rate
- **MD**: Pay interest, redeem principal (notional → 0)
- **PRD**: Adjust for purchase (simplified)
- **IP**: Pay accrued interest, reset to 0
- **IPCI**: Capitalize interest into notional
- **RR/RRF**: Update rate (scenario-aware)
- **FP**: Pay fees
- **SC**: Apply scaling
- **TD**: Terminate contract

### 7. IPamScenario
Interface for scenario-aware rate overrides:
```csharp
bool TryGetRateOverride(string contractId, DateTime eventDate, out double rate);
```

Allows scenarios to provide alternative rates for rate reset events.

## Usage Examples

### Simple Loan
```csharp
var loan = new PamContractModel
{
    ContractId = "LOAN-001",
    Currency = "USD",
    StatusDate = new DateTime(2024, 1, 1),
    InitialExchangeDate = new DateTime(2024, 1, 1),
    MaturityDate = new DateTime(2029, 1, 1),
    NotionalPrincipal = 1000000,
    NominalInterestRate = 0.05,
    ContractRole = "RPL", // Borrower
    CycleOfInterestPayment = "3M", // Quarterly
    CycleAnchorDateOfInterestPayment = new DateTime(2024, 4, 1)
};

// Generate events
var events = PamScheduler.Schedule(new DateTime(2030, 1, 1), loan);

// Initialize and apply
var state = PamState.InitFrom(loan);
PamEventApplier.ApplyEvents(events, loan, null, state);
```

### Floating Rate with Scenario
```csharp
var loan = new PamContractModel
{
    // ... basic setup ...
    CycleOfRateReset = "1Y",
    CycleAnchorDateOfRateReset = new DateTime(2025, 1, 1)
};

var stressScenario = new MyScenario(0.07); // 7% rate shock

var events = PamScheduler.Schedule(to, loan);
var state = PamState.InitFrom(loan);
PamEventApplier.ApplyEvents(events, loan, stressScenario, state);
```

### Bond with Capitalization
```csharp
var bond = new PamContractModel
{
    // ... basic setup ...
    CycleOfInterestPayment = "6M",
    CapitalizationEndDate = new DateTime(2026, 1, 1) // Capitalize for 2 years
};

var events = PamScheduler.Schedule(to, bond);
// All IP events before cap date are converted to IPCI
```

## Testing

Comprehensive test suite with 60 tests:
- **ScheduleFactoryTests** (12 tests): Cycle parsing and schedule generation
- **PamSchedulerTests** (11 tests): Event scheduling scenarios
- **PamStateTests** (6 tests): State initialization
- **PamEventApplierTests** (10 tests): Event application
- **PamIntegrationTests** (6 tests): Full contract lifecycles
- **PamExamples** (3 tests): Usage examples

Run tests:
```bash
dotnet test ActusDesk.Tests
```

## GPU Readiness

The implementation is designed for easy GPU porting:
- Flat data structures (no inheritance, minimal nesting)
- Value types where possible
- Minimal allocations in hot paths
- Separate scheduling and application phases
- Event lists can be converted to arrays

Next steps for GPU:
1. Convert `PamContractModel` to struct-of-arrays (SoA)
2. Serialize events to flat arrays
3. Implement state machine as GPU kernel
4. Batch-process multiple contracts in parallel

## Contract Role Convention

- **RPA** (Real Position Asset): Lender/Receiver → Notional = -1 × Principal
- **RPL** (Real Position Liability): Borrower/Payer → Notional = +1 × Principal

From the lender's perspective (RPA):
- Initial exchange: Cash outflow (-notional)
- Maturity: Cash inflow (+notional + interest)

From the borrower's perspective (RPL):
- Initial exchange: Cash inflow (+notional)
- Maturity: Cash outflow (-notional - interest)

## References

- ACTUS Financial Research Foundation: https://www.actusfrf.org/
- Java Reference Implementation: org.actus.contracts.PrincipalAtMaturity
- ACTUS Data Dictionary: https://www.actusfrf.org/dictionary
