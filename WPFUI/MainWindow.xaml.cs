using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Net.Mail;
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
using System.Text.RegularExpressions;
using System.Windows.Forms;
using ClassLibrary;
using DataAccessLibrary;
using DataAccessLibrary.Models;
using Microsoft.Extensions.Configuration;
using Ookii.Dialogs.Wpf;
using SharpVectors.Converters;
using SharpVectors.Renderers.Wpf;
using WebAccessLibrary;
using MessageBox = System.Windows.MessageBox;
using OpenFileDialog = Microsoft.Win32.OpenFileDialog;

namespace WPFUI
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            dg_List.DataContext = BindingListOfModels;
            CheckInternetConnection();
            OnWindowLoaded();
        }

        /// <summary>
        /// Variables and Static Members
        /// </summary>
        static readonly WebAccess webConnectionCheck = new WebAccess("https://egov.uscis.gov/casestatus/mycasestatus.do");
        static readonly List<WebAccess> webConnections = new List<WebAccess>();

        private SqliteCrud sql;
        public static BindingList<FullCaseModel> BindingListOfModels { get; set; } = new BindingList<FullCaseModel>();

        private void OnWindowLoaded()
        {
            string pathToFile = Directory.GetCurrentDirectory() + "\\icons\\";
            
            string database_addSVG = pathToFile + "database_add.svg";
            img_NewDatabase.Source = ConvertSVGtoXAML(database_addSVG);

            string database_goSVG = pathToFile + "database_go.svg";
            img_DatabaseLocation.Source = ConvertSVGtoXAML(database_goSVG);

            string database_saveSVG = pathToFile + "database_save.svg";
            img_SaveDatabase.Source = ConvertSVGtoXAML(database_saveSVG);
        }

        private static DrawingImage ConvertSVGtoXAML(string filePath)
        {
            DrawingImage output;

            WpfDrawingSettings settings = new WpfDrawingSettings();
            settings.IncludeRuntime = true;
            settings.TextAsGeometry = false;

            FileSvgReader converter = new FileSvgReader(settings);
            DrawingGroup drawing = converter.Read(filePath);

            output = new DrawingImage(drawing);
            
            return output;
        }


        /// <summary>
        /// Methods
        /// </summary>
        private void tb_testNumInput_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            Regex regex = new Regex("[^0-9]+");
            e.Handled = regex.IsMatch(e.Text);
        }

        private void tb_USCISCase_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            Regex regex = new Regex(@"[A-Z]{3}\d{10}");
            e.Handled = regex.IsMatch(e.Text);
        }

        private void btn_SelectFolderLocation_Click(object sender, RoutedEventArgs e)
        {
            string result = GetFilePathSave(); 
            tb_TargetFolder.Text = result;
            TabControl.SelectedIndex = 0;
        }

        private async void CheckInternetConnection()
        {
            tb_Status.Text = await webConnectionCheck.CheckInternet();
        }

        private async void  btn_NewListStart_Click(object sender, RoutedEventArgs e)
        {
            // check if the input of the number is ok
            if (Regex.IsMatch(tb_USCISCase.Text.ToUpper(), @"[A-Z]{3}\d{10}") == false)
            {
                MessageBox.Show("Please check case number is ok.");
                return;
            }

            if (tb_TargetFolder.Text.Length == 0)
            {
                MessageBox.Show("Please select files location and name.");
                return;
            }
            
            SqliteCrud sql = new SqliteCrud(GetConnectionString(tb_TargetFolder.Text));
            sql.CreateNewDb();
            
            uint before = uint.Parse(tb_LengthBefore.Text);
            uint after = uint.Parse(tb_LengthAfter.Text);
            Converter c = new Converter();
            List<string> listOfCaseNums = GenerateListOfCases(c, tb_USCISCase.Text, before, after);

            int numOfTasks = int.Parse(tb_NumOfTasksNew.Text);
            List<FullCaseModel> listOfCases = await GetCasesFromWebSiteParallelAsync(c, listOfCaseNums, numOfTasks);

            UpdateCase(sql, listOfCases);
            BindingListOfModels = new BindingList<FullCaseModel>();

            foreach (var item in listOfCases)
            {
                BindingListOfModels.Add(item);
            }
            dg_List.DataContext = BindingListOfModels;
            
        }

        /////////////////////////////////
        private void btn_SelectDBLocation_Click(object sender, RoutedEventArgs e)
        {
            string result = GetFilePathOpen();

            if (result=="")
            {
                MessageBox.Show("Please select database.");
                return;
            }

            l_filePath.Content = result;
            TabControl.SelectedIndex = 1;
            sql = new SqliteCrud(GetConnectionString(result));

            var listOfFullCases = GetAllCases(sql);

            // Set number of oldest cases to update
            BindingListOfModels = new BindingList<FullCaseModel>();
            foreach (var item in listOfFullCases)
            {
                BindingListOfModels.Add(item);
            }

            dg_List.DataContext = BindingListOfModels;
        }

        private async void btn_UpdateExistingDB_Click(object sender, RoutedEventArgs e)
        {
            Converter c = new Converter();

            string labelLenghtCheck = l_filePath.Content.ToString();
            if (labelLenghtCheck != null && labelLenghtCheck.Length == 0)
            {
                MessageBox.Show("Please select files location and name.");
                return;
            }

            int numOfCasesToUpdate = int.Parse(tb_NumOfCasesToUpdate.Text);

            if (cb_CaseType.Text.Length == 0)
            {
                MessageBox.Show("Form Type.");
                cb_CaseType.Focus();
                return;
            }
            var formType = cb_CaseType.Text;

            var listOfBasicCases = FilterBaseCaseModelByForm(sql, formType);
            
            // Set number of oldest cases to update
            listOfBasicCases = listOfBasicCases.OrderBy(c => c.RefreshDateTime).ToList();
            listOfBasicCases = listOfBasicCases.GetRange(0, numOfCasesToUpdate);

            var listOfCases = new List<string>();
            foreach (var item in listOfBasicCases)
            {
                listOfCases.Add(item.Id);
            }

            var numOfTasks = int.Parse(tb_NumOfTasksOpen.Text);
            var listOfDownloadedCases = await GetCasesFromWebSiteParallelAsync(c, listOfCases, numOfTasks);
            UpdateCaseStatus(sql, listOfDownloadedCases, formType);

            listOfDownloadedCases = listOfDownloadedCases.OrderBy(c => c.Id).ToList();

            BindingListOfModels = new BindingList<FullCaseModel>();
            foreach (var item in listOfDownloadedCases)
            {
                BindingListOfModels.Add(item);
            }
            ICollectionView cv_sortList = CollectionViewSource.GetDefaultView(dg_List.ItemsSource);
            //dg_List.DataContext = BindingListOfModels;
        }


        //private void dg_List_Sorting(object sender, DataGridSortingEventArgs e)
        //{
        //    DataGridColumn column = e.Column;

        //    IComparer comparer = null;

        //    //i do some custom checking based on column to get the right comparer
        //    //i have different comparers for different columns. I also handle the sort direction
        //    //in my comparer

        //    // prevent the built-in sort from sorting
        //    e.Handled = true;

        //    ListSortDirection direction = (column.SortDirection != ListSortDirection.Ascending) ? ListSortDirection.Ascending : ListSortDirection.Descending;

        //    //set the sort order on the column
        //    column.SortDirection = direction;

        //    //use a ListCollectionView to do the sort.
        //    ListCollectionView lcv = (ListCollectionView)CollectionViewSource.GetDefaultView(this.ItemsSource);

        //    //this is my custom sorter it just derives from IComparer and has a few properties
        //    //you could just apply the comparer but i needed to do a few extra bits and pieces
        //    //comparer = new ResultSort(direction);

        //    //apply the sort
        //    //lcv.CustomSort = comparer;
        //}

        /////////////////////////////////

        /// <summary>
        /// Update Form Types for CaseIDs DB
        /// </summary>
        /// <param name="sql"></param>
        /// <param name="list"></param>
        public static void UpdateFormType(SqliteCrud sql, List<FullCaseModel> list)
        {
            foreach (var item in list)
            {
                sql.UpdateFormTypeForCaseIds(item.Id, item.FormType);
            }
        }

        /// <summary>
        /// Update case statuses in the DB
        /// </summary>
        /// <param name="sql"></param>
        /// <param name="listOfCases"></param>
        /// <param name="formType"></param>
        /// <returns></returns>
        public static void UpdateCaseStatus(SqliteCrud sql, List<FullCaseModel> listOfCases, string formType)
        {
            List<FullCaseModel> output = new List<FullCaseModel>();
            //get list of cases from the db
            List<FullCaseModel> listOfStatuses = FilterStatusByForm(sql, formType);

            //compare if there are members of listOfCases that are not contained in list of cases in db and update refresh time in CaseIdDB
            foreach (var item in listOfCases)
            {
                sql.UpdateRefreshTimeForCaseIds(item);
                
                var temp = listOfStatuses.Where(x => (x.Id == item.Id) && (x.LastUpDateTime == item.LastUpDateTime))
                    .ToList();
                if (temp.Count == 0)
                {
                    output.Add(item);
                }
            }

            foreach (var item in output)
            {
                sql.UpsertCase(item);
            }
        }

        /// <summary>
        /// Update Cases from the list
        /// </summary>
        /// <param name="sql"></param>
        /// <param name="listOfCases"></param>
        public static void UpdateCase(SqliteCrud sql, List<FullCaseModel> listOfCases)
        {
            foreach (var item in listOfCases)
            {
                sql.UpsertCase(item);
            }
        }

        /// <summary>
        /// Get connection string 
        /// </summary>
        /// <param name="connectionStringName"></param>
        /// <returns></returns>
        public static string GetConnectionString(string path)
        {
            string output = "Data Source=" + path + ";Version=3;";

            return output;
        }


        /// <summary>
        /// Generates list of cases based on input case
        /// </summary>
        /// <param name="c"></param>
        /// <param name="startCase"></param>
        /// <param name="before"></param>
        /// <param name="after"></param>
        /// <returns></returns>
        public static List<string> GenerateListOfCases(Converter c, string startCase, uint before, uint after)
        {
            List<string> output = new List<string>();
            output = c.GenerateCaseList(c.SplitUscisNum(startCase), before, after);

            return output;
        }


        /// <summary>
        /// Get all case of certain form type
        /// </summary>
        /// <param name="sql"></param>
        /// <param name="formType"></param>
        /// <returns></returns>
        public static List<string> FilterCaseIdsByForm(SqliteCrud sql, string formType)
        {
            List<string> output = new List<string>();
            List<BasicCaseModel> listOfCases = sql.FilterCaseIdsByForm(formType);
            listOfCases = listOfCases.OrderBy(c=>c.RefreshDateTime).ToList();

            foreach (var item in listOfCases)
            {
                output.Add(item.Id);
            }

            return output;
        }

        public static List<BasicCaseModel> FilterBaseCaseModelByForm(SqliteCrud sql, string formType)
        {
            List<BasicCaseModel> output = sql.FilterCaseIdsByForm(formType);
            
            return output;
        }


        /// <summary>
        /// Get all statuses of certain type
        /// </summary>
        /// <param name="sql"></param>
        /// <param name="formType"></param>
        /// <returns></returns>
        public static List<FullCaseModel> FilterStatusByForm(SqliteCrud sql, string formType)
        {
            List<FullCaseModel> output = new List<FullCaseModel>();
            List<FullCaseModel> listOfCases = sql.FilterStatusesByForm(formType);
            output = listOfCases.OrderBy(c => c.Id).ThenBy(c => c.LastUpDateTime).ToList();
            return output;
        }

        public static List<FullCaseModel> GetAllCases(SqliteCrud sql)
        {
            List<FullCaseModel> output = sql.GetAllFullCasesAsModels();
            return output;
        }

        /// <summary>
        /// Update list of case and return new status for the cases
        /// </summary>
        /// <param name="web"></param>
        /// <param name="idList"></param>
        /// <returns></returns>
        public static async Task<List<Tuple<string, string>>> UpdateCaseStatusAsync(WebAccess web, List<string> idList)
        {
            // initialize list of tuples to store USCIS feedback
            List<Tuple<string, string>> htmlList = new List<Tuple<string, string>>();

            foreach (var id in idList)
            {
                htmlList.Add(await web.GetIdAsync("appReceiptNum", id));
            }

            return htmlList;
        }

        /// <summary>
        /// Download data from USCIS website
        /// </summary>
        /// <param name="c"></param>
        /// <param name="listOfCaseNums"></param>
        /// <param name="numOfTasks"></param>
        /// <returns></returns>
        public static async Task<List<FullCaseModel>> GetCasesFromWebSiteParallelAsync(Converter c,
            List<string> listOfCaseNums, int numOfTasks)
        {
            List<FullCaseModel> output = new List<FullCaseModel>();

            var splits = c.SplitList(listOfCaseNums, numOfTasks);

            List<Task<List<Tuple<string, string>>>> list = new List<Task<List<Tuple<string, string>>>>();

            foreach (var item in splits)
            {
                WebAccess newWebForTask = new WebAccess("https://egov.uscis.gov/casestatus/mycasestatus.do");
                webConnections.Add(newWebForTask);
                list.Add(UpdateCaseStatusAsync(newWebForTask, item));
            }

            var results = await Task.WhenAll(list);
            var htmlList = results.Aggregate(new List<Tuple<string, string>>(), (x, y) => x.Concat(y).ToList());

            List<FullCaseModel> caseList = new List<FullCaseModel>();
            foreach (var html in htmlList)
            {
                caseList.Add(new FullCaseModel(c.ExtractCaseData(html.Item1, html.Item2)));
            }

            output = caseList;

            return output;
        }

        public static string GetFilePathSave()
        {
            string output;

            using (FileDialog sfd = new SaveFileDialog())
            {
                sfd.InitialDirectory = Directory.GetCurrentDirectory();
                sfd.Filter = "SQLite Database (*.db, *.db3, *.sqlite, *.sqlite3)|*.db;*.db3;*.sqlite;*.sqlite3";
                sfd.ShowDialog();
                output = sfd.FileName;
            }

            return output;
        }

        public static string GetFilePathOpen()
        {
            string output;

            OpenFileDialog ofd = new OpenFileDialog();
            ofd.InitialDirectory = Directory.GetCurrentDirectory();
            ofd.Filter = "SQLite Database (*.db, *.db3, *.sqlite, *.sqlite3)|*.db;*.db3;*.sqlite;*.sqlite3";
            ofd.ShowDialog();
            output = ofd.FileName;

            return output;
        }

        
    }

}