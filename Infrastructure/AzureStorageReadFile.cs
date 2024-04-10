using Box.V2.Models;
using CRES.DataContract;
using CRES.Utilities;
using ExcelDataReader;
using Microsoft.Azure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Web;
using System.Web.Helpers;
using CRES.BusinessLogic;

namespace CRES.Services
{
    public class AzureStorageReadFile
    {
        IConfigurationSection Sectionroot = null;
        static IConfigurationSection SectionrootStatic = null;
        public string connectionString = "";
        public string sourceContainerName = "";

        public AzureStorageReadFile()
        {
            GetConfigSetting();
            connectionString = Sectionroot.GetSection("storage:container:connectionstring").Value;
            sourceContainerName = Sectionroot.GetSection("storage:container:name").Value;
        }


        public void GetConfigSetting()
        {
            if (Sectionroot == null)
            {
                IConfigurationBuilder builder = new ConfigurationBuilder();
                builder.AddJsonFile(Path.Combine(Directory.GetCurrentDirectory(), "appsettings.json"));
                var root = builder.Build();
                Sectionroot = root.GetSection("Application");
            }
        }

        public static void GetConfigSettingforStatic()
        {
            if (SectionrootStatic == null)
            {
                IConfigurationBuilder builder = new ConfigurationBuilder();
                builder.AddJsonFile(Path.Combine(Directory.GetCurrentDirectory(), "appsettings.json"));
                var root = builder.Build();
                SectionrootStatic = root.GetSection("Application");
            }
        }

        /// <summary>
        /// ExtractExcelData
        /// Extracts the data and leave the header
        /// </summary>
        /// <param name="excelData"></param>
        /// <returns></returns>
        private static DataTable ExtractExcelData(DataSet excelData, string sourceBlobFileName, string DisplayFileName, string _fromdate, string _todate, string isconfirmed)
        {
            var dt = new DataTable();
            dt.Columns.Add("NoteID", typeof(string));
            dt.Columns.Add("TransactionType", typeof(string));
            dt.Columns.Add("TransactionDate", typeof(DateTime));
            // dt.Columns.Add("EffectiveDate", typeof(DateTime));            
            dt.Columns.Add("DateDue", typeof(DateTime));
            dt.Columns.Add("PrincipalPayment", typeof(decimal));
            dt.Columns.Add("InterestPayment", typeof(decimal));
            dt.Columns.Add("ErrorMessage", typeof(string));
            try
            {

                string[] FixedcolumnNames = { "Note ID", "Transaction Type", "Effective Date", "Date Due", "Principal", "Interest Payment" };
                //string[] FixedcolumnNames = { "Note ID", "Transaction Type", "Transaction Date", "Date Due", "Principal Payment", "Interest Payment" };

                string[] columnNames = excelData.Tables[0].Columns.Cast<DataColumn>().Select(x => x.ColumnName).ToArray();

                var resCount = columnNames.Intersect(FixedcolumnNames);
                //var resCount = columnNames.Where(x => FixedcolumnNames.Any(y => y.Contains(x)));

                if (resCount.Count() != FixedcolumnNames.Count())
                {
                    DataRow dr = dt.NewRow();
                    dr["ErrorMessage"] = "File is not in correct format.";
                    dt.Rows.Add(dr);
                    return dt;
                }
                else
                {
                    System.Data.DataView dv = new System.Data.DataView(excelData.Tables[0]);

                    //   dv.RowFilter = "[Transaction Type]='02' or [Transaction Type]='04'";
                    //   dt= excelData.Tables[0].Select("[Note ID],[Transaction Type],[Transaction Date],[Date Due],[Principal Payment],[Interest Payment]").CopyToDataTable();
                    DataTable dtn = dv.ToTable(false, "Note ID", "Transaction Type", "Effective Date", "Date Due", "Principal", "Interest Payment");
                    if (dtn.Rows.Count > 0)
                    {
                        dtn.Columns["Note ID"].ColumnName = "NoteID";
                        dtn.Columns["Transaction Type"].ColumnName = "TransactionType";
                        dtn.Columns["Effective Date"].ColumnName = "TransactionDate";
                        dtn.Columns["Date Due"].ColumnName = "DateDue";
                        dtn.Columns["Principal"].ColumnName = "PrincipalPayment";
                        dtn.Columns["Interest Payment"].ColumnName = "InterestPayment";
                        dtn.AcceptChanges();

                        var mindate = dtn.Compute("min([DateDue])", string.Empty);
                        var maxdate = dtn.Compute("max([DateDue])", string.Empty);

                        if (isconfirmed != "true")
                        {
                            //    DateTime dtmindate = Convert.ToDateTime(mindate.ToString(), "MM/dd/yyyy hh:mm:ss", CultureInfo.InvariantCulture);

                            //  DataTable Dt1 = dv.ToTable();
                            // int maxdate = Convert.ToInt32(dtn.AsEnumerable().Max(row => row["TransactionDate"]));

                            DateTime excmindate = DateTime.ParseExact(Convert.ToDateTime(mindate.ToString()).ToString("MM/dd/yyyy"), "MM/dd/yyyy", CultureInfo.InvariantCulture);
                            DateTime startdate = DateTime.ParseExact(_fromdate, "MM/dd/yyyy", CultureInfo.InvariantCulture);

                            DateTime excmaxdate = DateTime.ParseExact(Convert.ToDateTime(maxdate.ToString()).ToString("MM/dd/yyyy"), "MM/dd/yyyy", CultureInfo.InvariantCulture);
                            DateTime enddate = DateTime.ParseExact(_todate, "MM/dd/yyyy", CultureInfo.InvariantCulture);


                            DataRow dr = dt.NewRow();
                            if (excmindate > startdate)
                            {
                                dr["ErrorMessage"] = "The start date (" + startdate.ToString("MM/dd/yyyy") + ") should not be less than minimum due date of sheet (" + excmindate.ToString("MM/dd/yyyy") + ").";
                                //  msg = "The minimum due date of sheet (" + excmindate.ToString("MM/dd/yyyy") + ") should not be less than start date (" + startdate.ToString("MM/dd/yyyy") + ").";
                                dt.Rows.Add(dr);
                                return dt;
                            }
                            if (excmaxdate < enddate)
                            {
                                dr["ErrorMessage"] = "The end date (" + enddate.ToString("MM/dd/yyyy") + ") should not be greater than maximum due date of sheet (" + excmaxdate.ToString("MM/dd/yyyy") + ").";
                                // msg =  "The maximum due date of sheet (" + excmaxdate.ToString("MM/dd/yyyy") + ") should not be grater than end date (" + enddate.ToString("MM/dd/yyyy") + ").";
                                dt.Rows.Add(dr);
                                return dt;
                            }
                            else if (startdate == excmindate && enddate == excmaxdate)
                            {
                                foreach (DataRow dwr in dtn.Rows)
                                {
                                    dt.ImportRow(dwr);
                                }
                                return dt;
                            }
                            else if (excmindate < startdate || excmaxdate > enddate)
                            {
                                dr["ErrorMessage"] = "File uploaded contains transactions for " + excmindate.ToString("MM/dd/yyyy") + " and " + excmaxdate.ToString("MM/dd/yyyy") + " , although you selected a date range between " + startdate.ToString("MM/dd/yyyy") + " and " + enddate.ToString("MM/dd/yyyy") + ". System will replace transactions in selected date range. Do you want to continue?";
                                dt.Rows.Add(dr);
                                return dt;
                            }
                            //dr["ErrorMessage"] = msg;
                            //if (msg != "")
                            //{
                            //    dt.Rows.Add(dr);
                            //    // return dt;

                            //}
                        }
                        else
                        {
                            //_fromdate = mindate.ToString();
                            //_todate = maxdate.ToString();

                            foreach (DataRow dwr in dtn.Rows)
                            {
                                dt.ImportRow(dwr);
                            }
                            // dt.Columns.Remove("ErrorMessage");
                        }
                        return dt;


                    }
                    else
                    {
                        DataRow dr = dt.NewRow();
                        dr["ErrorMessage"] = " does not have data.";
                        dt.Rows.Add(dr);
                        return dt;

                    }
                }
            }
            catch (Exception ex)
            {
                DataRow dr = dt.NewRow();
                dr["ErrorMessage"] = ex.Message;
                dt.Rows.Add(dr);
                return dt;
            }
        }


