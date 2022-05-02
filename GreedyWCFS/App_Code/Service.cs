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
	public AllocationData GetAllocationsGreedy(int timeout, ConfigData cd, int ID)
	{
        //Create new allocation object, populate number of tasks, get already invalid tasks,
        // runtime and energy values and convert arrays to 2d

		AllocationData allocationData = new AllocationData();
        var random = new ThreadLocal<Random>(() => new Random(ID*(Guid.NewGuid().GetHashCode())));

        int numberOfProcessors = cd.NumberOfProcessors;
        int numberOfTasks = cd.NumberOfTasks;
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

        //Randomizes the allocation matrix, then tries first task on first processor, second on first etc
        while (!allocationFound)
        {
            int newRandom = random.Value.Next();
            int tasksAllocated = 0;
            int[] processorFull = new int[numberOfProcessors];
            int[] taskAllocated = new int[numberOfTasks];
            double totalEnergy = 0;
            double[] totalProcessorRuntimes = new double[numberOfProcessors];
            int[,] allocationMatrix = invalidAllocations.Clone() as int[,];

            allocationMatrix = RandomiseMatrix(allocationMatrix, numberOfProcessors, numberOfTasks, newRandom);

            for (int p = 0; p < numberOfProcessors; p++)
            {
                for (int t = 0; t < numberOfTasks && processorFull[p] == 0; t++)
                {
                    if (taskAllocated[t] == 0 && allocationMatrix[p, t] == 0)
                    {
                        totalProcessorRuntimes[p] += runtimeMatrix[p, t];

                        if (stopwatch.ElapsedMilliseconds > timeout) //stop searching if time limit is reached
                        {
                            string message = "Server operation timed out";
                            throw new FaultException<TimeoutFault>(new TimeoutFault(message));
                        }
                        else if (totalProcessorRuntimes[p] > cd.Duration) // processor full, remove added runtime
                        {
                            totalProcessorRuntimes[p] -= runtimeMatrix[p, t];
                            processorFull[p] = 1;
                        }
                        else // allocate task
                        {
                            allocationMatrix[p, t] = 1;
                            totalEnergy += energyMatrix[p, t];
                            taskAllocated[t] = 1;
                            tasksAllocated++;
                        }
                    }
                }
            }
            //If allocation found add map and energy valies, if not search for another allocation
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
    private int[,] RandomiseMatrix(int[,] matrix, int rows, int cols, int rand)
    {
        var random = new ThreadLocal<Random>(() => new Random(rand*(Guid.NewGuid().GetHashCode())));
        int[,] newMatrix = matrix;

        for (int col = 0; col < cols; col++)
        {
            int count = 0;
            double reduce = (double)rows / 4;
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

    private int[] ConvertTo1D(int [,] matrix, int rows, int cols)
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
