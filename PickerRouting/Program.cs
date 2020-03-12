using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Security.Cryptography.X509Certificates;

namespace PickerRouting
{
    class Program
    {
        static void Main(string[] args)
        {
           
            var id = "TL586883";

            var reader = new SQLReader(id);
            var locations = new List<string>();
            locations = reader.GetLocations();
            var distance = new Dictionary<String, Dictionary<String, long>>();
            var orig_distnace = new Double();            
            distance = reader.GetDistanceMatrix();
            reader.Read();
            
            




            orig_distnace = reader.GetRouteDistance();
            
            Console.WriteLine("Hebele {0}", orig_distnace);
            //Tsp_Patch_Heuristic tsp_Patch_Heuristic = new Tsp_Patch_Heuristic(locations, distance, 100000);
            //tsp_Patch_Heuristic.Run();
            Simulated_Annealing test_heuristc = new Simulated_Annealing(locations, distance);
            test_heuristc.Run();
            //Test test = new Test(@"C:\Workspace\PickerRouting\veri.csv", 10);
            //test.Run();
            //TestHeuristics heuristic = new TestHeuristics(@"C:\Users\cagri.iyican\Desktop\Idlist.csv", 10);
            //heuristic.Run();

        }

    }
}