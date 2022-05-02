using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Assignment2
{
    public class Task
    {
        public int ID { get; set; }
        public double Runtime { get; set; }
        public double RefFrequency { get; set; }
        public int RequiredRam { get; set; }
        public int RequiredDownload { get; set; }
        public int RequiredUpload { get; set; }

        public double[] LocalEnergy { get; set; }
        public double[] RemoteEnergy { get; set; }

        public Task() { }
        public Task(int id, double runtime, double refFreq, int reqRam, int reqDownload, int reqUpload)
        {
            ID = id;
            Runtime = runtime;
            RefFrequency = refFreq;
            RequiredRam = reqRam;
            RequiredDownload = reqDownload;
            RequiredUpload = reqUpload;
        }

        public Task(Task task)
        {
            ID = task.ID;
            Runtime = task.Runtime;
            RefFrequency = task.RefFrequency;
            RequiredRam = task.RequiredRam;
            RequiredDownload = task.RequiredDownload;
            RequiredUpload = task.RequiredUpload;
        }

        public double CalculateLocalEnergy()
        {
            double energy = 0;

            foreach (double task in LocalEnergy)
            {
                energy += task;
            }

            return energy;
        }

        public double CalculateRemoteEnergy()
        {
            double energy = 0;

            foreach (double task in RemoteEnergy)
            {
                energy += task;
            }

            return energy;
        }
    }
}
