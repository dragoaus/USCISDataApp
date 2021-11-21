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

        public List<FullCaseModel> GetAllFullCasesAsModels()
        {
            string sql = "SELECT * FROM Cases";
            return db.LoadData<FullCaseModel,dynamic>(sql, new { }, _connectionString);
        }

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


        public void CreateCase(FullCaseModel uscisCase)
        {
            string sql = "INSERT INTO Cases (Id, Status, LastUpdate, Form, Refresh, CaseDetails) values (@Id, @Status, @LastUpdate, @Form, @Refresh, @CaseDetails);";
            db.SaveData(sql,new{Id=uscisCase.Id, Status= uscisCase.CaseStatus, LastUpdate=uscisCase.LastUpDateTime, Form=uscisCase.FormType, Refresh= uscisCase.RefreshDateTime,CaseDetails= uscisCase.CaseInfos}, _connectionString);
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
                db.SaveData(sql, new {Id=uscisCase.Id, Form= uscisCase.FormType, Refresh=uscisCase.RefreshDateTime },_connectionString);
            }
            else
            {
                sql = "UPDATE CaseIDs SET Refresh=@Refresh where Id=@Id";
                db.SaveData(sql, new { Id = uscisCase.Id,  Refresh = uscisCase.RefreshDateTime }, _connectionString);
            }


            sql = "SELECT * FROM Cases where Id=@Id";
            List<FullCaseModel> listOfCases = db.LoadData<FullCaseModel, dynamic>(sql, new { Id = uscisCase.Id }, _connectionString);
            if (listOfCases.Count == 0)
            {
                sql = "INSERT INTO Cases (Id, Status, LastUpdate, Form, Refresh, CaseDetails) values (@Id, @Status, @LastUpdate, @Form, @Refresh, @CaseDetails);";
                db.SaveData(sql, new { Id = uscisCase.Id, Status = uscisCase.CaseStatus, LastUpdate = uscisCase.LastUpDateTime, Form = uscisCase.FormType, Refresh = uscisCase.RefreshDateTime, CaseDetails = uscisCase.CaseInfos }, _connectionString);
            }
            else if (listOfCases.Last().CaseStatus!=uscisCase.CaseStatus)
            {
                sql = "INSERT INTO Cases (Id, Status, LastUpdate, Form, Refresh, CaseDetails) values (@Id, @Status, @LastUpdate, @Form, @Refresh, @CaseDetails);";
                db.SaveData(sql, new { Id = uscisCase.Id, Status = uscisCase.CaseStatus, LastUpdate = uscisCase.LastUpDateTime, Form = uscisCase.FormType, Refresh = uscisCase.RefreshDateTime, CaseDetails = uscisCase.CaseInfos }, _connectionString);
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
            
            string sql = "SELECT Id, Form, Refresh FROM CaseIDs where Form=@formType";
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
            string sql = "SELECT Id FROM CaseIDs where Form=@formType";
            output = db.LoadData<string, dynamic>(sql,
                new {Form = formType}, _connectionString);
            return output;
        }


        /// <summary>
        /// 
        /// </summary>
        /// <param name="formType"></param>
        /// <returns></returns>
        public List<FullCaseModel> GetListOfFullModelsByForm(string formType)
        {
            List<FullCaseModel> output = new List<FullCaseModel>();

            string sql = "SELECT * FROM Cases where Form=@formType";
            output = db.LoadData<FullCaseModel, dynamic>(sql, new { Form = formType }, _connectionString);

            return output;
        }

        public void UpdateFormTypeForCaseIds(string id, string formType)
        {
            string sql = "UPDATE CaseIDs SET Form=@Form where Id=@Id";
            db.SaveData(sql,new {Id=id, Form=formType},_connectionString);
        }

        public void UpdateFormTypeForListOfCaseIds(List<FullCaseModel> listOfCases)
        {
            foreach (var item in listOfCases)
            {
                UpdateFormTypeForCaseIds(item.Id, item.FormType);
            }
        }

        public void UpdateRefreshTimeForCaseIds(FullCaseModel uscisCase)
        {
            string sql = "UPDATE CaseIDs Set Refresh=@Refresh where Id=@Id";
            db.SaveData(sql, new{Id=uscisCase.Id, Refresh=uscisCase.RefreshDateTime},_connectionString);
        }

        public List<BasicCaseModel> GetAllCaseIDsAsModels()
        {
            string sql = "SELECT * FROM CaseIDs";
            List<BasicCaseModel> output;

            List<BasicCaseModel> listOfCases = new List<BasicCaseModel>();
            listOfCases = db.LoadData<BasicCaseModel, dynamic>(sql, new { }, _connectionString);
            output = listOfCases;
            return output;
        }



    }
}
