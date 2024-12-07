using System.Text;
using System.IO.Ports;

namespace wsn_keboo.Data;

public class SerialHandler
{
    private SerialPort? _serialPort;
    private SemaphoreSlim _semaphore = new SemaphoreSlim(1, 1);

    public event Action<string>? DataReceived;
    public Action<string, string>? Connected;
    public Action<string>? Error;


    public async Task OpenPortAsync(string portName, int baudRate)
    {
        await _semaphore.WaitAsync();
        try
        {
            if (_serialPort != null)
            {
                _serialPort.DataReceived -= SerialPort_DataReceived;
                _serialPort.Close();
                _serialPort = null;
            }
            
            _serialPort = new SerialPort(portName, baudRate);
            _serialPort.DataReceived += SerialPort_DataReceived;
            _serialPort.Open();
            Connected?.Invoke("Connected", "" + DateTime.Now.ToString(" [h:mm:ss] ") + ": Opened port, Sent Command");
        }
        finally
        {
            _semaphore.Release();
        }
    }
    private void SerialPort_DataReceived(object sender, SerialDataReceivedEventArgs e)
    {
        _semaphore.Wait();
        if (_serialPort == null)
        {
            return;
        }
        try
        {
            int bytesToRead = _serialPort.BytesToRead;
            byte[] buffer = new byte[bytesToRead];
            _serialPort.Read(buffer, 0, bytesToRead);
            string data = Encoding.ASCII.GetString(buffer);
            DataReceived?.Invoke(data);
        }
        finally
        {
            _semaphore.Release();
        }
    }

    public async Task SendDataAsync(string data)
    {
        if (_serialPort != null && _serialPort.IsOpen)
        {
            await _semaphore.WaitAsync();
            try
            {
                await Task.Run(() => _serialPort.WriteLine(data));
                Connected?.Invoke("Opened", "Sent command");
            }
            finally
            {
                _semaphore.Release();
            }
        }
    }

    public async Task ClosePortAsync()
    {
        await _semaphore.WaitAsync();
        try
        {
            if (_serialPort != null)
            {
                _serialPort.DataReceived -= SerialPort_DataReceived;
                _serialPort.Close();
                _serialPort = null;

                Connected?.Invoke("Disconnected", "Closed port");
            }
        }
        finally
        {
            _semaphore.Release();
        }
    }
}
