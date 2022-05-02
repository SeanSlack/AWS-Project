using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Threading;
using System.Windows.Forms;
using System.Diagnostics;
using WCFSLibrary;
using System.ServiceModel;
using System.Net;
using Assignment2.GreedyAlgorithm;
using Assignment2.HeuristicAlgorithm;

namespace Assignment2
{
    public partial class MainForm : Form
    {
        Allocations Allocations = new Allocations();
        Configuration Configuration = new Configuration();
        AutoResetEvent autoResetEvent;
        List<AllocationData> AllocationsFound;

        bool isLocal = true;
        bool TAFFvalid;
        bool? CFFvalid;

        int searchesWithAlg;
        int completedSearches;
        int expectedSearches;

        readonly object aLock = new object();

        const string TAFF_LOG_START = "---- TAFF FILE ----";
        const string CFF_LOG_START = "---- CFF FILE ----";
        const string ALLOCATION_LOG = "---- ALLOCATIONS ----";
        const string END_CURRENT_LOG = "----------------------";
        const int TIMEOUT = 300000;

        public MainForm()
        {
            InitializeComponent();
        }

        // Validates both CFF and TAFF files when opening
        private void OpenToolStripMenuItem_Click(object sender, EventArgs e)
        {

            DialogResult result = fileDialog.ShowDialog();

            TAFFvalid = true;
            CFFvalid = true;
            ClearGUI();

            if (result == DialogResult.OK)
            {
                if (Allocations.GetCFFname(fileDialog.FileName) != null)
                {
                    ErrorLog.LogError(CFF_LOG_START);

                    if (!Configuration.ValidateCFF(Allocations.ConfigFile, isLocal))
                    {
                        CFFvalid = false;
                    }

                    ErrorLog.LogError(END_CURRENT_LOG);
                    ErrorLog.LogError(TAFF_LOG_START);

                    if (!Allocations.ValidateTAFF(fileDialog.FileName, Configuration))
                    {
                        TAFFvalid = false;
                    }
                }
                PrintGUI();
            }
        }

        private void ClearGUI()
        {
            foreach (Control control in mainPanel.Controls)
            {
                if (control is Label)
                {
                    Label lbl = (Label)control;
                    lbl.Text = string.Empty;
                    lbl.Visible = false;
                }
            }
        }

