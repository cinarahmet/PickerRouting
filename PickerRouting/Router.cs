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
        private List<string> _locations;

        private Dictionary<string, Dictionary<string, long>> _distances;

        private long _timeLimit;

        private List<string> _route;

        private double _objValue;
               
        private int _meta;

        private List<String> new_loc = new List<string>();

        public Router()
        {
            _route = new List<string>();

            _objValue = 0.0;
        }


        public void Run(List<string> locations, Dictionary<string, Dictionary<string, long>> distances, int meta, long _timeLimit=1)
        {
            _locations=locations;
            _distances = distances;
            this.new_loc.AddRange(_locations);
            

            _meta = meta;

            this._timeLimit = _timeLimit;
            if (_meta == 1)
            {
                Adjust();
            }
            
            
            int[] starts = new int[1];
            int[] tmeo = new int[1];
            
            _route = new List<string>();

            
            this.new_loc.Add("Start");
            starts[0] = new_loc.Count - 1;
            

            this.new_loc.Add("Last");
            tmeo[0] = new_loc.Count-1;
            RoutingIndexManager manager = new RoutingIndexManager(
                new_loc.Count,
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
                    return distances[new_loc[fromNode]][new_loc[toNode]];
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
                _route.Add(new_loc[manager.IndexToNode((int)index)]);
                index = solution.Value(routing.NextVar(index));
            }

            CalculateObjective();
        }

        private void Adjust()
        {
            var lastd = new Dictionary<string, long>();
            var startd = new Dictionary<string, long>();
            ;
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