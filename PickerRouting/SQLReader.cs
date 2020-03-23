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
        private List<String> _originalLocations;
        private Dictionary<String, Dictionary<String, long>> _DistanceMatrix;
        private String _connectionString;
        private Double _totalDistance;
        private bool sameSize;

        public double TotalDistance { get => _totalDistance; set => _totalDistance = value; }

        public SQLReader(String id)
        {
            _locations = new List<string>();
            ID = id;
            _originalLocations = new List<string>();
            _DistanceMatrix = new Dictionary<string, Dictionary<string, long>>();
            _connectionString = "Data Source = WMS-SQL;" +
                "Initial Catalog = LOSCM;" +
                "Persist Security Info = True;" +
                "User ID = access_user;" +
                "Password = @cS_905/*_";
            sameSize = true;
        }
        public void Read()
        {
            ReadLocations();
            ReadDistanceMatrix();
            ReadOriginalPickLocations();
            if (_locations.Count != _DistanceMatrix.Count || _locations == null)
            {
                sameSize = false;
            }
            else
            {
                CalculateOriginalRouteDistance();
            }
        }
        private void ReadLocations()
        {

            Console.WriteLine("Reading Locations matrix has started at {0}", DateTime.UtcNow);

            //var connecionString = "Data Source = WMS-SQL;" +
            //    "Initial Catalog = LOSCM;" +
            //    "Persist Security Info = True;" +
            //    "User ID = access_user;" +
            //    "Password = @cS_905/*_";



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

        private void ReadOriginalPickLocations()
        {

            Console.WriteLine("Reading Locations matrix has started at {0}", DateTime.UtcNow);

            //var connecionString = "Data Source = WMS-SQL;" +
            //    "Initial Catalog = LOSCM;" +
            //    "Persist Security Info = True;" +
            //    "User ID = access_user;" +
            //    "Password = @cS_905/*_";



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
                var query = "select DISTINCT(LEFT(WL.LOCA_CODE,8)),WT.WATA_ACTUALFINISHDATE " +
                    "from CVLWMS_WAREHOUSETASKPROCESSINGWEBSTORE WT with (nolock) INNER JOIN " +
                    "LWMS_LOCATION WL WITH (NOLOCK) ON WT.WATA_FROMLOCATIONID = WL.LOCA_ID " +
                    "where PRGR_ID = 2 " +
                    "AND WRHS_ID = 140 " +
                    "AND WATT_ID = 4 " +
                    "and WL.LOCA_WAREHOUSEID = 140 " +
                    "AND PILI_CODE ='" + (ID) + "'" +
                    "ORDER BY WT.WATA_ACTUALFINISHDATE ASC"
                    ;

                SqlCommand myCommand = new SqlCommand(query, connection);

                var reader = myCommand.ExecuteReader();

                while (reader.Read())
                {
                    var loc = reader.GetValue(0).ToString();
                    if (!_originalLocations.Contains(loc))
                        _originalLocations.Add(loc);
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

        public List<String> GetOriginalPickLocations()
        {
            return _originalLocations;
        }

        private void CalculateOriginalRouteDistance()
        {
            var totalDistance = 0.0;
            //_originalLocations = _originalLocations.Select(x => x.ToUpper()).ToList();
            for (int i = 0; i < _originalLocations.Count - 1; i++)
            {
                var localDistance = _DistanceMatrix[_originalLocations[i]][_originalLocations[i + 1]];
                totalDistance = totalDistance + localDistance;
            }
            _totalDistance = totalDistance;
        }

        public Double GetRouteDistance()
        {
            return _totalDistance;
        }

        public bool CheckDimensions()
        {
            return sameSize;
        }

    }
}

