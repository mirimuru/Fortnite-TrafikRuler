using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Threading;
using FortniteTrafficRuler.Models;
using FortniteTrafficRuler.Services;

namespace FortniteTrafficRuler;

public partial class MainWindow : Window
{
    private readonly NetworkAdapterMonitor _monitor = new();
    private readonly ObservableCollection<NetworkAdapterRow> _rows = [];
    private readonly DispatcherTimer _timer;

    public MainWindow()
    {
        InitializeComponent();
        AdaptersGrid.ItemsSource = _rows;

        _timer = new DispatcherTimer
        {
            Interval = TimeSpan.FromSeconds(1)
        };
        _timer.Tick += (_, _) => RefreshAdapters();
        Loaded += (_, _) =>
        {
            RefreshAdapters();
            _timer.Start();
        };
        Closed += (_, _) => _timer.Stop();
    }

    private void RefreshAdapters()
    {
        try
        {
            var snapshots = _monitor.ReadSnapshots();
            var currentIds = snapshots.Select(x => x.Id).ToHashSet();

            foreach (var old in _rows.Where(x => !currentIds.Contains(x.Id)).ToArray())
                _rows.Remove(old);

            foreach (var snapshot in snapshots)
            {
                var row = _rows.FirstOrDefault(x => x.Id == snapshot.Id);
                if (row is null)
                {
                    row = new NetworkAdapterRow { Id = snapshot.Id };
                    _rows.Add(row);
                }

                row.Name = snapshot.Name;
                row.Kind = snapshot.Kind;
                row.Status = snapshot.Status;
                row.ReceiveMbps = snapshot.ReceiveMbps;
                row.SendMbps = snapshot.SendMbps;
                row.LinkSpeedMbps = snapshot.LinkSpeedMbps;
            }

            ReceiveTotalText.Text = $"{snapshots.Sum(x => x.ReceiveMbps):F2} Mbps";
            SendTotalText.Text = $"{snapshots.Sum(x => x.SendMbps):F2} Mbps";
        }
        catch (Exception ex)
        {
            Title = $"Fortnite Traffic Ruler - 監視エラー: {ex.Message}";
        }
    }
}

public sealed class NetworkAdapterRow : System.ComponentModel.INotifyPropertyChanged
{
    private string _name = "";
    private string _kind = "";
    private string _status = "";
    private double _receiveMbps;
    private double _sendMbps;
    private double _linkSpeedMbps;

    public string Id { get; init; } = "";
    public string Name { get => _name; set => Set(ref _name, value); }
    public string Kind { get => _kind; set => Set(ref _kind, value); }
    public string Status { get => _status; set => Set(ref _status, value); }
    public double ReceiveMbps { get => _receiveMbps; set => Set(ref _receiveMbps, value); }
    public double SendMbps { get => _sendMbps; set => Set(ref _sendMbps, value); }
    public double LinkSpeedMbps { get => _linkSpeedMbps; set => Set(ref _linkSpeedMbps, value); }

    public event System.ComponentModel.PropertyChangedEventHandler? PropertyChanged;

    private void Set<T>(ref T field, T value, [System.Runtime.CompilerServices.CallerMemberName] string? name = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value)) return;
        field = value;
        PropertyChanged?.Invoke(this, new System.ComponentModel.PropertyChangedEventArgs(name));
    }
}
