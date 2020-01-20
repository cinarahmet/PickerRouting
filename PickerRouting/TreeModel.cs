using System;
using System.Collections.Generic;
using System.Text;
using ILOG.Concert;
using ILOG.CPLEX;

namespace PickerRouting
{
    class TreeModel
    {
        /// <summary>
        /// x[i,j] € {0,1} denotes whether picker goes from
        /// location i to location j
        /// </summary>
        private List<List<INumVar>> _x;

        /// <summary>
        /// Sequence of location i in route
        /// </summary>
        private List<INumVar> _a;

        private List<String> _locations;

        private int _startIndex;

        private int _endIndex;

        private List<String> _route;

        /// <summary>
        /// d[i,j] denotes distance from location i to location j
        /// </summary>
        private readonly Dictionary<String, Dictionary<String, Double>> d;
    

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
        private readonly long _timeLimit = 4500;

        /// <summary>
        /// How many seconds the solver worked..
        /// </summary>
        private double _solutionTime;

        /// <summary>
        /// Number of pick locations
        /// </summary>
        private int N;

        private int M = 10000;

        public TreeModel(List<String> locations, Dictionary<String, Dictionary<String, Double>> distance)
        {
            _solver = new Cplex();
            _solver.SetParam(Cplex.DoubleParam.TiLim, _timeLimit);
            //_solver.SetOut(null);



            _x = new List<List<INumVar>>();
            _a = new List<INumVar>();

            _locations = locations;

            d = distance;

            _route = new List<string>();

            N = _locations.Count;
        }

        /// <summary>
        /// Run method where the running engine is triggered.
        /// </summary>
        public void Run(List<Double> sequence)
        {
            BuildModel();
            AddInitialSolution(sequence);
            Solve();
            if (!(_status == Cplex.Status.Optimal || _status == Cplex.Status.Feasible))
            {
                Console.WriteLine("Solution is not found !");
                return;
            }
            Print();
            ConstructRoute();
        }

        /// <summary>
        /// Build the model:
        /// 1. Create decision variables
        /// 2. Create objective function
        /// 3. Create constraints
        /// </summary>
        private void BuildModel()
        {
            Console.WriteLine("Model construction starts at {0}", DateTime.Now);
            CreateDecisionVariables();
            CreateObjective();
            CreateConstraints();
            Console.WriteLine("Model construction ends at {0}", DateTime.Now);
        }

        /// <summary>
        /// Solve the mathematical model
        /// </summary>
        private void Solve()
        {
            Console.WriteLine("Algorithm starts running at {0}", DateTime.Now);
            var startTime = DateTime.Now;
            _solver.Solve();
            _solutionTime = (DateTime.Now - startTime).Seconds;
            _status = _solver.GetStatus();
            Console.WriteLine("Algorithm stops running at {0}", DateTime.Now);
        }

        private void CreateDecisionVariables()
        {
            //Create x[i,j] variables



            for (int i = 0; i < N; i++)
            {
                var x_i = new List<INumVar>();
                for (int j = 0; j < N; j++)
                {

                    var name = String.Format("x[{0}{1}]", (i + 1), (j + 1));
                    var x_ij = _solver.NumVar(0, 1, NumVarType.Int, name);
                    x_i.Add(x_ij);
                }
                _x.Add(x_i);
            }

            for (int i = 0; i < N; i++)
            {
                var lb = 1;
                var ub = N + 1;
                
                var a_i = _solver.NumVar(lb, ub, NumVarType.Int);
                _a.Add(a_i);
            }

        }

        private void CreateObjective()
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

        private void CreateConstraints()
        {
            Nonnegativity();
            EachNodeMustBeVisitedOnce();
            EachNodeMustBeLeftOnce();
            TreeConstraint();
            SubTourEleminationConstraint();
        }

        private void EachNodeMustBeVisitedOnce()
        {
            for (int i = 0; i < N; i++)
            {
                var constraint = _solver.LinearNumExpr();
                for (int j = 0; j < N; j++)
                {
                    constraint.AddTerm(1, _x[j][i]);
                }
                _solver.AddLe(constraint, 1);
            }
        }

        private void EachNodeMustBeLeftOnce()
        {
            for (int i = 0; i < N; i++)
            {
                var constraint = _solver.LinearNumExpr();

                for (int j = 0; j < N; j++)
                {
                    constraint.AddTerm(1, _x[i][j]);
                }
                _solver.AddLe(constraint, 1);
            }
        }

        private void TreeConstraint()
        {
            var constraint = _solver.LinearNumExpr();
            for (int i = 0; i < N; i++)
            {
                for (int j = 0; j < N; j++)
                {
                    constraint.AddTerm(1, _x[i][j]);
                }
            }
            _solver.AddEq(constraint, N - 1);
        }

        private void SubTourEleminationConstraint()
        {
            for (int i = 0; i < N; i++)
            {
                for (int j = 0; j < N; j++)
                {
                    var constraint = _solver.LinearNumExpr();
                    constraint.AddTerm(1, _a[i]);
                    constraint.AddTerm(-1, _a[j]);
                    constraint.AddTerm(M, _x[i][j]);
                    _solver.AddLe(constraint, M - 1);
                }
            }
        }

        private void Nonnegativity()
        {
            for (int i = 0; i < N; i++)
            {
                for (int j = 0; j < N; j++)
                {
                    _solver.AddGe(_x[i][j], 0);
                }
            }
        }

        private void AddInitialSolution(List<Double> sequence)
        {
            _solver.AddMIPStart(_a.ToArray(), sequence.ToArray());
        }

        private void FindStartAndEnd()
        {
            for (int i = 0; i < N; i++)
            {
                var startValue = 0.0;
                var endValue = 0.0;
                for (int j = 0; j < N; j++)
                {
                    startValue += _solver.GetValue(_x[i][j]);
                    endValue += _solver.GetValue(_x[j][i]);
                }
                if (startValue + endValue == 1)
                {
                    if (startValue == 1)
                        _startIndex = i;
                    if (endValue == 1)
                        _endIndex = i;
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
                if (x_ij == 1)
                {
                    result = _locations[i];
                }
            }
            return result;
        }

        private void ConstructRoute()
        {
            FindStartAndEnd();
            var start = _locations[_startIndex];
            _route.Add(start);

            while (_route.Count < N - 1)
            {
                var nextPoint = FindNextPoint(_route[_route.Count - 1]);
                _route.Add(nextPoint);
            }

            var end = _locations[_endIndex];
            _route.Add(end);

            foreach (var element in _route)
                Console.WriteLine(element);
        }

        private void Print()
        {
            var objValue = _solver.GetObjValue();
            _status = _solver.GetStatus();
            Console.WriteLine("Objective Value is: {0}", objValue);
            Console.WriteLine("Solution Status is: {0}", _status);

            //for (int i = 0; i < N; i++)
            //{
            //    for (int j = 0; j < N; j++)
            //    {
            //        var val = _solver.GetValue(_x[i][j]);
            //        if (val > 0)
            //            Console.WriteLine("x[{0}][{1}]={2}", i + 1, j + 1, val);
            //    }
            //}
        }
    }
}
