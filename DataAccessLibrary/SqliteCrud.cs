using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.Linq;
using System.Text;
using DataAccessLibrary.Models;

namespace DataAccessLibrary
{
    public class SqliteCrud
    {
        private readonly string _connectionString;
        private SqliteDataAccess db = new SqliteDataAccess();

        public SqliteCrud(string connectionString)
        {
            _connectionString = connectionString;
        }

        /// <summary>
        /// Create new Datebase
        /// </summary>
        public void CreateNewDb()
        {
            string sql;
            sql = "CREATE TABLE CaseIDs(" +
                  "Id    TEXT NOT NULL UNIQUE," +
                  "Form  TEXT NOT NULL," +
                  "Refresh  TEXT NOT NULL, " +
                  "PRIMARY KEY(Id))";
            db.CreateNewDb(sql, _connectionString);
            
            sql = "CREATE TABLE Cases(" +
                  "Id    TEXT NOT NULL," +
                  "Status    TEXT NOT NULL, " +
                  "LastUpdate    TEXT NOT NULL," +
                  "Form  TEXT NOT NULL," +
                  "Refresh   TEXT NOT NULL," +
                  "CaseDetails   TEXT NOT NULL)";
            db.CreateNewDb(sql, _connectionString);
        }

        /// <summary>
        /// Get all Full Cases in the Database
        /// </summary>
        /// <returns></returns>
        public List<FullCaseModel> GetAllFullCasesAsModels()
        {
            string sql = "SELECT * FROM Cases";
            return db.LoadData<FullCaseModel,dynamic>(sql, new { }, _connectionString);
        }

        /// <summary>
        /// Get all case IDs from DB
        /// </summary>
        /// <returns></returns>
        public List<string> GetAllCaseIDsAsStrings()
        {
            string sql = "SELECT * FROM CaseIDs";
            List<string> output = new List<string>();
            
            List<BasicCaseModel> listOfCases = new List<BasicCaseModel>();
            listOfCases = db.LoadData<BasicCaseModel, dynamic>(sql, new { }, _connectionString);

            foreach (var item in listOfCases)
            {
                output.Add(item.Id);
            }

            return output;
        }

        /// <summary>
        /// Create new Full Case in Database
        /// </summary>
        /// <param name="uscisCase"></param>
        public void CreateCase(FullCaseModel uscisCase)
        {
            string sql = "INSERT INTO Cases (Id, Status, LastUpdate, Form, Refresh, CaseDetails) values (@Id, @Status, @LastUpdate, @Form, @Refresh, @CaseDetails);";
            db.SaveData(sql,new{Id=uscisCase.Id, Status= uscisCase.CaseStatus, LastUpdate=uscisCase.LastStatusChange, Form=uscisCase.FormType, Refresh= uscisCase.RefreshDate,CaseDetails= uscisCase.CaseInfos}, _connectionString);
        }

        /// <summary>
        /// Update or Insert new case to the database, if the case already exists, it will be updated, otherways it will be created
        /// </summary>
        /// <param name="uscisCase"></param>
        public void UpsertCase(FullCaseModel uscisCase)
        {
            string sql = "SELECT Id, Form, Refresh FROM CaseIDs where Id=@Id";
            BasicCaseModel basicCase = null;
            
            basicCase = db.LoadData<BasicCaseModel, dynamic>(sql, new {Id = uscisCase.Id}, _connectionString)
                .FirstOrDefault();
            if (basicCase == null)
            {
                sql = "INSERT INTO CaseIDs (Id, Form, Refresh) values (@Id, @Form, @Refresh)";
                db.SaveData(sql, new {Id=uscisCase.Id, Form= uscisCase.FormType, Refresh=uscisCase.RefreshDate },_connectionString);
            }
            else
            {
                sql = "UPDATE CaseIDs SET Refresh=@Refresh where Id=@Id";
                db.SaveData(sql, new { Id = uscisCase.Id,  Refresh = uscisCase.RefreshDate }, _connectionString);
            }


            sql = "SELECT * FROM Cases where Id=@Id";
            List<FullCaseModel> listOfCases = db.LoadData<FullCaseModel, dynamic>(sql, new { Id = uscisCase.Id }, _connectionString);
            if (listOfCases.Count == 0)
            {
                sql = "INSERT INTO Cases (Id, Status, LastUpdate, Form, Refresh, CaseDetails) values (@Id, @Status, @LastUpdate, @Form, @Refresh, @CaseDetails);";
                db.SaveData(sql, new { Id = uscisCase.Id, Status = uscisCase.CaseStatus, LastUpdate = uscisCase.LastStatusChange, Form = uscisCase.FormType, Refresh = uscisCase.RefreshDate, CaseDetails = uscisCase.CaseInfos }, _connectionString);
            }
            else if (listOfCases.Last().CaseStatus!=uscisCase.CaseStatus)
            {
                sql = "INSERT INTO Cases (Id, Status, LastUpdate, Form, Refresh, CaseDetails) values (@Id, @Status, @LastUpdate, @Form, @Refresh, @CaseDetails);";
                db.SaveData(sql, new { Id = uscisCase.Id, Status = uscisCase.CaseStatus, LastUpdate = uscisCase.LastStatusChange, Form = uscisCase.FormType, Refresh = uscisCase.RefreshDate, CaseDetails = uscisCase.CaseInfos }, _connectionString);
            }
        }

