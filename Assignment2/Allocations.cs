using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Diagnostics;
using System.Text.RegularExpressions;

namespace Assignment2
{
    public class Allocations
    {
        // Properties of TAFF File to assign during validation
        public string ConfigFile { get; set; }
        public int Count { get; set; }
        public int NumberOfTasks { get; set; }
        public int NumberOfProcessors { get; set; }
        public int AllocationID { get; set; }
        public int[,] AllocationMap { get; set; }
        public string[] MapPrint { get; set; }
        public string stringCalculations { get; set; }
        public string stringTaskAllocations { get; set; }
        public int ErrorNum { get; set; }

        public List<Allocation> AllocationList { get; set; }
        public Configuration Configuration { get; }

        // Constants
        private const string END_BLOCK = "END-";
        private const string ERROR_LINE = " - Line: ";
        private const string ERROR_NUM = "ERROR ";
        private const string COMMENT = "//";
        private const Char EQUALS = '=';
        private const Char DOUBLE_QUOTE = '"';
        private const Char SEMI_COLAN = ';';
        private const string COLAN = ":";
        private const Char COMMA = ',';
        private const int START = 0;
        private const int VALUE_INDEX = 1;
        private const int FILENAME_INDEX = 1;
        private const int EXPECTED_ALLOCATION_LINES = 2;
        private const int EXPECTED_ALLOCATIONS_LINES = 3;

        public int[,] CreateAllocationMatrix(int[] allocationMap, int rows, int cols)
        {
            int[,] allocationMatrix = new int[rows, cols];
            int index = 0;
            for (int row = 0; row < rows; row++)
            {
                for (int col = 0; col < cols; col++)
                {
                    if (allocationMap[index] != -1) //skips invalid task allocations used for web service
                    {
                        allocationMatrix[row, col] = allocationMap[index];
                    }
                    index++;
                }
            }

            return allocationMatrix;
        }

        public Allocations()
        {
            AllocationList = new List<Allocation>();
        }

        /* Validates Allocations after validating both CFF and TAFF Files.
         * Includes checking if Download, Upload, Ram is suffienct for the tasks that have been assigned
         * as well as checking if a task has been allocated to more than one processor.
         * 
         * Output:
         * Returns a boolean value depending wether the set of allocations are valid or invalid
         */
        public Boolean ValidateAllocations()
        {
            Boolean valid = true;
            ErrorNum = START;

            foreach (Allocation allocation in AllocationList)
            {
                foreach (Processor processor in allocation.ProcessorList)
                {
                    if (!processor.IsDownloadSufficient())
                    {
                        ErrorLog.LogError(String.Format(ERROR_NUM + ErrorNum++ + COLAN + Errors.ALLOCATION_DOWNLOAD, allocation.ID, processor.ID));
                        valid = false;
                    }

                    if (!processor.IsUploadSufficient())
                    {
                        ErrorLog.LogError(String.Format(ERROR_NUM + ErrorNum++ + COLAN + Errors.ALLOCATION_UPLOAD, allocation.ID, processor.ID));
                        valid = false;
                    }

                    if (!processor.IsRamSufficient())
                    {
                        ErrorLog.LogError(String.Format(ERROR_NUM + ErrorNum++ + COLAN + Errors.ALLOCATION_UPLOAD, allocation.ID, processor.ID));
                        valid = false;
                    }
                }
                if (!allocation.ValidateAllocationMap())
                {
                    valid = false;
                }
            }
            return valid;
        }