        public DataTable GetExcelData(string sourceBlobFileName, string DisplayFileName, string _fromdate, string _todate, string isconfirmed)
        {

            DataTable refinedExcelData = new DataTable();
            try
            {
                var excelData = GetExcelBlobData(sourceBlobFileName, connectionString, sourceContainerName);
                refinedExcelData = ExtractExcelData(excelData, sourceBlobFileName, DisplayFileName, _fromdate, _todate, isconfirmed);
            }
            catch (Exception)
            {

                throw;
            }
            return refinedExcelData;
        }

        public DataTable ValidateDataFromConfig(DataTable dt, JsonFileConfiguration FC)
        {
            DataTable filterDT = new DataTable();
            string[] arr = new string[FC.MappingColumns.Count];
            string[] totalarr = new string[FC.MappingColumns.Count];

            dt.Columns.Cast<DataColumn>().Select(x => x.ColumnName).ToList().ForEach(y =>
            {
                if (Regex.IsMatch(y, @"_\d"))
                {
                    dt.Columns.Remove(y);
                }
            });


            string[] dtarr = new string[dt.Columns.Count];

            string filtervalues = "", colvalue = "";
            var k = 0; var index = 0;
            bool isDoubleBlankRow = false;
            try
            {
                if (FC.MappingColumns.Count < dt.Columns.Count)
                {
                    filterDT.Columns.Add("ErrorMessage", typeof(string));
                    DataRow dr = filterDT.NewRow();

                    dr["ErrorMessage"] = "File is not in correct format.";
                    filterDT.Rows.Add(dr);
                    return filterDT;

                }

                //Remove all blank rows from data table
                //    dt = dt.Rows.Cast<DataRow>().ToList().FindIndex(row => !row.ItemArray.All(field => field is DBNull || string.IsNullOrWhiteSpace(field as string) || field.ToString().Trim() == "Totals" || field.ToString().Trim() == "Total")).CopyToDataTable();
                if (FC.Check2BlankRow == true)
                {
                    var lstblankObject = dt.Rows.Cast<DataRow>().ToList().Select((x, i) => new { x, i }).Where(y => (y.x.ItemArray.All(field => field is DBNull || string.IsNullOrWhiteSpace(field as string)) == true)).Select(w => w.i).ToList();

                    if (lstblankObject.Count > 1)
                    {
                        for (int i = 0; i < lstblankObject.Count; i++)
                        {
                            if (lstblankObject[i] + 1 == lstblankObject[i + 1])
                            {
                                index = lstblankObject[i];
                                isDoubleBlankRow = true;
                                break;
                            }
                        }
                    }
                    if (isDoubleBlankRow)
                    {
                        for (var i = index; i < dt.Rows.Count; i++)
                        {
                            dt.Rows.RemoveAt(i);
                            i--;
                        }
                    }

                    //Remove all blank rows from data table
                    if (dt.Rows.Count > 0)
                    {
                        dt = dt.Rows.Cast<DataRow>().Where(row => !row.ItemArray.All(field => field is DBNull || string.IsNullOrWhiteSpace(field as string))).CopyToDataTable();
                    }

                }
                else
                {

                    var Onerowindex = dt.Rows.Cast<DataRow>().ToList().FindIndex(row => row.ItemArray.All(field => field is DBNull || string.IsNullOrWhiteSpace(field as string)));
                    //if (Onerowindex == 0)
                    //{
                    var cn = 0;
                    if (FC.SheetName == "Berkadia")
                    {
                        foreach (DataRow dr in dt.Rows)
                        {
                            if (dr["InvestorLoanNumber"].ToString() == "")
                            {
                                if (dr["DatePaymentDue"].ToString() == "" && dr["RemittanceDate"].ToString() == "")
                                {
                                    Onerowindex = cn;
                                    break;
                                }
                            }
                            else
                                cn++;
                        }
                    }
                    else
                    {
                        foreach (DataRow dr in dt.Rows)
                        {
                            if (dr["Note ID"].ToString() == "")
                            {
                                if (dr["Due Date"].ToString() == "" && dr["Remit Date"].ToString() == "")
                                {
                                    Onerowindex = cn;
                                    break;
                                }
                            }
                            else
                                cn++;
                        }
                    }
                    // }
                    if (Onerowindex > 0)
                    {
                        for (var i = Onerowindex; i < dt.Rows.Count; i++)
                        {
                            dt.Rows.RemoveAt(i);
                            i--;
                        }
                    }

                }

                foreach (DataColumn column in dt.Columns)
                {
                    dtarr[k] = column.ColumnName;
                    k++;
                }


                for (var i = 0; i < FC.MappingColumns.Count; i++)
                {
                    if (FC.MappingColumns[i].IsMandatoryColumn == "Y")
                    {
                        filtervalues = filtervalues + "" + FC.MappingColumns[i].SheetColumnName + ",";
                    }

                    colvalue = colvalue + "" + FC.MappingColumns[i].SheetColumnName + ",";
                }

                filtervalues = filtervalues.Remove(filtervalues.Length - 1, 1);
                arr = filtervalues.Split(',');
                var diff = dtarr.Intersect(arr);
                colvalue = colvalue.Remove(colvalue.Length - 1, 1);
                totalarr = colvalue.Split(',');
                //   var notexist = dtarr.Except(arr).ToArray();
                //Columns which are not in the excel sheet.
                var notexist = arr.Except(dtarr).ToArray();
                if (notexist.Length > 0)
                {

                    filterDT.Columns.Add("ErrorMessage", typeof(string));
                    DataRow dr = filterDT.NewRow();
                    string err = "Column ";
                    err = err + string.Join(",", notexist);
                    err = err + " not exist in file.";
                    dr["ErrorMessage"] = err;
                    filterDT.Rows.Add(dr);

                }
                else
                {
                    //check column have null or empty
                    var isempty = false;
                    string Emptyerr = "";
                    int j = 0;

                    foreach (DataColumn dc in dt.Columns)
                    {
                        if (j < FC.MappingColumns.Count)
                        {
                            if (dc.ColumnName.ToString() == FC.MappingColumns[j].SheetColumnName)
                            // if (arr.Contains(dc.ColumnName.ToString()))
                            //  if (totalarr.Contains(dc.ColumnName.ToString())) 
                            {
                                if (FC.MappingColumns[j].IsMandatoryValue == "Y")
                                {
                                    for (var i = 0; i < dt.Rows.Count; i++)
                                    {
                                        if (dt.Rows[i][dc].ToString() == "")
                                        {
                                            isempty = true;
                                            break;
                                        }
                                    }
                                }
                            }
                            j++;
                            if (isempty == true)
                            {
                                Emptyerr = Emptyerr + dc.ColumnName + " , ";
                                isempty = false;
                            }
                        }
                    }

                    if (Emptyerr != "")
                    {
                        filterDT.Columns.Add("ErrorMessage", typeof(string));
                        Emptyerr = Emptyerr.Remove(Emptyerr.Length - 2, 2);
                        DataRow dr = filterDT.NewRow();
                        dr["ErrorMessage"] = Emptyerr + "column values are empty.";
                        filterDT.Rows.Add(dr);

                    }
                    else
                    {
                        System.Data.DataView dv = new System.Data.DataView(dt);
                        //  filterDT = dv.ToTable(true, diff.ToArray());

                        filterDT = dv.ToTable(false, totalarr.Distinct().ToArray());
                        filterDT.Columns.Add("ErrorMessage", typeof(string));
                        DataRow dr = filterDT.NewRow();
                        dr["ErrorMessage"] = "";
                        filterDT.Rows.Add(dr);

                    }

                }
            }
            catch (Exception ex)
            {
                filterDT.Columns.Add("ErrorMessage", typeof(string));
                DataRow dr = filterDT.NewRow();
                string err = ex.ToString();

                dr["ErrorMessage"] = err;//"Some error occured.";
                filterDT.Rows.Add(dr);
            }

            return filterDT;
        }

