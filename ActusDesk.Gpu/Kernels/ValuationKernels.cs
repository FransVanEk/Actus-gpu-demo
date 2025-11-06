using ILGPU;
using ILGPU.Runtime;
using ILGPU.Algorithms;

namespace ActusDesk.Gpu.Kernels;

/// <summary>
/// GPU kernels for ACTUS contract valuation
/// </summary>
public static class ValuationKernels
{
    /// <summary>
    /// Simple PV calculation kernel for fixed-income contracts
    /// Each thread processes one contract
    /// </summary>
    public static void ValueContractsSimple(
        Index1D index,
        ArrayView<float> notional,
        ArrayView<float> rate,
        ArrayView<int> startYmd,
        ArrayView<int> matYmd,
        ArrayView<byte> typeCode,
        float discountRate,
        ArrayView<float> outPV)
    {
        int i = index;
        if (i >= notional.Length)
            return;

        // Simple PV calculation: notional * (1 + rate * years) / (1 + discount)^years
        int days = matYmd[i] - startYmd[i];
        float years = days / 365.25f;
        
        float fv = notional[i] * (1.0f + rate[i] * years);
        float pv = fv / XMath.Pow(1.0f + discountRate, years);
        
        outPV[i] = pv;
    }

    /// <summary>
    /// Apply parallel rate shock to curve
    /// </summary>
    public static void ApplyParallelShock(
        Index1D index,
        ArrayView<float> baseCurve,
        float shockBps,
        ArrayView<float> outCurve)
    {
        int i = index;
        if (i >= baseCurve.Length)
            return;

        outCurve[i] = baseCurve[i] + shockBps / 10000.0f;
    }

    /// <summary>
    /// Aggregate results by contract type
    /// Uses atomic operations for thread-safe accumulation
    /// </summary>
    public static void AggregateByType(
        Index1D index,
        ArrayView<float> pv,
        ArrayView<byte> typeCode,
        ArrayView<float> sumByType,
        ArrayView<int> countByType)
    {
        int i = index;
        if (i >= pv.Length)
            return;

        byte type = typeCode[i];
        if (type < sumByType.Length)
        {
            Atomic.Add(ref sumByType[(int)type], pv[i]);
            Atomic.Add(ref countByType[(int)type], 1);
        }
    }
}
