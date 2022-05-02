using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Diagnostics;
using System.Text.RegularExpressions;
using System.Net;

namespace Assignment2
{
    public class Configuration
    {
        //Limit propeties
        public string LogFile { get; set; }
        public int MinTasks { get; set; }
        public int MaxTasks { get; set; }
        public int MinProc { get; set; }
        public int MaxProc { get; set; }
        public double MinProcFreq { get; set; }
        public double MaxProcFreq { get; set; }
        public int MinRam { get; set; }
        public int MaxRam { get; set; }
        public int MinDownload { get; set; }
        public int MaxDownload { get; set; }
        public int MinUpload { get; set; }
        public int MaxUpload { get; set; }

        //Program properties
        public double MaxDuration { get; set; }
        public int NumOfTasks { get; set; }
        public int NumOfProc { get; set; }

        //Task runtime and requirments
        public int TaskID { get; set; }
        public double TaskRuntime { get; set; }
        public double TaskRefFreq { get; set; }
        public int RequiredRam { get; set; }
        public int RequiredDownload { get; set; }
        public int RequiredUpload { get; set; }

        //Processor specs
        public int ProcID { get; set; }
        public string ProcType { get; set; }
        public double ProcFreq { get; set; }
        public int ProcRam { get; set; }
        public int ProcDownload { get; set; }
        public int ProcUpload { get; set; }

        //Processor coefficients
        public string ProcName { get; set; }
        public double Coeff2 { get; set; }
        public double Coeff1 { get; set; }
        public double Coeff0 { get; set; }

        //Local and Remote enery units per task
        public string LocalMap { get; set; }
        public string RemoteMap { get; set; }

        public int ErrorNum { get; set; }

        public List<Task> Tasks { get; set; }
        public List<Processor> Processors { get; set; }
        public List<ProcessorSpec> ProcessorSpecs { get; set; }

        //Constants
        private const string END_BLOCK = "END-";
        private const string COMMENT = "//";
        private const Char EQUALS = '=';
        private const Char DOUBLE_QUOTE = '"';
        private const string ERROR_NUM = "ERROR ";
        private const string COLAN = ":";
        private const string ERROR_LINE = " - Line: ";
        private const int START = 0;
        private const int VALUE_INDEX = 1;
        private const int EXPECTED_LOGFILE_LINES = 1;
        private const int EXPECTED_LIMIT_LINES = 12;
        private const int EXPECTED_PROGRAM_LINES = 3;
        private const int EXPECTED_TASK_LINES = 6;
        private const int EXPECTED_PROCESSOR_LINES = 6;
        private const int EXPECTED_PROCESSOR_TYPE_LINES = 4;
        private const int EXPECTED_LOCAL_LINES = 1;
        private const int EXPECTED_REMOTE_LINES = 1;

        public Double[] CreateAllocatedRuntimeArray()
        {
            double taskRuntime;
            double[] runtimeArray = new double[NumOfTasks * NumOfProc];
            int i = 0;

            foreach (Processor processor in Processors)
                {
                foreach (Task task in Tasks)
                {
                    taskRuntime = task.Runtime * (task.RefFrequency / processor.Frequency);
                    runtimeArray[i] = taskRuntime;
                    i++;
                }
            }

            return runtimeArray;
        }

        public Double[] CreateEnergyArray()
        {
            double taskEnergy;
            double[] energyArray = new double[NumOfTasks * NumOfProc];
            int i = 0;

            foreach (Processor processor in Processors)
            {
                foreach (Task task in Tasks)
                {
                    taskEnergy = processor.ProcessorSpec.Energy(processor.Frequency, (task.Runtime * (task.RefFrequency / processor.Frequency)));
                    energyArray[i] = taskEnergy;
                    i++;
                }
            }

            return energyArray;
        }

        public Double[] CreateTaskRuntimeArray()
        {
            double taskRuntime;
            double[] runtimeArray = new double[NumOfTasks];
            int i = 0;

            foreach (Task task in Tasks)
            {
                taskRuntime = task.Runtime;
                runtimeArray[i] = taskRuntime;
                i++;
            }

            return runtimeArray;
        }