        /* Method:
         * Loops through the Taff File line by line, assigns each of the properties as they are
         * picked up in the file and checks for any invalid lines.
         * 
         * Input: 
         * TAFFfile - Parses the name of the TAFF File for checking
         * Configuration - Data from a validated CFF File
         * 
         * Output:
         * valid - Returns a boolean value depending wether the file is valid or invalid
        */ 
        public Boolean ValidateTAFF(string TAFFfile, Configuration configuration)
        {
            Boolean valid = true;
            string currentBlock = null;
            string currentSubBlock = null;
            int validatedLines = 0;
            int validAllocationLines = 0;
            int validatedSubBlocks = 0;
            int expectedSubBlocks = 0;

            StreamReader taffFile = new StreamReader(TAFFfile);
            string line;

            AllocationList = new List<Allocation>();

            while (!taffFile.EndOfStream)
            {
                line = taffFile.ReadLine().Trim();
                if (line.Length == 0)
                {
                    //Blank line, do nothing
                }
                else if (line.StartsWith(COMMENT))
                {
                    //Comment line, do nothing
                }
                else if (line.StartsWith(AllocationKeys.CONFIG_DATA))
                {
                    //Already checked (add check in GetCFFname)
                }
                else if (line.StartsWith(AllocationKeys.FILENAME))
                {
                    //Already checked
                }
                else if (line.StartsWith(END_BLOCK + AllocationKeys.CONFIG_DATA))
                {
                    //Already checked (add check in GetCFFname)
                }
                else if (line.StartsWith(AllocationKeys.ALLOCATIONS))
                {
                    currentBlock = AllocationKeys.ALLOCATIONS;
                    validatedLines = 0;
                    validatedSubBlocks = 0;
                }
                else if (line.StartsWith(AllocationKeys.NUM_ALLOCATIONS))
                {
                    string[] lineData = line.Split(new Char[] { EQUALS });
                    int tempInt;
                    int.TryParse(lineData[VALUE_INDEX], out tempInt);
                    Count = tempInt;
                    expectedSubBlocks = Count;
                    validatedLines++;
                }
                else if (line.StartsWith(AllocationKeys.NUM_TASKS))
                {
                    string[] lineData = line.Split(new Char[] { EQUALS });
                    int tempInt;
                    int.TryParse(lineData[VALUE_INDEX], out tempInt);
                    NumberOfTasks = tempInt;
                    validatedLines++;
                }
                else if (line.StartsWith(AllocationKeys.NUM_PROC))
                {
                    string[] lineData = line.Split(new Char[] { EQUALS });
                    int tempInt;
                    int.TryParse(lineData[VALUE_INDEX], out tempInt);
                    NumberOfProcessors = tempInt;
                    validatedLines++;
                }
                else if (line.Equals(AllocationKeys.ALLOCATION))
                {
                    currentSubBlock = AllocationKeys.ALLOCATION;
                    validAllocationLines = 0;
                }
                else if (line.StartsWith(AllocationKeys.ALLOCATION_ID))
                {
                    string[] lineData = line.Split(new Char[] { EQUALS });

                    if (lineData[VALUE_INDEX] == null) { ErrorLog.LogError(ERROR_NUM + ErrorNum++ + COLAN + Errors.ID_MISSING + ERROR_LINE + line); }

                    if (int.TryParse(lineData[VALUE_INDEX], out int tempInt))
                    {
                        AllocationID = tempInt;
                        validAllocationLines++;
                    }
                    else
                    {
                        ErrorLog.LogError(ERROR_NUM + ErrorNum++ + COLAN + Errors.NOT_INTEGER + ERROR_LINE + line);
                    }
                }
                else if (Regex.IsMatch(line, RegexStrings.ALLOCATION_MAP))
                {
                    string[] lineData = line.Split(new Char[] { EQUALS });
                    string allocationMap = lineData[VALUE_INDEX];

                    string[] mapProcessors = allocationMap.Split(new char[] { SEMI_COLAN });
                    MapPrint = mapProcessors;

                    AllocationMap = CreateMapArray(mapProcessors, line);
                    validAllocationLines++;

                    if (AllocationMap == null)
                    {
                        valid = false;
                    }
                }
                else if (line.Equals(END_BLOCK + AllocationKeys.ALLOCATION))
                {
                    if (currentSubBlock != AllocationKeys.ALLOCATION)
                    {
                        ErrorLog.LogError(ERROR_NUM + ErrorNum++ + COLAN + Errors.ALLOCATION_HEADER + AllocationID);
                        valid = false;
                    }

                    if (validAllocationLines == EXPECTED_ALLOCATION_LINES)
                    {
                        validatedSubBlocks++;
                    }
                    else
                    {
                        valid = false;
                    }

                    Allocation newAllocation = new Allocation(AllocationID, AllocationMap);

                    if (AllocationMap != null)
                    {
                        newAllocation.AddTaskList(configuration.Tasks);
                        newAllocation.AddProcessorList(configuration.Processors);
                        newAllocation.AssignTasksToProcessor();
                        newAllocation.CalculateTotalEnergy();
                        newAllocation.CalculateProgramRuntime();
                        AllocationList.Add(newAllocation);
                    }

                    currentSubBlock = null;
                }
                else if (line.StartsWith(END_BLOCK + AllocationKeys.ALLOCATIONS))
                {
                    if (currentBlock != AllocationKeys.ALLOCATIONS)
                    {
                        ErrorLog.LogError(ERROR_NUM + ErrorNum++ + COLAN + Errors.ALLOCATIONS_HEADER);
                        valid = false;
                    }

                    if (validatedLines == EXPECTED_ALLOCATIONS_LINES)
                    {
                        //Valid
                    }
                    else
                    {
                        valid = false;
                    }

                    if (validatedSubBlocks == expectedSubBlocks)
                    {
                        //Valid
                    }
                    else if (validatedSubBlocks > expectedSubBlocks)
                    {
                        ErrorLog.LogError(ERROR_NUM + ErrorNum++ + COLAN + Errors.MORE_ALLOCATIONS);
                        valid = false;
                    }
                    else
                    {
                        ErrorLog.LogError(ERROR_NUM + ErrorNum++ + COLAN + Errors.LESS_ALLOCATIONS );
                        valid = false;
                    }

                    currentBlock = null;
                }
                else
                {
                    ErrorLog.LogError(ERROR_NUM + ErrorNum++ + COLAN + Errors.LINE_ERROR + ERROR_LINE + line);
                    valid = false;
                }
            }
            return (valid);
        }