        // Prints the current allocations to main GUI
        private void PrintGUI()
        {
            int topControl = 0;

            Label cffFileValid = new Label();
            mainPanel.Controls.Add(cffFileValid);
            cffFileValid.Top = topControl * 4;
            cffFileValid.Left = 20;
            cffFileValid.Font = new Font("Arial", 9);
            cffFileValid.AutoSize = true;

            if (CFFvalid == false)
            {
                cffFileValid.Text = "Configuration file is invalid";
            }
            else
            {
                cffFileValid.Text = "Configuration file is valid";
            }

            topControl = topControl + 4;

            Label taffFileValid = new Label();
            mainPanel.Controls.Add(taffFileValid);
            taffFileValid.Top = topControl * 4;
            taffFileValid.Left = 20;
            taffFileValid.Font = new Font("Arial", 9);
            taffFileValid.AutoSize = true;

            if(CFFvalid != null)
            {
                if (!TAFFvalid)
                {
                    taffFileValid.Text = "Allocations file is invalid";
                }
                else
                {
                    taffFileValid.Text = "Allocations file is valid";
                }
            }

            topControl = topControl + 10;

            foreach (Allocation allocation in Allocations.AllocationList)
            {
                double leftControl = 0;
                Label allocationLabel = new Label();
                
                mainPanel.Controls.Add(allocationLabel);
                allocationLabel.Top = topControl * 4;
                allocationLabel.Left = 20;
                allocationLabel.Text = "Allocation ID = " + allocation.ID + ",      Runtime = " + string.Format("{0:N2}", allocation.ProgramRuntime) + ",       Energy: " + string.Format("{0:N2}", allocation.Energy);
                allocationLabel.Font = new Font("Arial", 9, FontStyle.Bold);
                allocationLabel.AutoSize = true;
                topControl = topControl + 2;

                int rowLength = allocation.Map.GetLength(0);
                int colLength = allocation.Map.GetLength(1);

                int mapControl = topControl;
                for (int i = 0; i < rowLength; i++)
                {
                    mapControl = mapControl + 5;
                    Label taskLabel = AddBorderedLabel(rowLength, 150, mapControl * 4);
                    mainPanel.Controls.Add(taskLabel);

                    for (int j = 0; j < colLength; j++)
                    {
                        leftControl += 1.2;
                        taskLabel.Text += ((allocation.Map[i, j]) + "  ");
                    }
                }

                foreach (Processor processor in allocation.ProcessorList)
                {
                    topControl = topControl + 5;
                    Label processorLabel = new Label();
                    mainPanel.Controls.Add(processorLabel);
                    processorLabel.Top = topControl * 4;
                    processorLabel.Left = 30;
                    processorLabel.Text = "Processor ID = " + processor.ID;
                    processorLabel.Font = new Font("Arial", 8, FontStyle.Bold);

                    Label ramLabel = AddBorderedLabel(processor.ID, (int)leftControl + 220, topControl * 4);
                    mainPanel.Controls.Add(ramLabel);
                    ramLabel.Text = processor.RequiredRam() + " / " + processor.Ram + " GB";

                    Label downloadLabel = AddBorderedLabel(processor.ID, (int)leftControl + 285, topControl * 4);
                    mainPanel.Controls.Add(downloadLabel);
                    downloadLabel.Text = processor.RequiredDownloadSpeed() + " / " + processor.DownloadSpeed + " Gbps";

                    Label uploadLabel = AddBorderedLabel(processor.ID, (int)leftControl + 385, topControl * 4);
                    mainPanel.Controls.Add(uploadLabel);
                    uploadLabel.Text = processor.RequiredUploadSpeed() + " / " + processor.UploadSpeed + " Gbps";
                }

                topControl = topControl + 10;
            }


            if ((CFFvalid == true || CFFvalid == null) && TAFFvalid == true)
            {
                allocationsToolStripMenuItem.Enabled = true;
            }
            else
            {
                allocationsToolStripMenuItem.Enabled = false;
            }
        }

        private void ExitToolStripMenuItem_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        private void AboutToolStripMenuItem_Click(object sender, EventArgs e)
        {
            About aboutBox = new About();
            aboutBox.ShowDialog();
        }

        private void ErrorsToolStripMenuItem_Click(object sender, EventArgs e)
        {

            ErrorLog.ErrorWindow.Show();
        }

        private void WebBrowser1_DocumentCompleted(object sender, WebBrowserDocumentCompletedEventArgs e)
        {

        }

        private void testToolStripMenuItem_Click(object sender, EventArgs e)
        {

        }

        private void allocationsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            ErrorLog.LogError(ALLOCATION_LOG);

            int topControl = 5;

            Label allocationValid = new Label();
            mainPanel.Controls.Add(allocationValid);
            allocationValid.Top = topControl * 4;
            allocationValid.Left = 20;
            allocationValid.Font = new Font("Arial", 9);
            allocationValid.AutoSize = true;

            if (!Allocations.ValidateAllocations())
            {
                allocationValid.Text = "Allocations are invalid";
            }
            else
            {
                allocationValid.Text = "Allocations are valid";
            }
        }

        private void MainForm_Load(object sender, EventArgs e)
        {
        }

        Label AddBorderedLabel(int i, int start, int end)
        {
            Label newLabel = new Label();
            newLabel.Name = "textbox" + i.ToString();
            newLabel.Font = new Font("Arial", 8, FontStyle.Bold);
            newLabel.Location = new Point(start, end);
            newLabel.BorderStyle = System.Windows.Forms.BorderStyle.Fixed3D;
            newLabel.BackColor = Color.FromArgb(220, 220, 220);
            newLabel.AutoSize = true;
            return newLabel;
        }

        private void flowLayoutPanel1_Paint(object sender, PaintEventArgs e)
        {

        }

        private void comboBox1_SelectedIndexChanged(object sender, EventArgs e)
        {

        }

