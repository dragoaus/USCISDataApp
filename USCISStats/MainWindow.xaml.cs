﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using ClassLibrary;
using DataAccessLibrary;
using DataAccessLibrary.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Win32;
using USCISStats.helpers;

namespace USCISStats
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            LoadContents();


        }
        /// <summary>
        /// properties 
        /// </summary>
        private Converter _converter;
        private SqliteCrud _sql;
        private List<WebAccessClient> _webConnections;
        private int _numOfTasks;
        private BindingList<FullCaseModel> _listOfCasesDB;
        private int _progress;
        private WebAccessClient _webAccessClient;

        public async void LoadContents()
        {
            _converter = new Converter();
            _webConnections = new List<WebAccessClient>();
            _listOfCasesDB = new BindingList<FullCaseModel>();
            _numOfTasks = Int32.Parse(TbThreads.Text);
            _webAccessClient = new WebAccessClient("https://egov.uscis.gov/casestatus/mycasestatus.do");
            this.InternetStatus.Content = await _webAccessClient.CheckInternet();
            this.InternetStatus.Background = new SolidColorBrush(Colors.LawnGreen);
        }

        private void BtnOpenDatabase_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.InitialDirectory = Directory.GetCurrentDirectory();
            openFileDialog.Filter = "Database files (*.db)|*.db|All files (*.*)|*.*";
            string fileName = "";

            if (openFileDialog.ShowDialog() == true)
            {
                fileName = openFileDialog.FileName;
                string connectionStringName = $"Data Source={fileName};Version=3;";
                _sql = new SqliteCrud(connectionStringName);
                _listOfCasesDB = new BindingList<FullCaseModel>(_sql.GetAllFullCasesAsModels());
                this.DgData.ItemsSource = _listOfCasesDB;
                TbInfoBlock.Text = $"Cases in Table: {_listOfCasesDB.Count}";
                BtnUpdate.IsEnabled = true;
                BtnStatistics.IsEnabled = true;
            }
        }

        private void BtnNewDb_Click(object sender, RoutedEventArgs e)
        {
            SaveFileDialog saveFileDialog = new SaveFileDialog();
            saveFileDialog.InitialDirectory = Directory.GetCurrentDirectory();
            saveFileDialog.Filter = "Database files (*.db)|*.db|All files (*.*)|*.*";
            string fileName = "";

            if (saveFileDialog.ShowDialog() == true)
            {
                fileName = saveFileDialog.FileName;
                string connectionStringName = $"Data Source={fileName};Version=3;";
                _sql = new SqliteCrud(connectionStringName);
                _sql.CreateNewDb();
            }
        }

        private void dg_Sorting(object sender, DataGridSortingEventArgs e)
        {
            if (e.Column.Header.ToString() == "Id")
            {
                this.DgData.ItemsSource = _listOfCasesDB.OrderBy(c => c.Id);
            }
            if (e.Column.Header.ToString() == "FormType")
            {
                this.DgData.ItemsSource = _listOfCasesDB.OrderBy(c => c.FormType);
            }
        }

        private void BtnFilter_Click(object sender, RoutedEventArgs e)
        {
            //this.dg_Data.ItemsSource = _listOfCasesDB;
            if (ComboBoxFilter.Text == "All Cases")
            {
                this.DgData.ItemsSource = _listOfCasesDB;
                TbInfoBlock.Text = $"Cases in Table: {_listOfCasesDB.Count}";
            }
            else
            {
                var filteredList = _listOfCasesDB.Where(c => c.FormType == ComboBoxFilter.Text).ToList();
                this.DgData.ItemsSource = new BindingList<FullCaseModel>(filteredList);
                TbInfoBlock.Text = $"Cases in Table: {filteredList.Count}";
            }
        }

        private async void BtnUpdate_Click(object sender, RoutedEventArgs e)
        {
            BtnUpdate.IsEnabled = false;
            TxtBlkStatus.Text = $"Starting...";

            List<string> listOfUniqueCaseNums = GetListOfUniqueCaseNums();
            
            await GetCases(listOfUniqueCaseNums);
            BtnUpdate.IsEnabled = true;
            BtnSaveUpdateToDb.IsEnabled = true;
            
        }

        private async void BtnNewBatch_Click(object sender, RoutedEventArgs e)
        {
            TxtBlkStatus.Text = $"Starting...";

            List<string> listOfCaseNums = new List<string>();
            uint casesBefore = UInt32.Parse(TboxCasesBefore.Text);
            uint casesAfter = UInt32.Parse(TboxCasesAfter.Text);
            listOfCaseNums = _converter.GenerateCaseList(TboxUscisNum.Text, casesBefore, casesAfter);

            await GetCases(listOfCaseNums);
            BtnSaveUpdateToDb.IsEnabled = true;
            BtnUpdate.IsEnabled = true;
            BtnStatistics.IsEnabled = true;
        }

        private async Task GetCases(List<string> listOfCaseNums)
        {
            var splits = _converter.SplitList(listOfCaseNums, _numOfTasks);

            List<Task<List<Tuple<string, string>>>> list = new List<Task<List<Tuple<string, string>>>>();
            var progressIndicator = new Progress<MyTaskProgressReport>(ReportProgress);
            PbStatus.Maximum = listOfCaseNums.Count;
            foreach (var item in splits)
            {
                WebAccessClient newWebForTask = new WebAccessClient("https://egov.uscis.gov/casestatus/mycasestatus.do");
                list.Add(newWebForTask.GetListOfIdAsync("appReceiptNum", item, progressIndicator));
            }

            var results = await Task.WhenAll(list);

            var htmlList = results.Aggregate(new List<Tuple<string, string>>(), (x, y) => x.Concat(y).ToList());

            List<FullCaseModel> caseList = new List<FullCaseModel>();
            foreach (var html in htmlList)
            {
                caseList.Add(new FullCaseModel(_converter.ExtractCaseData(html.Item1, html.Item2)));
            }

            _listOfCasesDB = new BindingList<FullCaseModel>(caseList);



            this.DgData.ItemsSource = _listOfCasesDB;

            TxtBlkStatus.Text = $"Finished";
        }
        
        
        /// <summary>
        /// Reports progress of getting cases from USCIS website to UI
        /// </summary>
        /// <param name="progress"></param>
        private async void ReportProgress(MyTaskProgressReport progress)
        {
            _progress += progress.CurrentProgressAmount;
            PbStatus.Value =_progress;
            TxtBlkStatus.Text = $"{_progress}/{PbStatus.Maximum}";
        }

        private async void BtnSaveUpdateToDb_Click(object sender, RoutedEventArgs e)
        {
            if (_sql != null)
            {
                BtnUpdate.IsEnabled = false;
                TxtBlkStatus.Text = $"Saving started...";

                foreach (var c in _listOfCasesDB)
                {
                    _sql.UpsertCase(c);
                }

                BtnSaveUpdateToDb.IsEnabled = false;
                TxtBlkStatus.Text = $"Finished";
                BtnUpdate.IsEnabled = true;
            }
            else
            {
                MessageBox.Show("Please select create new database or open existing one!", "Database error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }


        private async void BtnStatistics_Click(object sender, RoutedEventArgs e)
        {
            PbStatus.IsIndeterminate = true;
            TxtBlkStatus.Text = "Starting Stats Calculation";
            List<FullCaseModel> listOfFullCases = new List<FullCaseModel>((IEnumerable<FullCaseModel>) this.DgData.ItemsSource);
            List<FullCaseModel> latestCaseStatus = new List<FullCaseModel>();

            await Task.Run(() =>
                {
                    List<string> listOfUniqueCaseNums = GetListOfUniqueCaseNums();
                    var orderedList = listOfFullCases.OrderByDescending(c => c.LastUpDateTime).ToList();
                    foreach (var item in listOfUniqueCaseNums)
                    {
                        latestCaseStatus.Add(orderedList.Where(c => c.Id == item).First());
                    }
                }
            );
            
            DataStatisticsModel statistics = new DataStatisticsModel(latestCaseStatus);

            string basicStatistics="";
            basicStatistics += statistics.GetBasicStatisticsForFullCases(statistics.UntouchedCasesStatistics,
                "Untouched - Cases that only have NOA1 status -> if noa was in Jan, than case is in Jan");
            basicStatistics += statistics.GetBasicStatisticsForFullCases(statistics.OpenCasesStatistics,
                "It shows monthly processing rate -> month when new status was applied");
            basicStatistics += statistics.GetBasicStatisticsForFullCases(statistics.ClosedCasesStatistics,
                "It shows monthly processing rate -> month when the case was closed");
            basicStatistics += "Done";

            this.TbStatsOverview.Text = basicStatistics;
            TabStats.Focus();
            PbStatus.IsIndeterminate = false;
            TxtBlkStatus.Text = "Finished";
        }

        public List<string> GetListOfUniqueCaseNums()
        {
            List<string> output = new List<string>();
            List<FullCaseModel> listOfFullCasesFromView = new List<FullCaseModel>((IEnumerable<FullCaseModel>)this.DgData.ItemsSource);
            output = listOfFullCasesFromView.Select(x => new string(x.Id)).Distinct().ToList();
            return output;
        }

    }
}
