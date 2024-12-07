using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace wsn_keboo.Data;

public interface ICommunicationHandler
{
    Task OpenAsync(string port, int baudRate);
    Task SendDataAsync(string data);
    event Action<string> DataReceived;
}
