using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using ILOG.Concert;

namespace PickerRouting
{
    public class Test
    {
        private List<string> _idList;

        private List<string> locations;

        private Dictionary<string, Dictionary<string, long>> d;

        private List<string> originalLocations;

        private double originalRouteDistance;

        private Dictionary<string, double> _originalRouteDistances;

        private double N;

        private int _firstN;

        private long _baseTimeLimit;

        private List<Double> _baseModelSequence;

        private Dictionary<string, List<Double>> _baseModelSequences;

        private List<string> _baseModelRoute;

        private Dictionary<string, List<string>> _baseModelRoutes;

        private double _objValue;

        private Dictionary<string, double> _baseModelObjValues;

        private long _testTimeLimit;

        private List<string> _testRoute;

        private Dictionary<string, List<string>> _testRoutes;

        private Dictionary<string, double> _testModelObjValues;

        private Dictionary<string, int> _orderCounts;

        private string _fileLocation;

        private bool sameSize;

        StreamWriter objectives = new StreamWriter("C:/Users/cagri.iyican/Desktop/objectives11.csv");

        StreamWriter file = new StreamWriter("C:/Users/cagri.iyican/Desktop/routes11.csv");

        StreamWriter baseRoute = new StreamWriter("C:/Users/cagri.iyican/Desktop/baseRoute11.csv");

        public Test(string fileLocation, int firstN)
        {
            _idList = new List<string>();

            _fileLocation = fileLocation;

            _firstN = firstN;

            _baseTimeLimit = 10;

            _testTimeLimit = 60;

            _baseModelSequences = new Dictionary<string, List<double>>();

            _baseModelObjValues = new Dictionary<string, double>();

            _baseModelRoutes = new Dictionary<string, List<string>>();

            _testRoutes = new Dictionary<string, List<string>>();

            _testModelObjValues = new Dictionary<string, double>();

            _originalRouteDistances = new Dictionary<string, double>();

        }

        public void Run()
        {
            ReadPickListIds();
            foreach (string id in _idList)
            {
                Read(id);
                if (sameSize)
                {
                    _originalRouteDistances.Add(id, originalRouteDistance);

                    RunTestModel();
                    if (_testRoute.Count == originalLocations.Count)
                    {
                        _baseModelSequences.Add(id, _baseModelSequence);
                        _baseModelObjValues.Add(id, _objValue);
                        _baseModelRoutes.Add(id, _baseModelRoute);

                        _testRoutes.Add(id, _testRoute);
                        _testModelObjValues.Add(id, _objValue);

                        WriteToCsv(id);
                    }
                    
                }
            }
        }

        private void ReadPickListIds()
        {
            var reader = new CsvReader();
            reader.ReadCsv(_fileLocation);
            _idList = reader.GetPickListIds();
            _orderCounts = reader.GetOrderCounts();
        }

        private void Read(string id)
        {
            var reader = new SQLReader(id);
            reader.Read();
            sameSize = reader.CheckDimensions();
            if (sameSize)
            {
                locations = reader.GetLocations();
                d = reader.GetDistanceMatrix();
                originalLocations = reader.GetOriginalPickLocations();

                N = locations.Count;

                originalRouteDistance = reader.GetRouteDistance();
            }
        }

        private void RunBaseModel()
        {
            Model model = new Model(locations, d, _baseTimeLimit);
            model.Run(-1);
            _baseModelSequence = model.GetSequence();
            _baseModelRoute = model.GetRoute();
            _objValue = model.GetObjectiveValue();
        }

        private void RunTestModel()
        {
            _testRoute = new List<string>();
            bool isFeasible = true;
            long timeLimit = _baseTimeLimit;
            var startIndex= -1;
            for (int iteration = 1; iteration - 1 < N / _firstN; iteration++)
            {
                var model = new Model(locations, d, timeLimit);
                Console.WriteLine("Iteration: {0}\t|Locations|: {1}", iteration, locations.Count);
                model.Run(startIndex);
                string[] freezed = new string[_firstN+1];
                if (iteration == 1)
                {
                    _baseModelSequence = model.GetSequence();
                    _baseModelRoute = model.GetRoute();
                    _objValue = model.GetObjectiveValue();
                    freezed = new string[_firstN];
                }
                isFeasible = model.GetStatus();
                if (isFeasible)
                {
                    freezed = model.GetFirstNLocation(_firstN + Convert.ToInt32(iteration != 1)).ToArray();
                    locations = locations.Except(model.GetFirstNLocation(_firstN-Convert.ToInt32(iteration == 1))).ToList();
                    startIndex = locations.IndexOf(freezed[freezed.Count()-1]);
                    _testRoute.AddRange(freezed.TakeLast(_firstN));
                    timeLimit = _testTimeLimit;
                    continue;
                }

                freezed = model.GetFirstNLocation(locations.Count-1).ToArray();
                _testRoute.AddRange(freezed);
                break;
            }

            CalculateObjective();
        }

        private void CalculateObjective()
        {
            var N = _testRoute.Count;
            _objValue = 0.0; 
            for (int i = 0; i < N - 1; i++)
            {
                _objValue += d[_testRoute[i]][_testRoute[i + 1]];
            }
            Console.WriteLine("Tested Objective: {0}", _objValue);
        }

        private void WriteToCsv(string id)
        {
            objectives.Write(id);
            objectives.Write(",");
            objectives.Write(_baseModelObjValues[id]);
            objectives.Write(",");
            objectives.Write(_testModelObjValues[id]);
            objectives.Write(",");
            objectives.Write(100 * (_baseModelObjValues[id] - _testModelObjValues[id]) / _baseModelObjValues[id]);
            objectives.Write(",");
            objectives.Write(100 * (_originalRouteDistances[id] - _testModelObjValues[id]) / _originalRouteDistances[id]);
            objectives.Write("\n");
            objectives.Flush();
            
            


            file.Write(id);
            file.Write(",");
            for (int i = 0; i < _orderCounts[id]; i++)
            {
                file.Write(_testRoutes[id][i]);
                file.Write(",");
            }

            file.Write("\n");
            file.Flush();


            baseRoute.Write(id);
            baseRoute.Write(",");
            for (int i = 0; i < _orderCounts[id]; i++)
            {
                baseRoute.Write(_baseModelRoutes[id][i]);
                baseRoute.Write(",");
            }

            baseRoute.Write("\n");
            baseRoute.Flush();
        }

    }
}

