using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Google.OrTools.ConstraintSolver;
using Google.Protobuf.WellKnownTypes;

namespace PickerRouting
{
    public class TestHeuristics
    {
        private List<string> _idList;

        private List<string> locations;

        private List<string> __locations;

        private Dictionary<string, Dictionary<string, long>> d;

        private List<string> originalLocations;

        private double originalRouteDistance;

        private Dictionary<string, double> _originalRouteDistances;

        private double N;

        private int _firstN;

        private long _baseTimeLimit;

        private List<string> _route;

        private List<string> _testRoute;

        private Dictionary<string, List<string>> _routes;

        private Dictionary<string, List<string>> _testRoutes;

        private double _objValue;

        private Dictionary<string, double> _objValues;

        private double _testObjValue;

        private Dictionary<string, double> _testObjValues;

        private long _testTimeLimit;

        private Dictionary<string, int> _orderCounts;

        private string _fileLocation;

        private bool sameSize;

        private List<string> _baseTestRoute;

        private double _baseObjValue;

        StreamWriter objectives = new StreamWriter("C:/Users/cagri.iyican/Desktop/objectives3.csv");

        StreamWriter file = new StreamWriter("C:/Users/cagri.iyican/Desktop/objectives1.csv");

        StreamWriter baseRoute = new StreamWriter("C:/Users/cagri.iyican/Desktop/objectives2.csv");

        private string filePath = "C:/Users/cagri.iyican/Desktop/";

