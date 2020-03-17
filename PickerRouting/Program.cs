using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using Google.OrTools.ConstraintSolver;

namespace PickerRouting
{
    class Program
    {
        static void Main(string[] args)
        {
            var total_pick = new List<String>();
            var obj_values = new List<List<Double>>();
            using (var sr = File.OpenText(@"C:\Users\cagri.iyican\Desktop\2-6 mart picklists.csv"))
            {
                
                String s = sr.ReadLine();
                while ((s = sr.ReadLine()) != null)
                {
                    var line = s.Split(',');
                    var pick_list = line[0];
                    total_pick.Add(pick_list);
                }
            }

            //var id = "TL586883";                                         
            //parameters gibi bir class olsun mu?
            //meta için enumeration olsun mu?
            
            

            for (int j = 0; j < total_pick.Count ; j++)
            {
                var reader = new SQLReader(total_pick[j]);
                reader.Read();
                var locations = reader.GetLocations();
                var distance_matrix = reader.GetDistanceMatrix();
                var orig_distance = reader.GetRouteDistance();
                Console.WriteLine("The initial route length that picker had {0}", orig_distance);
                var router = new Router();
                for (int i = 0; i < 5; i++)
                {
                    var meta = i;

                    if (reader.CheckDimensions())
                    {                       
                        router.Run(locations, distance_matrix, meta);
                        var route = router.GetRoute();
                        obj_values[j][i] = router.GetRouteLength();
                        Console.WriteLine($"{obj_values[j][i]}");
                    }
                    
                   
                }


            }

        }
            

    }
}