using System;
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
            _c = new Converter();
            _webConnections = new List<WebAccessClient>();
            _listOfCases = new BindingList<FullCaseModel>();
            _numOfTasks = 200;
            this.dg_Data.ItemsSource= _listOfCases;
        }
        /// <summary>
        /// properties 
        /// </summary>
        private Converter _c;
        private SqliteCrud _sql;
        private List<WebAccessClient> _webConnections;
        private int _numOfTasks;
        private BindingList<FullCaseModel> _listOfCases;




        private void OpenDatabase(object sender, RoutedEventArgs e)
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
                _listOfCases = new BindingList<FullCaseModel>(_sql.GetAllFullCasesAsModels());
                this.dg_Data.ItemsSource = _listOfCases;
            }

            
        }

        public static string GetConnectionString(string connectionStringName = "Default")
        {
            string output = "";
            var builder = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json");

            var config = builder.Build();

            output = config.GetConnectionString(connectionStringName);

            return output;
        }


        private void dg_Sorting(object sender, DataGridSortingEventArgs e)
        {
            if (e.Column.Header.ToString() == "Id")
            {
                this.dg_Data.ItemsSource = _listOfCases.OrderBy(c => c.Id);
            }
            if (e.Column.Header.ToString() == "FormType")
            {
                this.dg_Data.ItemsSource = _listOfCases.OrderBy(c => c.FormType);
            }

        }

        private void btn_Filter_Click(object sender, RoutedEventArgs e)
        {
            //this.dg_Data.ItemsSource = _listOfCases;
            if (ComboBoxFilter.Text == "All Cases")
            {
                this.dg_Data.ItemsSource = _listOfCases;
            }
            else
            {
                this.dg_Data.ItemsSource = new BindingList<FullCaseModel>(_listOfCases.Where(c => c.FormType == ComboBoxFilter.Text).ToList());
            }
        }

        private async void BtnUpdate_Click(object sender, RoutedEventArgs e)
        {
            List<string> listOfCaseNums = new List<string>();

            foreach (var item in _listOfCases)
            {
                listOfCaseNums.Add(item.Id);
            }

            var splits = _c.SplitList(listOfCaseNums, _numOfTasks);

            List<Task<List<Tuple<string, string>>>> list = new List<Task<List<Tuple<string, string>>>>();

            foreach (var item in splits)
            {
                WebAccessClient newWebForTask = new WebAccessClient("https://egov.uscis.gov/casestatus/mycasestatus.do");
                _webConnections.Add(newWebForTask);
                list.Add(newWebForTask.GetListOfIdAsync("appReceiptNum", item));
            }

            var results = await Task.WhenAll(list);
            var htmlList = results.Aggregate(new List<Tuple<string, string>>(), (x, y) => x.Concat(y).ToList());

            List<FullCaseModel> caseList = new List<FullCaseModel>();
            foreach (var html in htmlList)
            {
                caseList.Add(new FullCaseModel(_c.ExtractCaseData(html.Item1, html.Item2)));
            }

            _listOfCases = new BindingList<FullCaseModel>(caseList);
            this.dg_Data.ItemsSource = _listOfCases;
        }
    }
}
