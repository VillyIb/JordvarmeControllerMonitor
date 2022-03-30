using System.Net;
using System.Net.Mail;
using System.Timers;

namespace JordvarmeMonitor.MonitorActivity
{
    internal enum Mode
    {
        Running,
        Stopped
    }

    internal class MonitorDirectory
    {
        private readonly FileSystemWatcher watcher;

        private static System.Timers.Timer timer;

#if DEBUG
        private static readonly double IntervalInMiliSeconds = 1000 * 30; // half minute
        private static readonly double SlowIntervalInMiliSeconds = 1000 * 60 * 2; // two minutes
        private static readonly string WatchPath = @"C:\Development\JordvarmeMonitor\MonitorDirectory\";
#else
        private static readonly double IntervalInMiliSeconds = 1000 * 60 * 2; // two minute
        private static readonly double SlowIntervalInMiliSeconds = 1000 * 60 * 60; // one hour
        private static readonly string WatchPath = @"C:\GitRepositories\JordvarmeController\JordvarmeController\BoosterLog\";
#endif

        private static string smtpAddress = "smtp-mail.outlook.com";
        private static int portNumber = 587;
        private static bool enableSSL = true;
        private static string emailFromAddress = "villy.ib.jorgensen@outlook.com"; //Sender Email Address  
        private static string password = "Antananarivo447"; //Sender Password  
        private static string emailToAddress = "villy.ib.jorgensen@gmail.com"; //Receiver Email Address  

        private static readonly string StartMessage = @"Jordvarme styring monitor startet";
        private static readonly string ReadyMessage = @"Ready";
        private static readonly string RunningMessage = @"Jordvarme styring kører";
        private static readonly string StoppedMessage = @"Jordvarme styring: stoppet";
        private static readonly string StillStoppedMessage = @"Jordvarme styring: fortsat stoppet";
        private static readonly string DailyMessage = @"Jordvarme styring: kører fortsat";


        private static Mode Mode;

        public MonitorDirectory()
        {
            Mode = Mode.Running;

            AdvanceNextTimeToSendDailyMessage();
            SendEmail(StartMessage);

            watcher = new FileSystemWatcher(WatchPath);
            watcher.Changed += OnChanged;

            watcher.Filter = "*.log";
            watcher.IncludeSubdirectories = false;
            watcher.EnableRaisingEvents = true;

            timer = new System.Timers.Timer(IntervalInMiliSeconds);
            timer.Elapsed += OnTimedEvent;
            timer.AutoReset = true;
            timer.Enabled = true;

            Console.WriteLine(ReadyMessage);
        }

        public static void SendEmail(string subjectAndBody)
        {
            using (MailMessage mail = new MailMessage())
            {
                mail.From = new MailAddress(emailFromAddress);
                mail.To.Add(emailToAddress);
                mail.Subject = subjectAndBody;
                mail.Body = subjectAndBody;
                mail.IsBodyHtml = true;
                //mail.Attachments.Add(new Attachment("D:\\TestFile.txt"));//--Uncomment this to send any attachment  
                using (SmtpClient smtp = new SmtpClient(smtpAddress, portNumber))
                {
                    smtp.Credentials = new NetworkCredential(emailFromAddress, password);
                    smtp.DeliveryMethod = SmtpDeliveryMethod.Network;
                    smtp.EnableSsl = enableSSL;
                    smtp.Send(mail);
                }
            }
        }

        private static void ResetTimer()
        {
            if (Mode.Stopped == Mode)
            {
                SendEmail(RunningMessage);
                Mode = Mode.Running;
            }

            timer.Interval =  IntervalInMiliSeconds;
        }

        private static void SlowdownTimer()
        {
            timer.Interval = SlowIntervalInMiliSeconds;
            Mode = Mode.Stopped;
        }

        private static DateTime NextTimeToSendDailyMessage { get; set; }

        private static void AdvanceNextTimeToSendDailyMessage()
        {
            var now = DateTime.Now;

            var todayAt0600 = new DateTime(now.Year, now.Month, now.Day, 06, 00, 00);

            NextTimeToSendDailyMessage = (now < todayAt0600) ? todayAt0600 : todayAt0600.AddDays(1);
        }

        private static void SendDailyMessage()
        {
            if (DateTime.Now < NextTimeToSendDailyMessage) { return; }

            SendEmail(DailyMessage);
            AdvanceNextTimeToSendDailyMessage();
        }

        private static void OnChanged(object sender, FileSystemEventArgs e)
        {
            Console.WriteLine("{0:HH:mm:ss.fff} - The timer was reset ", DateTime.Now);
            ResetTimer();
            SendDailyMessage();
        }

        private static void OnTimedEvent(object source, ElapsedEventArgs e)
        {
            Console.WriteLine("{0:HH:mm:ss.fff} - The Elapsed event was raised ({1})", e.SignalTime, timer.Interval);
            SendEmail(Mode.Running == Mode ? StoppedMessage: StillStoppedMessage);
            SlowdownTimer();
        }
    }
}
