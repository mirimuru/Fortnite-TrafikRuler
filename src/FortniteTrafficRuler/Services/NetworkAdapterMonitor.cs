using System.Net.NetworkInformation;
using FortniteTrafficRuler.Models;

namespace FortniteTrafficRuler.Services;

/// <summary>
/// Reads per-interface byte counters and calculates rates between samples.
/// This is adapter-level monitoring, not per-process or per-flow accounting.
/// </summary>
public sealed class NetworkAdapterMonitor
{
    private readonly Dictionary<string, Sample> _previous = new(StringComparer.Ordinal);
    private DateTimeOffset? _lastSampleAt;

    public IReadOnlyList<NetworkAdapterSnapshot> ReadSnapshots()
    {
        var now = DateTimeOffset.UtcNow;
        var elapsed = _lastSampleAt is { } previousAt
            ? (now - previousAt).TotalSeconds
            : 0;

        var result = new List<NetworkAdapterSnapshot>();

        foreach (var adapter in NetworkInterface.GetAllNetworkInterfaces())
        {
            if (adapter.NetworkInterfaceType is NetworkInterfaceType.Loopback or NetworkInterfaceType.Tunnel)
                continue;

            ulong received;
            ulong sent;
            try
            {
                var stats = adapter.GetIPStatistics();
                received = checked((ulong)Math.Max(0, stats.BytesReceived));
                sent = checked((ulong)Math.Max(0, stats.BytesSent));
            }
            catch (NetworkInformationException)
            {
                continue;
            }

            var receiveMbps = 0d;
            var sendMbps = 0d;
            if (elapsed > 0 && _previous.TryGetValue(adapter.Id, out var old))
            {
                var receivedDelta = received >= old.Received ? received - old.Received : 0;
                var sentDelta = sent >= old.Sent ? sent - old.Sent : 0;
                receiveMbps = receivedDelta * 8d / elapsed / 1_000_000d;
                sendMbps = sentDelta * 8d / elapsed / 1_000_000d;
            }

            _previous[adapter.Id] = new Sample(received, sent);

            result.Add(new NetworkAdapterSnapshot(
                adapter.Id,
                adapter.Name,
                GetKind(adapter.NetworkInterfaceType),
                GetStatus(adapter.OperationalStatus),
                receiveMbps,
                sendMbps,
                adapter.Speed > 0 ? adapter.Speed / 1_000_000d : 0));
        }

        _lastSampleAt = now;
        return result.OrderBy(x => x.Kind).ThenBy(x => x.Name).ToArray();
    }

    private static string GetKind(NetworkInterfaceType type) => type switch
    {
        NetworkInterfaceType.Wireless80211 => "Wi-Fi",
        NetworkInterfaceType.Ethernet or NetworkInterfaceType.Ethernet3Megabit => "Ethernet",
        _ => type.ToString()
    };

    private static string GetStatus(OperationalStatus status) => status switch
    {
        OperationalStatus.Up => "接続中",
        OperationalStatus.Down => "切断",
        OperationalStatus.Dormant => "待機中",
        _ => status.ToString()
    };

    private sealed record Sample(ulong Received, ulong Sent);
}