        //Creates a 1 dimensional array for all possible allocations to be used as a matrix,
        //initilizes to 0 for unallocated, sets allocations that do not meet requirement to -1
        public int[] CreateAllocationMatrixSerializable()
        {
            int[] allocationMatrix = new int[NumOfProc * NumOfTasks];

            int i = 0;
            foreach (Processor processor in Processors)
            {
                foreach (Task task in Tasks)
                {
                    if (task.RequiredRam > processor.Ram || task.RequiredDownload > processor.DownloadSpeed || task.RequiredUpload > processor.UploadSpeed)
                    {
                        allocationMatrix[i] = -1;
                    }
                    else
                    {
                        allocationMatrix[i] = 0;
                    }
                    i++;
                }
            }
            return allocationMatrix;
        }

        /* Validates Configutation file and assigns all properties for limits. Creates new processor, task
         * and processor type objects, then assigns the processor types to each processor.
         * Includes checking if Download, Upload, Ram is suffienct for the tasks that have been assigned
         * 
         * Output:
         * Returns a boolean value depending wether Configuration file is valid or invalid
         */
        public Boolean ValidateCFF(string CFFfile, bool isLocal)
        {
            Boolean valid = true;
            string currentBlock = null;
            string currentSubBlock = null;
            int validatedLines = 0;
            int validatedSubBlocks = 0;
            int expectedSubBlocks = 0;
            ErrorNum = START;

            Tasks = new List<Task>();
            Processors = new List<Processor>();
            ProcessorSpecs = new List<ProcessorSpec>();

            {
                StreamReader cffFile;
                if (isLocal)
                {
                    string dir = AppDomain.CurrentDomain.BaseDirectory;
                    cffFile = new StreamReader(dir + @"\Data Files\" + CFFfile);
                }
                else
                {
                    WebClient configWebClient = new WebClient();
                    Stream configSteam = configWebClient.OpenRead(CFFfile);
                    cffFile = new StreamReader(configSteam);
                }
                
                string line;

                while (!cffFile.EndOfStream)
                {
                    line = cffFile.ReadLine().Trim();
                    if (line.Length == 0)
                    {
                        //Blank line, do nothing
                    }
                    else if (line.StartsWith(COMMENT))
                    {
                        //Comment line, do nothing
                    }
                    else if (line.StartsWith(ConfigurationKeys.LOGFILE))
                    {
                        currentBlock = ConfigurationKeys.LOGFILE;
                        validatedLines = 0;
                    }
                    else if (Regex.IsMatch(line, RegexStrings.LOG_FILENAME))
                    {
                        string[] lineData = line.Split(new Char[] { EQUALS });
                        LogFile = lineData[VALUE_INDEX].Trim(new Char[] { DOUBLE_QUOTE });
                        validatedLines++;
                    }
                    else if (line.StartsWith(END_BLOCK + ConfigurationKeys.LOGFILE))
                    {

                        if (currentBlock != ConfigurationKeys.LOGFILE)
                        {
                            ErrorLog.LogError(ERROR_NUM + ErrorNum++ + COLAN + Errors.LOGFILE_HEADER);
                        }

                        if (validatedLines == EXPECTED_LOGFILE_LINES)
                        {
                            //Valid Block
                        }
                        else
                        {
                            ErrorLog.LogError(ERROR_NUM + ErrorNum++ + COLAN + Errors.LOGFILE);
                        }

                        currentBlock = null;
                    }
                    else if (line.StartsWith(ConfigurationKeys.LIMITS))
                    {
                        currentBlock = ConfigurationKeys.LIMITS;
                        validatedLines = 0;
                    }
                    else if (line.StartsWith(ConfigurationKeys.MIN_TASKS))
                    {
                        string[] lineData = line.Split(new Char[] { EQUALS });

                        if (int.TryParse(lineData[VALUE_INDEX], out int tempInt))
                        {
                            MinTasks = tempInt;
                            validatedLines++;
                        }
                        else
                        {
                            ErrorLog.LogError(ERROR_NUM + ErrorNum++ + COLAN + Errors.NOT_INTEGER + ERROR_LINE + line);
                        }
                    }
                    else if (line.StartsWith(ConfigurationKeys.MAX_TASKS))
                    {
                        string[] lineData = line.Split(new Char[] { EQUALS });

                        if (int.TryParse(lineData[VALUE_INDEX], out int tempInt))
                        {
                            MaxTasks = tempInt;
                            validatedLines++;
                        }
                        else
                        {
                            ErrorLog.LogError(ERROR_NUM + ErrorNum++ + COLAN + Errors.NOT_INTEGER + ERROR_LINE + line);
                        }
                    }
                    else if (line.StartsWith(ConfigurationKeys.MIN_PROC))
                    {
                        string[] lineData = line.Split(new Char[] { EQUALS });

                        if (int.TryParse(lineData[VALUE_INDEX], out int tempInt))
                        {
                            MinProc = tempInt;
                            validatedLines++;
                        }
                        else
                        {
                            ErrorLog.LogError(ERROR_NUM + ErrorNum++ + COLAN + Errors.NOT_INTEGER + ERROR_LINE + line);
                        }
                    }
                    else if (line.StartsWith(ConfigurationKeys.MAX_PROC))
                    {
                        string[] lineData = line.Split(new Char[] { EQUALS });

                        if (int.TryParse(lineData[VALUE_INDEX], out int tempInt))
                        {
                            MaxProc = tempInt;
                            validatedLines++;
                        }
                        else
                        {
                            ErrorLog.LogError(ERROR_NUM + ErrorNum++ + COLAN + Errors.NOT_INTEGER + ERROR_LINE + line);
                        }
                    }
                    else if (line.StartsWith(ConfigurationKeys.MIN_PROC_FREQ))
                    {
                        string[] lineData = line.Split(new Char[] { EQUALS });

                        if (double.TryParse(lineData[VALUE_INDEX], out double tempDouble))
                        {
                            MinProcFreq = tempDouble;
                            validatedLines++;
                        }
                        else
                        {
                            ErrorLog.LogError(ERROR_NUM + ErrorNum++ + COLAN + Errors.NOT_DOUBLE + ERROR_LINE + line);
                        }
                    }
                    else if (line.StartsWith(ConfigurationKeys.MAX_PROC_FREQ))
                    {
                        string[] lineData = line.Split(new Char[] { EQUALS });

                        if (double.TryParse(lineData[VALUE_INDEX], out double tempDouble))
                        {
                            MaxProcFreq = tempDouble;
                            validatedLines++;
                        }
                        else
                        {
                            ErrorLog.LogError(ERROR_NUM + ErrorNum++ + COLAN + Errors.NOT_DOUBLE + ERROR_LINE + line);
                        }
                    }
                    else if (line.StartsWith(ConfigurationKeys.MIN_RAM))
                    {
                        string[] lineData = line.Split(new Char[] { EQUALS });

                        if (int.TryParse(lineData[VALUE_INDEX], out int tempInt))
                        {
                            MinRam = tempInt;
                            validatedLines++;
                        }
                        else
                        {
                            ErrorLog.LogError(ERROR_NUM + ErrorNum++ + COLAN + Errors.NOT_INTEGER + ERROR_LINE + line);
                        }
                    }
                    else if (line.StartsWith(ConfigurationKeys.MAX_RAM))
                    {
                        string[] lineData = line.Split(new Char[] { EQUALS });

                        if (int.TryParse(lineData[VALUE_INDEX], out int tempInt))
                        {
                            MaxRam = tempInt;
                            validatedLines++;
                        }
                        else
                        {
                            ErrorLog.LogError(ERROR_NUM + ErrorNum++ + COLAN + Errors.NOT_INTEGER + ERROR_LINE + line);
                        }
                    }
                    else if (line.StartsWith(ConfigurationKeys.MIN_DOWNLOAD))
                    {
                        string[] lineData = line.Split(new Char[] { EQUALS });

                        if (int.TryParse(lineData[VALUE_INDEX], out int tempInt))
                        {
                            MinDownload = tempInt;
                            validatedLines++;
                        }
                        else
                        {
                            ErrorLog.LogError(ERROR_NUM + ErrorNum++ + COLAN + Errors.NOT_INTEGER + ERROR_LINE + line);
                        }
                    }
                    else if (line.StartsWith(ConfigurationKeys.MAX_DOWNLOAD))
                    {
                        string[] lineData = line.Split(new Char[] { EQUALS });

                        if (int.TryParse(lineData[VALUE_INDEX], out int tempInt))
                        {
                            MaxDownload = tempInt;
                            validatedLines++;
                        }
                        else
                        {
                            ErrorLog.LogError(ERROR_NUM + ErrorNum++ + COLAN + Errors.NOT_INTEGER + ERROR_LINE + line);
                        }
                    }
                    else if (line.StartsWith(ConfigurationKeys.MIN_UPLOAD))
                    {
                        string[] lineData = line.Split(new Char[] { EQUALS });

                        if (int.TryParse(lineData[VALUE_INDEX], out int tempInt))
                        {
                            MinUpload = tempInt;
                            validatedLines++;
                        }
                        else
                        {
                            ErrorLog.LogError(ERROR_NUM + ErrorNum++ + COLAN + Errors.NOT_INTEGER + ERROR_LINE + line);
                        }
                    }
                    else if (line.StartsWith(ConfigurationKeys.MAX_UPLOAD))
                    {
                        string[] lineData = line.Split(new Char[] { EQUALS });

                        if (int.TryParse(lineData[VALUE_INDEX], out int tempInt))
                        {
                            MaxUpload = tempInt;
                            validatedLines++;
                        }
                        else
                        {
                            ErrorLog.LogError(ERROR_NUM + ErrorNum++ + COLAN + Errors.NOT_INTEGER + ERROR_LINE + line);
                        }
                    }
                    else if (line.StartsWith(END_BLOCK + ConfigurationKeys.LIMITS))
                    {

                        if (currentBlock != ConfigurationKeys.LIMITS)
                        {
                            ErrorLog.LogError(ERROR_NUM + ErrorNum++ + COLAN + Errors.LIMITS_HEADER + ERROR_LINE + line);
                        }

                        if (validatedLines == EXPECTED_LIMIT_LINES)
                        {
                            //Valid
                        }
                        else
                        {
                            ErrorLog.LogError(ERROR_NUM + ErrorNum++ + COLAN + Errors.LIMITS);
                        }

                        currentBlock = null;
                    }
                    else if (line.StartsWith(ConfigurationKeys.PROGRAM))
                    {
                        currentBlock = ConfigurationKeys.PROGRAM;
                        validatedLines = 0;
                    }
                    else if (line.StartsWith(ConfigurationKeys.DURATION))
                    {
                        string[] lineData = line.Split(new Char[] { EQUALS });

                        if (double.TryParse(lineData[VALUE_INDEX], out double tempDouble))
                        {
                            MaxDuration = tempDouble;
                            validatedLines++;
                        }
                        else
                        {
                            ErrorLog.LogError(ERROR_NUM + ErrorNum++ + COLAN + Errors.NOT_DOUBLE + ERROR_LINE + line);
                        }
                    }
                    else if (line.StartsWith(ConfigurationKeys.NUM_TASKS) && (currentBlock == ConfigurationKeys.PROGRAM))
                    {
                        string[] lineData = line.Split(new Char[] { EQUALS });

                        if (int.TryParse(lineData[VALUE_INDEX], out int tempInt))
                        {
                            NumOfTasks = tempInt;
                            validatedLines++;
                        }
                        else
                        {
                            ErrorLog.LogError(ERROR_NUM + ErrorNum++ + COLAN + Errors.NOT_INTEGER + ERROR_LINE + line);
                        }
                    }
                    else if (line.StartsWith(ConfigurationKeys.NUM_PROC) && (currentBlock == ConfigurationKeys.PROGRAM))
                    {
                        string[] lineData = line.Split(new Char[] { EQUALS });

                        if (int.TryParse(lineData[VALUE_INDEX], out int tempInt))
                        {
                            NumOfProc = tempInt;
                            validatedLines++;
                        }
                        else
                        {
                            ErrorLog.LogError(ERROR_NUM + ErrorNum++ + COLAN + Errors.NOT_INTEGER + ERROR_LINE + line);
                        }
                    }
                    else if (line.StartsWith(END_BLOCK + ConfigurationKeys.PROGRAM))
                    {

                        if (currentBlock != ConfigurationKeys.PROGRAM)
                        {
                            ErrorLog.LogError(ERROR_NUM + ErrorNum++ + COLAN + Errors.PROGRAM_HEADER);
                        }

                        if (validatedLines == EXPECTED_PROGRAM_LINES)
                        {
                            //Valid
                        }
                        else
                        {
                            ErrorLog.LogError(ERROR_NUM + ErrorNum++ + COLAN + Errors.PROGRAM);
                        }

                        currentBlock = null;
                    }
                    else if (line.StartsWith(ConfigurationKeys.TASKS) && (currentBlock == null))
                    {
                        currentBlock = ConfigurationKeys.TASKS;
                        expectedSubBlocks = NumOfTasks;
                        validatedSubBlocks = 0;
                    }
                    else if (line.StartsWith(ConfigurationKeys.TASK))
                    {
                        currentSubBlock = ConfigurationKeys.TASK;
                        validatedLines = 0;
                    }
                    else if (line.StartsWith(ConfigurationKeys.TASK_ID) && (currentBlock == ConfigurationKeys.TASKS))
                    {
                        string[] lineData = line.Split(new Char[] { EQUALS });

                        if (int.TryParse(lineData[VALUE_INDEX], out int tempInt))
                        {
                            TaskID = tempInt;
                            validatedLines++;
                        }
                        else
                        {
                            ErrorLog.LogError(ERROR_NUM + ErrorNum++ + COLAN + Errors.NOT_INTEGER + ERROR_LINE + line);
                        }
                    }
                    else if (line.StartsWith(ConfigurationKeys.TASK_RUNTIME))
                    {
                        string[] lineData = line.Split(new Char[] { EQUALS });

                        if (double.TryParse(lineData[VALUE_INDEX], out double tempDouble))
                        {
                            TaskRuntime = tempDouble;
                            validatedLines++;
                        }
                        else
                        {
                            ErrorLog.LogError(ERROR_NUM + ErrorNum++ + COLAN + Errors.NOT_DOUBLE + ERROR_LINE + line);
                        }
                    }
                    else if (line.StartsWith(ConfigurationKeys.REF_FREQ))
                    {
                        string[] lineData = line.Split(new Char[] { EQUALS });

                        if (double.TryParse(lineData[VALUE_INDEX], out double tempDouble))
                        {
                            TaskRefFreq = tempDouble;
                            validatedLines++;
                        }
                        else
                        {
                            ErrorLog.LogError(ERROR_NUM + ErrorNum++ + COLAN + Errors.NOT_DOUBLE + ERROR_LINE + line);
                        }
                    }
                    else if (line.StartsWith(ConfigurationKeys.REQ_RAM) && (currentBlock == ConfigurationKeys.TASKS))
                    {
                        string[] lineData = line.Split(new Char[] { EQUALS });

                        if (int.TryParse(lineData[VALUE_INDEX], out int tempInt))
                        {
                            RequiredRam = tempInt;
                            validatedLines++;
                        }
                        else
                        {
                            ErrorLog.LogError(ERROR_NUM + ErrorNum++ + COLAN + Errors.NOT_INTEGER + ERROR_LINE + line);
                        }
                    }
                    else if (line.StartsWith(ConfigurationKeys.REQ_DOWNLOAD) && (currentBlock == ConfigurationKeys.TASKS))
                    {
                        string[] lineData = line.Split(new Char[] { EQUALS });

                        if (int.TryParse(lineData[VALUE_INDEX], out int tempInt))
                        {
                            RequiredDownload = tempInt;
                            validatedLines++;
                        }
                        else
                        {
                            ErrorLog.LogError(ERROR_NUM + ErrorNum++ + COLAN + Errors.NOT_INTEGER + ERROR_LINE + line);
                        }
                    }
                    else if (line.StartsWith(ConfigurationKeys.REQ_UPLOAD) && (currentBlock == ConfigurationKeys.TASKS))
                    {
                        string[] lineData = line.Split(new Char[] { EQUALS });

                        if (int.TryParse(lineData[VALUE_INDEX], out int tempInt))
                        {
                            RequiredUpload = tempInt;
                            validatedLines++;
                        }
                        else
                        {
                            ErrorLog.LogError(ERROR_NUM + ErrorNum++ + COLAN + Errors.NOT_INTEGER + ERROR_LINE + line);
                        }
                    }
                    else if (line.Equals(END_BLOCK + ConfigurationKeys.TASK))
                    {
                        if (currentSubBlock != ConfigurationKeys.TASK)
                        {
                            ErrorLog.LogError(ERROR_NUM + ErrorNum++ + COLAN + Errors.TASK_HEADER);
                        }

                        if (validatedLines == EXPECTED_TASK_LINES)
                        {
                            //Add new task once subblock is validated
                            Tasks.Add(new Task(TaskID, TaskRuntime, TaskRefFreq, RequiredRam, RequiredDownload, RequiredUpload));
                            validatedSubBlocks++;
                        }
                        else
                        {
                            ErrorLog.LogError(ERROR_NUM + ErrorNum++ + COLAN + Errors.TASK);
                        }

                        currentSubBlock = null;
                    }
                    else if (line.StartsWith(END_BLOCK + ConfigurationKeys.TASKS))
                    {

                        if (currentBlock != ConfigurationKeys.TASKS)
                        {
                            ErrorLog.LogError(ERROR_NUM + ErrorNum++ + COLAN + Errors.TASKS_HEADER);
                        }

                        if (validatedSubBlocks == expectedSubBlocks)
                        {
                            //Tasks Validated
                        }
                        else if (validatedSubBlocks > expectedSubBlocks)
                        {
                            ErrorLog.LogError(ERROR_NUM + ErrorNum++ + COLAN + Errors.TASKS_MORE);
                        }
                        else
                        {
                            ErrorLog.LogError(ERROR_NUM + ErrorNum++ + COLAN + Errors.TASKS_LESS);
                        }

                        currentBlock = null;
                    }
                    else if (line.Equals(ConfigurationKeys.PROCESSORS) && (currentBlock == null))
                    {
                        currentBlock = ConfigurationKeys.PROCESSORS;
                        expectedSubBlocks = NumOfProc;
                        validatedSubBlocks = 0;
                    }
                    else if (line.Equals(ConfigurationKeys.PROCESSOR))
                    {
                        currentSubBlock = ConfigurationKeys.PROCESSOR;
                        validatedLines = 0;
                    }
                    else if (line.StartsWith(ConfigurationKeys.PROC_ID) && (currentBlock == ConfigurationKeys.PROCESSORS)) // as ID key in file has multiple meanings this checks which data block it is
                    {
                        string[] lineData = line.Split(new Char[] { EQUALS });

                        if (int.TryParse(lineData[VALUE_INDEX], out int tempInt))
                        {
                            ProcID = tempInt;
                            validatedLines++;
                        }
                        else
                        {
                            ErrorLog.LogError(ERROR_NUM + ErrorNum++ + COLAN + Errors.NOT_INTEGER + ERROR_LINE + line);
                        }
                    }
                    else if (line.StartsWith(ConfigurationKeys.PROC_TYPE))
                    {
                        string[] lineData = line.Split(new Char[] { EQUALS });
                        string tempString = lineData[VALUE_INDEX];
                        ProcType = tempString;
                        validatedLines++;
                    }
                    else if (line.StartsWith(ConfigurationKeys.PROC_FREQ))
                    {
                        string[] lineData = line.Split(new Char[] { EQUALS });

                        if (double.TryParse(lineData[VALUE_INDEX], out double tempDouble))
                        {
                            ProcFreq = tempDouble;
                            validatedLines++;
                        }
                        else
                        {
                            ErrorLog.LogError(ERROR_NUM + ErrorNum++ + COLAN + Errors.NOT_DOUBLE + ERROR_LINE + line);
                        }
                    }
                    else if (line.StartsWith(ConfigurationKeys.PROC_RAM) && (currentBlock == ConfigurationKeys.PROCESSORS)) // as RAM key in file has multiple meanings this checks which data block it is
                    {
                        string[] lineData = line.Split(new Char[] { EQUALS });

                        if (int.TryParse(lineData[VALUE_INDEX], out int tempInt))
                        {
                            ProcRam = tempInt;
                            validatedLines++;
                        }
                        else
                        {
                            ErrorLog.LogError(ERROR_NUM + ErrorNum++ + COLAN + Errors.NOT_INTEGER + ERROR_LINE + line);
                        }
                    }
                    else if (line.StartsWith(ConfigurationKeys.PROC_DOWNLOAD) && (currentBlock == ConfigurationKeys.PROCESSORS)) // as DOWNLOAD key in file has multiple meanings this checks which data block it is
                    {
                        string[] lineData = line.Split(new Char[] { EQUALS });

                        if (int.TryParse(lineData[VALUE_INDEX], out int tempInt))
                        {
                            ProcDownload = tempInt;
                            validatedLines++;
                        }
                        else
                        {
                            ErrorLog.LogError(ERROR_NUM + ErrorNum++ + COLAN + Errors.NOT_INTEGER + ERROR_LINE + line);
                        }
                    }
                    else if (line.StartsWith(ConfigurationKeys.PROC_UPLOAD) && (currentBlock == ConfigurationKeys.PROCESSORS)) // as UPLOAD key in file has multiple meanings this checks which data block it is
                    {
                        string[] lineData = line.Split(new Char[] { EQUALS });

                        if (int.TryParse(lineData[VALUE_INDEX], out int tempInt))
                        {
                            ProcUpload = tempInt;
                            validatedLines++;
                        }
                        else
                        {
                            ErrorLog.LogError(ERROR_NUM + ErrorNum++ + COLAN + Errors.NOT_INTEGER + ERROR_LINE + line);
                        }
                    }
                    else if (line.Equals(END_BLOCK + ConfigurationKeys.PROCESSOR)) //using StartsWith for PROCESSOR clashes with PROCESSORS, change equals later
                    {

                        if (currentSubBlock != ConfigurationKeys.PROCESSOR)
                        {
                            ErrorLog.LogError(ERROR_NUM + ErrorNum++ + COLAN + Errors.PROCESSOR_HEADER);
                        }

                        if (validatedLines == EXPECTED_PROCESSOR_LINES)
                        {
                            //Add new processor after validated
                            Processors.Add(new Processor(ProcID, ProcType, ProcFreq, ProcRam, ProcDownload, ProcUpload));
                            validatedSubBlocks++;
                        }
                        else
                        {
                            ErrorLog.LogError(ERROR_NUM + ErrorNum++ + COLAN + Errors.PROCESSOR_DATA);
                        }

                        currentSubBlock = null;
                    }
                    else if (line.Equals(END_BLOCK + ConfigurationKeys.PROCESSORS))
                    {

                        if (currentBlock != ConfigurationKeys.PROCESSORS)
                        {
                            ErrorLog.LogError(ERROR_NUM + ErrorNum++ + COLAN + Errors.PROCESSORS_HEADER);
                        }

                        if (validatedSubBlocks == expectedSubBlocks)
                        {
                            //Valid
                        }
                        else if (validatedSubBlocks > expectedSubBlocks)
                        {
                            ErrorLog.LogError(ERROR_NUM + ErrorNum++ + COLAN + Errors.PROCESSORS_MORE);
                        }

                        currentBlock = null;
                    }
                    else if (line.StartsWith(ConfigurationKeys.PROC_TYPES_COEFF) && (currentBlock == null))
                    {
                        currentBlock = ConfigurationKeys.PROC_TYPES_COEFF;
                    }
                    else if (line.Equals(ConfigurationKeys.PROC_TYPE_COEFF))
                    {
                        currentSubBlock = ConfigurationKeys.PROC_TYPE_COEFF;
                        validatedLines = 0;
                    }
                    else if (line.StartsWith(ConfigurationKeys.PROC_NAME_COEFF))
                    {
                        string[] lineData = line.Split(new Char[] { EQUALS });
                        string tempString = lineData[VALUE_INDEX];
                        ProcName = tempString;
                        validatedLines++;
                    }
                    else if (line.StartsWith(ConfigurationKeys.PROC_COEFF_2))
                    {
                        string[] lineData = line.Split(new Char[] { EQUALS });

                        if (double.TryParse(lineData[VALUE_INDEX], out double tempDouble))
                        {
                            Coeff2 = tempDouble;
                            validatedLines++;
                        }
                        else
                        {
                            ErrorLog.LogError(ERROR_NUM + ErrorNum++ + COLAN + Errors.NOT_DOUBLE + ERROR_LINE + line);
                        }
                    }
                    else if (line.StartsWith(ConfigurationKeys.PROC_COEFF_1))
                    {
                        string[] lineData = line.Split(new Char[] { EQUALS });

                        if (double.TryParse(lineData[VALUE_INDEX], out double tempDouble))
                        {
                            Coeff1 = tempDouble;
                            validatedLines++;
                        }
                        else
                        {
                            ErrorLog.LogError(ERROR_NUM + ErrorNum++ + COLAN + Errors.NOT_DOUBLE + ERROR_LINE + line);
                        }
                    }
                    else if (line.StartsWith(ConfigurationKeys.PROC_COEFF_0))
                    {
                        string[] lineData = line.Split(new Char[] { EQUALS });

                        if (double.TryParse(lineData[VALUE_INDEX], out double tempDouble))
                        {
                            Coeff0 = tempDouble;
                            validatedLines++;
                        }
                        else
                        {
                            ErrorLog.LogError(ERROR_NUM + ErrorNum++ + COLAN + Errors.NOT_DOUBLE + ERROR_LINE + line);
                        }
                    }
                    else if (line.Equals(END_BLOCK + ConfigurationKeys.PROC_TYPE_COEFF))
                    {

                        if (currentSubBlock != ConfigurationKeys.PROC_TYPE_COEFF)
                        {
                            ErrorLog.LogError(ERROR_NUM + ErrorNum++ + COLAN + Errors.PROCESSOR_TYPE_HEADER);
                        }

                        if (validatedLines == EXPECTED_PROCESSOR_TYPE_LINES)
                        {
                            ProcessorSpecs.Add(new ProcessorSpec(ProcName, Coeff2, Coeff1, Coeff0));
                        }

                        currentSubBlock = null;
                    }
                    else if (line.StartsWith(END_BLOCK + ConfigurationKeys.PROC_TYPES_COEFF))
                    {
                        if (currentBlock != ConfigurationKeys.PROC_TYPES_COEFF)
                        {
                            ErrorLog.LogError(ERROR_NUM + ErrorNum++ + COLAN + Errors.PROCESSOR_TYPES_HEADER);
                        }
                        else
                        {
                            foreach (Processor processor in Processors)
                            {
                                foreach (ProcessorSpec procType in ProcessorSpecs)
                                {
                                    if (processor.Type == procType.Name)
                                    {
                                        processor.AddType(procType);
                                    }
                                }
                            }
                        }

                        currentBlock = null;
                    }
                    else if (line.StartsWith(ConfigurationKeys.LOCAL_COMM))
                    {
                        currentBlock = ConfigurationKeys.LOCAL_COMM;
                        validatedLines = 0;
                    }
                    else if ((Regex.IsMatch(line, RegexStrings.LOCAL) && (currentBlock == ConfigurationKeys.LOCAL_COMM)))
                    {
                        string[] lineData = line.Split(new Char[] { EQUALS });
                        LocalMap = lineData[VALUE_INDEX];
                        validatedLines++;
                    }
                    else if (line.StartsWith(END_BLOCK + ConfigurationKeys.LOCAL_COMM))
                    {

                        if (currentBlock != ConfigurationKeys.LOCAL_COMM)
                        {
                            ErrorLog.LogError(ERROR_NUM + ErrorNum++ + COLAN + Errors.LOCAL_HEADER);
                        }

                        if (validatedLines == EXPECTED_LOCAL_LINES)
                        {
                            //Valid
                        }
                        else
                        {
                            ErrorLog.LogError(ERROR_NUM + ErrorNum++ + COLAN + Errors.LOCAL_DATA);
                        }

                        currentBlock = null;
                    }
                    else if (line.StartsWith(ConfigurationKeys.REMOTE_COMM))
                    {
                        currentBlock = ConfigurationKeys.REMOTE_COMM;
                        validatedLines = 0;
                    }
                    else if ((Regex.IsMatch(line, RegexStrings.REMOTE) && (currentBlock == ConfigurationKeys.REMOTE_COMM)))
                    {
                        string[] lineData = line.Split(new Char[] { EQUALS });
                        RemoteMap = lineData[VALUE_INDEX];
                        validatedLines++;
                    }
                    else if (line.StartsWith(END_BLOCK + ConfigurationKeys.REMOTE_COMM))
                    {

                        if (currentBlock != ConfigurationKeys.REMOTE_COMM)
                        {
                            ErrorLog.LogError(ERROR_NUM + ErrorNum++ + COLAN + Errors.REMOTE_HEADER);
                        }

                        if (validatedLines == EXPECTED_REMOTE_LINES)
                        {
                            //Validated
                        }
                        else
                        {
                            ErrorLog.LogError(ERROR_NUM + ErrorNum++ + COLAN + Errors.REMOTE_DATA);
                        }

                        currentBlock = null;
                    }
                    else
                    {
                        ErrorLog.LogError(ERROR_NUM + ErrorNum++ + COLAN + Errors.LINE_ERROR + ERROR_LINE + line);
                        valid = false;
                    }
                }
            }

            return (valid);
        }
    }
}
