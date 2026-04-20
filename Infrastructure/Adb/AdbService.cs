using System.Diagnostics;
using AdvancedSharpAdbClient;
using AdvancedSharpAdbClient.DeviceCommands;
using AdvancedSharpAdbClient.Models;
using Core.Abstractions;
using Core.Models;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Png;
using SixLabors.ImageSharp.PixelFormats;

namespace Infrastructure.Adb;

/// <summary>
/// 基于 AdvancedSharpAdbClient 的跨平台 ADB 服务实现。
/// </summary>
public sealed class AdbService : IAdbService
{
    private readonly AdbClient _adbClient;
    private readonly AdbServer _adbServer;
    private readonly string? _adbExecutablePath;
    private readonly SemaphoreSlim _initializeLock = new(1, 1);
    private bool _serverStarted;

    public AdbService(string? adbExecutablePath = null, AdbClient? adbClient = null, AdbServer? adbServer = null)
    {
        _adbExecutablePath = AdbExecutableLocator.Resolve(adbExecutablePath);
        _adbClient = adbClient ?? new AdbClient();
        _adbServer = adbServer ?? new AdbServer();
    }

    public event EventHandler<OperationLogEntry>? OperationLogged;

    public async Task<IReadOnlyList<AdbDevice>> ScanDevicesAsync(CancellationToken cancellationToken = default)
    {
        await EnsureServerStartedAsync(cancellationToken);

        return await ExecuteLoggedAsync(
            "ADB",
            "扫描设备",
            async token =>
            {
                var devices = await _adbClient.GetDevicesAsync(token);
                return devices.Select(MapDevice).ToArray();
            },
            result => $"找到 {result.Length} 台设备。",
            cancellationToken);
    }

    public async Task<AdbScreenshot> CaptureScreenshotAsync(AdbDevice device, CancellationToken cancellationToken = default)
    {
        await EnsureServerStartedAsync(cancellationToken);

        return await ExecuteLoggedAsync(
            "ADB",
            $"截图 {device.Serial}",
            async token =>
            {
                var adbDevice = await FindDeviceBySerialAsync(device.Serial, token);
                var framebuffer = await _adbClient.GetFrameBufferAsync(adbDevice, token);
                return await ConvertFramebufferToScreenshotAsync(framebuffer, token);
            },
            result => $"截图完成：{result.Width}x{result.Height}。",
            cancellationToken);
    }

    public async Task<string> DumpUiHierarchyAsync(AdbDevice device, CancellationToken cancellationToken = default)
    {
        await EnsureServerStartedAsync(cancellationToken);

        return await ExecuteLoggedAsync(
            "ADB",
            $"拉取 UI 树 {device.Serial}",
            async token =>
            {
                var adbDevice = await FindDeviceBySerialAsync(device.Serial, token);
                var client = new DeviceClient(_adbClient, adbDevice);
                var xmlDocument = await client.DumpScreenAsync(token);

                return xmlDocument?.OuterXml
                       ?? throw new InvalidOperationException("未获取到有效的 UI dump XML。");
            },
            result => $"UI 树拉取完成：{result.Length} 个字符。",
            cancellationToken);
    }

    public async Task<bool> IsDeviceOnlineAsync(AdbDevice device, CancellationToken cancellationToken = default)
    {
        await EnsureServerStartedAsync(cancellationToken);

        var devices = await _adbClient.GetDevicesAsync(cancellationToken);
        return devices.Any(current =>
            string.Equals(current.Serial, device.Serial, StringComparison.OrdinalIgnoreCase) &&
            current.State == DeviceState.Online);
    }

    public async Task<string> ConnectDeviceAsync(string host, int port, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(host))
        {
            throw new ArgumentException("host 不能为空。", nameof(host));
        }

        if (port is <= 0 or > 65535)
        {
            throw new ArgumentOutOfRangeException(nameof(port), "port 必须在 1-65535 之间。");
        }

        await EnsureServerStartedAsync(cancellationToken);

