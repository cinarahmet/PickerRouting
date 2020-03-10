using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;
using ILOG.Concert;


namespace PickerRouting
{
     class Simulated_Annealing
     {                
        private List<string> _locations;

        private Dictionary<string, Dictionary<string, long>> d;

        private Double alpha ;

        private Double Tempereature ;

        private Double Epsilon = 0.001;

        private Double Difference;

        private Int32 N;     
        
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

        private double propa;

        private Random random;

        private Int32 iteration = 0;

        public Simulated_Annealing(List<String> locations, Dictionary<String, Dictionary<String, long>> distance)
        {
            _locations = locations;

            d = distance;
            _route = new List<string>();

            N = _locations.Count;

            alpha = new double();
            Difference = new double();
            Epsilon = new double();
            random = new Random();
            iteration = new Int32();

        }


        public void Run()
        {
            Algorithm(_locations, d);            
        }

        private void Algorithm(List<String> locs, Dictionary<String, Dictionary<String, long>> distance)
        {
            var old_route = new List<String>();

            old_route.AddRange(locs);
            alpha = 0.999;
            Tempereature = 1000;
            while (Tempereature>Epsilon)
            {
                iteration++;
                var distance_old = new Double();
                var distance_new = new Double();                
                var new_route = new List<String>();

                distance_old=Compute_Distance(old_route);
                new_route=Neighborhood_Generation(old_route);
                distance_new = Compute_Distance(new_route);
                Difference = distance_new - distance_old;

                if (Difference < 0)
                {
                    old_route.Clear();
                    old_route = new_route;
                    distance_old = distance_old + Difference;
                }
                else
                {
                    propa = random.NextDouble();

                    if (propa < Math.Exp(-Difference / Tempereature))
                    {
                        old_route.Clear();
                        old_route = new_route;
                        distance_old = distance_old + Difference;
                    }
                }
                Tempereature *= alpha;
                Console.WriteLine(distance_old); 
                
            }
            Print(old_route);

        }
        private void Print(List<String> best_location)
        {   var best_dist = new Double();
            best_dist = Compute_Distance(best_location);
            Console.WriteLine("Best distance{0}", best_dist);
        }
        private List<String> Neighborhood_Generation(List<String> locs)
        {   var indexA =new Int32();
            var indexB = new Int32();
            var temp ="";
            var revised_route= new List<String>();
            //Random 2 opt
            indexA = random.Next(1, N - 1);
            indexB = random.Next(1, N - 1);
            //Swap phase 
            revised_route.AddRange(locs);
            temp = revised_route[indexA];
            revised_route[indexA] = revised_route[indexB];
            revised_route[indexB] = temp;
         

            return revised_route;
        }

        private Double Compute_Distance(List<String> locs)
        {
            var total_distance = new Double();
            for (int i = 1; i < locs.Count; i++)
            {
                total_distance += d[locs[i-1]][locs[i]];
            }

            return total_distance;
        }
    }
}
