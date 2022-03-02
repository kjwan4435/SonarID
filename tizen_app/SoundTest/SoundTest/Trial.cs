using System;
using System.Collections.Generic;

namespace SoundTest
{
    public class PointTime
    {
        public int x; public int y; public long t;
        public PointTime(int _x, int _y, long _t) { x = _x; y = _y; t = _t; }
        PointTime() { }
    }

    public class Trial
    {
        public int targetNum;
        public int finger;
        public int posture;

        public long startTime;
        public long touchDownTime;
        public long touchUpTime;
        public long endTime;
        public long touchDownIndex;

        public List<PointTime> pts;

        public bool correctDown;

        public Trial(int t, int f, int p)
        {
            posture = p;
            targetNum = t;
            finger = f;
            startTime = touchDownTime = touchUpTime = endTime = -1;
            pts = new List<PointTime>();
            pts.Clear();
            correctDown = false;
        }

        public Trial(Trial tIn)
        {
            targetNum = tIn.targetNum;
            finger = tIn.finger;
            startTime = tIn.startTime;
            touchDownTime = tIn.touchDownTime;
            touchUpTime = tIn.touchUpTime;
            endTime = tIn.endTime;
            correctDown = tIn.correctDown;
            touchDownIndex = tIn.touchDownIndex;

            pts = new List<PointTime>();
            pts.Clear();
            foreach (PointTime pt in tIn.pts)
            {
                pts.Add(new PointTime(pt.x, pt.y, pt.t));
            }       
        }

        bool correct() { return correctDown;  }

        String getString()
        {
            String s = targetNum + "," + targetNum % 3 + "," + targetNum / 3 + "," + finger + "," +  // trial data
                       startTime + "," + touchDownTime + "," + touchDownIndex + "," + touchUpTime + "," + endTime + "," +
                       correctDown + ",";
            s += pts[0].x + "_" + pts[0].y + "_" + pts[0].t + ",";
            return s;
        }


    }

    public class Trials
    {
        public List<Trial> trials;
        public List<Trial> trialsDone;
        public int numberBlocksLeft;

        public void initTrialLog()
        { trialsDone = new List<Trial>(); }

        public bool initTrials(int numPostures, int numTargets, int numFingers, int numRepsBlock)
        {
            numberBlocksLeft = numberBlocksLeft - 1;
            if (numberBlocksLeft < 0)
                return false;

            trials = new List<Trial>();
            for (int b = 0; b < numRepsBlock; b++)
                for (int p = 0; p < numPostures; p++)
                    for (int f = 0; f < numFingers; f++)
                        for (int t = 0; t < numTargets; t++)
                            trials.Add(new Trial(t, f, p));
            trials = shuffleTrials(trials);

            return true;
        }

        public List<Trial> shuffleTrials(List<Trial> arrList)
        {
            if (trials != null)
            {
                Random r = new Random();
                for (int cnt = 0; cnt < arrList.Count; cnt++)
                {
                    Trial tmp = arrList[cnt];
                    int idx = r.Next(arrList.Count - cnt) + cnt;
                    arrList[cnt] = arrList[idx];
                    arrList[idx] = tmp;
                }
            }
            return arrList;
        }

        public void startTrial(long t)
        {
            Trial tl = trials[0];
            tl.startTime = t;
            Global.logMessage("Start Time: " + t);
        }

        public void addStartTrialTime(long t)
        {
            startTrial(t);
        }

        public void touchTrial(long t, int x, int y, bool correctDown)
        {
            Trial tl = trials[0];
            tl.touchDownTime = t;
            tl.correctDown = correctDown;
            tl.pts.Add(new PointTime(x, y, t));
            Global.logMessage("Down Time: " + t);
        }

        public void pointTrial(long t, int x, int y)
        {
            Trial tl = trials[0];
            tl.pts.Add(new PointTime(x, y, t));
        }

        public void releaseTrial(long t, int x, int y, bool correctUp)
        {
            Trial tl = trials[0];
            tl.touchUpTime = t;
            tl.pts.Add(new PointTime(x, y, t));
            Global.logMessage("Up Time: " + t);
        }

        void endTrial(long t)
        {
            Trial tl = trials[0];
            tl.endTime = t;
            Global.logMessage("END: " + t);
            trialsDone.Add(new Trial(tl));
        }

        public Trials()
        {

        }

    }
}