        private void button1_Click(object sender, EventArgs e)
        {
            ClearGUI();

            //Creates new allocation and config object
            Boolean isLocal = false;
            String configFileName = configFileList.Text.Trim();
            Configuration = new Configuration();
            Allocations = new Allocations();
            AllocationsFound = new List<AllocationData>();
            autoResetEvent = new AutoResetEvent(false);

            //imports data from remote CFF file
            Configuration.ValidateCFF(configFileName, isLocal);
            int numOfTasks = Configuration.NumOfTasks;
            int numOfProcessors = Configuration.NumOfProc;

            //Creates new config object for sending to WCF services
            ConfigData cd = new ConfigData();
            cd.Duration = Configuration.MaxDuration;
            cd.NumberOfProcessors = numOfProcessors;
            cd.NumberOfTasks = numOfTasks;
            cd.AllocatedRuntimes = Configuration.CreateAllocatedRuntimeArray();
            cd.Energies = Configuration.CreateEnergyArray();
            cd.InvalidAllocations = Configuration.CreateAllocationMatrixSerializable();

            System.Threading.Tasks.Task.Run(() => AsyncWCFSCalls(cd));
            autoResetEvent.WaitOne(TIMEOUT);

            //After waiting check if any allocations were found, if find allocations with minimum energy and print to console
            bool isEmpty = !AllocationsFound.Any();
            lock (aLock)
            {
                if (isEmpty)
                {
                    Debug.WriteLine("No allocations found");
                }
                else
                {
                    double lowestEnergy = AllocationsFound.First().Energy;
                    double currentEnergy;
                    foreach (AllocationData allocation in AllocationsFound)
                    {
                        currentEnergy = allocation.Energy;
                        if (lowestEnergy > currentEnergy)
                        {
                            lowestEnergy = currentEnergy;
                        }
                    }

                    int count = 0;
                    foreach (AllocationData allocation in AllocationsFound)
                    {
                        if (allocation.Energy == lowestEnergy)
                        {
                            int[,] map = Allocations.CreateAllocationMatrix(allocation.Map, cd.NumberOfProcessors, cd.NumberOfTasks);

                            Allocation newAllocation = new Allocation(count, map);
                            newAllocation.AddProcessorList(Configuration.Processors);
                            newAllocation.AddTaskList(Configuration.Tasks);
                            newAllocation.AssignTasksToProcessor();
                            newAllocation.CalculateProgramRuntime();
                            newAllocation.CalculateTotalEnergy();
                            Allocations.AllocationList.Add(newAllocation);

                            count++;
                        }
                    }
                    Debug.WriteLine(lowestEnergy);
                }
            }

            TAFFvalid = true;
            CFFvalid = null;

            PrintGUI();
        }

        private void AsyncWCFSCalls(ConfigData cd)
        {
            //Remote Greedy/Heuristic
            //GreedyALB.ServiceClient greedyALB = new GreedyALB.ServiceClient();
            //greedyALB.GetAllocationsGreedyCompleted += GreedyALB_GetAllocationsGreedyCompleted;

            //HeuristicALB.ServiceClient heuristicALB = new HeuristicALB.ServiceClient();
            //heuristicALB.GetAllocationsHeuristicCompleted += HeuristicALB_GetAllocationsHeuristicCompleted;


            //Local Greedy/Heuristic
            GreedyAlgorithm.ServiceClient webServiceGreedy = new GreedyAlgorithm.ServiceClient();
            webServiceGreedy.GetAllocationsGreedyCompleted += WebServiceGreedy_GetAllocationsGreedyCompleted;

            HeuristicAlgorithm.ServiceClient webServiceHeuristic = new HeuristicAlgorithm.ServiceClient();
            webServiceHeuristic.GetAllocationsHeuristicCompleted += WebServiceHeuristic_GetAllocationsHeuristicCompleted;


            //Set number of searches for both algorithms and expected results
            expectedSearches = 10;
            searchesWithAlg = 5;
            completedSearches = 0;

            ////REMOTE CALLS
            //for (int count = 0; count < searchesWithAlg; count++)
            //{
            //    greedyALB.GetAllocationsGreedyAsync(20000, cd, count);
            //    heuristicALB.GetAllocationsHeuristicAsync(TIMEOUT, cd, count);
            //}

            //LOCAL CALLS
            for (int count = 0; count < searchesWithAlg; count++)
            {
                webServiceGreedy.GetAllocationsGreedyAsync(20000, cd, count);
                webServiceHeuristic.GetAllocationsHeuristicAsync(TIMEOUT, cd, count);
            }
        }

