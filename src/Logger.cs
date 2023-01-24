using System;
using System.IO;

namespace GeometryFriendsAgents
{
    class Logger
    {
        private string _filePath;

        // Initialize log file and folder if necessary
        public Logger()
        {
            string logsPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Logs");
            if (!Directory.Exists(logsPath))
            {
                Directory.CreateDirectory(logsPath);
            }
            _filePath = Path.Combine(logsPath, string.Format("log_{0}.csv", DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss")));
        }

        //Write logs into file
        public void Log(string message, string agentType, string cooperationStatus, string circleAction, string rectAction, float circleX, float circleY, float rectX, float rectY, string toCollectCollectibles)
        {
            using (var writer = new StreamWriter(_filePath, true))
            {
                writer.WriteLine(DateTime.Now.ToString() + "," + message + "," + agentType + "," + cooperationStatus + "," + circleAction + "," + rectAction + "," + (int) circleX + "," + (int) circleY + "," + (int) rectX + "," + (int) rectY + "," + toCollectCollectibles);
            }
        }
    }
}
