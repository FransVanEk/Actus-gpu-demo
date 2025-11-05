using ILGPU;
using ILGPU.Runtime;
using ILGPU.Runtime.Cuda;
using Microsoft.Extensions.Logging;

namespace ActusDesk.Gpu;

/// <summary>
/// GPU context managing ILGPU accelerator and streams
/// Singleton - lives for entire application lifetime
/// </summary>
public class GpuContext : IDisposable
{
    private readonly ILogger<GpuContext>? _logger;
    private readonly Context _context;
    private readonly Accelerator _accelerator;
    private readonly AcceleratorStream _loadStream;
    private readonly AcceleratorStream _computeStream;
    private readonly AcceleratorStream _storeStream;

    public GpuContext(ILogger<GpuContext>? logger = null)
    {
        _logger = logger;
        _logger?.LogInformation("Initializing GPU context");

        _context = Context.Create(builder => builder.Default().EnableAlgorithms());
        
        try
        {
            // Try to get CUDA accelerator
            _accelerator = _context.GetPreferredDevice(preferCPU: false)
                .CreateAccelerator(_context);
            _logger?.LogInformation("GPU Accelerator: {Name}, Memory: {Memory:N0} bytes", 
                _accelerator.Name, _accelerator.MemorySize);
        }
        catch
        {
            // Fallback to CPU if no GPU available
            _logger?.LogWarning("No GPU available, falling back to CPU accelerator");
            _accelerator = _context.GetPreferredDevice(preferCPU: true)
                .CreateAccelerator(_context);
        }

        // Create three streams for overlap: H2D, Compute, D2H
        _loadStream = _accelerator.CreateStream();
        _computeStream = _accelerator.CreateStream();
        _storeStream = _accelerator.CreateStream();
    }

    public Context Context => _context;
    public Accelerator Accelerator => _accelerator;
    public AcceleratorStream LoadStream => _loadStream;
    public AcceleratorStream ComputeStream => _computeStream;
    public AcceleratorStream StoreStream => _storeStream;

    public void Dispose()
    {
        _logger?.LogInformation("Disposing GPU context");
        _storeStream?.Dispose();
        _computeStream?.Dispose();
        _loadStream?.Dispose();
        _accelerator?.Dispose();
        _context?.Dispose();
    }
}

/// <summary>
/// Device-side contract data in SoA format
/// Lives for entire application lifetime, reused across valuation runs
/// </summary>
public class DeviceContracts : IDisposable
{
    public int Count { get; init; }
    public MemoryBuffer1D<float, Stride1D.Dense>? Notional { get; init; }
    public MemoryBuffer1D<float, Stride1D.Dense>? Rate { get; init; }
    public MemoryBuffer1D<int, Stride1D.Dense>? StartYMD { get; init; }
    public MemoryBuffer1D<int, Stride1D.Dense>? MaturityYMD { get; init; }
    public MemoryBuffer1D<byte, Stride1D.Dense>? TypeCode { get; init; }

    public void Dispose()
    {
        Notional?.Dispose();
        Rate?.Dispose();
        StartYMD?.Dispose();
        MaturityYMD?.Dispose();
        TypeCode?.Dispose();
    }
}

/// <summary>
/// Device-side scenario data
/// </summary>
public class ScenarioDevice : IDisposable
{
    public int ScenarioCount { get; init; }
    public MemoryBuffer1D<float, Stride1D.Dense>? CurveData { get; init; }

    public void Dispose()
    {
        CurveData?.Dispose();
    }
}