        public TestHeuristics(string fileLocation, int firstN)
        {
            _idList = new List<string>();

            _fileLocation = fileLocation;

            _firstN = firstN;

            _baseTimeLimit = 1;

            _testTimeLimit = 120;

            _routes = new Dictionary<string, List<string>>();

            _originalRouteDistances = new Dictionary<string, double>();

            _objValues = new Dictionary<string, double>();

            _testRoutes = new Dictionary<string, List<string>>();

            _testObjValues = new Dictionary<string, double>();

            _baseTestRoute = new List<string>();

            _baseObjValue = 0.0;
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
                    __locations = new List<string>();
                    __locations.AddRange(locations);

                    RunHeuristic(_baseTimeLimit, -1, 3);
                    _routes.Add(id, _route);
                    _objValues.Add(id, _objValue);

                    locations = __locations;

                    TestHeuristic(_baseTimeLimit, _testTimeLimit, 3);
                    _testRoutes.Add(id, _testRoute);
                    _testObjValues.Add(id, _objValue);

                    WriteToCsv(9999);
                }
            }

        }

        public void RunForInitials()
        {
            ReadPickListIds();

            for (int meta = 4; meta < 6; meta++)
            {
                locations = __locations;

                foreach (string id in _idList)
                {
                    Read(id);

                    if (sameSize)
                    {
                        _originalRouteDistances.Add(id, originalRouteDistance);

                        TestHeuristic(_baseTimeLimit, _testTimeLimit, meta);
                        _routes.Add(id, _baseTestRoute);
                        _objValues.Add(id, _baseObjValue);
                        _testRoutes.Add(id, _testRoute);
                        _testObjValues.Add(id, _objValue);

                        WriteToCsv(meta);

                    }
                }
            }
        }

        public void RunHeuristic(long _timeLimit, int startIndex, int meta)
        {
            
            int[] starts = new int[1];
            int[] tmeo = new int[1];
            
            _route = new List<string>();

            if (startIndex!=-1)
            {
                starts[0] = startIndex;
            }
            else
            {
                locations.Add("Start");
                starts[0] = locations.Count - 1;
            }

            locations.Add("Last");
            tmeo[0] = locations.Count-1;
            RoutingIndexManager manager = new RoutingIndexManager(
                locations.Count,
                1,
                starts,
                tmeo);

            RoutingModel routing = new RoutingModel(manager);

            int transitCallbackIndex = routing.RegisterTransitCallback(
                (long fromIndex, long toIndex) =>
                {
                    // Convert from routing variable Index to distance matrix NodeIndex.
                    var fromNode = manager.IndexToNode(fromIndex);
                    var toNode = manager.IndexToNode(toIndex);
                    return d[locations[fromNode]][locations[toNode]];
                });

            routing.SetArcCostEvaluatorOfAllVehicles(transitCallbackIndex);
            

            RoutingSearchParameters searchParameters =
                operations_research_constraint_solver.DefaultRoutingSearchParameters();
            searchParameters.FirstSolutionStrategy =
                FirstSolutionStrategy.Types.Value.PathCheapestArc;
            searchParameters.TimeLimit = new Duration {Seconds = _timeLimit};
            searchParameters.LocalSearchMetaheuristic = (LocalSearchMetaheuristic.Types.Value) meta;

            Assignment solution = routing.SolveWithParameters(searchParameters);
            Console.WriteLine("Status: {0}", routing.GetStatus());
            _objValue = solution.ObjectiveValue();
            var index = routing.Start(0);
            while (routing.IsEnd(index) == false)
            {
                if (startIndex == -1)
                {
                    startIndex = -2;
                    //index = solution.Value(routing.NextVar(index));
                    continue;
                }
                _route.Add(locations[manager.IndexToNode((int)index)]);
                index = solution.Value(routing.NextVar(index));
            }

            Console.WriteLine("Objective of the iteration: {0}. Status of the iteration: {1}.", _objValue, routing.GetStatus());
        }

        public void TestHeuristic(long baseTimeLimit, long testTimeLimit, int meta)
        {
            _testRoute = new List<string>();
            string[] freezed = new string[_firstN+1];
            long timeLimit = baseTimeLimit;
            var startIndex = -1;

            for (int iteration = 1; iteration - 1 < N / _firstN; iteration++)
            {
                Console.WriteLine("Iteration: {0}\t|Locations|: {1}", iteration, locations.Count);
                RunHeuristic(timeLimit, startIndex, meta);

                if (iteration == 1)
                {
                    _baseTestRoute = _route;
                    _baseObjValue = _objValue;

                    freezed = new string[_firstN];
                }

                locations.Remove("Last");

                freezed = GetFirstNLocation(_firstN + Convert.ToInt32(iteration != 1)).ToArray();
                locations = locations.Except(GetFirstNLocation(_firstN - Convert.ToInt32(iteration == 1))).ToList();
                startIndex = locations.IndexOf(freezed[freezed.Count() - 1]);
                _testRoute.AddRange(freezed.TakeLast(freezed.Length - Convert.ToInt32(iteration != 1)));
                timeLimit = testTimeLimit;
            }

            CalculateObjective();
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
                var lastd = new Dictionary<string, long>();
                var startd = new Dictionary<string, long>();
                foreach (var loc in locations)
                {
                    d[loc].Add("Last", 0);
                    d[loc].Add("Start", Int64.MaxValue);
                    lastd.Add(loc, Int64.MaxValue);
                    startd.Add(loc, 0);
                }
                d.Add("Start", startd);
                d.Add("Last", lastd);

                d["Start"].Add("Start", 0);
                d["Start"].Add("Last", 0);
                d["Last"].Add("Start", Int64.MaxValue);
                d["Last"].Add("Last", Int64.MaxValue);

                originalLocations = reader.GetOriginalPickLocations();

                N = d.Count;

                originalRouteDistance = reader.GetRouteDistance();
            }
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

        private List<string> GetFirstNLocation(int firstN)
        {
            return _route.Take(firstN).ToList();
        }

        private void WriteToCsv(int meta)
        {
            StreamWriter fileWriter = new StreamWriter(filePath + Convert.ToString(meta) + ".csv");

            foreach (var r in _routes.Keys)
            {
                var line = r + ", " + _originalRouteDistances[r] + ", " + _objValues[r] + ", " + _testObjValues[r] + ", " + 100*(_objValues[r]-_testObjValues[r])/_objValues[r] + ", " + 100 * (_originalRouteDistances[r] - _testObjValues[r]) / _originalRouteDistances[r];
                fileWriter.WriteLine(line);
            }

            fileWriter.Close();
        }
    }
}