        public DataTable GetFileDataNoteMatrix(string sourceBlobFileName, string DisplayFileName, JsonFileConfiguration fileConf)
        {
            int? headerposition = fileConf.HeaderPosition;
            string SheetName = fileConf.SheetName;
            DataTable refinedExcelData = new DataTable();
            //  SheetName = SheetName.Split(',')[0];
            var excelData = GetExcelBlobDataNoteMatrix(sourceBlobFileName, connectionString, sourceContainerName, headerposition, SheetName, fileConf);

            if (excelData == null)
            {
                refinedExcelData.Columns.Add("ErrorMessage", typeof(string));
                DataRow dr = refinedExcelData.NewRow();
                dr["ErrorMessage"] = SheetName + " sheet does not exist in the file.";
                refinedExcelData.Rows.Add(dr);

            }
            //else
            //{
            //    refinedExcelData = ValidateDataFromConfig(excelData, fileConf);
            //}
            return excelData;
        }
        public DataTable GetFileData(string sourceBlobFileName, string DisplayFileName, JsonFileConfiguration fileConf)
        {
            int? headerposition = fileConf.HeaderPosition;
            string SheetName = fileConf.SheetName;
            DataTable refinedExcelData = new DataTable();

            var excelData = GetExcelBlobData(sourceBlobFileName, connectionString, sourceContainerName, headerposition, SheetName, fileConf);

            if (excelData == null)
            {
                refinedExcelData.Columns.Add("ErrorMessage", typeof(string));
                DataRow dr = refinedExcelData.NewRow();
                dr["ErrorMessage"] = SheetName + " sheet does not exist in the file.";
                refinedExcelData.Rows.Add(dr);

            }
            else
            {
                refinedExcelData = ValidateDataFromConfig(excelData, fileConf);
            }
            return refinedExcelData;
        }


        public JsonFileConfiguration GetJsonFile(string FileName, string ServicerName)
        {

            DataTable dt = new DataTable();
            string json = "";
            string fileext = FileName.Split('.')[1];

            JsonFileConfiguration JFile = new JsonFileConfiguration();


            // string filepath = AppDomain.CurrentDomain.BaseDirectory + @"\JSONFile\" + ServicerName + "_" + fileext.ToUpper() + ".json";
            string filepath = Directory.GetCurrentDirectory() + @"\wwwroot\JSONFile\" + ServicerName + "_" + fileext.ToUpper() + ".json";
            try
            {
                using (StreamReader r = new StreamReader(filepath))
                {
                    json = r.ReadToEnd();
                    JFile = JsonConvert.DeserializeObject<JsonFileConfiguration>(json);
                }

                var sortlist = JFile.MappingColumns.OrderBy(i => i.SheetColumnName).ToList();

                JFile.MappingColumns = sortlist;
            }
            catch (Exception ex)
            {
                string exc = ex.ToString();
            }
            return JFile;

        }


        /// <summary>
        /// GetExcelBlobData
        /// Gets the Excel file Blob data and returns a dataset
        /// </summary>
        /// <param name="filename"></param>
        /// <param name="connectionString"></param>
        /// <param name="containerName"></param>
        /// <returns></returns>
        public static DataSet GetExcelBlobData(string filename, string connectionString, string containerName)
        {
            Logger.Write("GetExcelBlobData", MessageLevel.Info, "GetExcelBlobData");
            // Retrieve storage account from connection string.
            CloudStorageAccount storageAccount = CloudStorageAccount.Parse(connectionString);

            // Create the blob client.
            CloudBlobClient blobClient = storageAccount.CreateCloudBlobClient();

            // Retrieve reference to a previously created container.
            CloudBlobContainer container = blobClient.GetContainerReference(containerName);

            // Retrieve reference to a blob named "test.xlsx"
            CloudBlockBlob blockBlobReference = container.GetBlockBlobReference(filename);
            DataSet ds;
            try
            {

                using (var memoryStream = new MemoryStream())
                {
                    //downloads blob's content to a stream
                    blockBlobReference.DownloadToStream(memoryStream);
                    var excelReader = ExcelReaderFactory.CreateOpenXmlReader(memoryStream);

                    var conf = new ExcelDataSetConfiguration
                    {
                        ConfigureDataTable = _ => new ExcelDataTableConfiguration
                        {
                            UseHeaderRow = true
                        }
                    };

                    ds = excelReader.AsDataSet(conf);
                    //var dataSet = reader.AsDataSet(conf);

                    excelReader.Close();
                }

            }
            catch (Exception ex)
            {
                var error = ex.Message;
                throw;

            }


            return ds;
        }

