using System;
using Tizen;

namespace FingerID
{
    public class Global
    {
        public static int PORT = 50005;
        public static String IP_ADDRESS = null;
        public static int SubId = 9999;
        public static void logMessage(String str) { Log.Info("LOG_TAG", str); }
        public static string CurrentFinger = "NONE";
        //try
        //{
        //    
        //}
        //catch (Exception error)
        //{
        //    Global.logMessage(Convert.ToString(error));
        //}

        public Global()
        {

        }
    }
}
