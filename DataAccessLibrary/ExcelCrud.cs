using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using DataAccessLibrary.Models;
using NPOI;
using NPOI.SS.UserModel;
using NPOI.XSSF.UserModel;

namespace DataAccessLibrary
{
    /// <summary>
    /// Excel operations
    /// </summary>
    public class ExcelCrud
    {
        public IWorkbook workbook;
        public ExcelCrud()
        {
            workbook = new XSSFWorkbook();
        }

        /// <summary>
        /// Save excel Workbook
        /// </summary>
        /// <param name="path"></param>
        public void SaveWorkbook(string path)
        {
            FileStream sw = File.Create(path);
            workbook.Write(sw);
            sw.Close();
        }

        /// <summary>
        /// Write data from table to excel file and save
        /// </summary>
        /// <param name="list"></param>
        /// <param name="path"></param>
        public void WriteDataToExcel(List<FullCaseModel> listOfCase, string path)
        {

            DataTable dataTable = new DataTable();
            dataTable.TableName = "USCISCases";
            dataTable.Columns.Add("USCISNum", typeof(string));
            dataTable.Columns.Add("Status", typeof(string));
            dataTable.Columns.Add("LastUpdate", typeof(string));
            dataTable.Columns.Add("Form", typeof(string));
            dataTable.Columns.Add("RefreshTime", typeof(string));
            dataTable.Columns.Add("CaseDetails", typeof(string));
            foreach (var c in listOfCase)
            {
                DataRow r = dataTable.NewRow();
                r["USCISNum"] = c.Id;
                r["Status"] = c.CaseStatus;
                r["LastUpdate"] = c.LastStatusChange;
                r["Form"] = c.FormType;
                r["RefreshTime"] = c.RefreshDate;
                r["CaseDetails"] = c.CaseInfos;
                dataTable.Rows.Add(r);
            }

            ISheet sheet = workbook.CreateSheet($"{dataTable.TableName}");
            IRow row = sheet.CreateRow(0);
            ICell cell = row.CreateCell(0);

            int rowsCount = dataTable.Rows.Count;
            int columnsCount = dataTable.Columns.Count;

            for (int i = 0; i < columnsCount; i++)
            {
                cell = row.CreateCell(i);
                cell.SetCellValue(dataTable.Columns[i].ColumnName);
            }

            for (int i = 0; i < rowsCount; i++)
            {
                row = sheet.CreateRow(i+1);
                var dataRow = dataTable.Rows[i];

                for (int j = 0; j < columnsCount; j++)
                {
                    cell = row.CreateCell(j);
                    cell.SetCellValue(dataRow.ItemArray[j].ToString());
                }
            }

            SaveWorkbook(path);
        }
    }
}