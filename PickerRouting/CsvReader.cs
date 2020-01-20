using System;
using System.Collections.Generic;
using System.Text;
using System.IO;

namespace PickerRouting
{
    public class CsvReader
    {
        private List<string> _pickListIds;

        private Dictionary<string, int> _orderCounts;

        public CsvReader()
        {
            _pickListIds = new List<string>();

            _orderCounts = new Dictionary<string, int>();
        }

        public void ReadCsv(string fileLocation)
        {
            using (var reader = new StreamReader(fileLocation))
            {
                var s = reader.ReadLine();
                while (!reader.EndOfStream)
                {
                    var line = reader.ReadLine();
                    var values = line.Split(',');

                    _pickListIds.Add(values[0]);
                    _orderCounts[values[0]] = (Convert.ToInt32(values[1]));
                }
            }
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