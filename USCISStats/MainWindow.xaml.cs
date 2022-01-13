using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;
using ClassLibrary;
using DataAccessLibrary;
using DataAccessLibrary.Models;
using LiveCharts;
using LiveCharts.Wpf;
using Microsoft.Win32;

namespace USCISStats;

/// <summary>
///     Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window
{
    /// <summary>
    ///     properties
    /// </summary>
    private Converter _converter;

    private BindingList<FullCaseModel> _listOfCasesDB;
    private int _numOfTasks;
    private int _progress;
    
    private SqliteCrud _sql;
    private WebAccessClient _webAccessClient;
    private List<WebAccessClient> _webConnections;

    public SeriesCollection SeriesCollection { get; set; }
    public List<string> Labels { get; set; }
    //public Func<double, string> Formatter { get; set; }

    public SeriesCollection SeriesCollectionRowChart { get; set; }
    public string[] LabelsRowChart { get; set; }
    public Func<double, string> Formatter { get; set; }



    public MainWindow()
    {
        InitializeComponent();
        LoadContents();
    }

    public async void LoadContents()
    {
        _converter = new Converter();
        _webConnections = new List<WebAccessClient>();
        _listOfCasesDB = new BindingList<FullCaseModel>();
        _numOfTasks = int.Parse(TbThreads.Text);
        _webAccessClient = new WebAccessClient("https://egov.uscis.gov/casestatus/mycasestatus.do");
        InternetStatus.Content = await _webAccessClient.CheckInternet();
        InternetStatus.Background = new SolidColorBrush(Colors.LawnGreen);
    }

    

    private void UpdateButtonsStatus(bool btnUpdate = false, bool btnStatistics = false,
        bool btnSaveUpdateToDb = false, bool btnExportExcel = false, bool btnClearview = false)
    {
        BtnUpdate.IsEnabled = btnUpdate;
        BtnStatistics.IsEnabled = btnStatistics;
        BtnSaveUpdateToDb.IsEnabled = btnSaveUpdateToDb;
        BtnExportExcel.IsEnabled = btnExportExcel;
        BtnClearView.IsEnabled = btnClearview;
    }

    private void UpdateInfoBlock(int numOfCases)
    {
        TbInfoBlock.Text = $"Cases in Table: {numOfCases}";
    }

    private async void SetProgressBar(bool IsIndeterminate, string message)
    {
        PbStatus.IsIndeterminate = IsIndeterminate;
        TxtBlkStatus.Text = message;
    }

    /// <summary>
    ///     Reports progress of getting cases from USCIS website to UI
    /// </summary>
    /// <param name="progress"></param>
    private async void ReportProgress(MyTaskProgressReport progress)
    {
        _progress += progress.CurrentProgressAmount;
        PbStatus.Value = _progress;
        SetProgressBar(false, $"{_progress}/{PbStatus.Maximum}");
    }

    private void BtnOpenDatabase_Click(object sender, RoutedEventArgs e)
    {
        var openFileDialog = new OpenFileDialog();
        openFileDialog.InitialDirectory = Directory.GetCurrentDirectory();
        openFileDialog.Filter = "Database files (*.db)|*.db|All files (*.*)|*.*";
        var fileName = "";

        if (openFileDialog.ShowDialog() == true)
        {
            fileName = openFileDialog.FileName;
            var connectionStringName = $"Data Source={fileName};Version=3;";
            _sql = new SqliteCrud(connectionStringName);
            _listOfCasesDB = new BindingList<FullCaseModel>(_sql.GetAllFullCasesAsModels());
            DgData.ItemsSource = _listOfCasesDB;
            UpdateInfoBlock(_listOfCasesDB.Count);
            UpdateButtonsStatus(true, true, false, true, true);
        }
    }

    private void BtnNewDb_Click(object sender, RoutedEventArgs e)
    {
        var saveFileDialog = new SaveFileDialog();
        saveFileDialog.InitialDirectory = Directory.GetCurrentDirectory();
        saveFileDialog.Filter = "Database files (*.db)|*.db|All files (*.*)|*.*";
        var fileName = "";

        if (saveFileDialog.ShowDialog() == true)
        {
            fileName = saveFileDialog.FileName;
            var connectionStringName = $"Data Source={fileName};Version=3;";
            _sql = new SqliteCrud(connectionStringName);
            _sql.CreateNewDb();
        }
    }

    private void Dg_Sorting(object sender, DataGridSortingEventArgs e)
    {
        if (e.Column.Header.ToString() == "Id")
            DgData.ItemsSource =
                new List<FullCaseModel>((IEnumerable<FullCaseModel>) DgData.ItemsSource).OrderBy(c => c.Id);

        if (e.Column.Header.ToString() == "FormType")
            DgData.ItemsSource =
                new List<FullCaseModel>((IEnumerable<FullCaseModel>) DgData.ItemsSource).OrderBy(c =>
                    c.FormType);

        if (e.Column.Header.ToString() == "CaseStatus")
            DgData.ItemsSource =
                new List<FullCaseModel>((IEnumerable<FullCaseModel>) DgData.ItemsSource).OrderBy(c =>
                    c.CaseStatus);

        if (e.Column.Header.ToString() == "LastStatusChange")
            DgData.ItemsSource =
                new List<FullCaseModel>((IEnumerable<FullCaseModel>) DgData.ItemsSource).OrderBy(c =>
                    c.LastStatusChange);

        if (e.Column.Header.ToString() == "RefreshDate")
            DgData.ItemsSource =
                new List<FullCaseModel>((IEnumerable<FullCaseModel>) DgData.ItemsSource).OrderBy(c =>
                    c.RefreshDate);
    }

    private void ComboBoxFilter_DropDownClosed(object sender, EventArgs e)
    {
        if (ComboBoxFilter.Text == "All Cases")
        {
            DgData.ItemsSource = _listOfCasesDB;
            UpdateInfoBlock(_listOfCasesDB.Count);
        }
        else
        {
            var viewList = (IEnumerable<FullCaseModel>) DgData.ItemsSource;
            var filteredList = viewList.Where(c => c.FormType == ComboBoxFilter.Text).ToList();
            DgData.ItemsSource = new BindingList<FullCaseModel>(filteredList);
            UpdateInfoBlock(filteredList.Count);
        }
    }

    private async void BtnUpdate_Click(object sender, RoutedEventArgs e)
    {
        BtnUpdate.IsEnabled = false;
        UpdateButtonsStatus();
        TxtBlkStatus.Text = "Starting...";

        var listOfUniqueCaseNums = GetListOfUniqueCaseNums();
        await GetCases(listOfUniqueCaseNums);
        UpdateInfoBlock(_listOfCasesDB.Count);
        UpdateButtonsStatus(true, true, true, true, true);
    }

    private async void BtnNewBatch_Click(object sender, RoutedEventArgs e)
    {
        UpdateButtonsStatus();
        BtnNewBatch.IsEnabled = false;
        SetProgressBar(false, "Starting...");
        var listOfCaseNums = new List<string>();
        var casesBefore = uint.Parse(TboxCasesBefore.Text);
        var casesAfter = uint.Parse(TboxCasesAfter.Text);
        listOfCaseNums = _converter.GenerateCaseList(TboxUscisNum.Text, casesBefore, casesAfter);

        await GetCases(listOfCaseNums);
        UpdateButtonsStatus(true, true, true, true, true);
        BtnNewBatch.IsEnabled = true;
    }

    private async Task GetCases(List<string> listOfCaseNums)
    {
        var splits = _converter.SplitList(listOfCaseNums, _numOfTasks);

        var list = new List<Task<List<Tuple<string, string>>>>();
        var progressIndicator = new Progress<MyTaskProgressReport>(ReportProgress);
        PbStatus.Maximum = listOfCaseNums.Count;
        foreach (var item in splits)
        {
            var newWebForTask =
                new WebAccessClient("https://egov.uscis.gov/casestatus/mycasestatus.do");
            list.Add(newWebForTask.GetListOfIdAsync("appReceiptNum", item, progressIndicator));
        }

        var results = await Task.WhenAll(list);

        var htmlList = results.Aggregate(new List<Tuple<string, string>>(), (x, y) => x.Concat(y).ToList());

        var caseList = new List<FullCaseModel>();
        foreach (var html in htmlList)
            caseList.Add(new FullCaseModel(_converter.ExtractCaseData(html.Item1, html.Item2)));

        _listOfCasesDB = new BindingList<FullCaseModel>(caseList);

        DgData.ItemsSource = _listOfCasesDB;
        SetProgressBar(false, "Finished");
    }

    private async void BtnSaveUpdateToDb_Click(object sender, RoutedEventArgs e)
    {
        if (_sql != null)
        {
            UpdateButtonsStatus();
            SetProgressBar(true, "Saving started...");

            await Task.Run(() =>
            {
                foreach (var c in _listOfCasesDB) _sql.UpsertCase(c);
            });

            SetProgressBar(false, "Finished");
            UpdateInfoBlock(_listOfCasesDB.Count);
            UpdateButtonsStatus(true, true, false, true, true);
        }
        else
        {
            MessageBox.Show("Please select create new database or open existing one!", "Database error",
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }


    private async void BtnStatistics_Click(object sender, RoutedEventArgs e)
    {
        SetProgressBar(true, "Starting Stats Calculation");
        var listOfFullCases =
            new List<FullCaseModel>((IEnumerable<FullCaseModel>) DgData.ItemsSource);
        var latestCaseStatus = new List<FullCaseModel>();

        await Task.Run(() =>
            {
                var listOfUniqueCaseNums = GetListOfUniqueCaseNums();
                var orderedList = listOfFullCases.OrderByDescending(c => c.LastStatusChange).ToList();
                foreach (var item in listOfUniqueCaseNums)
                    latestCaseStatus.Add(orderedList.Where(c => c.Id == item).First());
            }
        );

        var statistics = new DataStatisticsModel(latestCaseStatus);

        var basicStatistics = "";
        basicStatistics += statistics.GetBasicStatisticsForFullCases(statistics.UntouchedCasesStatistics,
            "Untouched - Cases that only have NOA1 status -> if noa was in Jan, than case is in Jan");
        basicStatistics += statistics.GetBasicStatisticsForFullCases(statistics.OpenCasesStatistics,
            "It shows monthly processing rate -> month when new status was applied");
        basicStatistics += statistics.GetBasicStatisticsForFullCases(statistics.ClosedCasesStatistics,
            "It shows monthly processing rate -> month when the case was closed");
        basicStatistics += statistics.GetBasicStatisticsForUscisGroups(statistics.StatusPerUscisGroup,
            "Status per USICS group - first 9 characters WAC2190093072 => WAC2190093xxx ");

        basicStatistics += "Done";

        TbStatsOverview.Text = basicStatistics;
        TabStats.Focus();
        GetBasicStackedColumnChart(statistics.OpenCasesStatistics, statistics.ClosedCasesStatistics);
        GetBasicRowChart();
        SetProgressBar(false, "Finished");
    }


    public List<string> GetListOfUniqueCaseNums()
    {
        var output = new List<string>();
        var listOfFullCasesFromView =
            new List<FullCaseModel>((IEnumerable<FullCaseModel>) DgData.ItemsSource);
        output = listOfFullCasesFromView.Select(x => new string(x.Id)).Distinct().ToList();
        return output;
    }

    private void BtnExportExcel_Click(object sender, RoutedEventArgs e)
    {
        var saveFileDialog = new SaveFileDialog();
        saveFileDialog.InitialDirectory = Directory.GetCurrentDirectory();
        saveFileDialog.Filter = "Excel files (*.xlsx)|*.xlsx|All files (*.*)|*.*";
        var fileName = "";

        if (saveFileDialog.ShowDialog() == true)
        {
            fileName = saveFileDialog.FileName;
            var excel = new ExcelCrud();
            SetProgressBar(true, "Starting...");
            var dataViewBindingList = new BindingList<FullCaseModel>((IList<FullCaseModel>) this.DgData.ItemsSource);
            excel.WriteDataToExcel(new List<FullCaseModel>(dataViewBindingList), fileName);
            SetProgressBar(false, "Finished");
        }
    }

    private void BtnClearView_Click(object sender, RoutedEventArgs e)
    {
        UpdateButtonsStatus();
        _listOfCasesDB = new BindingList<FullCaseModel>();
        DgData.ItemsSource = _listOfCasesDB;
        UpdateInfoBlock(_listOfCasesDB.Count);
        _sql = null;
        SetProgressBar(false, "View cleared");
    }


    private void GetBasicStackedColumnChart(List<Tuple<DateTime, int>> inProcessing, List<Tuple<DateTime, int>> closedCases)
    {
        
        List<DateTime> dates = new List<DateTime>();

        foreach (var item in inProcessing)
        {
            dates.Add(item.Item1);
        }

        foreach (var item in closedCases)
        {
            dates.Add(item.Item1);
        }

        dates = dates.Select(c => c).Distinct().OrderBy(c=>c).ToList();

        ChartValues<int> closedChartValues = GetChartValues(dates, closedCases);

        ChartValues<int> inProcessingChartValues =  GetChartValues(dates, inProcessing); ;


        Labels = new List<string>();
        foreach (var d in dates)
        {
            Labels.Add($"{d.Year}-{d.Month}");
        }

        SeriesCollection = new SeriesCollection()
        {

            new StackedColumnSeries() //Cases in processing
            {
                Values = closedChartValues,
                StackMode = StackMode.Values, // this is not necessary, values is the default stack mode
                DataLabels = true,
                Title = "Closed:"
            },
            new StackedColumnSeries() //Cases in processing
            {

                Values = inProcessingChartValues,
                StackMode = StackMode.Values, // this is not necessary, values is the default stack mode
                DataLabels = true,
                Title = "In Processing:"
            },

        };

        DataContext = this;

    }


    public ChartValues<int> GetChartValues(List<DateTime> dates, List<Tuple<DateTime, int>> cases)
    {
        Dictionary<DateTime, int> statisticsDictionary = new Dictionary<DateTime, int>();
        foreach (var date in dates)
        {
            var element = cases.FirstOrDefault(e => e.Item1 == date.Date);
            if (element != null)
            {
                statisticsDictionary.Add(date, element.Item2);
            }
            else
            {
                statisticsDictionary.Add(date, 0);
            }
        }
        ChartValues<int> inProcessingChartValues = new ChartValues<int>();
        foreach (var m in statisticsDictionary)
        {
            inProcessingChartValues.Add(m.Value);
        }

        ChartValues<int> statisticsChartValues = new ChartValues<int>();
        foreach (var m in statisticsDictionary)
        {
            statisticsChartValues.Add(m.Value);
        }

        return statisticsChartValues;
    }

    private void GetBasicRowChart()
    {
        SeriesCollectionRowChart = new SeriesCollection
        {
            new RowSeries
            {
                Title = "2015",
                Values = new ChartValues<double> { 1000, 5000, 3900, 5000 }
            }
        };

        //adding series will update and animate the chart automatically
        SeriesCollectionRowChart.Add(new RowSeries
        {
            Title = "2016",
            Values = new ChartValues<double> { 1100, 5600, 4200 ,4800 }
        });

        ////also adding values updates and animates the chart automatically
        //SeriesCollectionRowChart[1].Values.Add(48d);

        LabelsRowChart = new[] { "Maria", "Susan", "Charles", "Frida" };
        Formatter = value => value.ToString("N");

        DataContext = this;
    }

    /// <summary>
    /// Formats columns during autogeneration
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    private void DgData_AutoGeneratingColumn(object sender, DataGridAutoGeneratingColumnEventArgs e)
    {
        if (e.PropertyType == typeof(System.DateTime))
            (e.Column as DataGridTextColumn).Binding.StringFormat = "yyyy-MM-dd";
    }

    /// <summary>
    /// Filter LastStatus 
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    private void TbDateFilter_KeyUp(object sender, System.Windows.Input.KeyEventArgs e)
    {
        var filteredList = _listOfCasesDB.Where(c => c.LastStatusChange.ToString().Contains(TbDateFilter.Text)).ToList();
        DgData.ItemsSource = new BindingList<FullCaseModel>(filteredList);
        UpdateInfoBlock(filteredList.Count);
    }

}