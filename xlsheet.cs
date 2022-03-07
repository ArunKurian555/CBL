using System;
using ExcelDataReader;
using sys = System.IO ;
using System.Data;
using Crestron.SimplSharp;
using ClosedXML.Excel;




namespace CBL
{
    class Xlsheet
    {
        public string[,] ReadExcel(string path, int sheetNo)
        {

            try
            {
                using (sys.FileStream fileStream = sys.File.Open(path, sys.FileMode.Open, sys.FileAccess.Read))
                {
                    IExcelDataReader reader;
                    reader = ExcelDataReader.ExcelReaderFactory.CreateReader(fileStream);
                    var conf = new ExcelDataSetConfiguration
                    {
                        ConfigureDataTable = _ => new ExcelDataTableConfiguration
                        {
                            UseHeaderRow = true
                        }
                    };
                    var dataSet = reader.AsDataSet(conf);
                    var dataTable = dataSet.Tables[sheetNo]; //ExcelDataReader Worksheet Start at 0.
                    int i, j;
                    i = 0;


                    string[,] datasheet = new string[dataTable.Rows.Count + 2, dataTable.Columns.Count + 2];
                    for (int k = 0; k < dataTable.Columns.Count; k++)
                    {
                        datasheet[0, i] = "";
                    }
                    foreach (DataRow row in dataTable.Rows)
                    {
                        i++;
                        j = 0;
                        foreach (DataColumn column in dataTable.Columns)
                        {
                            
                            datasheet[i, j] = row[column].ToString();
                            datasheet[0, j] = column.ColumnName.ToString();
                            j++;
                        }
                    }

                    return datasheet;

                }
            }
            catch (Exception ex)
            {
                CrestronConsole.PrintLine(ex.ToString());
                return null;
            }
        }


        public void WriteExcel(string[,] table,string sceneFilePath, int sheetNo)
            {

            try
            {

                var wbook = new XLWorkbook(sceneFilePath);
                var ws = wbook.Worksheets.Worksheet(sheetNo+1); // ClosedXML worksheet start at 1.
                
                    for (int i = 0; i < table.GetLength(0); i++)
                        for (int j = 0; j < table.GetLength(1); j++)
                            if (table[i, j] != null)
                            ws.Cell(i + 1, j + 1).Value = table[i, j].ToString();
                            


                wbook.SaveAs(sceneFilePath);
            }

            catch(Exception ex)
            {
                CrestronConsole.PrintLine(ex.ToString());

            }

        }
    }
}
