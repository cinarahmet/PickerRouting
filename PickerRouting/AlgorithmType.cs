using System;
using System.Collections.Generic;
using System.Text;

namespace PickerRouting
{
    public class AlgorithmType
    {
        public enum Metas
        {
            GreedyDescent = 1,
            GuidedLocalSearch = 2,
            SimulatedAnnealing = 3,
            TabuSearch = 4,
            ObjectiveTabuSearch = 5
        }
    }
}
