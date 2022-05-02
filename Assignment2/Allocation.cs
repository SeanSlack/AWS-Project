using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;

namespace Assignment2
{
    public class Allocation
    {
        // Properties for a single allocation
        public int ID { get; set; }
        public int[,] Map { get; set; }
        //public string[] MapForPrinting { get; set; }
        public double ProgramRuntime { get; set; }
        public double Energy { get; set; }
        public List<Task> TaskList { get; set; }

        public List<Processor> ProcessorList { get; set; }

        public Allocation()
        {
            TaskList = new List<Task>();
            ProcessorList = new List<Processor>();
        }

        public Allocation(int id, int[,] map)
        {
            ID = id;
            Map = map;
            TaskList = new List<Task>();
            ProcessorList = new List<Processor>();
            //MapForPrinting = mapPrint;
        }

        public void AddTaskList(List<Task> tasks)
        {
            List<Task> oldList = new List<Task>(tasks);
            List<Task> newList = new List<Task>(oldList.Count);

            oldList.ForEach((item) =>
            {
                newList.Add(new Task(item));
            });

            TaskList = newList;
        }

        public void AddProcessorList(List<Processor> processors)
        {
            List<Processor> oldList = new List<Processor>(processors);
            List<Processor> newList = new List<Processor>(oldList.Count);

            oldList.ForEach((item) =>
            {
                newList.Add(new Processor(item));
            });

            ProcessorList = newList;
        }

        /* 
         * Method:
         * Assigns each task from the TaskList to a processor in ProcessorList, using the 2D Array Map
        */
        public void AssignTasksToProcessor()
        {
            int procRows = Map.GetLength(0);
            int taskCols = Map.GetLength(1);

            for (int procRow = 0; procRow < procRows; procRow++)
            {
                for (int taskCol = 0; taskCol < taskCols; taskCol++)
                {
                    if (Map[procRow, taskCol] == 1)
                    {
                        foreach (Task task in TaskList)
                        {
                            foreach (Processor processor in ProcessorList)
                            {
                                if (Map[procRow, taskCol] == 1 && task.ID == taskCol && processor.ID == procRow)
                                {
                                    processor.AddTask(task);
                                }
                            }
                        }
                    }
                }
            }
        }

        /* 
         * Method:
         * Loops through each processor, retrieves the Total Runtime of each task allocated and find the maximum
         */
        public double CalculateProgramRuntime()
        {
            double longestRuntime = 0;
            double currentRuntime;

            foreach (Processor processor in ProcessorList)
            {
                currentRuntime = processor.TotalTaskRunTime();
                if (longestRuntime < currentRuntime)
                {
                    longestRuntime = currentRuntime;
                }
            }
            ProgramRuntime = longestRuntime;

            return ProgramRuntime;
        }

        /* 
         * Method:
         * Loops through each processor and calculates total energy using frequency, cooefficients and total runtime.
         */
        public double CalculateTotalEnergy()
        {
            double processorEnergy = 0;

            foreach (Processor processor in ProcessorList)
            {
                    processorEnergy += processor.ProcessorSpec.Energy(processor.Frequency, processor.TotalTaskRunTime());
            }

            Energy = processorEnergy;

            return processorEnergy;
        }

        /* Method:
        * Loops through the allocation map to check if a task is not allocated to any processor
        * or if a task has been allocated to multiple processors.
        * 
        * Output:
        * Returns a boolean value depending wether the file is valid or invalid
        */
        public bool ValidateAllocationMap()
        {
            bool valid = true;
            int procRows = Map.GetLength(1);
            int taskCols = Map.GetLength(0);
            Debug.Print("Proc Rows: " + procRows);
            Debug.Print("Task Cols: " + taskCols);

            List<int> taskSum = new List<int>();

            for (int procRow = 0; procRow < procRows; procRow++)
            {
                int sum = 0;
                for (int taskCol = 0; taskCol < taskCols; taskCol++)
                {
                    if (Map[taskCol, procRow] == 1) // ignore -1 in matrix
                    {
                        sum = sum + Map[taskCol, procRow];
                    }
                }
                taskSum.Add(sum);
            }

            int taskID = 0;
            foreach (int task in taskSum)
            {
                if (task > 1)
                {
                    ErrorLog.LogError(String.Format(Errors.ALLOCATION_MORE_TASKS, task, ID));
                    valid = false;
                }
                else if (task < 1)
                {
                    ErrorLog.LogError(String.Format(Errors.ALLOCATION_LESS_TASKS, task));
                    valid = false;
                }
                taskID++;
            }

            return valid;
        }
    }
}