        return await ExecuteLoggedAsync(
            "ADB",
            $"连接 TCP/IP 设备 {host}:{port}",
            token => _adbClient.ConnectAsync(host, port, token),
            result => $"连接结果：{result}",
            cancellationToken);
    }

    private async Task EnsureServerStartedAsync(CancellationToken cancellationToken)
    {
        if (_serverStarted)
        {
            return;
        }

        await _initializeLock.WaitAsync(cancellationToken);

        try
        {
            if (_serverStarted)
            {
                return;
            }

            if (string.IsNullOrWhiteSpace(_adbExecutablePath))
            {
                throw new InvalidOperationException(
                    "未找到 adb 可执行文件。请先安装 Android platform-tools，并确保 adb 已加入 PATH 或设置 ANDROID_SDK_ROOT/ANDROID_HOME。");
            }

            Log("ADB", OperationLogLevel.Info, $"启动 ADB Server：{_adbExecutablePath}");
            var stopwatch = Stopwatch.StartNew();
            var result = await Task.Run(() => _adbServer.StartServer(_adbExecutablePath, restartServerIfNewer: false), cancellationToken);
            stopwatch.Stop();

            if (result is not StartServerResult.Started and not StartServerResult.AlreadyRunning)
            {
                throw new InvalidOperationException($"ADB Server 启动失败，结果：{result}。");
            }

            _serverStarted = true;
            Log("ADB", OperationLogLevel.Success, $"ADB Server 已就绪：{result}", stopwatch.ElapsedMilliseconds);
        }
        finally
        {
            _initializeLock.Release();
        }
    }

    private async Task<DeviceData> FindDeviceBySerialAsync(string serial, CancellationToken cancellationToken)
    {
        var devices = await _adbClient.GetDevicesAsync(cancellationToken);
        return devices.FirstOrDefault(device => string.Equals(device.Serial, serial, StringComparison.OrdinalIgnoreCase))
               ?? throw new InvalidOperationException($"未找到序列号为 {serial} 的设备。");
    }

    private async Task<AdbScreenshot> ConvertFramebufferToScreenshotAsync(Framebuffer framebuffer, CancellationToken cancellationToken)
    {
        var width = checked((int)framebuffer.Header.Width);
        var height = checked((int)framebuffer.Header.Height);
        var rawData = framebuffer.Data ?? throw new InvalidOperationException("Framebuffer.Data 为空。");
        var bytesPerPixel = checked((int)Math.Max(1u, framebuffer.Header.Bpp / 8u));
        var normalized = NormalizeFramebufferData(rawData, framebuffer.Header.Width, framebuffer.Header.Height, bytesPerPixel);
        var rgbaBytes = ConvertToRgba32Bytes(normalized, width, height, bytesPerPixel, framebuffer.Header);

        using var image = Image.LoadPixelData<Rgba32>(rgbaBytes, width, height);
        await using var stream = new MemoryStream();
        await image.SaveAsync(stream, new PngEncoder(), cancellationToken);
        return new AdbScreenshot(stream.ToArray(), width, height);
    }

    private static byte[] NormalizeFramebufferData(byte[] data, uint width, uint height, int bytesPerPixel)
    {
        var expectedSize = checked((int)(width * height * (uint)bytesPerPixel));
        if (data.Length == expectedSize)
        {
            return data;
        }

        if (height == 0 || data.Length % height != 0)
        {
            throw new InvalidOperationException("Framebuffer 数据长度异常，无法推导 stride。");
        }

        var stride = data.Length / checked((int)height);
        var validRowBytes = checked((int)width * bytesPerPixel);
        if (stride < validRowBytes)
        {
            throw new InvalidOperationException("Framebuffer stride 小于有效像素宽度。");
        }

        var normalized = new byte[expectedSize];
        for (var row = 0; row < checked((int)height); row++)
        {
            Buffer.BlockCopy(data, row * stride, normalized, row * validRowBytes, validRowBytes);
        }

        return normalized;
    }

    private static byte[] ConvertToRgba32Bytes(
        byte[] pixelBytes,
        int width,
        int height,
        int bytesPerPixel,
        FramebufferHeader header)
    {
        var rgba = new byte[width * height * 4];

        for (var y = 0; y < height; y++)
        {
            for (var x = 0; x < width; x++)
            {
                var sourceIndex = (y * width + x) * bytesPerPixel;
                uint pixelValue = 0;

                for (var index = 0; index < bytesPerPixel; index++)
                {
                    pixelValue |= (uint)pixelBytes[sourceIndex + index] << (index * 8);
                }

                var targetIndex = (y * width + x) * 4;
                rgba[targetIndex] = ExtractChannel(pixelValue, header.Red);
                rgba[targetIndex + 1] = ExtractChannel(pixelValue, header.Green);
                rgba[targetIndex + 2] = ExtractChannel(pixelValue, header.Blue);
                rgba[targetIndex + 3] = header.Alpha.Length == 0 ? byte.MaxValue : ExtractChannel(pixelValue, header.Alpha);
            }
        }

        return rgba;
    }

    private static byte ExtractChannel(uint pixelValue, ColorData channel)
    {
        if (channel.Length == 0)
        {
            return 0;
        }

        var mask = (1u << checked((int)channel.Length)) - 1u;
        var value = (pixelValue >> checked((int)channel.Offset)) & mask;
        if (mask == byte.MaxValue)
        {
            return (byte)value;
        }

        return (byte)Math.Round(value * 255d / mask);
    }

    private static AdbDevice MapDevice(DeviceData device)
    {
        var connectionType = !string.IsNullOrWhiteSpace(device.Usb)
            ? "usb"
            : device.Serial.Contains(':', StringComparison.Ordinal) ? "tcpip" : "usb";

        return new AdbDevice
        {
            Serial = device.Serial,
            Model = device.Model,
            State = device.State.ToString().ToLowerInvariant(),
            ConnectionType = connectionType,
            Product = device.Product,
            TransportId = device.TransportId
        };
    }

    private async Task<T> ExecuteLoggedAsync<T>(
        string category,
        string operation,
        Func<CancellationToken, Task<T>> action,
        Func<T, string>? successMessageFactory,
        CancellationToken cancellationToken)
    {
        Log(category, OperationLogLevel.Info, $"{operation}开始。");
        var stopwatch = Stopwatch.StartNew();

        try
        {
            var result = await action(cancellationToken);
            stopwatch.Stop();
            Log(
                category,
                OperationLogLevel.Success,
                successMessageFactory?.Invoke(result) ?? $"{operation}成功。",
                stopwatch.ElapsedMilliseconds);

            return result;
        }
        catch (OperationCanceledException)
        {
            stopwatch.Stop();
            Log(category, OperationLogLevel.Warning, $"{operation}已取消。", stopwatch.ElapsedMilliseconds);
            throw;
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            Log(category, OperationLogLevel.Error, $"{operation}失败：{ex.Message}", stopwatch.ElapsedMilliseconds);
            throw;
        }
    }

    private void Log(string category, OperationLogLevel level, string message, long? elapsedMilliseconds = null)
    {
        OperationLogged?.Invoke(
            this,
            new OperationLogEntry(DateTimeOffset.Now, category, level, message, elapsedMilliseconds));
    }
}
