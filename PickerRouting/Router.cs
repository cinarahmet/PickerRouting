using System;
using System.Collections.Generic;
using System.ComponentModel.Design;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using Google.OrTools.ConstraintSolver;
using Google.Protobuf.WellKnownTypes;
using Enum = System.Enum;



namespace PickerRouting
{
    public class Router
    {
        /// <summary>
        /// List of locations picker must visit.
        /// </summary>
        private List<string> _locations;

        /// <summary>
        /// Dictionary (a,b)-->c
        /// Manhattan distance from location a to location b is c units.
        /// </summary>
        private Dictionary<string, Dictionary<string, long>> _distances;

        /// <summary>
        /// Time limit for meta-heuristic approach in seconds.
        /// </summary>
        private long _timeLimit;

        /// <summary>
        /// Resulting route.
        /// </summary>
        private List<string> _route;

        /// <summary>
        /// Total length of the resulting route.
        /// </summary>
        private double _objValue;

        private bool _predetermined;

        /// <summary>
        /// Selected meta-heuristic approach to route. 
        /// </summary>
        private AlgorithmType.Metas _meta;

        public Router()
        {
            _route = new List<string>();

            _objValue = 0.0;
        }


        public void Run(List<string> locations, Dictionary<string, Dictionary<string, long>> distances, AlgorithmType.Metas meta, long timeLimit = 1)
        {
       
            _locations = new List<string>(locations);

            _distances = new Dictionary<string, Dictionary<string, long>>(distances.Select(x=>new KeyValuePair<string, Dictionary<string, long>>(x.Key,new Dictionary<string, long>(x.Value))));

            AdjustDistanceMatrix();

            _meta = meta;

            _timeLimit = timeLimit;
            

            int[] starts = new int[1];
            int[] end = new int[1];

            _route = new List<string>();



            starts[0] = 0;
            end[0] = _locations.Count - 1;

            RoutingIndexManager manager = new RoutingIndexManager(
                _locations.Count,
                1,
                starts,
                end);

            RoutingModel routing = new RoutingModel(manager);

            int transitCallbackIndex = routing.RegisterTransitCallback(
                (long fromIndex, long toIndex) =>
                {
                // Convert from routing variable Index to distance matrix NodeIndex.
                var fromNode = manager.IndexToNode(fromIndex);
                    var toNode = manager.IndexToNode(toIndex);
                    return _distances[_locations[fromNode]][_locations[toNode]];
                });

            routing.SetArcCostEvaluatorOfAllVehicles(transitCallbackIndex);

            RoutingSearchParameters searchParameters =
                operations_research_constraint_solver.DefaultRoutingSearchParameters();
            searchParameters.FirstSolutionStrategy =
                FirstSolutionStrategy.Types.Value.PathCheapestArc;
            searchParameters.TimeLimit = new Duration { Seconds = _timeLimit };
            searchParameters.LocalSearchMetaheuristic = (LocalSearchMetaheuristic.Types.Value) _meta;

            Assignment solution = routing.SolveWithParameters(searchParameters);

            _objValue = solution.ObjectiveValue();

            
            var index = routing.Start(0);
            while (routing.IsEnd(index) == false)
            {
                _route.Add(_locations[manager.IndexToNode((int)index)]);
                index = solution.Value(routing.NextVar(index));
            }
            _route.Add(_locations[manager.IndexToNode((int)index)]);
            

            CalculateObjective();
        }

        private void AdjustDistanceMatrix()
        {
            var keys = _distances.Keys.ToList();
            var zones = keys.Select(a => a.Split(".")[0]);

            if (zones.Distinct().Count() == 1)
            {
                return;
            }

            foreach (var zone in zones.Distinct())
            {
                var list = keys.Where(a => a.Split(".")[0] != zone).ToList();

                foreach (var k in keys)
                {
                    if (zone == k.Split(".")[0])
                    {
                        foreach (var item in list)
                        {
                            _distances[k].Add(item, Int32.MaxValue / (zones.Distinct().Count() + 1));
                        }
                    }
                }
            }
        }

        private void CalculateObjective()
        {
            _objValue = 0.0;
            for (int i = 0; i < _route.Count - 1; i++)
            {
                _objValue += _distances[_route[i]][_route[i + 1]];
            }
        }

        public List<string> GetRoute()
        {
            return _route;
        }

        public double GetRouteLength()
        {
            return _objValue;
        }
    }
} 