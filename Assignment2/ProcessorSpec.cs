using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Assignment2
{
    public class ProcessorSpec
    {
        public string Name { get; set;}
        public double C2 { get; set; }
        public double C1 { get; set; }
        public double C0 { get; set; }

        public ProcessorSpec(string name, double c2, double c1, double c0)
        {
            Name = name;
            C2 = c2;
            C1 = c1;
            C0 = c0;
        }
        public double EnergyPerSecond(double frequency)
        {
            return C2 * frequency * frequency + C1 * frequency + C0;
        }

        public double Energy(double frequency, double runtime)
        {
            return ((EnergyPerSecond(frequency)) * runtime);
        }
    }
}
