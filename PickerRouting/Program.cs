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
            Test test = new Test(@"C:\Users\yahya.geckil\Desktop\vv.csv", 20);
            test.Run();
        }

    }
}