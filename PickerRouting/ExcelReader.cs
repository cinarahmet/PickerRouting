using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Excel = Microsoft.Office.Interop.Excel;

namespace PickerRouting
{
    public class ExcelReader
    {
        private List<string> _pickListIds;

        private Dictionary<string, int> _orderCounts;

        public ExcelReader()
        {
            _pickListIds = new List<string>();

            _orderCounts = new Dictionary<string, int>();
        }
        public void ReadExcel(string fileLocation)
        {
            Excel.Application xlApp = new Excel.Application();
            Excel.Workbook xlWorkbook = xlApp.Workbooks.Open(fileLocation);
            
            var xlWorksheet = (Excel.Worksheet)xlWorkbook.Worksheets.Item["Sheet1"];

            Excel.Range xlRange = xlWorksheet.UsedRange;

            var rowCount = xlRange.Rows.Count;

            for (int j = 2; j <= rowCount; j++)
            {
                _pickListIds.Add(xlRange.Cells[j, 1].ToString());
                _orderCounts.Add(xlRange.Cells[j,1].ToString(), Convert.ToInt32(xlRange.Cells[j,2]));
            }

            GC.Collect();
            GC.WaitForPendingFinalizers();
            Marshal.ReleaseComObject(xlRange);
            Marshal.ReleaseComObject(xlWorksheet);
            xlWorkbook.Close();
            Marshal.ReleaseComObject(xlWorkbook);
            xlApp.Quit();
            Marshal.ReleaseComObject(xlApp);
        }

        public List<string> GetPickListIds()
        {
            return _pickListIds;
        }

        public Dictionary<string, int> GetOrderCounts()
        {
            return _orderCounts;
        }
    }
}
