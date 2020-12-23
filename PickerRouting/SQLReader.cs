using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;
using System.Data.SqlClient;

namespace PickerRouting
{
    public class SQLReader
    {
        private String ID;
        private List<String> _locations;
        private Dictionary<String, Dictionary<String, long>> _DistanceMatrix;
        private String _connectionString;

        public SQLReader(String id)
        {
            _locations = new List<string>();
            ID = id;
            _DistanceMatrix = new Dictionary<string, Dictionary<string, long>>();
            _connectionString = "Data Source = WMS-SQL;" +
                "Initial Catalog = LOSCM;" +
                "Persist Security Info = True;" +
                "User ID = access_user;" +
                "Password = @cS_905/*_";
        }

        public void Read()
        {
            ReadLocations();
            
            ReadDistanceMatrix();
        }

        
        private void ReadLocations()
        {

            Console.WriteLine("Reading Locations matrix has started at {0}", DateTime.UtcNow);

            var connection = new SqlConnection(_connectionString);
            try
            {
                connection.Open();
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
            }

            try
            {
                var query = "select DISTINCT(LEFT(WL.LOCA_CODE,8)),WL.LOCA_PICKINGPRIORITY " +
                    "from CVLWMS_WAREHOUSETASKPROCESSINGWEBSTORE WT with (nolock) INNER JOIN " +
                    "LWMS_LOCATION WL WITH (NOLOCK) ON WT.WATA_FROMLOCATIONID = WL.LOCA_ID " +
                    "where PRGR_ID = 2 " +
                    "AND WRHS_ID = 140 " +
                    "AND WATT_ID = 4 " +
                    "and WL.LOCA_WAREHOUSEID = 140 " +
                    "AND PILI_CODE ='" + (ID) + "'" +
                    "ORDER BY WL.LOCA_PICKINGPRIORITY ASC"
                    ;

                SqlCommand myCommand = new SqlCommand(query, connection);

                var reader = myCommand.ExecuteReader();

                while (reader.Read())
                {
                    var fromLoc = reader.GetValue(0).ToString();
                    if (!_locations.Contains(fromLoc))
                        _locations.Add(fromLoc);
                }
                reader.Close();
                connection.Close();
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
            }
            Console.WriteLine("Reading Locations has ended at {0}", DateTime.UtcNow);
        }


        public Dictionary<String, Dictionary<String, long>> GetDistanceMatrix()
        {
            return _DistanceMatrix;
        }

        public List<String> GetLocations()
        {
            return _locations;
        }
        private void ReadDistanceMatrix()
        {
            var connection = new SqlConnection(_connectionString);
            try
            {
                connection.Open();
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
            }

            try
            {
                var query = "select FromRackCode, ToRackCode, Distance " +
                        "from DISTANCEMATRIX WITH (NOLOCK) " +
                        "WHERE FromRackCode in " +
                        "(select DISTINCT(LEFT(WL.LOCA_CODE,8)) " +
                        "from CVLWMS_WAREHOUSETASKPROCESSINGWEBSTORE WT with (nolock) " +
                        "INNER JOIN LWMS_LOCATION WL WITH (NOLOCK) ON WT.WATA_FROMLOCATIONID = WL.LOCA_ID " +
                        "where PRGR_ID = 2 " +
                        "AND WRHS_ID = 140 " +
                        "AND WATT_ID = 4 " +
                        "AND WL.LOCA_WAREHOUSEID = 140 " +
                        "AND PILI_CODE ='" + (ID) + "'" +
                        ")"
                        ;

                SqlCommand myCommand = new SqlCommand(query, connection);

                var reader = myCommand.ExecuteReader();
                while (reader.Read())
                {
                    var fromLocation = reader.GetValue(0).ToString();
                    var toLocation = reader.GetValue(1).ToString();
                    var dist = Convert.ToInt64(reader.GetValue(2));

                    if (!_DistanceMatrix.ContainsKey(fromLocation))
                    {
                        var distances = new Dictionary<String, long>();
                        distances.Add(toLocation, dist);
                        _DistanceMatrix.Add(fromLocation, distances);
                    }
                    else
                    {
                        var distances = _DistanceMatrix[fromLocation];
                        if (!distances.ContainsKey(toLocation))
                            distances.Add(toLocation, dist);
                    }


                }

                reader.Close();
                connection.Close();
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
            }

        }

    }
}

