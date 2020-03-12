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

        private Double Epsilon ;

        private Double Difference;

        private Int32 N;     
        
        private List<string> _route;

        
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
            Console.WriteLine("Simualted annealing started at{0}", DateTime.Now);
            var old_route = new List<String>();

            old_route.AddRange(locs);
            alpha = 0.999;
            Tempereature = 700;
            Epsilon = 0.00001;
            var count = 0;
            while (Tempereature>Epsilon)
            {
                iteration++;
                var distance_old = new Double();
                var distance_new = new Double();                
                var new_route = new List<String>();
                var random_select = new Int32();
                random_select = random.Next(0, 4);

                distance_old=Compute_Distance(old_route);
                new_route= Select_Neighborhood_Method(old_route,random_select,10);
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
                        count += 1;
                        old_route.Clear();
                        old_route = new_route;
                        distance_old = distance_old + Difference;
                    }
                }
                Tempereature *= alpha;
                //Console.WriteLine(distance_old); 

            }
            Print(old_route);

        }
        
        private void Print(List<String> best_location)
        {   var best_dist = new Double();
            best_dist = Compute_Distance(best_location);
            Console.WriteLine("Algortihm stops at {0}", DateTime.Now);
            Console.WriteLine("Best distance{0}", best_dist);
        }
        private List<String> Neighborhood_Generation_2opt(List<String> locs)
        {   var indexA =new Int32();
            var indexB = new Int32();
            var temp ="";
            var revised_route= new List<String>();
            //Random 2 opt
            indexA = random.Next(0, N);
            indexB = random.Next(0, N);
            //Swap phase 
            revised_route.AddRange(locs);
            if (indexA != indexB)
            {                
                temp = revised_route[indexA];
                revised_route[indexA] = revised_route[indexB];
                revised_route[indexB] = temp;
            }         
         

            return revised_route;
        }
        private List<String> Neighborhood_Generation_Portional_2opt(List<String> locs)
        {
            var start = new Int32();
            var end = new Int32();
            var indexA = new Int32();
            var indexB = new Int32();
            var temp = "";
            var revised_route = new List<String>();
            //Random 2 opt
            start=random.Next(0, N);
            end=random.Next(start, N);
            indexA = random.Next(start, end);
            indexB = random.Next(start, end);
            //Swap phase 
            revised_route.AddRange(locs);
            if (indexA != indexB)
            {
                temp = revised_route[indexA];
                revised_route[indexA] = revised_route[indexB];
                revised_route[indexB] = temp;
            }


            return revised_route;
        }

        private List<String> Neighborhood_Generation_Decided_Portional_2opt(List<String> locs, Int32 range)
        {
            var start = new Int32();
            var indexA = new Int32();
            var indexB = new Int32();
            var temp = "";
            var revised_route = new List<String>();
            //Random 2 opt
            start = random.Next(0, N-range);
           
            indexA = random.Next(start, start+range);
            indexB = random.Next(start, start+range);
            //Swap phase 
            revised_route.AddRange(locs);
            if (indexA != indexB)
            {
                temp = revised_route[indexA];
                revised_route[indexA] = revised_route[indexB];
                revised_route[indexB] = temp;
            }


            return revised_route;
        }
        private List<String> Neighborhood_Generation_3opt(List<String> locs)
        {
            var indexA = new Int32();
            var indexB = new Int32();
            var indexC = new Int32();
            var temp = "";
            var revised_route = new List<String>();
            //Random 2 opt
            indexA = random.Next(0, N);
            indexB = random.Next(0, N);
            indexC = random.Next(0, N);
            //Swap phase 
            revised_route.AddRange(locs);
            if (indexA != indexB )
            {
                temp = revised_route[indexA];
                revised_route[indexA] = revised_route[indexB];
                revised_route[indexB] = revised_route[indexC];
                revised_route[indexC] = temp;
            }


            return revised_route;
        }
        private List<String> Select_Neighborhood_Method(List<String> locs,Int32 select,Int32 Range)
        {
            var generated_list = new List<String>();

            if (select == 1)
            {
                generated_list=Neighborhood_Generation_2opt(locs);
            }
            else if (select == 2)
            {
                generated_list = Neighborhood_Generation_Portional_2opt(locs);
            }
            else if (select == 3)
            {
                generated_list = Neighborhood_Generation_Decided_Portional_2opt(locs, Range);
            }
            else if (select==4)
            {
                generated_list = Neighborhood_Generation_3opt(locs);
            }
            return generated_list;
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
