using System;
using System.IO;
using System.Numerics;
using Tizen;
using Tizen.NUI;
using Tizen.NUI.BaseComponents;
using Tizen.NUI.Components;
using System.ComponentModel;
using System.Collections.Generic;

using System.Threading.Tasks;

namespace FingerID
{
    class Program : NUIApplication
    {
        Window window;

        // INIT PAGE
        public TextField IpEntry;
        public TextField SubIdEntry;
        public Button InitButton;

        // BLOCK INIT PAGE
        public TextLabel BlockInitLabel;
        public Button BlockInitButton;

        // On TRIAL PAGE
        string ImageFilePath = "/opt/usr/apps/org.tizen.example.FingerID/shared/res/";
        public Button Stimuli;

        public Button TrialInitButton;      // TRIAL INIT PAGE
        public TextLabel TrialWaitLabel;    // TRIAL WAIT PAGE
        public TextLabel TrialEndLabel;     // TRIAL END  PAGE
        public TextLabel EndLabel;          // END PAGE

        

        // TRIAL
        Trials trials;
        int numPostures = 1;
        int numTargets = 7;                 // total number of targets
        int numFingers = 3;                 // number of fingers we will consider
        int numRepsBlock = 5;               // number of reps in each block
        int numBlocks = 4;                  // number of blocks in the study

        // SoundPlayer
        Complex[] seq;
        int dur;
        double[] wave;
        dataPlayer player;

        // TcpStreamer
        TcpStreamer tcpStreamer;

        int TIME_FIX = 500;                 // fix time
        int TIME_TRIAL = 3000;              // timeout time
        int TIME_PAUSE = 500;               // pause time

        void Initialize()
        {
            window = new Window(new Rectangle(0, 0, 360, 360));
            window.KeyEvent += OnKeyEvent;
            createElements();
            window.Show();
        }

        public void BlockInitPage()
        {
            BlockInitLabel.Show();
            BlockInitButton.Show();

            if (trials.trialsDone.Count == 0)
            {
                IpEntry.Hide();
                SubIdEntry.Hide();
                InitButton.Hide();
            }
            else
                TrialEndLabel.Hide();
            
            BlockInitLabel.Text = "Block: " + (numBlocks - trials.numberBlocksLeft + 1);
        }

        public void TrialInitPage()
        {
            TrialInitButton.Show();
            int targetNum = trials.trials[0].targetNum;
            int finger = trials.trials[0].finger;
            Position target = getTargetPoint(targetNum);
            Stimuli.Position = target;
            Stimuli.BackgroundImage = ImageFilePath + "hand" + (finger + 1) + ".png";
            Stimuli.EnableGestureDetection(Gesture.GestureType.Tap);
            

            if (trials.trials.Count == numFingers * numPostures * numRepsBlock * numTargets)
            {
                BlockInitLabel.Hide();
                BlockInitButton.Hide();
            }
                
            else
                TrialEndLabel.Hide();
            
        }

        public void TrialWaitPage()
        {
            TrialWaitLabel.Show();
            TrialInitButton.Hide();
        }

        public void OnTrialPage()
        {
            Stimuli.Show();
            window.TouchEvent += OnBackgroundTouchEvent;
            TrialWaitLabel.Hide();
        }

        public void TrialEndPage()
        {
            window.TouchEvent -= OnBackgroundTouchEvent;
            Stimuli.Hide();
            TrialEndLabel.Show();
        }

        public void EndPage()
        {
            TrialEndLabel.Hide();
            EndLabel.Show();
        }


        Position getTargetPoint(int targetNum)
        {
            float diagLength7 = 80;
            switch (targetNum)
            {
                case 0: return new Position(120, 120, 1);
                case 1: return new Position(180, 120-diagLength7, 1);
                case 2: return new Position(220, 120, 1);
                case 3: return new Position(180, 120+diagLength7, 1);
                case 4: return new Position(60, 120 + diagLength7, 1);
                case 5: return new Position(20, 120, 1);
                case 6: return new Position(60, 120 - diagLength7, 1);
            }
            return new Position(999, 999, 1);
        }