        /// <summary>
        /// GetExcelBlobData
        /// Gets the Excel file Blob data and returns a Datatable
        /// </summary>
        /// <param name="filename"></param>
        /// <param name="connectionString"></param>
        /// <param name="containerName"></param>
        /// <returns></returns>
        private static DataTable GetExcelBlobData(string filename, string connectionString, string containerName, int? headerposition, string SheetName, JsonFileConfiguration fc)
        {

            DataTable dt = new DataTable();

            string fileext = filename.Split('.')[1];
            try
            {
                // Retrieve storage account from connection string.
                CloudStorageAccount storageAccount = CloudStorageAccount.Parse(connectionString);

                DataSet ds;


                // Create the blob client.
                CloudBlobClient blobClient = storageAccount.CreateCloudBlobClient();

                // Retrieve reference to a previously created container.
                CloudBlobContainer container = blobClient.GetContainerReference(containerName);

                // Retrieve reference to a blob named "test.xlsx"
                CloudBlockBlob blockBlobReference = container.GetBlockBlobReference(filename);
                using (var memoryStream = new MemoryStream())
                {
                    if (fileext.ToLower() == "xlsx")
                    {
                        //downloads blob's content to a stream
                        blockBlobReference.DownloadToStream(memoryStream);
                        // if (fileext.ToLower() == "xlsx") { 
                        var excelReader = ExcelReaderFactory.CreateOpenXmlReader(memoryStream);

                        var conf = new ExcelDataSetConfiguration
                        {
                            ConfigureDataTable = _ => new ExcelDataTableConfiguration
                            {
                                UseHeaderRow = true,

                            }
                        };
                        ds = excelReader.AsDataSet(conf);


                        //if (headerposition == 0)
                        //{
                        //    var conf = new ExcelDataSetConfiguration
                        //    {
                        //        ConfigureDataTable = _ => new ExcelDataTableConfiguration
                        //        {
                        //            UseHeaderRow = true,

                        //        }
                        //    };
                        //    ds = excelReader.AsDataSet(conf);
                        //}
                        //else
                        //{
                        //    var i = 0;
                        //    var conf = new ExcelDataSetConfiguration
                        //    {
                        //        ConfigureDataTable = _ => new ExcelDataTableConfiguration
                        //        {
                        //            UseHeaderRow = true,
                        //            ReadHeaderRow = (rowReader) =>
                        //            {
                        //                while (i < (headerposition - 1))
                        //                {
                        //                    rowReader.Read();
                        //                    i++;
                        //                }
                        //            }
                        //        }
                        //    };

                        if (SheetName == "Berkadia")
                        {
                            dt = excelReader.AsDataSet(conf).Tables[0];
                        }
                        else
                        {
                            dt = excelReader.AsDataSet(conf).Tables[SheetName];
                        }
                        //  ds = excelReader.AsDataSet(conf);
                        //}
                        excelReader.Close();
                        //if (SheetName != "")
                        //{
                        //    dt = ds.Tables[SheetName];
                        //}
                        //else
                        //{
                        //    dt = ds.Tables[0];
                        //}


                        if (dt != null)
                        {
                            for (var i = 0; i < (headerposition - 2); i++)
                            {
                                dt.Rows.RemoveAt(0);
                            }


                            foreach (DataColumn column in dt.Columns)
                            {
                                string cName = dt.Rows[0][column.ColumnName].ToString();
                                if (!dt.Columns.Contains(cName) && cName != "")
                                {
                                    column.ColumnName = cName;
                                }

                            }
                            if (dt.Rows.Count > 1)
                            {
                                dt.Rows[0].Delete(); //If you don't need that row any more
                            }
                            dt.AcceptChanges();

                            if (SheetName == "Remit")
                            {

                                foreach (DataColumn column in dt.Columns)
                                {
                                    string ColName = column.ColumnName;
                                    if (column.ColumnName.Contains("Column"))
                                    {
                                        dt.Columns.Remove(ColName);
                                        break;
                                    }
                                }
                                dt.AcceptChanges();

                            }

                            if (SheetName == "Berkadia")
                            {
                                if (dt != null)
                                {
                                    DataRow headerRow = dt.NewRow();

                                    for (int i = 0; i < dt.Columns.Count; i++)
                                    {
                                        headerRow[i] = (dt.Rows[0][i].ToString() + dt.Rows[1][i].ToString() + dt.Rows[2][i].ToString()).Replace(" ", "");
                                    }

                                    if (dt.Rows.Count > 1)
                                    {
                                        dt.Rows.RemoveAt(0);
                                        dt.Rows.RemoveAt(0);
                                        dt.Rows.RemoveAt(0);

                                        for (int i = 0; i < dt.Columns.Count && i < headerRow.ItemArray.Length; i++)
                                        {
                                            dt.Columns[i].ColumnName = headerRow[i].ToString();
                                        }

                                        dt.AcceptChanges();
                                    }



                                    foreach (DataColumn column in dt.Columns)
                                    {
                                        string ColName = column.ColumnName;
                                        if (column.ColumnName.Contains("Column"))
                                        {
                                            dt.Columns.Remove(ColName);
                                            break;
                                        }
                                    }
                                    dt.AcceptChanges();
                                }
                            }
                        }
                    }
                    //========CSV==============
                    if (fileext.ToLower() == "csv")
                    {
                        //downloads blob's content to a stream
                        blockBlobReference.DownloadToStream(memoryStream);
                        var excelReader = ExcelReaderFactory.CreateCsvReader(memoryStream);

                        if (headerposition == 0)
                        {
                            var conf = new ExcelDataSetConfiguration
                            {
                                UseColumnDataType = true,
                                ConfigureDataTable = _ => new ExcelDataTableConfiguration
                                {
                                    UseHeaderRow = true,
                                }
                            };

                            ds = excelReader.AsDataSet(conf);
                        }
                        else
                        {
                            var i = 0;
                            var conf = new ExcelDataSetConfiguration
                            {

                                ConfigureDataTable = _ => new ExcelDataTableConfiguration
                                {
                                    UseHeaderRow = true,
                                    ReadHeaderRow = (rowReader) =>
                                    {
                                        while (i < (headerposition - 1))
                                        {
                                            rowReader.Read();
                                            i++;
                                        }
                                    }

                                }
                            };
                            ds = excelReader.AsDataSet(conf);
                        }
                        dt = ds.Tables[0];
                        excelReader.Close();
                    }
                    //=====================================
                    if (dt != null)
                    {
                        var ordercolumns = dt.Columns.Cast<DataColumn>().ToList().OrderBy(x => x.ColumnName).Select(x => x.ColumnName).ToList();
                        dt = new DataView(dt).ToTable(false, ordercolumns.ToArray());
                    }
                    //=====================================
                }
            }
            catch (Exception ex)
            {
                var error = ex.Message;
                throw;

                Logger.Write(ModuleName.FileUpload.ToString(), ex);
            }

            return dt;
        }
        private static DataTable GetExcelBlobDataNoteMatrix(string filename, string connectionString, string containerName, int? headerposition, string SheetName, JsonFileConfiguration fc)
        {

            DataTable dt = new DataTable();
            DataTable dtFin = new DataTable();

            for (var k = 0; k < fc.MappingColumns.Count; k++)
            {
                dtFin.Columns.Add(fc.MappingColumns[k].SheetColumnName, typeof(string));
            }


            try
            {
                // Retrieve storage account from connection string.
                CloudStorageAccount storageAccount = CloudStorageAccount.Parse(connectionString);

                DataSet ds;

                // Create the blob client.
                CloudBlobClient blobClient = storageAccount.CreateCloudBlobClient();

                // Retrieve reference to a previously created container.
                CloudBlobContainer container = blobClient.GetContainerReference(containerName);

                // Retrieve reference to a blob named "test.xlsx"
                CloudBlockBlob blockBlobReference = container.GetBlockBlobReference(filename);
                using (var memoryStream = new MemoryStream())
                {

                    //downloads blob's content to a stream
                    blockBlobReference.DownloadToStream(memoryStream);
                    var excelReader = ExcelReaderFactory.CreateOpenXmlReader(memoryStream);
                    ds = excelReader.AsDataSet();

                    string[] Sheets = SheetName.Split(',');
                    for (var i = 0; i < ds.Tables.Count; i++)
                    {
                        //if (ds.Tables[i].TableName == "Delphi - Note Matrix" || ds.Tables[i].TableName == "TRE ACR - Note Matrix" || ds.Tables[i].TableName == "ACORE Credit IV - Note Matrix")
                        if (ds.Tables[i].TableName == "Delphi" || ds.Tables[i].TableName == "TRE ACR" || ds.Tables[i].TableName == "ACORE Credit IV" ||
                            ds.Tables[i].TableName == "ACP II" || ds.Tables[i].TableName == "ACSS" || ds.Tables[i].TableName == "AHIP" ||
                            ds.Tables[i].TableName == "Delaware Life" || ds.Tables[i].TableName == "Harel" || ds.Tables[i].TableName == "SILAC" ||
                            ds.Tables[i].TableName == "Equitrust" || ds.Tables[i].TableName == "Fee"
                            )
                        {
                            for (int z = 0; z < headerposition; z++)
                            {
                                ds.Tables[Sheets[i]].Rows.RemoveAt(0);
                            }

                            foreach (DataColumn column in ds.Tables[Sheets[i]].Columns)
                            {
                                string cName = ds.Tables[Sheets[i]].Rows[0][column.ColumnName].ToString();
                                if (!ds.Tables[Sheets[i]].Columns.Contains(cName) && cName != "")
                                {
                                    column.ColumnName = cName;
                                }


                            }
                            ds.Tables[Sheets[i]].Rows.RemoveAt(0);

                            ds.Tables[Sheets[i]].Columns.Add("SheetName", typeof(string));
                            for (var t = 0; t < ds.Tables[Sheets[i]].Rows.Count; t++)
                            {
                                ds.Tables[Sheets[i]].Rows[t]["SheetName"] = ds.Tables[i].TableName;

                            }

                            foreach (DataRow dr in ds.Tables[Sheets[i]].Rows)
                            {
                                dtFin.ImportRow(dr);
                            }



                        }
                    }
                    ds.AcceptChanges();
                    excelReader.Close();
                    //=====================================
                    var ordercolumns = dtFin.Columns.Cast<DataColumn>().ToList().OrderBy(x => x.ColumnName).Select(x => x.ColumnName).ToList();
                    dtFin = new DataView(dtFin).ToTable(false, ordercolumns.ToArray());
                    //=====================================
                }



            }
            catch (Exception ex)
            {
                var error = ex.Message;
                // throw;

                Logger.Write("Error detail", ex);
            }

            return dtFin;
        }
        public static DataTable ReadAcoreReportingTemplate(string filename)
        {

            DataTable dt = new DataTable();
            DataSet ds;

            string fileext = Path.GetExtension(filename).Replace(".", "");

            int headerposition = 0;
            string SheetName = "001_Aflac_TRE Misc";
            try
            {
                // Retrieve storage account from connection string.

                using (Stream memoryStream = File.OpenRead(filename))
                {
                    if (fileext.ToLower() == "xlsx")
                    {
                        // if (fileext.ToLower() == "xlsx") { 
                        var excelReader = ExcelReaderFactory.CreateOpenXmlReader(memoryStream);

                        if (headerposition == 0)
                        {
                            var conf = new ExcelDataSetConfiguration
                            {
                                ConfigureDataTable = _ => new ExcelDataTableConfiguration
                                {
                                    UseHeaderRow = true,

                                }
                            };
                            ds = excelReader.AsDataSet(conf);
                        }
                        else
                        {
                            var i = 0;
                            var conf = new ExcelDataSetConfiguration
                            {

                                ConfigureDataTable = _ => new ExcelDataTableConfiguration
                                {
                                    UseHeaderRow = true,
                                    ReadHeaderRow = (rowReader) =>
                                    {
                                        while (i < (headerposition - 1))
                                        {
                                            rowReader.Read();
                                            i++;
                                        }
                                    }
                                }
                            };
                            ds = excelReader.AsDataSet(conf);
                        }
                        excelReader.Close();
                        if (SheetName != "")
                        {
                            dt = ds.Tables[SheetName];
                        }
                        else
                        {
                            dt = ds.Tables[0];
                        }
                    }
                    //========CSV==============
                    if (fileext.ToLower() == "csv")
                    {
                        //downloads blob's content to a stream

                        var excelReader = ExcelReaderFactory.CreateCsvReader(memoryStream);





                        if (headerposition == 0)
                        {
                            var conf = new ExcelDataSetConfiguration
                            {
                                UseColumnDataType = true,
                                ConfigureDataTable = _ => new ExcelDataTableConfiguration
                                {
                                    UseHeaderRow = true,
                                }
                            };

                            ds = excelReader.AsDataSet(conf);

                        }
                        else
                        {
                            var i = 0;
                            var conf = new ExcelDataSetConfiguration
                            {

                                ConfigureDataTable = _ => new ExcelDataTableConfiguration
                                {
                                    UseHeaderRow = true,

                                    ReadHeaderRow = (rowReader) =>
                                    {
                                        while (i < (headerposition - 1))
                                        {
                                            rowReader.Read();
                                            i++;
                                        }
                                    }

                                }
                            };
                            ds = excelReader.AsDataSet(conf);


                            //File.WriteAllBytes("C:\\TestCSV\\test.csv", memoryStream.);

                        }

                        DataTable dtSource = new DataTable();
                        dtSource.Columns.Add("PORTFOLIO");
                        dtSource.Columns.Add("CURRENCY");
                        dtSource.Columns.Add("CUSIP");
                        dtSource.Columns.Add("SETTLE_DATE");
                        dtSource.Columns.Add("SUB_TRAN_TYPE");
                        dtSource.Columns.Add("TRADE_DATE");
                        dtSource.Columns.Add("TRAN_TYPE");
                        dtSource.Columns.Add("COMMISSION");
                        dtSource.Columns.Add("INTEREST");
                        dtSource.Columns.Add("PRINCIPAL");
                        dtSource.Columns.Add("Other Fees");
                        dtSource.Columns.Add("AUTHORIZED_BY");
                        dtSource.Columns.Add("COMMENTS");
                        dtSource.Columns.Add("CONFIRMED_BY");
                        for (int i = 0; i < 5; i++)
                        {
                            DataRow dr = dtSource.NewRow();

                            dr["PORTFOLIO"] = "JPDACRCSH";
                            dr["CURRENCY"] = "USD";
                            dr["CUSIP"] = "ACR004990";
                            dr["SETTLE_DATE"] = "1/13/2020";
                            dr["SUB_TRAN_TYPE"] = "LIB_INT";
                            dr["TRADE_DATE"] = "1/13/2020";
                            dr["TRAN_TYPE"] = "MISC";

                            dr["COMMISSION"] = "";
                            dr["INTEREST"] = "219,877.78";
                            dr["PRINCIPAL"] = "";
                            dr["Other Fees"] = "";
                            dr["AUTHORIZED_BY"] = "RK";
                            dr["COMMENTS"] = "GREENSPOINT PLACE NOTE A-1 INTEREST PAYMENT";
                            dr["CONFIRMED_BY"] = "RK";
                            dtSource.Rows.Add(dr);
                        }


                        using (StreamWriter writer = new StreamWriter("C:\\TestCSV\\dump.csv"))
                        {
                            WriteDataTable(dtSource, writer, true, 9, excelReader);
                        }
                        dt = ds.Tables[0];
                        excelReader.Close();
                    }
                    //=====================================
                    var ordercolumns = dt.Columns.Cast<DataColumn>().ToList().OrderBy(x => x.ColumnName).Select(x => x.ColumnName).ToList();
                    dt = new DataView(dt).ToTable(false, ordercolumns.ToArray());
                    //=====================================
                }
            }
            catch (Exception ex)
            {
                var error = ex.Message;
                // throw;

                Logger.Write("Error detail", ex);
            }

            return dt;
        }

