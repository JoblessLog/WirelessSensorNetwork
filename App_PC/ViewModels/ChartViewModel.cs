using CommunityToolkit.Mvvm.ComponentModel;
using LiveCharts;
using LiveCharts.Wpf;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Timers;
using wsn_keboo.Data;
using Application = System.Windows.Application;
using Timer = System.Timers.Timer;

namespace wsn_keboo.ViewModels;
/// <summary>
/// Handle Charting 
/// </summary>
public partial class ChartViewModel : ObservableObject
{
    public Timer timer;
    [ObservableProperty]
    private SeriesCollection _seriesCollection;

    [ObservableProperty]
    private SeriesCollection _memoryChartSeries;

    public ChartViewModel(ObservableCollection<DataReceiveCom> data, ObservableCollection<string> ids)
    {
        SeriesCollection = new SeriesCollection();

        foreach (var id in ids)
        {
            var dataForId = data.Where(d => d.Id == id).ToList();

            LineSeries series = new LineSeries
            {
                Title = $"Temperature for ID {id}",
                Values = new ChartValues<double>(dataForId.Where(d => !string.IsNullOrEmpty(d.Temperature)).Select(d => double.Parse(d.Temperature ?? "0"))),
                PointGeometry = DefaultGeometries.Circle
            };

            SeriesCollection.Add(series);
        }

        MemoryChartSeries = new SeriesCollection
        {
            new LineSeries
            {
                Title = "Memory",
                Values = new ChartValues<double> { },
                StrokeThickness = 2,
                //Fill = new SolidColorBrush(Color.FromRgb(0, 0, 255)),
                PointGeometry = null
            }
        };

        timer = new Timer(1000); //raise elapse event every 1 second
        timer.Elapsed += UpdateMemoryChart;
        timer.Start();
    }

    private void UpdateMemoryChart(object? sender, ElapsedEventArgs e)
{
    try 
    {
        var memoryUsage = Process.GetCurrentProcess().PrivateMemorySize64 / (1024.0 * 1024.0);
        if (Application.Current != null)
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                if (MemoryChartSeries != null && MemoryChartSeries.Count > 0 && MemoryChartSeries[0].Values != null)
                {
                    MemoryChartSeries[0].Values.Add(memoryUsage);
                    if (MemoryChartSeries[0].Values.Count > 60)
                    {
                        MemoryChartSeries[0].Values.RemoveAt(0);
                    }
                }
            });
        }
    }
    catch (ArgumentOutOfRangeException)
    {
        // ignored
    }
}
}