        public void InitButtonClicked(object sender, Button.ClickEventArgs e)
        {
            Global.IP_ADDRESS = IpEntry.Text;
            if (SubIdEntry.Text == "") Global.SubId = 0;
            else Global.SubId = Convert.ToInt32(SubIdEntry.Text);
            Global.logMessage("IP: " + Global.IP_ADDRESS + "/ Sub: " + Global.SubId);
            tcpStreamer = new TcpStreamer();
            tcpStreamer.sendInitMsg(Utilities.leftPad(Convert.ToString(Global.SubId), 4) + DateTime.UtcNow.Day + DateTime.UtcNow.Millisecond);
            trials.initTrialLog();
            BlockInitPage();
        }

        public void BlockInitButtonClicked(object sender, Button.ClickEventArgs e)
        {
            if (!trials.initTrials(numPostures, numTargets, numFingers, numRepsBlock))
            { Global.logMessage("No blocks in the study. Ending"); }
            Global.logMessage("Trials generated: " + Convert.ToString(trials.trials.Count));
            TrialInitPage();
        }

        async public void TrialInitButtonClicked(object sender, Button.ClickEventArgs e)
        {
            long validator = -1;
            TrialWaitPage();
            player.play();
            try
            {
                await Task.Delay(500);
                tcpStreamer.startStreaming();
                trials.trials[0].startTime = DateTime.UtcNow.Ticks;
                validator = trials.trials[0].startTime;
                await Task.Delay(500);
                OnTrialPage();
                await Task.Delay(3000);
            }
            catch (Exception error)
            {
                Global.logMessage(Convert.ToString(error));
            }

            if (trials.trials.Count != 0)
                if (trials.trials[0].touchDownTime == -1 && validator == trials.trials[0].startTime)
                {
                    Global.logMessage("TIMEOUT CALLED");
                    TrialEndPage();
                    trialFinish();
                }
        }

        async void autoInit()
        {
            long validator = -1;
            TrialWaitPage();
            player.play();
            try
            {
                await Task.Delay(500);
                tcpStreamer.startStreaming();
                trials.trials[0].startTime = DateTime.UtcNow.Ticks;
                validator = trials.trials[0].startTime;
                await Task.Delay(500);
                OnTrialPage();
                await Task.Delay(3000);
            }
            catch (Exception error)
            {
                Global.logMessage(Convert.ToString(error));
            }

            if (trials.trials.Count != 0)
                if (trials.trials[0].touchDownTime == -1 && validator == trials.trials[0].startTime)
                {
                    Global.logMessage("TIMEOUT CALLED");
                    TrialEndPage();
                    trialFinish();
                }
        }

        void OnStimuliClickedEvent()
        {
            trials.trials[0].touchDownTime = DateTime.UtcNow.Ticks;
            trials.trials[0].correctDown = true;
            trialFinish();
        }

        void OnBackgroundClickedEvent()
        {
            trials.trials[0].touchDownTime = DateTime.UtcNow.Ticks;
            trialFinish();
        }

        async void trialFinish()
        {
            //await Task.Delay(500);
            tcpStreamer.stop();
            player.stop();
            trials.trials[0].endTime = DateTime.UtcNow.Ticks;
            tcpStreamer.transferData();
            //tcpStreamer.receiveData();
            //TrialEndLabel.Text = Global.CurrentFinger;
            TrialEndPage();
            Global.logMessage(trials.trials[0].correctDown + " | t: " + (trials.trials[0].touchDownTime - trials.trials[0].startTime) / 10000);
            tcpStreamer.sendBlockMsg(trials.trials[0]);
            TcpStreamer.indexer += 1;
            trials.trialsDone.Add(trials.trials[0]);
            trials.trials.RemoveAt(0);
            await Task.Delay(500);
            Global.logMessage("TrialsDone: " + trials.trialsDone.Count + ", Left: " + trials.trials.Count);
            await Task.Delay(500);
            if (trials.trials.Count == 0)
            {
                Global.logMessage("Block Left: " + trials.numberBlocksLeft);
                if (trials.numberBlocksLeft == 0)
                    EndPage();
                else
                    BlockInitPage();
            }
            else
            {
                TrialInitPage();
            }
            
        }