        public static void WriteDataTable(DataTable sourceTable, TextWriter writer, bool includeHeaders, int HeaderPosition, IExcelDataReader excelreader)
        {
            int count = 0;
            while (excelreader.Read())
            {
                string row = "";
                for (int i = 0; i < excelreader.FieldCount; i++)
                {
                    row = row + "," + excelreader.GetValue(i);
                }


                if (count < HeaderPosition)
                {
                    if (row.Trim().TrimStart(',').TrimEnd(',') == "")
                    {
                        writer.WriteLine();
                    }
                    else
                    {
                        writer.WriteLine(row.Trim().TrimStart(',').TrimEnd(','));
                    }
                }
                else
                {
                    break;
                }
                count += 1;
            }

            //if (HeaderPosition > 0)
            //{
            //    for (int i = 0; i < HeaderPosition; i++)
            //    {
            //        writer.WriteLine();
            //    }
            //}


            if (includeHeaders)
            {
                IEnumerable<String> headerValues = sourceTable.Columns.OfType<DataColumn>().Select(column => QuoteValue(column.ColumnName));
                writer.WriteLine(String.Join(",", headerValues));
            }

            IEnumerable<String> items = null;

            foreach (DataRow row in sourceTable.Rows)
            {
                items = row.ItemArray.Select(o => QuoteValue(o?.ToString() ?? String.Empty));
                writer.WriteLine(String.Join(",", items));
            }

            writer.Flush();
        }



        public static List<string> DeleteBlobFileByNumberofdays(string FolderName, int numberOfDays)
        {
            try
            {
                List<string> FilesDealeted = new List<string>();
                GetConfigSettingforStatic();
                var Container = SectionrootStatic.GetSection("storage:container:name").Value;
                CloudBlobContainer container = BlobUtilities.GetBlobClient.GetContainerReference(Container);
                // Create the blob client.
                CloudBlobDirectory dr = container.GetDirectoryReference(FolderName);

                var listFiles = dr.ListBlobsSegmentedAsync(true, BlobListingDetails.Metadata, 100000, null, null, null).Result;
                foreach (var blob in listFiles.Results)
                {

                    if (blob is CloudBlockBlob blockBlob)
                    {

                        var time = blockBlob.Properties.LastModified;

                        var DeletedDate = DateTime.Now.AddDays(-numberOfDays);
                        if (time < DeletedDate)
                        {
                            FilesDealeted.Add(blockBlob.Name);
                            blockBlob.Delete();
                        }
                    }

                }
                return FilesDealeted;
            }
            catch (Exception ex)
            {
                throw;
            }
        }


        public static bool DeleteBlobFileByFileName(DataTable dt)
        {
            TagXIRRLogic tagXIRRLogic = new TagXIRRLogic();
            try
            {
                string FolderName;
                GetConfigSettingforStatic();
                var Container = SectionrootStatic.GetSection("storage:container:name").Value;
              ///  Container = Container + "/" + FolderName;
                CloudBlobContainer container = BlobUtilities.GetBlobClient.GetContainerReference(Container);
                // Create the blob client.
               
                if (dt != null)
                {
                    for (int i = 0; i < dt.Rows.Count; i++)
                    {
                        FolderName = dt.Rows[i]["Path"].ToString();
                        CloudBlobDirectory dr = container.GetDirectoryReference(FolderName);
                        var inputFile = dt.Rows[i]["FileName"].ToString();
                        var blob = dr.GetBlobReference(inputFile);
                        bool isdeleted = blob.DeleteIfExists();
                        if (isdeleted == true)
                        {
                            tagXIRRLogic.UpdateXIRRDeleteBlobFiles(Convert.ToInt32(dt.Rows[i]["XIRRDeleteBlobFilesID"]));
                        }

                    }
                }



            }
            catch (Exception)
            {

                throw;
            }
            return true;
        }