        public void UpsertListOfCases(List<FullCaseModel> listOfCases)
        {
            List<FullCaseModel> currentCases = GetAllFullCasesAsModels();

            List<FullCaseModel> listOfUpdates = new List<FullCaseModel>();

            foreach (var n in listOfCases)
            {
                var test = currentCases.Any(c => c.Id == n.Id && c.LastStatusChange == n.LastStatusChange);
                if (test == false)
                {
                    listOfUpdates.Add(n);
                }
            }

            foreach (var newCase in listOfUpdates)
            {
                UpsertCase(newCase);
            }
        }

        /// <summary>
        /// Return list of USCIS Models of specific formType
        /// </summary>
        /// <param name="formType"></param>
        /// <returns></returns>
        public List<BasicCaseModel> GetListOfBasicModelsByForm(string formType)
        {
            List<BasicCaseModel> output = new List<BasicCaseModel>();
            
            string sql = "SELECT Id, Form, Refresh FROM CaseIDs where Form=@form";
            output = db.LoadData<BasicCaseModel, dynamic>(sql, new { Form = formType }, _connectionString);

            return output;
        }


        /// <summary>
        /// Return list of strings of USCIS codes of specific type
        /// </summary>
        /// <param name="formType"></param>
        /// <returns></returns>
        public List<string> GetListOfCaseIdsByForm(string formType)
        {
            List<string> output = new List<string>();
            string sql = "SELECT Id FROM CaseIDs where Form=@form";
            output = db.LoadData<string, dynamic>(sql,
                new {Form = formType}, _connectionString);
            return output;
        }

        /// <summary>
        /// Get list of full cases of certain formType
        /// </summary>
        /// <param name="formType"></param>
        /// <returns></returns>
        public List<FullCaseModel> GetListOfFullModelsByForm(string formType)
        {
            List<FullCaseModel> output = new List<FullCaseModel>();

            string sql = "SELECT * FROM Cases where Form=@form";
            output = db.LoadData<FullCaseModel, dynamic>(sql, new { Form = formType }, _connectionString);

            return output;
        }

        /// <summary>
        /// Update formType for one case
        /// </summary>
        /// <param name="id"></param>
        /// <param name="formType"></param>
        public void UpdateFormTypeForCaseIds(string id, string formType)
        {
            string sql = "UPDATE CaseIDs SET Form=@Form where Id=@Id";
            db.SaveData(sql,new {Id=id, Form=formType},_connectionString);
        }

        /// <summary>
        /// Update formType for list of cases
        /// </summary>
        /// <param name="listOfCases"></param>
        public void UpdateFormTypeForListOfCaseIds(List<FullCaseModel> listOfCases)
        {
            foreach (var item in listOfCases)
            {
                UpdateFormTypeForCaseIds(item.Id, item.FormType);
            }
        }
        /// <summary>
        /// Update refresh time for list of cases
        /// </summary>
        /// <param name="uscisCase"></param>
        public void UpdateRefreshTimeForCaseIds(FullCaseModel uscisCase)
        {
            string sql = "UPDATE CaseIDs Set Refresh=@Refresh where Id=@Id";
            db.SaveData(sql, new{Id=uscisCase.Id, Refresh=uscisCase.RefreshDate},_connectionString);
        }

        /// <summary>
        /// Get all case IDs in database
        /// </summary>
        /// <returns></returns>
        public List<BasicCaseModel> GetAllCaseIDsAsModels()
        {
            string sql = "SELECT * FROM CaseIDs";
            List<BasicCaseModel> output;

            List<BasicCaseModel> listOfCases = new List<BasicCaseModel>();
            listOfCases = db.LoadData<BasicCaseModel, dynamic>(sql, new { }, _connectionString);
            output = listOfCases;
            return output;
        }

        /// <summary>
        /// Update case statuses in the database
        /// </summary>
        /// <param name="sql"></param>
        /// <param name="listOfCases"></param>
        /// <param name="formType"></param>
        /// <returns></returns>
        public void UpdateCaseStatus(List<FullCaseModel> listOfCases, string formType)
        {
            List<FullCaseModel> output = new List<FullCaseModel>();
            //get list of cases from the db
            List<FullCaseModel> listOfStatuses = GetListOfFullModelsByForm(formType);

            //compare if there are members of listOfCases that are not contained in list of cases in db
            foreach (var item in listOfCases)
            {
                var temp = listOfStatuses.Where(x => (x.Id == item.Id) && (x.LastStatusChange == item.LastStatusChange)).ToList();
                if (temp.Count == 0)
                {
                    output.Add(item);
                }
            }

            foreach (var item in output)
            {
                UpsertCase(item);
            }
        }

        public List<FullCaseModel> GetLatestStatusInDb(string formType)
        {
            List<FullCaseModel> fullCases = new List<FullCaseModel>();
            List<string> caseIds = new List<string>();

            fullCases = GetListOfFullModelsByForm(formType);
            caseIds = fullCases.Select(c => c.Id).Distinct().ToList();

            return fullCases;
        }
    }
}
