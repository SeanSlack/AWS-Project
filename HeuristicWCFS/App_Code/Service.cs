using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.ServiceModel;
using System.ServiceModel.Web;
using System.Text;
using WCFSLibrary;
using System.Diagnostics;
using System.Threading;

public class Service : IService
{
    public AllocationData GetAllocationsHeuristic(int timeout, ConfigData cd, int ID)
    {
        //Create new allocation object, populate number of tasks, get already invalid tasks,
        // runtime and energy values and convert arrays to 2d

        AllocationData allocationData = new AllocationData();
        int numberOfProcessors = cd.NumberOfProcessors;
        int numberOfTasks = cd.NumberOfTasks;
        int rowLast = numberOfProcessors - 1;
        int colLeft;
        int colRight;
        bool addLeft;
        bool addRight;

        int[] invalidAllocation1D = cd.InvalidAllocations;
        double[] runtimes1D = cd.AllocatedRuntimes;

        double[] energies1D = cd.Energies;

        int[,] invalidAllocations = ConvertTo2D(invalidAllocation1D, numberOfProcessors, numberOfTasks);
        double[,] runtimeMatrix = ConvertTo2D(runtimes1D, numberOfProcessors, numberOfTasks);
        double[,] energyMatrix = ConvertTo2D(energies1D, numberOfProcessors, numberOfTasks);

        bool allocationFound = false;
        Stopwatch stopwatch = new Stopwatch();
        allocationData.Count = ID;

        stopwatch.Start();

        while (!allocationFound)
        {
            int tasksAllocated = 0;
            double totalEnergy = 0;

            int[] processorFull = new int[numberOfProcessors];
            int[] taskAllocated = new int[numberOfTasks];
            double[] totalProcessorRuntimes = new double[numberOfProcessors];
            int[,] allocationMatrix = invalidAllocations.Clone() as int[,];
            allocationMatrix = RandomiseMatrix(allocationMatrix, numberOfProcessors, numberOfTasks);

            //Search starting from first processor, adding right elements first and filling with smaller energy element
            //from the left until it it gets as close to duration as possible, then moves onto the next processor.
            for (int p = 0; p < numberOfProcessors; p++)
            {
                colLeft = 0;
                colRight = numberOfTasks - 1;
                addLeft = false;
                addRight = true;
                while (colLeft <= colRight && processorFull[p] == 0)
                {
                    if (addRight == true)
                    {
                        if (taskAllocated[colRight] == 0 && allocationMatrix[p, colRight] == 0)
                        {
                            totalProcessorRuntimes[p] += runtimeMatrix[p, colRight];

                            if (stopwatch.ElapsedMilliseconds > timeout) //stop searching if time limit is reached
                            {
                                string message = "Server operation timed out";
                                throw new FaultException<TimeoutFault>(new TimeoutFault(message));
                            }
                            else if (totalProcessorRuntimes[p] > cd.Duration) // processor full, remove added runtime
                            {
                                totalProcessorRuntimes[p] -= runtimeMatrix[p, colRight];
                            }
                            else // allocate task
                            {
                                allocationMatrix[p, colRight] = 1;
                                taskAllocated[colRight] = 1;
                                totalEnergy += energyMatrix[p, colRight];
                                tasksAllocated++;
                            }
                            addRight = false;
                            addLeft = true;
                        }
                        colRight--;
                    }
                    // once right side is completed if this hits -1 ends up in infinite loop
                    if (addLeft == true)
                    {
                        if (taskAllocated[colLeft] == 0 && allocationMatrix[p, colLeft] == 0)
                        {
                            totalProcessorRuntimes[p] += runtimeMatrix[p, colLeft];

                            if (stopwatch.ElapsedMilliseconds > timeout) //stop searching if time limit is reached
                            {
                                string message = "Server operation timed out";
                                throw new FaultException<TimeoutFault>(new TimeoutFault(message));
                            }
                            else if (totalProcessorRuntimes[p] > cd.Duration) // processor full, remove added runtime
                            {
                                totalProcessorRuntimes[p] -= runtimeMatrix[p, colLeft];
                                processorFull[p] = 1;
                            }
                            else // allocate task
                            {
                                allocationMatrix[p, colLeft] = 1;
                                taskAllocated[colLeft] = 1;
                                totalEnergy += energyMatrix[p, colLeft];
                                tasksAllocated++;
                            }
                            addRight = true;
                            addLeft = false;
                        }
                        colLeft++;
                    }
                }
            }

            if (tasksAllocated == numberOfTasks)
            {
                allocationData.Map = ConvertTo1D(allocationMatrix, numberOfProcessors, numberOfTasks);
                allocationData.Energy = totalEnergy;
                allocationFound = true;
            }
        }

        return allocationData;
    }

    //Counts the number of already invalid tasks, adds randomization to search space taking this into consideration
    // Also limits the amount this space is reduced depending on number of tasks and processor.
    private int[,] RandomiseMatrix(int[,] matrix, int rows, int cols)
    {
        var random = new ThreadLocal<Random>(() => new Random(Guid.NewGuid().GetHashCode()));
        int[,] newMatrix = matrix;

        for (int col = 0; col < cols; col++)
        {
            int count = 0;
            double reduce = (double)rows/4;
            int numberToRemove = (int)Math.Ceiling(reduce);

            //dont reduce anymore if already too many invalid tasks in column
            for (int row = 0; row < rows; row++)
            {
                if (newMatrix[row, col] == -1)
                {
                    count++;
                }
            }

            while (count < numberToRemove)
            {
                int row = random.Value.Next(0, rows);
                if (newMatrix[row, col] == 0)
                {
                    newMatrix[row, col] = -1;
                    count++;
                }
            }
        }
        return newMatrix;
    }

    private int[] ConvertTo1D(int[,] matrix, int rows, int cols)
    {
        int[] matrix1D = new int[rows * cols];
        int index = 0;

        for (int row = 0; row < rows; row++)
        {
            for (int col = 0; col < cols; col++)
            {
                matrix1D[index] = matrix[row, col];
                index++;
            }
        }
        return matrix1D;
    }

    private int[,] ConvertTo2D(int[] matrixData, int rows, int cols)
    {
        int[,] matrix2D = new int[rows, cols];
        int index = 0;
        for (int row = 0; row < rows; row++)
        {
            for (int col = 0; col < cols; col++)
            {
                matrix2D[row, col] = matrixData[index];
                index++;
            }
        }
        return matrix2D;
    }

    private double[,] ConvertTo2D(double[] matrixData, int rows, int cols)
    {
        double[,] matrix2D = new double[rows, cols];
        int index = 0;
        for (int row = 0; row < rows; row++)
        {
            for (int col = 0; col < cols; col++)
            {
                matrix2D[row, col] = matrixData[index];
                index++;
            }
        }
        return matrix2D;
    }
}
