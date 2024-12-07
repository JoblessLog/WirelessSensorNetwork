using CommunityToolkit.Mvvm.ComponentModel;
using System.Net.Sockets;
using System.Net;
using System.Text;
using System.IO;

namespace wsn_keboo.Data;

public partial class TcpIpHandler : ObservableObject
{
    public int NPort { get; set; }
    public string? Ipaddr { get; set; }
    private TcpListener? _listener;
    private List<TcpClient> _connectedClients = new List<TcpClient>();
    private CancellationTokenSource _cts = new CancellationTokenSource();
    public event Action<string>? DataReceived;
    public Action<string, string>? ClientConnected;
    public Action<string>? Error;
    private Dictionary<string, bool> clientStatus = new Dictionary<string, bool>();

    public TcpIpHandler(int nPort, string ipAddr)
    {
        NPort = nPort;
        Ipaddr = ipAddr;
        _listener = new TcpListener(IPAddress.Parse(Ipaddr), NPort);
    }
    public TcpIpHandler(){}
    
    public async Task StartListeningAsync(string initialMessage)
    {
        _cts = new CancellationTokenSource();
        _listener = new TcpListener(IPAddress.Any, NPort);
         _listener.Server.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
        _listener.Start();
        while (true)
        {
            try 
            { 
                if (_listener != null && !_listener.Server.IsBound) break;
                if (_listener is null) throw new ArgumentNullException(nameof(_listener));
                TcpClient client = await _listener.AcceptTcpClientAsync();

                string? clientIp = client.Client.RemoteEndPoint?.ToString();

                if(clientIp == null) continue;
                ClientConnected?.Invoke("Connected", "Server" + DateTime.Now.ToString(" [h:mm:ss] ") + ": Sent Node Priority");

                if (!clientStatus.ContainsKey(clientIp) || !clientStatus[clientIp])
                {
                    await HandleClient(client, initialMessage);
                    clientStatus[clientIp] = true;
                }
                else
                {
                    await HandleClient(client, "");
                }
                ClientConnected?.Invoke("Disconnected", "Waiting for client to connect...");
            }
            catch (SocketException)
            {
                ClientConnected?.Invoke("Disconnected", "Waiting for client to connect...");
                break;
            }
            catch (ObjectDisposedException)
            {
                ClientConnected?.Invoke("Disconnected", "Waiting for client to connect...");
                break;
            }
            catch (InvalidOperationException)
            {
                ClientConnected?.Invoke("Disconnected", "Waiting for client to connect...");
                break;
            }
        }
    }

    private async Task HandleClient(TcpClient client, string message)
    {
        _connectedClients.Add(client);
        NetworkStream stream = client.GetStream();
        byte[] buffer = Encoding.ASCII.GetBytes(message);
        await stream.WriteAsync(buffer, 0, buffer.Length);

        buffer = new byte[1024];
        while (client.Connected)
        {
            try { 
            int bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length, _cts.Token);
            if (bytesRead == 0)
            {
                break;
            }
            string data = Encoding.ASCII.GetString(buffer, 0, bytesRead);
            DataReceived?.Invoke(data);
            }
            catch (IOException)
            {
                Error?.Invoke("Error: Client disconnected");
                break;
            }
            catch (OperationCanceledException)
            {
                break;
            }
        }
    }

    public async Task SendStringAsync(string message)
    {
        
        byte[] buffer = Encoding.ASCII.GetBytes(message);

        foreach (var client in _connectedClients)
        {
            if (client.Connected)
            {
                try
                {
                NetworkStream stream = client.GetStream();
                await stream.WriteAsync(buffer, 0, buffer.Length);
                }
                catch (IOException ex)
                {
                    // Handle exception
                    Error?.Invoke("Error: " + ex.Message);
                }
            }
        }
    }
    public void Stop()
    {
        _cts.Cancel();
    }
    public void StopListening()
    {        
        foreach (var client in _connectedClients)
        {
            NetworkStream stream = client.GetStream();
            try
            {
                stream.Close();
            }
            catch (IOException ex)
            {
                // Handle exception
                Error?.Invoke("Error: " + ex.Message);
            }
            client.Close();
        }
        if (_listener != null)
        {
            _listener.Stop();
        }
        _connectedClients.Clear();
        _cts.Cancel();
        //_listener = null;
        //_cts.Dispose();
    }
}    
