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
            var pick_list = "TL622267";
            var meta = 2;

            var reader = new SQLReader(pick_list);
            reader.Read();

            var locations = reader.GetLocations();
            var distance_matrix = reader.GetDistanceMatrix();

            var router = new Router();
            router.Run(locations, distance_matrix, (Router.Metas)meta);
            var route = router.GetRoute();
        }
    }
}