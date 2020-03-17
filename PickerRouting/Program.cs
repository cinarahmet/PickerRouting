using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using Google.OrTools.ConstraintSolver;

namespace PickerRouting
{
    class Program
    {
        static void Main(string[] args)
        {
            var id = "TL586883";

            var reader = new SQLReader(id);
            reader.Read();

            var router = new Router();

            //parameters gibi bir class olsun mu?
            //meta için enumeration olsun mu?
            var locations = reader.GetLocations();
            var distance = reader.GetDistanceMatrix();
            var orig_distance = reader.GetRouteDistance();
            Console.WriteLine("The initial route length that picker had {0}",orig_distance);


            for (int i = 1; i < 5; i++)
            {
                var meta = i;

                if (reader.CheckDimensions())
                {
                    router.Run(locations, distance);
                }
                else
                {
                    //discuss
                }

                var route = router.GetRoute();
                var objective = router.GetRouteLength();

                Console.WriteLine($"{objective}");
                
            }
            

        }

    }
}