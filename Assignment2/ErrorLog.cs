using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.IO;

namespace Assignment2
{
    class ErrorLog
    {
        public static string LogFilePath { get; set; }
        public static Form ErrorWindow { get; set; }

        public static void CreateWindow()
        {
            ErrorWindow = new ErrorsForm();
        }

        public static void LogToFile(string log)
        {
            if (LogFilePath != null)
            {
                try
                {
                    StreamWriter file = new StreamWriter($"{LogFilePath}\\log.txt", append: true);
                    file.WriteLine(log);
                }
                catch(Exception ex)
                {
                    MessageBox.Show(ex.Message, "Log File Error");
                }
            }
        }

        public static void LogError(string error)
        {
            if (LogFilePath != null)
            {
                LogToFile(error);
            }

            if (ErrorWindow == null)
            {
                CreateWindow();
            }

            var listBox = ErrorWindow.Controls["errorList"] as ListBox;
            listBox.Items.Add(error);
        }
    }
}
