using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;

namespace Assignment2
{
    public class Processor
    {
        // Processor properties
        public int ID { get; set; }
        public string Type { get; set; }
        public double Frequency { get; set; }
        public int Ram { get; set; }
        public int DownloadSpeed { get; set; }
        public int UploadSpeed { get; set; }
        public ProcessorSpec ProcessorSpec { get; set; }
        public List<Task> TasksAllocated { get; set; }

        public Processor() { }

        // Creates a new processor with an empty list of allocated tasks
        public Processor(int id, string type, double freq, int ram, int downloadSpeed, int uploadSpeed)
        {
            ID = id;
            Type = type;
            Frequency = freq;
            Ram = ram;
            DownloadSpeed = downloadSpeed;
            UploadSpeed = uploadSpeed;
            TasksAllocated = new List<Task>();
        }

        public Processor(Processor processor)
        {
            ID = processor.ID;
            Type = processor.Type;
            Frequency = processor.Frequency;
            Ram = processor.Ram;
            DownloadSpeed = processor.DownloadSpeed;
            UploadSpeed = processor.UploadSpeed;
            ProcessorSpec = processor.ProcessorSpec;
            TasksAllocated = new List<Task>();
        }

        public void AddTask(Task task)
        {
            TasksAllocated.Add(task);
        }

        public void AddType(ProcessorSpec procSpec)
        {
            ProcessorSpec = procSpec;
        }

        public int RequiredDownloadSpeed()
        {
            int largestDownload = 0;
            int currentDownload;

            foreach (Task task in TasksAllocated)
            {
                currentDownload = task.RequiredDownload;

                if (largestDownload < currentDownload)
                {
                        largestDownload = currentDownload;
                }
            }
            return largestDownload;
        }

        public int RequiredUploadSpeed()
        {
            int largestUpload = 0;
            int currentUpload;

            foreach (Task task in TasksAllocated)
            {
                currentUpload = task.RequiredUpload;

                if (largestUpload < currentUpload)
                {
                    largestUpload = currentUpload;
                }
            }
            return largestUpload;
        }

        public int RequiredRam()
        {
            int largestRam = 0;
            int currentRam;
            foreach (Task task in TasksAllocated)
            {
                currentRam = task.RequiredRam;

                if (largestRam < currentRam)
                {
                    largestRam = currentRam;
                }
            }
            return largestRam;
        }

        public double TotalTaskRunTime()
        {
            double totalTaskRuntime = 0;

            foreach (Task task in TasksAllocated)
            {
                double taskRuntime = task.Runtime * (task.RefFrequency / Frequency);
                totalTaskRuntime += taskRuntime;
            }
            return totalTaskRuntime;
        }

        public Boolean IsRamSufficient()
        {
            if (RequiredRam() > Ram)
            {
                return false;
            }
            return true;
        }

        public Boolean IsDownloadSufficient()
        {
            if (RequiredDownloadSpeed() > DownloadSpeed)
            {
                return false;
            }
            return true;
        }

        public Boolean IsUploadSufficient()
        {
            if (RequiredUploadSpeed() > UploadSpeed)
            {
                return false;
            }
            return true;
        }
    }
}