        /* Method:
         * Creates a 2D array from the allocations map string
         * 
         * Input: 
         * map - Parses the map string for processing
         * line - Current line from file for error logging
         * 
         * Output:
         * mapArray - Returns the 2D Array with map data
        */
        public int[,] CreateMapArray(string[] map, string line)
        {
            int[,] mapArray = new int[NumberOfProcessors, NumberOfTasks];
            int processorCount = 0;

            if (map.Length != NumberOfProcessors)
            {
                ErrorLog.LogError(ERROR_NUM + ErrorNum++ + COLAN + Errors.ALLOCATION_MAP_PROCESSORS + ERROR_LINE + line);
                return null;
            }

            foreach (string processor in map)
            {
                int taskCount = 0;
                string[] taskData = processor.Split(new char[] { COMMA });

                if (taskData.Length == NumberOfTasks)
                {
                    foreach (string task in taskData)
                    {
                        int.TryParse(task, out mapArray[processorCount, taskCount]);
                        taskCount++;
                    }
                    processorCount++;
                }
                else
                {
                    ErrorLog.LogError(ERROR_NUM + ErrorNum++ + COLAN + Errors.ALLOCATION_MAP_TASKS + ERROR_LINE + line);
                    return null;
                }
            }

            return mapArray;
        }

        /* Method:
        // * Retrieves the CFF Filename from TAFF File
        // * 
        // * Input: 
        // * TAFFfile - The TAFF File for processing
        // * 
        // * Output:
        // * ConfigFile - Returns the name of the configuration file
        */
        public String GetCFFname(string TAFFfile)
        {
            StreamReader taffFile = new StreamReader(TAFFfile);
            string line;
            Boolean fileFound = false;

            while (!taffFile.EndOfStream)
            {
                line = taffFile.ReadLine().Trim();
                if (line.StartsWith(AllocationKeys.FILENAME))
                {
                    fileFound = true;
                    string[] lineData = line.Split(new Char[] { EQUALS });
                    ConfigFile = lineData[FILENAME_INDEX].Trim(new Char[] { DOUBLE_QUOTE });
                    break;
                }
            }

            taffFile.Close();
            if (fileFound)
                return (ConfigFile);
            else
                return (null);
        }
    }
}
