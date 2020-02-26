using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;
using ILOG.Concert;
using ILOG.CPLEX;

namespace PickerRouting
{
    class Tsp_Patch_Heuristic
    {    /// <summary>
         /// x[i,j] € {0,1} denotes whether picker goes from
         /// location i to location j
         /// </summary>
        private List<List<INumVar>> _x;

        /// <summary>
        /// d[i,j] denotes distance from location i to location j
        /// </summary>
        private readonly Dictionary<String, Dictionary<String, Double>> d;

        private List<String> _locations;

        private int _startIndex;

        private int _endIndex;

        private List<String> _route;

        /// <summary>
        /// Sequence of location i in route
        /// </summary>
        private List<INumVar> _a;

        private List<Double> _sequence;

        /// <summary>
        /// CPLEX solver object
        /// </summary>
        private readonly Cplex _solver;

        /// <summary>
        /// Objective instance which stores objective value
        /// </summary>
        private ILinearNumExpr _objective;

        /// <summary>
        /// Solution status: 0 - Optimal; 1 - Feasible...
        /// </summary>
        private Cplex.Status _status;

        /// <summary>
        /// Time limit is given in seconds.
        /// </summary>
        private long _timeLimit = 60;

        /// <summary>
        /// How many seconds the solver worked..
        /// </summary>
        private double _solutionTime;

        private double _objValue;

        /// <summary>
        /// Number of pick locations
        /// </summary>
        private int N;

        private int M = 10000;

        private Double _epsilon = 0.00001;

        public Tsp_Patch_Heuristic(List<String> locations, Dictionary<String, Dictionary<String, Double>> distance, long timeLimit)
        {
            _timeLimit = timeLimit;
            _solver = new Cplex();
            _solver.SetParam(Cplex.DoubleParam.TiLim, _timeLimit);
            _solver.SetOut(null);

            _locations = locations;

            _x = new List<List<INumVar>>();
            _a = new List<INumVar>();
            _sequence = new List<double>();

            d = distance;

            _route = new List<string>();

            N = _locations.Count;

        }

        public void Run()
        {
            Build_Model();
            Solve();
            Subtours();
            Print();
        }

        private void Create_Decision_Variables()
        {
            for (int i = 0; i < N; i++)
            {
                var x_i = new List<INumVar>();
                for (int j = 0; j < N; j++)
                {
                    var lb_x = 0;
                    var ub_x = 1;

                    var name = String.Format("x[{0}][{1}]", (i + 1), (j + 1));
                    if (i == j)
                    {
                        ub_x = 0;
                    }
                    var x_ij = _solver.NumVar(lb_x, ub_x, NumVarType.Int, name);
                    x_i.Add(x_ij);

                }
                _x.Add(x_i); 
            }
        }

        private void Create_Constraints()
        {
            Node_Entrance_Constraint();
            Node_Outgoing_Constraint();
            At_Least_3Node_Constraint();

        }
        private void Node_Entrance_Constraint()
        {
            for (int j=0; j < N; j++)
            {
                var constraint = _solver.LinearNumExpr();
                for (int i = 0; i < N; i++)
                {
                    constraint.AddTerm(_x[i][j], 1);
                }
                _solver.AddEq(constraint, 1);
            }
        }
        private void Node_Outgoing_Constraint()
        {
            for (int i = 0; i < N; i++)
            {
                var constraint = _solver.LinearNumExpr();
                for (int j = 0; j < N; j++)
                {
                    constraint.AddTerm(_x[i][j], 1);

                }
                _solver.AddEq(constraint, 1);
            }

        }
        private List<List<String>> Subtours()
        {
            var visitedlocations = new List<Dictionary<string, string>>();
            var newlocations = new List<Dictionary<string, string>>();
            for (int i = 0; i < N; i++)
            {
                for (int j = 0; j < N; j++)
                {
                   
                    if (_solver.GetValue(_x[i][j]) > 0.9)
                    {
                        var arcs = new Dictionary<String, String>();
                        var loc1 = _locations[i];
                        var loc2 = _locations[j];
                        arcs.Add(loc1, loc2);
                        visitedlocations.Add(arcs);
                        newlocations = visitedlocations;
                        
                    }
                }

            }
            var subtours = new List< List<String>>();            
            
            for (int i=0; i < visitedlocations.Count; i++)
            {
                var routes = new List<String>();
                var start = visitedlocations[i].First();                
                routes.Add(start.Key);
                routes.Add(visitedlocations[i][start.Key]);
                var searchkey = visitedlocations[i][start.Key];
                visitedlocations.Remove(visitedlocations[i]);                
                for (int j = 0; j < visitedlocations.Count; j++)
                {                   

                    if (visitedlocations[j].First().Key==searchkey)
                    {                       
                        routes.Add(visitedlocations[j].First().Value);
                        searchkey = visitedlocations[j].First().Value;
                        visitedlocations.Remove(visitedlocations[j]);
                        //visitedlocations.Remove(visitedlocations[i]);
                        j = -1;
                        if (routes[0] == routes[routes.Count - 1])
                        {
                            subtours.Add(routes);
                        }

                    }
                    
                }              
                
            }
            return subtours;
        }
        private void At_Least_3Node_Constraint()
        {   
            for (int i = 0; i < N; i++)
            {
                for (int j = 0; j < N; j++)
                {
                    var constraint = _solver.LinearNumExpr();
                    constraint.AddTerm(_x[i][j], 1);
                    constraint.AddTerm(_x[j][i], 1);
                    _solver.AddLe(constraint, 1);
                }
            }
        }
        private String FindNextPoint(String point)
        {
            var result = "";
            var index = _locations.IndexOf(point);
            for (int i = 0; i < N; i++)
            {
                var x_ij = _solver.GetValue(_x[index][i]);
                if (x_ij >= 1.0 - _epsilon)
                {
                    result = _locations[i];
                }
            }
            return result;
        }

        private void Create_Objective()
        {
            _objective = _solver.LinearNumExpr();
            for (int i = 0; i < N; i++)
            {
                var fromLocation = _locations[i];
                var sub_d = d[fromLocation];
                for (int j = 0; j < N; j++)
                {
                    var toLocation = _locations[j];
                    var d_ij = sub_d[toLocation];
                    _objective.AddTerm(d_ij, _x[i][j]);
                }
            }

            _solver.AddMinimize(_objective);

        }
        private void Build_Model()
        {
            Create_Decision_Variables();
            Create_Constraints();
            Create_Objective();
        }
        private void Solve()
        {
            Console.WriteLine("Algorithm starts running at {0}", DateTime.Now);
            _solver.Solve();
            _status = _solver.GetStatus();
        }
        private void Print()
        {
            _objValue = _solver.GetObjValue();
            _status = _solver.GetStatus();
            Console.WriteLine("Objective value is{0}", _objValue);
            Console.WriteLine("Status is{0}", _status);
        }
    }
}
