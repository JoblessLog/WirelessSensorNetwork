using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using CommunityToolkit.Mvvm.ComponentModel;

namespace wsn_keboo.Data;


public partial class NodeInfo : ObservableObject 
{
    [ObservableProperty]
    private string _id;

    [ObservableProperty]
    private string _temp;

    [ObservableProperty]
    private string _snr;

    [ObservableProperty]
    private string _rssi;

    [ObservableProperty]
    private string _time;
    
    public NodeInfo(string id, string time, string temp, string rssi, string snr)
    {
        _id = id;
        _temp = temp;
        _snr = snr;
        _rssi = rssi;
        _time = time;
    }

}