        private void HeuristicALB_GetAllocationsHeuristicCompleted(object sender, HeuristicALB.GetAllocationsHeuristicCompletedEventArgs e)
        {
            try
            {
                lock (aLock)
                {
                    AllocationData result = e.Result;

                    completedSearches++;

                    AllocationsFound.Add(result);
                    Debug.WriteLine("Found allocation - Heuristic");

                    if (completedSearches == expectedSearches)
                    {
                        autoResetEvent.Set();
                    }
                }
            }
            catch (Exception ex) when (ex.InnerException is TimeoutException tex)
            {
                lock (aLock)
                {
                    completedSearches++;

                    Debug.WriteLine("Local timeout");

                    if (completedSearches == expectedSearches)
                    {
                        autoResetEvent.Set();
                    }
                }
            }
            catch (Exception ex) when (ex.InnerException is FaultException<WCFSLibrary.TimeoutFault> tof)
            {
                lock (aLock)
                {
                    completedSearches++;

                    Debug.WriteLine("WCFS Timeout", tof.Detail.Message);

                    if (completedSearches == expectedSearches)
                    {
                        autoResetEvent.Set();
                    }
                }
            }
            catch (Exception ex) when (ex.InnerException is FaultException fex)
            {
                lock (aLock)
                {
                    completedSearches++;

                    Debug.WriteLine("WCFS Unknown Fault", fex.Message);

                    if (completedSearches == expectedSearches)
                    {
                        autoResetEvent.Set();
                    }
                }
            }
            catch (Exception ex) when (ex.InnerException is CommunicationException cex)
            {
                lock (aLock)
                {
                    completedSearches++;

                    Debug.WriteLine("Comm Exception");

                    if (completedSearches == expectedSearches)
                    {
                        autoResetEvent.Set();
                    }
                }
            }
            catch (Exception ex) when (ex.InnerException is WebException wex)
            {
                lock (aLock)
                {
                    completedSearches++;

                    Debug.WriteLine("Web Exception");

                    if (completedSearches == expectedSearches)
                    {
                        autoResetEvent.Set();
                    }
                }
            }
        }

        private void GreedyALB_GetAllocationsGreedyCompleted(object sender, GreedyALB.GetAllocationsGreedyCompletedEventArgs e)
        {
            try
            {
                lock (aLock)
                {
                    AllocationData result = e.Result;

                    completedSearches++;

                    AllocationsFound.Add(result);
                    Debug.WriteLine("Found allocation - Greedy");

                    if (completedSearches == expectedSearches)
                    {
                        autoResetEvent.Set();
                    }
                }
            }
            catch (Exception ex) when (ex.InnerException is TimeoutException tex)
            {
                lock (aLock)
                {
                    completedSearches++;

                    Debug.WriteLine("Local timeout");

                    if (completedSearches == expectedSearches)
                    {
                        autoResetEvent.Set();
                    }
                }
            }
            catch (Exception ex) when (ex.InnerException is FaultException<WCFSLibrary.TimeoutFault> tof)
            {
                lock (aLock)
                {
                    completedSearches++;

                    Debug.WriteLine("WCFS Timeout", tof.Detail.Message);

                    if (completedSearches == expectedSearches)
                    {
                        autoResetEvent.Set();
                    }
                }
            }
            catch (Exception ex) when (ex.InnerException is FaultException fex)
            {
                lock (aLock)
                {
                    completedSearches++;

                    Debug.WriteLine("WCFS Unknown Fault", fex.Message);

                    if (completedSearches == expectedSearches)
                    {
                        autoResetEvent.Set();
                    }
                }
            }
            catch (Exception ex) when (ex.InnerException is CommunicationException cex)
            {
                lock (aLock)
                {
                    completedSearches++;

                    Debug.WriteLine("Comm Exception");

                    if (completedSearches == expectedSearches)
                    {
                        autoResetEvent.Set();
                    }
                }
            }
            catch (Exception ex) when (ex.InnerException is WebException wex)
            {
                lock (aLock)
                {
                    completedSearches++;

                    Debug.WriteLine("Web Exception");

                    if (completedSearches == expectedSearches)
                    {
                        autoResetEvent.Set();
                    }
                }
            }
        }

