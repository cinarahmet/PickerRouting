using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using Google.OrTools.ConstraintSolver;

namespace PickerRouting
{
    class Program
    {
        static void Main(string[] args)
        {
            var lines = new List<String>();

            using (var sr = File.OpenText(@"C:\Workspace\PickerRouting\2-6 mart picklists.csv"))
            {
                var s = sr.ReadLine();
                while ((s = sr.ReadLine()) != null)
                {
                    var pick_list = s.Split(',')[0];

                    var reader = new SQLReader(pick_list);
                    reader.Read();

                    var locations = reader.GetLocations();
                    var distance_matrix = reader.GetDistanceMatrix();
                    var orig_distance = reader.GetRouteDistance();

                    var router = new Router();
                    var line = pick_list + "," + orig_distance;
                    for (int i = 1; i <= 5; i++)
                    {
                        if (reader.CheckDimensions())
                        {
                            router.Run(locations, distance_matrix, i);
                            line += ", " + router.GetRouteLength();
                        }
                    }
                    lines.Add(line);
                }
            }

            StreamWriter writer = new StreamWriter(File.Open(@"C:\Workspace\PickerRouting\Objective Outputs.csv", FileMode.Create), Encoding.UTF8);
            var header = "Pick List ID, Original Route, Greedy Descent, Guided Local Search, Simulated Annealing, Tabu Search, Objective Tabu Search";
            writer.WriteLine(header);
            foreach (var line in lines)
            {
                writer.WriteLine(line);
            }
            writer.Close();
        }
    }
}

//var id = "TL586883";                                         
//parameters gibi bir class olsun mu?
//meta için enumeration olsun mu?