        public static List<string> DeleteBlobFileByFileNameOld(DataTable dt, string FolderName)
        {
            try
            {
                List<string> FilesDealeted = new List<string>();
                GetConfigSettingforStatic();
                var Container = SectionrootStatic.GetSection("storage:container:name").Value;
                Container = Container + "/" + FolderName;
                CloudBlobContainer container = BlobUtilities.GetBlobClient.GetContainerReference(Container);
                // Create the blob client.
                CloudBlobDirectory dr = container.GetDirectoryReference(FolderName);

                var listallFiles = dr.ListBlobsSegmentedAsync(false, BlobListingDetails.Metadata, 100000, null, null, null).Result;



                if (dt != null)
                {
                    for (int i = 0; i < dt.Rows.Count; i++)
                    {
                        var inputFile = dt.Rows[i]["FileName_Input"].ToString();
                        var inputName = inputFile.Split("_");
                        var removestr = "_" + inputName[inputName.Length - 1];
                        var deleteFileName = inputFile.Replace(removestr, "");
                        var BlobFiles = AzureStorageReadFile.DeleteFile(listallFiles, FolderName, inputFile, deleteFileName);
                        //  string inputNamelen = inputName.LastIndexOf(inputName);

                    }

                }


                return FilesDealeted;
            }
            catch (Exception ex)
            {
                throw;
            }
        }



        public static Boolean DeleteFile(BlobResultSegment listallFiles, string FolderName, string fileName, string deleteFileName)
        {
            List<string> FilesDealeted = new List<string>();
            GetConfigSettingforStatic();
            var Container = SectionrootStatic.GetSection("storage:container:name").Value;
            CloudBlobContainer container = BlobUtilities.GetBlobClient.GetContainerReference(Container);
            // Create the blob client.
            CloudBlobDirectory dr = container.GetDirectoryReference(FolderName);


            foreach (var blob in listallFiles.Results)
            {

                if (blob is CloudBlockBlob blockBlob)
                {
                    if (blockBlob.Name != fileName)
                    {
                        if (blockBlob.Name.Contains(deleteFileName))
                        {
                            FilesDealeted.Add(blockBlob.Name);
                            //  blockBlob.Delete();
                        }
                    }
                    else
                    {
                        var ss = blockBlob.Name;
                    }
                    //if (blockBlob.Name.Contains(fileName))
                    //{
                    //    FilesDealeted.Add(blockBlob.Name);
                    //    blockBlob.Delete();
                    //}
                }

            }
            return true;

        }




        private static string QuoteValue(string value)
        {
            return String.Concat("\"",
            value.Replace("\"", "\"\""), "\"");
        }

        public static async Task<DataSet> ReadAccountingReportTemplateAsDataSet(ReportFileDataContract ReportFileDC)
        {
            GetConfigSettingforStatic();
            DataTable dt = new DataTable();
            DataSet ds;
            string fileName = "";
            Stream TemplateMemoryStream = new MemoryStream();
            IExcelDataReader iExcelDataReader = null;
            List<string> lstTemplateLines = new List<string>();
            //392-AzureBlob, 459-Box, 641-LocalServer, 642-FTPServer
            if (ReportFileDC.SourceStorageTypeID == 641)
            {
                fileName = Path.Combine(Directory.GetCurrentDirectory() + "/wwwroot", ReportFileDC.SourceStorageLocation + "/" + ReportFileDC.ReportFileTemplate);
                TemplateMemoryStream = File.OpenRead(fileName);
            }
            else if (ReportFileDC.SourceStorageTypeID == 392)
            {
                var Container = SectionrootStatic.GetSection("storage:container:name").Value;
                CloudBlobContainer container = BlobUtilities.GetBlobClient.GetContainerReference(Container);

                // Create the blob client.
                CloudBlobDirectory dr = container.GetDirectoryReference(ReportFileDC.SourceStorageLocation);
                CloudBlockBlob cloudBlockBlob = dr.GetBlockBlobReference(ReportFileDC.ReportFileTemplate);
                cloudBlockBlob.DownloadToStream(TemplateMemoryStream);
            }
            else if (ReportFileDC.SourceStorageTypeID == 459)
            {
                DocumentDataContract _docDC = new DocumentDataContract();
                _docDC.DocumentStorageID = ReportFileDC.DocumentStorageID;
                TemplateMemoryStream = await new CRES.Services.Infrastructure.BoxHelper().DownloadFile(_docDC);
            }
            else if (ReportFileDC.SourceStorageTypeID == 642)
            {

            }

            try
            {
                // Retrieve storage account from connection string.

                using (Stream memoryStream = TemplateMemoryStream)
                {

                    if (ReportFileDC.ReportFileFormat.ToLower() == "xlsx")
                    {
                        // if (fileext.ToLower() == "xlsx") { 
                        iExcelDataReader = ExcelReaderFactory.CreateOpenXmlReader(memoryStream);

                    }

                    //========CSV==============
                    if (ReportFileDC.ReportFileFormat.ToLower() == "csv" || ReportFileDC.ReportFileFormat.ToLower() == "csvpipe")
                    {
                        iExcelDataReader = ExcelReaderFactory.CreateCsvReader(memoryStream);
                    }

                    ds = iExcelDataReader.AsDataSet();

                    //if all table rows are empty than clear table
                    foreach (DataTable dt1 in ds.Tables)
                    {
                        DataColumn[] columns = ds.Tables[0].Columns.Cast<DataColumn>().ToArray();
                        bool anyNonEmptyEntry = ds.Tables[0].AsEnumerable().Any(row => columns.Any(col => row[col].ToString() != ""));
                        if (!anyNonEmptyEntry)
                            dt1.Clear();

                    }

                }
                //
                return ds;
            }
            catch (Exception ex)
            {
                var error = ex.Message;
                // throw;

                Logger.Write("Error detail", ex);
                throw ex;
            }

        }

        public static async Task<Stream> ReadAccountingReportInStream(ReportFileDataContract ReportFileDC)
        {
            GetConfigSettingforStatic();
            DataTable dt = new DataTable();
            DataSet ds;
            string fileName = "";
            Stream TemplateMemoryStream = new MemoryStream();
            IExcelDataReader iExcelDataReader = null;
            List<string> lstTemplateLines = new List<string>();
            //392-AzureBlob, 459-Box, 641-LocalServer, 642-FTPServer

            try
            {

                if (ReportFileDC.SourceStorageTypeID == 641)
                {
                    fileName = Path.Combine(Directory.GetCurrentDirectory() + "/wwwroot", ReportFileDC.SourceStorageLocation + "/" + ReportFileDC.ReportFileTemplate);

                    TemplateMemoryStream = File.OpenRead(fileName);
                }
                else if (ReportFileDC.SourceStorageTypeID == 392)
                {
                    var Container = SectionrootStatic.GetSection("storage:container:name").Value;
                    CloudBlobContainer container = BlobUtilities.GetBlobClient.GetContainerReference(Container);

                    // Create the blob client.
                    CloudBlobDirectory dr = container.GetDirectoryReference(ReportFileDC.SourceStorageLocation);
                    CloudBlockBlob cloudBlockBlob = dr.GetBlockBlobReference(ReportFileDC.ReportFileTemplate);
                    cloudBlockBlob.DownloadToStream(TemplateMemoryStream);
                }
                else if (ReportFileDC.SourceStorageTypeID == 459)
                {
                    DocumentDataContract _docDC = new DocumentDataContract();
                    _docDC.DocumentStorageID = ReportFileDC.DocumentStorageID;
                    TemplateMemoryStream = await new CRES.Services.Infrastructure.BoxHelper().DownloadFile(_docDC);
                }
                else if (ReportFileDC.SourceStorageTypeID == 642)
                {

                }

            }
            catch (Exception ex)
            {
                var error = ex.Message;
                // throw;

                Logger.Write("Error detail", ex);
                throw ex;
            }
            return TemplateMemoryStream;
        }


