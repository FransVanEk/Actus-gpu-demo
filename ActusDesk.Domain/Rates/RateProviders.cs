namespace ActusDesk.Domain.Rates;

/// <summary>
/// Simple constant rate provider for testing
/// </summary>
public sealed class ConstantRateProvider : IRateProvider
{
    private readonly float _constantRate;

    public ConstantRateProvider(float constantRate = 0.03f)
    {
        _constantRate = constantRate;
    }

    public float GetRate(string curve, int tenorMonths, DateOnly asOf)
    {
        return _constantRate;
    }
}

/// <summary>
/// Curve-based rate provider with linear interpolation
/// </summary>
public sealed class CurveRateProvider : IRateProvider
{
    private readonly Dictionary<string, RateCurve> _curves;

    public CurveRateProvider()
    {
        _curves = new Dictionary<string, RateCurve>();
    }

    public void AddCurve(string name, RateCurve curve)
    {
        _curves[name] = curve;
    }

    public float GetRate(string curve, int tenorMonths, DateOnly asOf)
    {
        if (!_curves.TryGetValue(curve, out var rateCurve))
        {
            throw new ArgumentException($"Curve not found: {curve}");
        }

        return rateCurve.GetRate(tenorMonths, asOf);
    }
}

/// <summary>
/// Rate curve with tenor points
/// </summary>
public sealed class RateCurve
{
    private readonly SortedDictionary<int, float> _tenorRates; // tenor months -> rate
    private readonly DateOnly _asOfDate;

    public RateCurve(DateOnly asOfDate)
    {
        _asOfDate = asOfDate;
        _tenorRates = new SortedDictionary<int, float>();
    }

    public void AddPoint(int tenorMonths, float rate)
    {
        _tenorRates[tenorMonths] = rate;
    }

    public float GetRate(int tenorMonths, DateOnly requestDate)
    {
        // Simple: return exact match if exists
        if (_tenorRates.TryGetValue(tenorMonths, out float exactRate))
        {
            return exactRate;
        }

        // Linear interpolation between surrounding points
        int lowerTenor = 0;
        float lowerRate = 0f;
        int upperTenor = int.MaxValue;
        float upperRate = 0f;

        foreach (var kvp in _tenorRates)
        {
            if (kvp.Key < tenorMonths && kvp.Key > lowerTenor)
            {
                lowerTenor = kvp.Key;
                lowerRate = kvp.Value;
            }
            if (kvp.Key > tenorMonths && kvp.Key < upperTenor)
            {
                upperTenor = kvp.Key;
                upperRate = kvp.Value;
                break;
            }
        }

        if (upperTenor == int.MaxValue)
        {
            // Extrapolate flat from last point
            return lowerRate;
        }

        if (lowerTenor == 0)
        {
            // Extrapolate flat from first point
            return upperRate;
        }

        // Linear interpolation
        float weight = (float)(tenorMonths - lowerTenor) / (upperTenor - lowerTenor);
        return lowerRate + weight * (upperRate - lowerRate);
    }

    public static RateCurve CreateFlatCurve(DateOnly asOfDate, float flatRate)
    {
        var curve = new RateCurve(asOfDate);
        // Add points for common tenors
        int[] tenors = { 1, 3, 6, 12, 24, 36, 60, 84, 120, 240, 360 };
        foreach (int tenor in tenors)
        {
            curve.AddPoint(tenor, flatRate);
        }
        return curve;
    }
}
