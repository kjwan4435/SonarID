using System;
using System.Collections.Generic;

namespace FingerID
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
        public int targetX;
        public int targetY;

        public long startTime;
        public long touchDownTime;
        public long endTime;

        public List<PointTime> pts;

        public bool correctDown;

        public Trial(int t, int f, int p)
        {
            posture = p;
            targetNum = t;
            finger = f;
            startTime = touchDownTime = endTime = -1;
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
            endTime = tIn.endTime;
            correctDown = tIn.correctDown;

            pts = new List<PointTime>();
            pts.Clear();
            foreach (PointTime pt in tIn.pts)
            {
                pts.Add(new PointTime(pt.x, pt.y, pt.t));
            }
        }

        bool correct() { return correctDown; }
    }

    public class Trials
    {
        public List<Trial> trials;
        public List<Trial> trialsDone;
        public int numberBlocksLeft;

        public void initTrialLog()
        {
            trialsDone = new List<Trial>();
         }

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

        public Trials()
        {

        }

    }
}