        public static void CreateAccountingReport(ReportFileDataContract ReportFileDC, DataTable dtreport, bool includeHeaders, List<string> lstTemplateLines)
        {

            string fileName = ReportFileDC.NewFileName;
            string filePath = "";
            //392-AzureBlob, 459-Box, 641-LocalServer, 642-FTPServer
            try
            {

                if (ReportFileDC.DestinationStorageTypeID == 641)
                {
                    filePath = Path.Combine(Directory.GetCurrentDirectory() + "/wwwroot", ReportFileDC.DestinationStorageLocation);
                    fileName = filePath + "/" + fileName;
                    if (!Directory.Exists(filePath))
                    {
                        Directory.CreateDirectory(filePath);
                    }

                    using (StreamWriter writer = new StreamWriter(fileName))
                    {
                        foreach (string str in lstTemplateLines)
                        {
                            writer.WriteLine(str);
                        }
                        if (includeHeaders)
                        {
                            IEnumerable<String> headerValues = dtreport.Columns.OfType<DataColumn>().Select(column => QuoteValue(column.ColumnName));
                            writer.WriteLine(String.Join(",", headerValues));
                        }

                        IEnumerable<String> items = null;

                        foreach (DataRow row in dtreport.Rows)
                        {
                            items = row.ItemArray.Select(o => QuoteValue(o?.ToString() ?? String.Empty));
                            writer.WriteLine(String.Join(",", items));
                        }

                        writer.Flush();
                    }
                }
            }
            catch (Exception ex)
            {

                Logger.Write("Error detail  (method : CreateAccountingReport) ", ex);
            }

        }


        public static async Task<DataSet> ReadFileAsDataSet(FileUploadParameterDataContract FileParams, string connectionString, string containerName)
        {

            DataTable dt = new DataTable();
            DataSet ds = null;
            string fileName = "";
            Stream TemplateMemoryStream = new MemoryStream();
            IExcelDataReader iExcelDataReader = null;
            List<string> lstTemplateLines = new List<string>();
            String FileExtension = Path.GetExtension(FileParams.FileName).Replace(".", "");
            //392-AzureBlob, 459-Box, 641-LocalServer, 642-FTPServer
            try
            {
                if (FileParams.StorageTypeID == 641)
                {
                    fileName = Path.Combine(Directory.GetCurrentDirectory() + "/wwwroot", FileParams.StorageLocation + "/" + FileParams.FileName);
                    TemplateMemoryStream = File.OpenRead(fileName);
                }
                else if (FileParams.StorageTypeID == 392)
                {
                    CloudStorageAccount storageAccount = CloudStorageAccount.Parse(connectionString);
                    // Create the blob client.
                    CloudBlobClient blobClient = storageAccount.CreateCloudBlobClient();
                    // Retrieve reference to a previously created container.
                    CloudBlobContainer container = blobClient.GetContainerReference(containerName);
                    // Create the blob client.
                    //CloudBlockBlob cloudBlockBlob = container.GetBlockBlobReference(FileParams.StorageLocation + "/" + FileParams.FileName);
                    //cloudBlockBlob.DownloadToStream(TemplateMemoryStream);

                    try
                    {
                        var prefilxplob = FileParams.StorageLocation + "/";

                        var blobs = container.ListBlobs(prefix: prefilxplob, useFlatBlobListing: true).OfType<CloudBlockBlob>().ToList();
                        var latestBlob = blobs.OrderByDescending(m => m.Properties.LastModified).ToList().First();
                        var LastModifiedDatetime = latestBlob.Properties.LastModified.Value.UtcDateTime;

                        if (LastModifiedDatetime.Date == DateTime.Now.Date)
                        {
                            CloudBlockBlob cloudBlockBlob = container.GetBlockBlobReference(latestBlob.Name);
                            cloudBlockBlob.DownloadToStream(TemplateMemoryStream);
                        }

                    }
                    catch (Exception e)
                    {
                        //  Block of code to handle errors
                        throw;

                    }
                }
                else if (FileParams.StorageTypeID == 459)
                {
                    DocumentDataContract _docDC = new DocumentDataContract();
                    //_docDC.DocumentStorageID = FileParams.DocumentStorageID;
                    string folder = FileParams.StorageLocation.Split('/').Last();
                    string boxStorageId = "";
                    if (!string.IsNullOrEmpty(FileParams.Servicer))
                    {

                        BoxCollection<BoxItem> docs = await new CRES.Services.Infrastructure.BoxHelper(FileParams.Servicer).GetDocumentFromBoxForWellsBerkadia(folder, "");
                        if (docs != null && docs.TotalCount > 0)
                        {
                            var fileNameWithoutExt = FileParams.FileName.Substring(0, FileParams.FileName.LastIndexOf("."));


                            var file = docs.Entries.OrderByDescending(i => i.CreatedAt).Where(j => j.Name.Contains(fileNameWithoutExt)).ToList();

                            //var file = docs.Entries.Where(i => i.Name == FileParams.FileName);
                            if (file != null && file.Count() > 0)
                            {
                                boxStorageId = file.FirstOrDefault().Id;
                            }

                            _docDC.DocumentStorageID = boxStorageId;
                        }
                        TemplateMemoryStream = await new CRES.Services.Infrastructure.BoxHelper(FileParams.Servicer).DownloadFileByID(boxStorageId, FileExtension);
                    }
                    else
                    {
                        BoxCollection<BoxItem> docs = await new CRES.Services.Infrastructure.BoxHelper().GetDocumentFromBox(folder, "");
                        if (docs != null && docs.TotalCount > 0)
                        {
                            var file = docs.Entries.Where(i => i.Name == FileParams.FileName);
                            if (file != null)
                            {
                                boxStorageId = file.FirstOrDefault().Id;
                            }

                            _docDC.DocumentStorageID = boxStorageId;
                        }
                        TemplateMemoryStream = await new CRES.Services.Infrastructure.BoxHelper().DownloadFile(_docDC);
                    }
                }
                else if (FileParams.StorageTypeID == 642)
                {

                }

                if (TemplateMemoryStream.Length == 0)
                {
                    return ds;
                }
                // Retrieve storage account from connection string.

                using (Stream memoryStream = TemplateMemoryStream)
                {

                    if (FileExtension.ToLower() == "xlsx")
                    {
                        // if (fileext.ToLower() == "xlsx") { 
                        iExcelDataReader = ExcelReaderFactory.CreateOpenXmlReader(memoryStream);

                    }

                    //========CSV==============
                    if (FileExtension.ToLower() == "csv")
                    {
                        iExcelDataReader = ExcelReaderFactory.CreateCsvReader(memoryStream);
                    }

                    if (FileExtension.ToLower() == "zip")
                    {
                        iExcelDataReader = ExcelReaderFactory.CreateOpenXmlReader(memoryStream);
                    }

                    if (iExcelDataReader != null)
                    {
                        var i = 0;
                        var conf = new ExcelDataSetConfiguration
                        {

                            ConfigureDataTable = _ => new ExcelDataTableConfiguration
                            {
                                UseHeaderRow = true,
                                ReadHeaderRow = (rowReader) =>
                                {
                                    if (FileParams.HeaderPosition > 0)
                                    {
                                        while (i < (FileParams.HeaderPosition - 1))
                                        {
                                            rowReader.Read();
                                            i++;
                                        }
                                    }
                                }
                            }
                        };

                        ds = iExcelDataReader.AsDataSet(conf);

                        //if all table rows are empty than clear table
                        //foreach (DataTable dt1 in ds.Tables)
                        //{
                        //    DataColumn[] columns = ds.Tables[0].Columns.Cast<DataColumn>().ToArray();
                        //    bool anyNonEmptyEntry = ds.Tables[0].AsEnumerable().Any(row => columns.Any(col => row[col].ToString() != ""));
                        //    if (!anyNonEmptyEntry)
                        //        dt1.Clear();

                        //}
                    }

                }
                //
                return ds;
            }
            catch (Exception ex)
            {
                var error = ex.Message;
                // throw;
                throw ex;
            }
            finally
            {
                TemplateMemoryStream.Dispose();
            }

        }