        private void OnKeyEvent(object sender, Window.KeyEventArgs e)
        {
            if (e.Key.State == Key.StateType.Down && (e.Key.KeyPressedName == "XF86Back" || e.Key.KeyPressedName == "Escape"))
            {
                Exit();
            }
        }

        public bool OnStimuliTouchEvent(object sender, View.TouchEventArgs e)
        {
            int x = (int)e.Touch.GetScreenPosition(0).X;
            int y = (int)e.Touch.GetScreenPosition(0).Y;
            long t = DateTime.UtcNow.Ticks;
            trials.trials[0].pts.Add(new PointTime(x, y, t));

            if (e.Touch.GetState(0) == PointStateType.Down && trials.trials[0].pts.Count == 1)
            {

                Global.logMessage("DOWN: " + e.Touch.GetScreenPosition(0).X + ", " + Convert.ToString(e.Touch.GetScreenPosition(0).Y));
                //OnStimuliClickedEvent();
            }
            else if (e.Touch.GetState(0) == PointStateType.Up)
            {
               
                OnStimuliClickedEvent();
                Global.logMessage("UP: " + e.Touch.GetScreenPosition(0).X + ", " + Convert.ToString(e.Touch.GetScreenPosition(0).Y));
            }
            return true;
        }

        public void OnBackgroundTouchEvent(object sender, Window.TouchEventArgs e)
        {
            int x = (int)e.Touch.GetScreenPosition(0).X;
            int y = (int)e.Touch.GetScreenPosition(0).Y;
            long t = DateTime.UtcNow.Ticks;
            trials.trials[0].pts.Add(new PointTime(x, y, t));

            if (e.Touch.GetState(0) == PointStateType.Down && trials.trials[0].pts.Count == 1)
            {
                Log.Info("LOG_TAG", "DOWN: " + e.Touch.GetScreenPosition(0).X + ", " + Convert.ToString(e.Touch.GetScreenPosition(0).Y));
                //OnBackgroundClickedEvent();
            }
            else if (e.Touch.GetState(0) == PointStateType.Up)
            {
                
                OnBackgroundClickedEvent();
                Global.logMessage("UP: " + e.Touch.GetScreenPosition(0).X + ", " + Convert.ToString(e.Touch.GetScreenPosition(0).Y));
            }
        }

        static void Main(string[] args)
        {
            var app = new Program();
            app.Run(args);
        }