        private void WebServiceGreedy_GetAllocationsGreedyCompleted(object sender, GetAllocationsGreedyCompletedEventArgs e)
        {
            try
            {
                lock (aLock)
                {
                    AllocationData result = e.Result;

                    completedSearches++;

                    AllocationsFound.Add(result);
                    Debug.WriteLine("Found allocation - Greedy");

                    if (completedSearches == expectedSearches)
                    {
                        autoResetEvent.Set();
                    }
                }
            }
            catch (Exception ex) when (ex.InnerException is TimeoutException tex)
            {
                lock (aLock)
                {
                    completedSearches++;

                    Debug.WriteLine("Local timeout");

                    if (completedSearches == expectedSearches)
                    {
                        autoResetEvent.Set();
                    }
                }
            }
            catch (Exception ex) when (ex.InnerException is FaultException<WCFSLibrary.TimeoutFault> tof)
            {
                lock (aLock)
                {
                    completedSearches++;

                    Debug.WriteLine("WCFS Timeout", tof.Detail.Message);

                    if (completedSearches == expectedSearches)
                    {
                        autoResetEvent.Set();
                    }
                }
            }

            catch (Exception ex) when (ex.InnerException is FaultException fex)
            {
                lock (aLock)
                {
                    completedSearches++;

                    Debug.WriteLine("WCFS Unknown Fault", fex.Message);

                    if (completedSearches == expectedSearches)
                    {
                        autoResetEvent.Set();
                    }
                }
            }

            catch (Exception ex) when (ex.InnerException is CommunicationException cex)
            {
                lock (aLock)
                {
                    completedSearches++;

                    Debug.WriteLine("Comm Exception");

                    if (completedSearches == expectedSearches)
                    {
                        autoResetEvent.Set();
                    }
                }
            }
            catch (Exception ex) when (ex.InnerException is WebException wex)
            {
                lock (aLock)
                {
                    completedSearches++;

                    Debug.WriteLine("Web Exception");

                    if (completedSearches == expectedSearches)
                    {
                        autoResetEvent.Set();
                    }
                }
            }
        }

        private void WebServiceHeuristic_GetAllocationsHeuristicCompleted(object sender, GetAllocationsHeuristicCompletedEventArgs e)
        {
            try
            {
                lock (aLock)
                {
                    AllocationData result = e.Result;

                    completedSearches++;

                    AllocationsFound.Add(result);
                    Debug.WriteLine("Found allocation - Heuristic");
                    if (completedSearches == expectedSearches)
                    {
                        autoResetEvent.Set();
                    }
                }
            }
            catch (Exception ex) when (ex.InnerException is TimeoutException tex)
            {
                lock (aLock)
                {
                    completedSearches++;

                    Debug.WriteLine("Local timeout");

                    if (completedSearches == expectedSearches)
                    {
                        autoResetEvent.Set();
                    }
                }
            }
            catch (Exception ex) when (ex.InnerException is FaultException<WCFSLibrary.TimeoutFault> tof)
            {
                lock (aLock)
                {
                    completedSearches++;

                    Debug.WriteLine("WCFS Timeout", tof.Detail.Message);

                    if (completedSearches == expectedSearches)
                    {
                        autoResetEvent.Set();
                    }
                }
            }
            catch (Exception ex) when (ex.InnerException is FaultException fex)
            {
                lock (aLock)
                {
                    completedSearches++;

                    Debug.WriteLine("WCFS Unknown Fault", fex.Message);

                    if (completedSearches == expectedSearches)
                    {
                        autoResetEvent.Set();
                    }
                }
            }
            catch (Exception ex) when (ex.InnerException is CommunicationException cex)
            {
                lock (aLock)
                {
                    completedSearches++;

                    Debug.WriteLine("Comm Exception");

                    if (completedSearches == expectedSearches)
                    {
                        autoResetEvent.Set();
                    }
                }
            }
            catch (Exception ex) when (ex.InnerException is WebException wex)
            {
                lock (aLock)
                {
                    completedSearches++;

                    Debug.WriteLine("Web Exception");

                    if (completedSearches == expectedSearches)
                    {
                        autoResetEvent.Set();
                    }
                }
            }
        }

        private void mainPanel_Paint(object sender, PaintEventArgs e)
        {

        }

        private void splitContainer1_Panel1_Paint(object sender, PaintEventArgs e)
        {

        }
    }
}
