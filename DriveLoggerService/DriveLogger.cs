using System;
using System.IO;
using System.ServiceProcess;
using System.Management;
using System.Timers;

namespace DriveLoggerService
{
    public partial class DriveLogger : ServiceBase
    {
        static string logPath = "";

        private static string BuildDriveList()
        {
            string msg = "";

            DriveInfo[] drives = DriveInfo.GetDrives();
            string nl = Environment.NewLine;

            foreach (DriveInfo d in drives)
            {
                msg += "Drive " + d.Name + nl;
                msg += "\tType: " + d.DriveType + nl;

                if (d.IsReady)
                {
                    msg += "\tSTATUS The drive is ready." + nl;
                }
                else
                {
                    msg += "\tSTATUS The drive is not ready." + nl;
                    msg += "\tERROR Unable to provide more information." + nl;
                }
            }

            return msg;
        }

        private static string BuildMessage(bool insert)
        {
            string msg = "";

            if (insert)
            {
                msg = "USB EVENT-> New plug in." + Environment.NewLine;
            }
            else
            {
                msg = "USB EVENT-> Deviced removed." + Environment.NewLine;
            }

            msg += "Drive List: " + Environment.NewLine;
            msg += BuildDriveList();
            msg += Environment.NewLine;

            return msg;
        }

        private static void WriteLog(string msg)
        {
            string currentText = File.ReadAllText(logPath);
            currentText += msg;
            File.WriteAllText(logPath, currentText);
        }

        private static void CheckPath()
        {
            if (!File.Exists(logPath))
            {
                File.Create(logPath).Close();
            }
        }

        public DriveLogger()
        {
            InitializeComponent();
        }

        protected override void OnStart(string[] args)
        {
            try
            {
                logPath = "C:\\devicelog.txt";
                CheckPath();

                String initMsg = "[STATUS] The logger has started." + Environment.NewLine;
                initMsg += "Initial Drive List: " + Environment.NewLine;
                initMsg += BuildDriveList();
                initMsg += Environment.NewLine;
                WriteLog(initMsg);

                var insertWatcher = new ManagementEventWatcher();
                var insertQuery = new WqlEventQuery("SELECT * FROM Win32_VolumeChangeEvent WHERE EventType = 2");
                insertWatcher.EventArrived += Watcher_EventArrived;
                insertWatcher.Query = insertQuery;
                insertWatcher.Start();

                var rmWatcher = new ManagementEventWatcher();
                var rmQuery = new WqlEventQuery("SELECT * FROM __InstanceDeletionEvent WITHIN 2 WHERE TargetInstance ISA 'Win32_USBHub'");
                rmWatcher.EventArrived += RmWatcher_EventArrived;
                rmWatcher.Query = rmQuery;
                rmWatcher.Start();

                Timer timer = new Timer();
                timer.Interval = 10000;
                timer.Elapsed += new ElapsedEventHandler(this.OnTimer);
                timer.Start();
            } catch (Exception e)
            {
            }
        }

        protected override void OnStop()
        {
        }

        public void OnTimer(object sender, ElapsedEventArgs args)
        {
            CheckPath();
        }

        private static void Watcher_EventArrived(object sender, EventArrivedEventArgs e)
        {
            String msg = BuildMessage(true);
            WriteLog(msg);
        }

        private static void RmWatcher_EventArrived(object sender, EventArrivedEventArgs e)
        {
            String msg = BuildMessage(false);
            WriteLog(msg);
        }
    }
}