        /*
        public static async Task<DataSet> ReadFileAsDataSet(FileUploadParameterDataContract FileParams, string connectionString, string containerName)
        {

            DataTable dt = new DataTable();
            DataSet ds = null;
            string fileName = "";
            Stream TemplateMemoryStream = new MemoryStream();
            IExcelDataReader iExcelDataReader = null;
            List<string> lstTemplateLines = new List<string>();
            String FileExtension = Path.GetExtension(FileParams.FileName).Replace(".", "");
            //392-AzureBlob, 459-Box, 641-LocalServer, 642-FTPServer
            try
            {
                if (FileParams.StorageTypeID == 641)
                {
                    fileName = Path.Combine(Directory.GetCurrentDirectory() + "/wwwroot", FileParams.StorageLocation + "/" + FileParams.FileName);
                    TemplateMemoryStream = File.OpenRead(fileName);
                }
                else if (FileParams.StorageTypeID == 392)
                {
                    CloudStorageAccount storageAccount = CloudStorageAccount.Parse(connectionString);
                    // Create the blob client.
                    CloudBlobClient blobClient = storageAccount.CreateCloudBlobClient();
                    // Retrieve reference to a previously created container.
                    CloudBlobContainer container = blobClient.GetContainerReference(containerName);
                    // Create the blob client.
                    //CloudBlockBlob cloudBlockBlob = container.GetBlockBlobReference(FileParams.StorageLocation + "/" + FileParams.FileName);
                    //cloudBlockBlob.DownloadToStream(TemplateMemoryStream);

                    try
                    {
                        var prefilxplob = FileParams.StorageLocation + "/";

                        var blobs = container.ListBlobs(prefix: prefilxplob, useFlatBlobListing: true).OfType<CloudBlockBlob>().ToList();
                        var latestBlob = blobs.OrderByDescending(m => m.Properties.LastModified).ToList().First();
                        var LastModifiedDatetime = latestBlob.Properties.LastModified.Value.UtcDateTime;

                        if (LastModifiedDatetime.Date == DateTime.Now.Date)
                        {
                            CloudBlockBlob cloudBlockBlob = container.GetBlockBlobReference(latestBlob.Name);
                            cloudBlockBlob.DownloadToStream(TemplateMemoryStream);
                        }

                    }
                    catch (Exception e)
                    {
                        //  Block of code to handle errors
                        throw;

                    }
                }
                else if (FileParams.StorageTypeID == 459)
                {
                    DocumentDataContract _docDC = new DocumentDataContract();
                    //_docDC.DocumentStorageID = FileParams.DocumentStorageID;
                    string folder = FileParams.StorageLocation.Split('/').Last();
                    string boxStorageId = "";
                    BoxCollection<BoxItem> docs = await new CRES.Services.Infrastructure.BoxHelper().GetDocumentFromBox(folder, "");
                    if (docs != null && docs.TotalCount > 0)
                    {
                        var file = docs.Entries.Where(i => i.Name == FileParams.FileName);
                        if (file != null)
                        {
                            boxStorageId = file.FirstOrDefault().Id;
                        }

                        _docDC.DocumentStorageID = boxStorageId;
                    }
                    TemplateMemoryStream = await new CRES.Services.Infrastructure.BoxHelper().DownloadFile(_docDC);
                }
                else if (FileParams.StorageTypeID == 642)
                {

                }

                if (TemplateMemoryStream.Length == 0)
                {
                    return ds;
                }
                // Retrieve storage account from connection string.

                using (Stream memoryStream = TemplateMemoryStream)
                {

                    if (FileExtension.ToLower() == "xlsx")
                    {
                        // if (fileext.ToLower() == "xlsx") { 
                        iExcelDataReader = ExcelReaderFactory.CreateOpenXmlReader(memoryStream);

                    }

                    //========CSV==============
                    if (FileExtension.ToLower() == "csv")
                    {
                        iExcelDataReader = ExcelReaderFactory.CreateCsvReader(memoryStream);
                    }

                    if (iExcelDataReader != null)
                    {
                        var i = 0;
                        var conf = new ExcelDataSetConfiguration
                        {

                            ConfigureDataTable = _ => new ExcelDataTableConfiguration
                            {
                                UseHeaderRow = true,
                                ReadHeaderRow = (rowReader) =>
                                {
                                    if (FileParams.HeaderPosition > 0)
                                    {
                                        while (i < (FileParams.HeaderPosition - 1))
                                        {
                                            rowReader.Read();
                                            i++;
                                        }
                                    }
                                }
                            }
                        };

                        ds = iExcelDataReader.AsDataSet(conf);

                        //if all table rows are empty than clear table
                        //foreach (DataTable dt1 in ds.Tables)
                        //{
                        //    DataColumn[] columns = ds.Tables[0].Columns.Cast<DataColumn>().ToArray();
                        //    bool anyNonEmptyEntry = ds.Tables[0].AsEnumerable().Any(row => columns.Any(col => row[col].ToString() != ""));
                        //    if (!anyNonEmptyEntry)
                        //        dt1.Clear();

                        //}
                    }

                }
                //
                return ds;
            }
            catch (Exception ex)
            {
                var error = ex.Message;
                // throw;
                throw ex;
            }
            finally
            {
                TemplateMemoryStream.Dispose();
            }

        }
        */

        public static Boolean SaveFundingJSONIntoBlob(string jsontext, string FileName)
        {
            try
            {
                GetConfigSettingforStatic();
                byte[] byteArray = Encoding.ASCII.GetBytes(jsontext);
                MemoryStream stream = new MemoryStream(byteArray);

                var Container = SectionrootStatic.GetSection("storage:container:name").Value;
                CloudBlobContainer container = BlobUtilities.GetBlobClient.GetContainerReference(Container);

                // Create the blob client.
                CloudBlobDirectory dr = container.GetDirectoryReference("creslogfiles");

                CloudBlockBlob cloudBlockBlob = dr.GetBlockBlobReference(FileName);
                cloudBlockBlob.UploadFromStreamAsync(stream);


                return true;
            }
            catch (Exception ex)
            {
                return false;
                throw;

            }

        }


        public static Boolean UploadJSONFileToAzureBlob(string jsontext, string FileName)
        {
            try
            {
                GetConfigSettingforStatic();
                byte[] byteArray = Encoding.ASCII.GetBytes(jsontext);
                MemoryStream stream = new MemoryStream(byteArray);
                var Container = SectionrootStatic.GetSection("storage:container:name").Value;
                // Get Blob Container
                CloudBlobContainer container = BlobUtilities.GetBlobClient.GetContainerReference(Container);
                CloudBlobDirectory blobDirectory = container.GetDirectoryReference("CalcDebug");
                CloudBlockBlob blockBlob = blobDirectory.GetBlockBlobReference(FileName);
                blockBlob.UploadFromStream(stream);
                return true;
            }
            catch (Exception ex)
            {
                return false;

            }
        }
        public static Boolean UploadRuleJSONFileToAzureBlob(string jsontext, string FileName)
        {
            try
            {
                GetConfigSettingforStatic();
                byte[] byteArray = Encoding.ASCII.GetBytes(jsontext);
                MemoryStream stream = new MemoryStream(byteArray);
                var Container = SectionrootStatic.GetSection("storage:calccontainer:name").Value;
                // Get Blob Container
                CloudBlobContainer container = BlobUtilities.GetBlobClient.GetContainerReference(Container);
                var Foldername = SectionrootStatic.GetSection("storage:calccontainer:folder").Value;
                // var Foldername = SectionrootStatic.GetSection("Application").GetSection("storage:calccontainer:folder").Value;
                CloudBlobDirectory blobDirectory = container.GetDirectoryReference(Foldername);
                CloudBlockBlob blockBlob = blobDirectory.GetBlockBlobReference(FileName);
                blockBlob.UploadFromStream(stream);
                return true;
            }
            catch (Exception ex)
            {
                return false;
                throw;
            }
        }

        public static Boolean UploadChathamFinancialJsonFileToAzureBlob(string jsontext, string FileName)
        {
            try
            {
                GetConfigSettingforStatic();
                byte[] byteArray = Encoding.ASCII.GetBytes(jsontext);
                MemoryStream stream = new MemoryStream(byteArray);
                var Container = SectionrootStatic.GetSection("storage:container:name").Value;
                // Get Blob Container
                CloudBlobContainer container = BlobUtilities.GetBlobClient.GetContainerReference(Container);
                CloudBlobDirectory blobDirectory = container.GetDirectoryReference("chatham");
                CloudBlockBlob blockBlob = blobDirectory.GetBlockBlobReference(FileName);
                blockBlob.UploadFromStream(stream);
                return true;
            }
            catch (Exception ex)
            {
                return false;
            }
        }


    }
}