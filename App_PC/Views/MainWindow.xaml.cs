using System.Windows.Controls;
using System.Windows;
using System.Windows.Input;

using System.Data;
using wsn_keboo.Views;

using SaveFileDialog = Microsoft.Win32.SaveFileDialog;
using Application = System.Windows.Application;
using ClosedXML.Excel;
using System.Collections;
using wsn_keboo.Data;
using System.Diagnostics;

namespace wsn_keboo;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow 
{
    private TcpIpHandler tcpIpHandler;
    public MainWindow()
    {
        LoadViewModelAsync();
        InitializeComponent();
        CommandBindings.Add(new CommandBinding(ApplicationCommands.Close, OnClose));
        CommandBindings.Add(new CommandBinding(RestartCommand, RestartApplicationExecuted));
        tcpIpHandler = new TcpIpHandler();
        this.Closing += MainWindow_Closing;
    }

    private void OnClose(object sender, ExecutedRoutedEventArgs e)
    {
        Close();
    }

    private async void LoadViewModelAsync()
    {
        DataContext = await MainWindowViewModel.CreateAsync(new YourDbContext());
    }
    private void ComboBox_SelectionChanged_1(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
    {

    }
    private void MainWindow_Closing(object? sender, System.ComponentModel.CancelEventArgs e)
    {
        tcpIpHandler.Stop();
    }

    private void Button_Click(object sender, RoutedEventArgs e)
    {
        //OpenFileDialog of = new OpenFileDialog();
        //of.Filter = "Excel Files | *.xlsx;";
        //of.Title = "Import Excel file.";
        //if (of.ShowDialog() == true)
        //{
        //    dgDisplay.ItemsSource = ImportExceltoDataTable(of.FileName).DefaultView;
        //}

        //ExportDataGridToExcel(dgDisplay);
        DataTable dataTable = new DataTable();
        foreach (DataGridColumn column in dgDisplay.Columns)
        {
            dataTable.Columns.Add(column.Header.ToString());
        }
        var itemsSource = dgDisplay.ItemsSource as IEnumerable;
        if (itemsSource != null)
        {
            foreach (var item in itemsSource)
            {
                var properties = item.GetType().GetProperties().Where(p => p.Name != "IdEnumerate");
                var row = dataTable.NewRow();
                foreach (var property in properties)
                {
                    row[property.Name] = property.GetValue(item);
                }
                dataTable.Rows.Add(row);
            }
        }
        var addProductWindow = new CustomReport();
        addProductWindow.ShowDialog();
    }
   
    public void ExportDataGridToExcel(DataGrid dataGrid)
    {
        DataTable dt = new DataTable();

        foreach (DataGridColumn column in dataGrid.Columns)
        {
            dt.Columns.Add(column.Header.ToString());
        }
        foreach (var item in dataGrid.Items)
        {
            DataRow dr = dt.NewRow();
            bool rowHasData = false;
            for (int i = 0; i < dataGrid.Columns.Count; i++)
            {
                var cellContent = dataGrid.Columns[i].GetCellContent(item);
                if (cellContent is TextBlock textBlock)
                {
                    dr[i] = textBlock.Text;
                    if (!string.IsNullOrEmpty(textBlock.Text))
                    {
                        rowHasData = true;
                    }
                }
            }
            if (rowHasData)
            {
                dt.Rows.Add(dr);
            }
        }
        SaveFileDialog saveFileDialog = new SaveFileDialog();
        saveFileDialog.Filter = "Excel files (*.xlsx)|*.xlsx";
        if (saveFileDialog.ShowDialog() == true)
        {
            using (XLWorkbook wb = new XLWorkbook())
            {
                wb.Worksheets.Add(dt, "Sheet1");
                wb.SaveAs(saveFileDialog.FileName);
            }
        }
    }
    public static DataTable ImportExceltoDataTable(string filePath)
    {
        using (XLWorkbook wb = new XLWorkbook(filePath))
        {
            IXLWorksheet ws = wb.Worksheet(1);
            int tl_Row = ws.FirstCellUsed().Address.RowNumber;
            int tl_Col = ws.FirstCellUsed().Address.ColumnNumber;
            int br_Row = ws.LastCellUsed().Address.RowNumber;
            int br_Col = ws.LastCellUsed().Address.ColumnNumber;
            DataTable dt = new DataTable();
            for (int i = tl_Col; i <= br_Col; i++)
            {
                dt.Columns.Add(ws.Cell(tl_Row, i).CachedValue.ToString());
            }
            IXLRow currentRow;
            for (int dtRow = 0; dtRow < br_Row - tl_Row; dtRow++)
            {
                currentRow = ws.Row(tl_Row + dtRow + 1);
                dt.Rows.Add();
                for (int dtCol = 0; dtCol < br_Col - tl_Col + 1; dtCol++)
                {
                    dt.Rows[dtRow][dtCol] = currentRow.Cell(tl_Col + dtCol).CachedValue;
                }
            }
            return dt;
        }
    }

    private void Button_Click_1(object sender, RoutedEventArgs e)
    {
    }
    public static RoutedCommand RestartCommand { get; } = new RoutedCommand();
    public void RestartApplicationExecuted(object sender, ExecutedRoutedEventArgs e)
    {
        string appPath = System.Reflection.Assembly.GetExecutingAssembly().Location;
        ProcessStartInfo startInfo = new ProcessStartInfo
        {
            FileName = "dotnet",
            Arguments = $"\"{appPath}\"",
            UseShellExecute = true
        };
        Process.Start(startInfo);
        Application.Current.Shutdown();

    }
}