        public void createElements()
        {
            IpEntry = new TextField
            {
                Text = "192.168.0.46",
                TextColor = Color.White,
                PlaceholderText = "Enter IP",
                PointSize = 8.0f,
                HorizontalAlignment = HorizontalAlignment.Center,
                PositionY = 80,
            };

            SubIdEntry = new TextField
            {
                PlaceholderText = "Enter Sub ID",
                TextColor = Color.White,
                PlaceholderTextColor = Color.White,
                PointSize = 8.0f,
                HorizontalAlignment = HorizontalAlignment.Center,
                PositionY = 140
            };

            InitButton = new Button();
            InitButton.Text = "START";
            InitButton.PointSize = 8.0f;
            InitButton.TextColor = Color.White;
            InitButton.BackgroundColor = Color.Blue;
            InitButton.Size = new Size(240, 60);
            InitButton.Position = new Position(60, 200);
            InitButton.StateChangedEvent += (s, e) => { InitButton.BackgroundColor = Color.Blue; };
            InitButton.ClickEvent += InitButtonClicked;

            BlockInitLabel = new TextLabel
            {
                Text = "Block: " + (numBlocks - trials.numberBlocksLeft + 1),
                PointSize = 8.0f,
                TextColor = Color.White,
                HorizontalAlignment = HorizontalAlignment.Center,
                PositionY = 120
            };

            BlockInitButton = new Button
            {
                Text = "Start New Block",
                PointSize = 8.0f,
                TextColor = Color.White,
                BackgroundColor = Color.Blue,
                Size = new Size(240, 60),
                Position = new Position(60, 200)
            };
            BlockInitButton.StateChangedEvent += (s, e) => { BlockInitButton.BackgroundColor = Color.Blue; };
            BlockInitButton.ClickEvent += BlockInitButtonClicked;

            TrialInitButton = new Button
            {
                SizeWidth = 360,
                SizeHeight = 360,
                BackgroundColor = Color.Black,
                TextColor = Color.White,
                Text = "TAP TO START",
                PointSize = 8.0f,
                Position = new Position(0, 0),
                IsSelectable = false
            };
            TrialInitButton.StateChangedEvent += (s, e) => {
                TrialInitButton.BackgroundColor = Color.Black;
            };
            TrialInitButton.ClickEvent += TrialInitButtonClicked;

            TrialWaitLabel = new TextLabel
            {
                BackgroundColor = Color.Black,
                TextColor = Color.White,
                Text = "·",
                PointSize = 30.0f,
                HorizontalAlignment = HorizontalAlignment.Center,
                PositionY = 110
            };

            Stimuli = new Button
            {
                SizeWidth = 120,
                SizeHeight = 120,
                PositionZ = 1,
                Text = "",
            };
            Stimuli.TouchEvent += OnStimuliTouchEvent;

            TrialEndLabel = new TextLabel
            {
                //Text = Global.CurrentFinger,
                Text = "Saving...",
                PointSize = 8.0f,
                TextColor = Color.White,
                HorizontalAlignment = HorizontalAlignment.Center,
                PositionY = 160
            };

            EndLabel = new TextLabel
            {
                Text = "END :)",
                PointSize = 8.0f,
                TextColor = Color.White,
                HorizontalAlignment = HorizontalAlignment.Center,
                PositionY = 160
            };

            window.Add(IpEntry);
            window.Add(SubIdEntry);
            window.Add(InitButton);
            window.Add(BlockInitLabel);
            window.Add(BlockInitButton);
            window.Add(Stimuli);
            window.Add(TrialInitButton);
            window.Add(TrialWaitLabel);
            window.Add(TrialEndLabel);
            window.Add(EndLabel);

            BlockInitLabel.Hide();
            BlockInitButton.Hide();
            Stimuli.Hide();
            TrialInitButton.Hide();
            TrialWaitLabel.Hide();
            TrialEndLabel.Hide();
            EndLabel.Hide();
    }

        protected override void OnCreate()
        {
            base.OnCreate();
            seq = soundGenerator.generateZCSeq(63, 127, 1024);
            //byte[] seqByte = File.ReadAllBytes("/home/owner/media/Sounds/zcseqUpscaled.bin");
            //double[] dataDouble = null;
            //try
            //{
            //    using (StreamReader sr = new StreamReader("/home/owner/media/Sounds/sound.csv"))
            //    {
            //        String line = sr.ReadLine();
            //        String[] data = line.Split(',');
            //        dataDouble = Array.ConvertAll(data, Double.Parse);
            //        Global.logMessage("readData: " + data.Length + ", " + dataDouble.Length + ", " +dataDouble[0] + "," + dataDouble[1]);
            //    }
            //}
            //catch(Exception e)
            //{
            //    Global.logMessage("sound loading error");
            //}

            dur = (int)Math.Ceiling((float)(TIME_FIX + TIME_TRIAL + TIME_PAUSE) / 21.33);
            wave = soundGenerator.generateCarrier(48000, 20250, dur, 5000, seq);
            Global.logMessage("WAVE: " + wave.Length + ", " + wave[0] + ", " + wave[1]);
            player = new dataPlayer(wave);
            Global.logMessage("Player prepared");
            trials = new Trials();
            trials.numberBlocksLeft = numBlocks;
            Initialize();
        }
        protected override void OnResume()
        {
            base.OnResume();
            Global.logMessage("OnResume called");
        }
        protected override void OnPause()
        {
            base.OnPause();
            Global.logMessage("OnPause called");
        }
        protected override void OnTerminate()
        {
            base.OnTerminate();
            Global.logMessage("OnTerminate called");
        }
    }

    
}
