using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.EntityFrameworkCore;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Data;
using System.IO.Ports;
using System.Net;
using System.Net.Sockets;
using wsn_keboo.Data;
using wsn_keboo.ViewModels;
using Application = System.Windows.Application;
using Timer = System.Threading.Timer;

namespace wsn_keboo;

/// <summary>
/// handle I/O and data processing
/// </summary>
public partial class MainWindowViewModel : ObservableObject, IDisposable
{
    #region tcp/ip
    [ObservableProperty]
    private string _strThisHostName;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(StrThisHostName))]
    private IPAddress _ipa;

    [ObservableProperty]
    private int _nPort;

    [ObservableProperty]
    private bool _isTcpIp;

    public TcpIpHandler TcpHandler { get; }
    private bool _isListening;
    #endregion

    #region chart
    [ObservableProperty]
    private ChartViewModel _chartViewModel;

    public List<string> Labels { get; set; }
    public Func<double, string> YAxisFormatter { get; set; }
    public Func<double, string> YAxis2Formatter { get; set; }
    public ObservableCollection<IdCount> IdCounts { get; } = new ObservableCollection<IdCount>();
    #endregion

    #region serial port
    [ObservableProperty]
    private bool _isOpened;

    [ObservableProperty]
    private string? _selectedPort;

    public ObservableCollection<DataReceiveCom> Source { get; set; }
    public string[] AvailablePorts{get { return SerialPort.GetPortNames();}}
    private readonly SerialHandler _serialHandler;
    #endregion

    #region MQTT
    private MqttBroker _broker;
    private MqttClientHandler _myClient { get; }
    readonly string topic1 = "mqttnet/samples/topic/1";
    readonly string topic2 = "mqttnet/samples/topic/2";

    [ObservableProperty]
    private bool _isMqtt;

    #endregion

    #region set level temp
    [ObservableProperty]
    public string _set1;

    [ObservableProperty]
    public string _set2;

    [ObservableProperty]
    public string _comboBoxSelectedItem;
    #endregion

    #region Set Node priority
    [ObservableProperty]
    private ObservableCollection<Node> _nodes;

    [ObservableProperty]
    private string? _selectedNodeInfo;

    public ObservableCollection<string> Ids { get; set; }
    public bool? IsAllItems1Selected
    {
        get
        {
            var selected = Nodes.Select(item => item.IsSelected).Distinct().ToList();
            return selected.Count == 1 ? selected.Single() : (bool?)null;
        }
        set
        {
            if (value.HasValue)
            {
                foreach (var item in Nodes)
                {
                    item.IsSelected = value.Value;
                }
                OnPropertyChanged();
            }
        }
    }
    #endregion

    #region other fields and properties
    [ObservableProperty]
    private string _statusLine;
    [ObservableProperty]
    private string _statusLine2;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(IncrementCountCommand))]
    private int _count;
    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(TextChangeCommand))]
    private int _count1;

    [ObservableProperty]
    private bool _isChecked;
    [ObservableProperty]
    private bool _isSerialMode;
    [ObservableProperty]
    private bool _isDbSave;

    [ObservableProperty]
    private string _currTemp;
    [ObservableProperty]
    private int _countNode1;
    [ObservableProperty]
    private int _countNode2;
    [ObservableProperty]
    private int _countNode3;

    private Timer timer;
    private Dictionary<string, (DateTime LastReceivedTime, bool IsDead)> lastReceivedTimes = new();
    private static readonly string Vinh = "vinh.nt202727@sis.hust.edu.vn";
    private static readonly string Long = "long.pd200370@sis.hust.edu.vn";
    #endregion

    #region Database
    private YourDbContext DbContext { get; }

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(TextChangeCommand))]
    private string? _savedTemp;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(TextChangeCommand))]
    private string? _savedRssi;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(TextChangeCommand))]
    private string? _savedSnr;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(TextChangeCommand))]
    private string? _savedId;

    #endregion

    #region ctor
    public MainWindowViewModel(YourDbContext dbContext) {

        timer = new Timer(CheckLastReceivedTimes, null, TimeSpan.Zero, TimeSpan.FromSeconds(1));
        
        DbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));

        Ids = new ObservableCollection<string>();
        Nodes = new ObservableCollection<Node>();

        ChartViewModel = new ChartViewModel(new ObservableCollection<DataReceiveCom>(), Ids);
        Labels = new List<string>();
        YAxisFormatter = value => value.ToString("F1");
        YAxis2Formatter = value => value.ToString("F0");

        ComboBoxSelectedItem = "abc1234567"; Set1 = "30"; Set2 = "60";

        Application.Current.Dispatcher.Invoke(async () =>
        {
            await LoadIdsFromNodesAsync();
            UpdatePriorityString();
        });

        StatusLine = "Not Connect"; StatusLine2 = "";
        CurrTemp = "Null ";StrThisHostName = ""; 
        Ipa = GetLocalIPv4(); NPort = 23;

        Source = new ObservableCollection<DataReceiveCom>();
        Source.CollectionChanged += DataCollection_CollectionChanged;

        SelectedPort = AvailablePorts.FirstOrDefault();
        IsSerialMode = true;
        _serialHandler = new SerialHandler();
        _serialHandler.Connected += (s, e) =>
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                StatusLine = s;
                StatusLine2 = e;
            });
        };
        _serialHandler.Error += (s) =>
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                StatusLine2 = s;
            });
        };


        TcpHandler = new TcpIpHandler(23, Ipa.ToString());
        TcpHandler.DataReceived += OnDataReceived;
        TcpHandler.Error += (s)=>
        {
            Application.Current.Dispatcher.Invoke(()=>
            {
                StatusLine2 = s;
            });
        };
        TcpHandler.ClientConnected += (s, e) =>
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
            StatusLine = s;
            StatusLine2 = e;
            });
        };

        _broker = new MqttBroker();
        _myClient = new MqttClientHandler();
        _myClient.Connected += (s, e) =>
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                StatusLine = s;
                StatusLine2 = e;
            });
        };
        _myClient.Error += (s) =>
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                StatusLine2 = s;
            });
        };
    }
    #endregion

    #region COM Method
    [RelayCommand(CanExecute = nameof (CanSendSetLevel))]
    private void TextChange1()
    {
        IsChecked = true;
    }
    #endregion

    #region TCP/IP Method
    public IPAddress GetLocalIPv4()
    {
        var host = Dns.GetHostEntry(Dns.GetHostName());
        foreach (var ip in host.AddressList)
        {
            if (ip.AddressFamily == AddressFamily.InterNetwork)
            {
                return ip;
            }
        }
        throw new Exception("No network adapters with an IPv4 address in the system!");
    }   
    #endregion

    #region Menu
    [RelayCommand(CanExecute = nameof(CanIncrementCount))]
    private void IncrementCount()
    {
        Count++;
        string url = "https://portal.azure.com/";
        System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
        ("cmd", $"/c start {url}"){ CreateNoWindow = true });    
    }

    [RelayCommand]
    private void Menu1()
    {
        SendMail(Vinh);
    }

    [RelayCommand]
    private void Menu2()
    {
        SendMail(Long);
    }

    private void SendMail(string to)
    {
        string subject = Uri.EscapeDataString("[Feedback] Issue about ");
        string body = Uri.EscapeDataString( $"Dear Dev,\n" +
                                            $"On {DateTime.Now},\n" +
                                            $"Description:\n" +
                                            $"1. OS:\n" +
                                            $"2. Steps to reproduce the behavior:\n" +
                                            $"3. Log failure:\n" +
                                            $"\nHope to hear you soon.\n" +
                                            $"Sincerely,\n");
        string url = $"mailto:{to}?subject={subject}&body={body}";
        var psi = new System.Diagnostics.ProcessStartInfo
        {
            FileName = url,
            UseShellExecute = true
        };
        System.Diagnostics.Process.Start(psi);
    }
    #endregion

    #region Can Excute

    private bool CanSendSetLevel() => Set1 != null & Set2 != null;
    private bool CanIncrementCount() => Count < 1;
    private bool CanIncrementCount1() => Count1 < 1;
    private bool CanRestart() => IsOpened || _isListening;

    #endregion

    #region Handle Data

    [RelayCommand]
    private async Task Restart()
    {
        Count1 = 0; 
        IsOpened = false;
        StatusLine = "Not Connect";
        StatusLine2 = "";
        if(IsSerialMode)
        {
            _serialHandler.DataReceived -= OnDataReceived;
            await _serialHandler.SendDataAsync("stop");
            await _serialHandler.ClosePortAsync();
        }
        else if (IsMqtt)
        {
            await _broker.Force_Disconnecting_Client();
            await _broker.StopBrokerAsync();
        }
        else if (IsTcpIp)
        {
            await TcpHandler.SendStringAsync("stop");
            TcpHandler.StopListening();
            await Task.Delay(500);
            await TextChange();
            Count1 = 0;
            await Task.Delay(500);
            TcpHandler.StopListening();
            _isListening = false;
        }
    }

    [RelayCommand(CanExecute = nameof(CanIncrementCount1))]
    public async Task TextChange()
    {
        if (SelectedNodeInfo == null)
        {
            StatusLine2 = "The system won't work. Please select at least a node.";
            return;
        }
        Count1++;
        if(IsSerialMode)
        {
            if (SelectedPort == null)
            {
                StatusLine2 = "Please select a port";
                return;
            }
            else
            {
                await _serialHandler.OpenPortAsync(SelectedPort,115200);
                IsOpened = true;
                await _serialHandler.SendDataAsync(SelectedNodeInfo);
                _serialHandler.DataReceived += OnDataReceived;
            }
        }
        else if (IsMqtt)
        {
            await _broker.StartMqttServer();
            await _myClient.SubscribeTopic(topic1);
            await _myClient.SendMessageAsync(SelectedNodeInfo, topic1);
            _myClient.DataReceived += OnDataReceived;
        }
        else if (IsTcpIp)
        {
            if (!_isListening) { 
                if (SelectedNodeInfo != null )
                {
                    await TcpHandler.StartListeningAsync(SelectedNodeInfo);
                    _isListening = true;
                }
                else
                {
                    throw new ArgumentNullException(nameof(SelectedNodeInfo));
                }
            }
        }
    }

    [RelayCommand]
    private void UpdatePriorityString()
    {
        SelectedNodeInfo = string.Join(" ", Nodes.Where(n => n.IsSelected).Select(n => $"{n.Id} {n.Priority}"));
    }

    private async void OnDataReceived(string data)
    {
        if (IsChecked && data.Length >= 10 && data.Substring(0, 10).Equals($"{ComboBoxSelectedItem}"))
        {
            IsChecked = false;
            if (IsSerialMode)
            {
                await _serialHandler.SendDataAsync($"Set {Set1} {Set2}");
            }
            else if (IsMqtt)
            {
                await _myClient.SendMessageAsync($"Set {Set1} {Set2}",topic2);
            }
            else if (IsTcpIp)
                await TcpHandler.SendStringAsync($"Set {Set1} {Set2}");
        }
        else
        {
            if (IsSerialMode)
            {
                await _serialHandler.SendDataAsync("ok");
            }
            else if (IsMqtt )
            {
                await _myClient.SendMessageAsync("ok",topic2);
            }
            else if (IsTcpIp) 
                await TcpHandler.SendStringAsync("ok");
        }
        ProcessData(data);
    }

    public void AddToSeries(DataReceiveCom data)
    {
        var series = ChartViewModel.SeriesCollection.FirstOrDefault(s => s.Title == $"Temperature for ID {data.Id}");

        if (series != null)
        {
            series.Values.Add(double.Parse(data.Temperature ?? "0"));
        }
    }

    private void ProcessData(string display)
    {
        Application.Current.Dispatcher.Invoke(() =>{ 
            Labels.Add(DateTime.Now.ToString());
            SavedId = display.Substring(0, 10);
            SavedRssi = int.Parse(display.Substring(14, 4).TrimStart('0')).ToString();
            SavedTemp = display.Substring(10, 4).TrimStart('0');
            SavedSnr = display.Substring(18, 4).TrimStart('0');
            CurrTemp = SavedTemp;
            DataReceiveCom newItem = new DataReceiveCom()
            {
                Id = SavedId,
                Rssi = SavedRssi,
                Temperature = SavedTemp,
                Time = DateTime.Now,
                Snr = SavedSnr
            };
            AddToSeries(newItem);
            Source.Add(newItem);
            DbContext.DataReceiveComs.Add(newItem);
            DbContext.SaveChanges();
    });
    }

    private void Node_PropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(Node.Id) || e.PropertyName == nameof(Node.IsSelected))
        {
            UpdatePriorityString();
        }
    }

    private void DataCollection_CollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        if (e.NewItems != null)
        {
            foreach (DataReceiveCom newItem in e.NewItems)
            {
                
                AddToSeries(newItem);

                if (newItem.Id is null) throw new ArgumentNullException(nameof(newItem.Id));
                lastReceivedTimes[newItem.Id] = (DateTime.Now, false);

                var idCount = IdCounts.FirstOrDefault(ic => ic.Id == newItem.Id);
                if (idCount == null)
                {
                    idCount = new IdCount { Id = newItem.Id };
                    IdCounts.Add(idCount);
                }
                idCount.Count++;
            }
        }
    }

    public async Task LoadIdsFromNodesAsync()
    {
        var ids = await DbContext.Nodes
                                .Where(node => node.Id != null)
                                .Select(node => node.Id!.ToString())
                                .ToListAsync();
        Ids = new ObservableCollection<string>(ids);

        var dbNodes = await DbContext.Nodes.ToListAsync();
        Nodes = new ObservableCollection<Node>(
            dbNodes.Select(dbNode => new Node
            {
                Id = dbNode.Id,
                Priority = dbNode.Priority,
                IsSelected = true
            }));
        if (Nodes != null)
        {
            foreach (var node in Nodes)
            {
                if (node != null)
                {
                    node.PropertyChanged += Node_PropertyChanged;
                }
            }
        }
    }

    public static async Task<MainWindowViewModel> CreateAsync(YourDbContext context)
    {
        var viewModel = new MainWindowViewModel(context);
        await viewModel.LoadIdsFromNodesAsync();
        viewModel.ChartViewModel = new ChartViewModel(new ObservableCollection<DataReceiveCom>(), viewModel.Ids);
        return viewModel;
    }

    private void CheckLastReceivedTimes(object? state)
    {
        foreach (var kvp in lastReceivedTimes.ToList())
        {
            if (DateTime.Now - kvp.Value.LastReceivedTime > TimeSpan.FromSeconds(10) && !kvp.Value.IsDead)
            {
                StatusLine2 += $"\n{kvp.Key} is dead";
                lastReceivedTimes[kvp.Key] = (kvp.Value.LastReceivedTime, true);
            }
        }
    }

    public void Dispose(){}

    #endregion
}

public partial class IdCount : ObservableObject
{
    [ObservableProperty]
    public string? _id;
    
    [ObservableProperty]
    public int _count;
}








