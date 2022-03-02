using System;
using System.IO;
using System.Text;
using System.Collections.Generic;

namespace SoundTest
{
    public class csvManager
    {
        public static void writeData(List<Trial> trials)
        {
            var csv = new StringBuilder();
            var index = string.Format("index,targetPosture,targetNum,finger,startTime,downTime,correctDown");
            csv.AppendLine(index);

            int counter = 0;
            foreach (var trial in trials)
            {
                csv.AppendLine(string.Format(counter+","+trial.posture+","+trial.targetNum+","+trial.finger+","+trial.startTime+","+trial.touchDownTime+","+trial.correctDown));
                counter += 1;
            }

            File.WriteAllText("/home/owner/media/Sounds/testCSV.csv", csv.ToString());
            Global.logMessage("CSV FILE PREPARED");
        }

        public csvManager()
        {
        }
    }
}
