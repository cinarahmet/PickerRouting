using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Google.OrTools.ConstraintSolver;
using Google.Protobuf.WellKnownTypes;

namespace PickerRouting
{
    public class Router
    {
        private List<string> locations;

        private Dictionary<string, Dictionary<string, long>> distances;

        private long _timeLimit;

        private List<string> _route;

        private double _objValue;

        private bool sameSize;

        private int meta;

        public Router()
        {
            _route = new List<string>();

            _objValue = 0.0;
        }


        public void Run(List<string> locations, Dictionary<string, Dictionary<string, long>> distances, int meta=2, long _timeLimit=1)
        {
            this.locations = locations;

            this.distances = distances;

            this.meta = meta;

            this._timeLimit = _timeLimit;

            Adjust();

            int[] starts = new int[1];
            int[] tmeo = new int[1];
            
            _route = new List<string>();

            
            locations.Add("Start");
            starts[0] = locations.Count - 1;
            

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
                    return distances[locations[fromNode]][locations[toNode]];
                });

            routing.SetArcCostEvaluatorOfAllVehicles(transitCallbackIndex);
            

            RoutingSearchParameters searchParameters =
                operations_research_constraint_solver.DefaultRoutingSearchParameters();
            searchParameters.FirstSolutionStrategy =
                FirstSolutionStrategy.Types.Value.PathCheapestArc;
            searchParameters.TimeLimit = new Duration {Seconds = _timeLimit};
            searchParameters.LocalSearchMetaheuristic = (LocalSearchMetaheuristic.Types.Value) meta;

            Assignment solution = routing.SolveWithParameters(searchParameters);
            
            _objValue = solution.ObjectiveValue();
            var index = solution.Value(routing.NextVar(routing.Start(0)));
            while (routing.IsEnd(index) == false)
            {
                _route.Add(locations[manager.IndexToNode((int)index)]);
                index = solution.Value(routing.NextVar(index));
            }

            CalculateObjective();
        }

        private void Adjust()
        {
            var lastd = new Dictionary<string, long>();
            var startd = new Dictionary<string, long>();
            foreach (var loc in locations)
            {
                distances[loc].Add("Last", 0);
                distances[loc].Add("Start", Int64.MaxValue);
                lastd.Add(loc, Int64.MaxValue);
                startd.Add(loc, 0);
            }
            distances.Add("Start", startd);
            distances.Add("Last", lastd);

            distances["Start"].Add("Start", 0);
            distances["Start"].Add("Last", 0);
            distances["Last"].Add("Start", Int64.MaxValue);
            distances["Last"].Add("Last", Int64.MaxValue);

        }

        private void CalculateObjective()
        {
            _objValue = 0.0; 
            for (int i = 0; i < _route.Count - 1; i++)
            {
                _objValue += distances[_route[i]][_route[i + 1]];
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