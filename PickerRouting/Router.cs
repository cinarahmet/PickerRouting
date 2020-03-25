using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
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

        /// <summary>
        /// Alternative meta-heuristic approaches.
        /// </summary>
        public enum Metas
        {
            GreedyDescent = 1,
            GuidedLocalSearch = 2,
            SimulatedAnnealing = 3,
            TabuSearch = 4,
            ObjectiveTabuSearch = 5
        }

        /// <summary>
        /// Selected meta-heuristic approach to route. 
        /// </summary>
        private Metas _meta;

        public Router()
        {
            _route = new List<string>();

            _objValue = 0.0;
        }


        public void Run(List<string> locations, Dictionary<string, Dictionary<string, long>> distances, Metas meta = Metas.GuidedLocalSearch, long timeLimit = 1)
        {
            _locations = new List<string>(locations);

            _distances = new Dictionary<string, Dictionary<string, long>>(distances.Select(x=>new KeyValuePair<string, Dictionary<string, long>>(x.Key,new Dictionary<string, long>(x.Value))));

            _meta = meta;

            _timeLimit = timeLimit;

            Adjust();

            int[] starts = new int[1];
            int[] tmeo = new int[1];

            _route = new List<string>();


            _locations.Add("Start");
            starts[0] = _locations.Count - 1;
            //starts[0] = 0;

            _locations.Add("Last");
            tmeo[0] = _locations.Count - 1;

            RoutingIndexManager manager = new RoutingIndexManager(
                _locations.Count,
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
            var index = solution.Value(routing.NextVar(routing.Start(0)));
            while (routing.IsEnd(index) == false)
            {
                _route.Add(_locations[manager.IndexToNode((int)index)]);
                index = solution.Value(routing.NextVar(index));
            }

            CalculateObjective();
        }

        private void Adjust()
        {
            var lastd = new Dictionary<string, long>();
            var startd = new Dictionary<string, long>();
            foreach (var loc in _locations)
            {
                _distances[loc].Add("Last", 0);
                _distances[loc].Add("Start", Int64.MaxValue);
                lastd.Add(loc, Int64.MaxValue);
                startd.Add(loc, 0);
            }
            _distances.Add("Start", startd);
            _distances.Add("Last", lastd);

            _distances["Start"].Add("Start", 0);
            _distances["Start"].Add("Last", 0);
            _distances["Last"].Add("Start", Int64.MaxValue);
            _distances["Last"].Add("Last", Int64.MaxValue);

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