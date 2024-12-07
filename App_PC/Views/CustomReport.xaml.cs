using System.ComponentModel;
using System.Data;
using System.IO;
using System.Windows;
using Microsoft.Reporting.WinForms;
using wsn_keboo.Data;
namespace wsn_keboo.Views;

/// <summary>
/// Interaction logic for CustomReport.xaml
/// </summary>

public partial class CustomReport : Window
{
    public CustomReport()
    {
        InitializeComponent();
        DeleteOldRecords();
    }

    private void Button_Click(object sender, RoutedEventArgs e)
    {
        DateTime? fromDate = FromDate.SelectedDate;
        DateTime? toDate = ToDate.SelectedDate;
        
        List<DataReceiveCom> data;
        using (var context = new YourDbContext())
        {
            IQueryable<DataReceiveCom> query = context.DataReceiveComs;

            if (fromDate.HasValue && toDate.HasValue)
            {
                query = query.Where(item => item.Time >= fromDate.Value && item.Time <= toDate.Value);
            }

            // Load the data.
            data = query.ToList();
        }
        DataTable dataTable = ConvertToDataTable(data);

        if (ReportViewerDemo != null && ReportViewerDemo.LocalReport != null)
        {
            ReportViewerDemo.Reset();
            ReportViewerDemo.LocalReport.DataSources.Clear();
            ReportViewerDemo.LocalReport.DataSources.Add(new ReportDataSource("DataSet1", dataTable));
            var projectDirectory = Directory.GetParent(Environment.CurrentDirectory)?.Parent?.Parent?.FullName;
            if (projectDirectory != null)
            {
                ReportViewerDemo.LocalReport.ReportPath = $"{projectDirectory}\\Data\\Report1.rdlc";
                ReportViewerDemo.RefreshReport(); 
            }
        }
    }

    private DataTable ConvertToDataTable<T>(IList<T> data)
    {
        PropertyDescriptorCollection properties = TypeDescriptor.GetProperties(typeof(T));
        DataTable table = new DataTable();
        foreach (PropertyDescriptor prop in properties)
            table.Columns.Add(prop.Name, Nullable.GetUnderlyingType(prop.PropertyType) ?? prop.PropertyType);
        foreach (T item in data)
        {
            DataRow row = table.NewRow();
            foreach (PropertyDescriptor prop in properties)
                row[prop.Name] = prop.GetValue(item) ?? DBNull.Value;
            table.Rows.Add(row);
        }
        return table;
    }

    public void DeleteOldRecords()
    {
        using (var context = new YourDbContext())
        {
            var sevenDaysAgo = DateTime.Now.AddDays(-7);
            var oldRecords = context.DataReceiveComs
                .AsEnumerable() // Move subsequent operations to client side
                .Where(d => d.Time != null && d.Time < sevenDaysAgo)
                .ToList();

            context.DataReceiveComs.RemoveRange(oldRecords);
            context.SaveChanges();
        }
    }
}
    
