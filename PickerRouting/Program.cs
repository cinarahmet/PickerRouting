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
            //Test test = new Test(@"C:\Workspace\PickerRouting\veri.csv", 10);
            //test.Run();
            var id = "TL576745";
            var reader = new SQLReader(id);
            var locations = new List<string>();
            var distance = new Dictionary<String, Dictionary<String, Double>>();
            reader.Read();
            locations = reader.GetLocations();
            distance = reader.GetDistanceMatrix();
            Tsp_Patch_Heuristic tsp_Patch_Heuristic = new Tsp_Patch_Heuristic(locations, distance, 100000);
            tsp_Patch_Heuristic.Run();
        }

    }
}