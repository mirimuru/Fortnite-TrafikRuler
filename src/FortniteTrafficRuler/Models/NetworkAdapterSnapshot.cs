namespace FortniteTrafficRuler.Models;

public sealed record NetworkAdapterSnapshot(
    string Id,
    string Name,
    string Kind,
    string Status,
    double ReceiveMbps,
    double SendMbps,
    double LinkSpeedMbps);
