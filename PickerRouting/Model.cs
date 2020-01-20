using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ILOG.Concert;
using ILOG.CPLEX;

namespace PickerRouting
{
    class Model
    {
        /// <summary>
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

        public Model(List<String> locations, Dictionary<String, Dictionary<String, Double>> distance, long timeLimit)
        {
            _timeLimit = timeLimit;
            _solver = new Cplex();
            _solver.SetParam(Cplex.DoubleParam.TiLim, _timeLimit);
            _solver.SetOut(null);

            _locations = locations;//.Take(10).ToList();

            _x = new List<List<INumVar>>();
            _a = new List<INumVar>();
            _sequence = new List<double>();

            d = distance;

            _route = new List<string>();

            N = _locations.Count;
        }

        /// <summary>
        /// Run method where the running engine is triggered.
        /// </summary>
        public void Run(int startIndex)
        {
            BuildModel(startIndex);
            Solve();
            if (!(_status == Cplex.Status.Optimal || _status == Cplex.Status.Feasible))
            {
                Console.WriteLine("Solution is not found !");
                return;
            }
            FillSequence();
            Print();
            ConstructRoute();
        }

        /// <summary>
        /// Build the model:
        /// 1. Create decision variables
        /// 2. Create objective function
        /// 3. Create constraints
        /// </summary>
        private void BuildModel(int startIndex)
        {
            Console.WriteLine("Model construction starts at {0}", DateTime.Now);
            CreateDecisionVariables();
            CreateObjective();
            CreateConstraints(startIndex);
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

                    var lb_x = 0;
                    var ub_x = 1;

                    var name = String.Format("x[{0}{1}]", (i + 1), (j + 1));
                    
                    if (i == j)
                    {
                        ub_x = 0; 
                    }
                    var x_ij = _solver.NumVar(lb_x, ub_x, NumVarType.Int, name);
                    x_i.Add(x_ij);
                }
                _x.Add(x_i);
            }

            //Creates a_i variables for sequensing
            for (int i = 0; i < N; i++)
            {
                var lb = 1;
                var ub = N + 1;
                if (i == 0)
                {
                    ub = 1;
                }
                if (i == N - 1)
                {
                    lb = N + 1;
                }
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

        private void CreateConstraints(int startIndex)
        {
            LeavingFromTheStartingLocation(startIndex);
            EnteringIntoLastLocation();
            Nonnegativity();
            EachNodeMustBeVisitedOnce();
            EachNodeMustBeLeftOnce();
            SubTourEleminationConstraint();
        }

        private void LeavingFromTheStartingLocation(int startIndex)
        {
            var constraint = _solver.LinearNumExpr();

            startIndex = (startIndex == -1) ? 0 : 1;

            for (int j = 1; j < N; j++)
            {
                constraint.AddTerm(1, _x[startIndex][j]);
            }
            _solver.AddEq(constraint, 1);
        }

        private void EnteringIntoLastLocation()
        {
            var constraint = _solver.LinearNumExpr();

            for (int i = 0; i < N-1; i++)
            {
                constraint.AddTerm(1, _x[i][N - 1]);
            }
            _solver.AddEq(constraint, 1);
        }

        private void EachNodeMustBeVisitedOnce()
        {
            for (int i = 1; i < N-1; i++)
            {
                var constraint = _solver.LinearNumExpr();
                for (int j = 0; j < N-1; j++)
                {
                    constraint.AddTerm(1, _x[j][i]);
                }
                _solver.AddEq(constraint, 1);
            }
        }

        private void EachNodeMustBeLeftOnce()
        {
            for (int i = 1; i < N-1; i++)
            {
                var constraint = _solver.LinearNumExpr();

                for (int j = 1; j < N; j++)
                {
                    constraint.AddTerm(1, _x[i][j]);
                }
                _solver.AddEq(constraint, 1);
            }
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

        public List<Double> GetSequence()
        {
            return _sequence;
        }

        private void FillSequence()
        {
            for (int i = 0; i < N; i++)
            {
                _sequence.Add(_solver.GetValue(_a[i]));
            }
        }

        private void FindStartAndEnd()
        {
            for (int i = 0; i < N ; i++)
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
                if (x_ij >= 1.0 - _epsilon)
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

            while (_route.Count < N-1)
            {
                var nextPoint = FindNextPoint(_route[_route.Count - 1]);
                _route.Add(nextPoint);
            }

            var end = _locations[_endIndex];
            _route.Add(end);

            //foreach (var element in _route)
            //    Console.WriteLine(element);
        }

        public List<string> GetFirstNLocation(int firstN)
        {
            return _route.Take(firstN).ToList();
        }

        public bool GetStatus()
        {
            return _solver.GetStatus() == Cplex.Status.Feasible;
        }

        public double GetObjectiveValue()
        {
            return _objValue;
        }

        public List<string> GetRoute()
        {
            return _route;
        }


        private void Print()
        {
            _objValue = _solver.GetObjValue();
            _status = _solver.GetStatus();
            Console.WriteLine("Objective Value is: {0}", _objValue);
            Console.WriteLine("Solution Status is: {0}", _status);

            //for (int i = 0; i < N; i++)
            //{
            //    var fromLocation = _locations[i];
            //    for (int j = 0; j < N; j++)
            //    {
            //        var toLocation = _locations[j];
            //        var val = _solver.GetValue(_x[i][j]);
            //        if (val > 0)
            //            Console.WriteLine("{0}--->{1}",fromLocation,toLocation);
            //    }
            //}
        }
    }
}
