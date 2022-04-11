using System;
using Crestron.SimplSharp;
using ExcelDataReader;
using sys = System.IO ;
using System.Data;
using ClosedXML.Excel;

namespace CBL
{
    class Xlsheet
    {

        public string[,] ReadExcel(string path, int sheetNo)
        {
            try
            {
                /*var workbook = new XLWorkbook(path);
                var ws1 = workbook.Worksheet(sheetNo + 1);
                int i, j;
                string[,] datasheet = new string[ws1.LastRowUsed().RowNumber(),ws1.LastColumnUsed().ColumnNumber()];
                

                for (i = 1; i <= ws1.LastRowUsed().RowNumber(); i++)
                {
                    for (j = 1; j <= ws1.LastColumnUsed().ColumnNumber(); j++)
                    {
                        if (ws1.Row(i).Cell(j) != null)
                            datasheet[i - 1, j - 1] = ws1.Row(i).Cell(j).Value.ToString();

                    }
                                    return datasheet;

                }*/

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

                    foreach (DataRow row in dataTable.Rows)
                    {
                        i++;
                        j = 0;
                        foreach (DataColumn column in dataTable.Columns)
                        {

                            datasheet[i, j] = row[column].ToString();
                            datasheet[0, j] = dataTable.Columns[j].ColumnName;
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



        public void WriteExcel(string[,] table, string FilePath, int sheetNo)
        {

            try
            {

                var wbook = new XLWorkbook(FilePath);
                var ws = wbook.Worksheets.Worksheet(sheetNo + 1); // ClosedXML worksheet start at 1.

                for (int i = 0; i < table.GetLength(0); i++)
                    for (int j = 0; j < table.GetLength(1); j++)
                        if (table[i, j] != null)
                            ws.Cell(i + 1, j + 1).Value = table[i, j].ToString();


                wbook.SaveAs(FilePath);
            }

            catch (Exception ex)
            {
                CrestronConsole.PrintLine(ex.ToString());

            }

        }



    }
}
