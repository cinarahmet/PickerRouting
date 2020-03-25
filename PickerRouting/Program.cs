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

            StreamWriter writer = new StreamWriter(File.Open(@"C:\Workspace\PickerRouting\Objective Outputs.csv", FileMode.Create), Encoding.UTF8);
            var header = "Pick List ID, Original Route, Greedy Descent, Guided Local Search, Simulated Annealing, Tabu Search, Objective Tabu Search";
            writer.WriteLine(header);

            StreamWriter writer2 = new StreamWriter(File.Open(@"C:\Workspace\PickerRouting\Problem Locations.csv", FileMode.Create), Encoding.UTF8);

            var problemLocations = new List<string>();

            using (var sr = File.OpenText(@"C:\Workspace\PickerRouting\süpermartpicklists.csv"))
            {
                var s = sr.ReadLine();
                while ((s = sr.ReadLine()) != null)
                {

                    var pick_list = s.Split(',')[0];

                    var newProblemLocations = new List<string>();

                    var reader = new SQLReader(pick_list);
                    reader.Read();
                    var locations = reader.GetLocations();
                    //var locations = reader.GetLocations().Select(x => x.ToUpper()).ToList();
                    var distance_matrix = reader.GetDistanceMatrix();

                    if (reader.CheckDimensions() && locations.Count>0)
                    {
                        var orig_distance = reader.GetRouteDistance();

                        var line = pick_list + "," + orig_distance;
                        for (int i = 1; i <= 5; i++)
                        {
                            if (reader.CheckDimensions())
                            {
                                var router = new Router();
                                router.Run(locations, distance_matrix, (Router.Metas)i);
                                line += ", " + router.GetRouteLength();
                            }
                        }

                        writer.WriteLine(line);
                        writer.Flush();
                    }

                    else
                    {
                        newProblemLocations = problemLocations.Union(locations.Except(distance_matrix.Keys).ToList()).ToList().Except(problemLocations).ToList();
                    }

                    foreach (var location in newProblemLocations)
                    {
                        writer2.WriteLine(location);
                        writer2.Flush();
                    }
                    
                    problemLocations = problemLocations.Union(newProblemLocations).ToList();
                }
            }

            writer.Close();
            writer2.Close();
        }
    }
}

//var id = "TL586883";                                         
//parameters gibi bir class olsun mu?
//meta için enumeration olsun mu?