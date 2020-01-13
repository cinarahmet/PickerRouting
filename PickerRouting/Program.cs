using System;
using System.Collections.Generic;
using System.Linq;

namespace PickerRouting
{
    class Program
    {
        static void Main(string[] args)
        {
            var reader = new SQLReader("TL571210");
            reader.Read();
            var locations = reader.GetLocations();
            Int32 n = 50;
            locations = locations.Take(n).ToList();
            var d = reader.GetDistanceMatrix();
            var originalLocations = reader.GetOriginalPickLocations();
            originalLocations = originalLocations.Take(n).ToList();

            Console.WriteLine("************************");
            foreach (var element in originalLocations)
                Console.WriteLine(element);
            Console.WriteLine("***********************");
            reader.CalculateOriginalRouteDistance(n);
            var originalRouteDistance = reader.GetRouteDistance();
            Console.WriteLine("************************");
            Console.WriteLine(originalRouteDistance);
            Console.WriteLine("************************");

            Console.WriteLine("\nBase Model\n");
            var model = new Model(originalLocations, d);
            model.Run();
            var sequence = model.GetSequence();
            Console.WriteLine("\nTree Model\n");

            var Tmodel = new TreeModel(originalLocations, d);
            Tmodel.Run(sequence);
            Console.WriteLine("Hello World!");
            Console.ReadKey();
        }
    }
}