//contains code to instantiate your common application within the Tizen framework.

using System;
using System.Linq;
using System.Numerics;
using System.Threading.Tasks;
using Tizen;
using Xamarin.Forms;


namespace SoundTest
{
    public static class Global
    {
        public static int PORT = 50005;
        public static String IP_ADDRESS = null;
        public static int SubId = 9999;
        public static void logMessage(String str) { Log.Info("LOG_TAG", str); }
    }

    public class App : Application
    {
        Label label;
        Entry IpEntry;
        Entry SubIdEntry;
        Button InitButton;
        Button InitBlockButton;
        Button TrialInitButton;
        ImageButton Stimuli;

        string ImageFilePath = "/opt/usr/apps/org.tizen.example.SoundTest.Tizen.Wearable/shared/res/";

        int TIME_FIX = 500;  // fix time
        int TIME_TRIAL = 3000; // timeout time
        int TIME_PAUSE = 500;

        int numPostures = 1;
        int numTargets = 1;    // total number of targets
        int numFingers = 3;                      // number of fingers we will consider (3)
        int numRepsBlock = 20;                      // number of reps in each block       (2)
        int numBlocks = 4;                      // number of blocks in the study.     (2)

        Complex[] seq;
        int dur;
        double[] wave;
        dataPlayer player;
        TcpStreamer tcpStreamer;
        Trials trials;

        public App()
        {
            IpEntry = new Entry
            {
                Text = "192.168.0.46",
                Keyboard = Keyboard.Numeric,
                Placeholder = "Enter IP",
                HorizontalOptions = LayoutOptions.Center,
                Scale = 1
            };

            SubIdEntry = new Entry
            {
                Keyboard = Keyboard.Numeric,
                Placeholder = "Enter SubID",
                HorizontalOptions = LayoutOptions.Center,
                Scale = 1
            };

            InitButton = new Button
            {
                Text = "Start",
                BackgroundColor = Color.Blue,
                HorizontalOptions = LayoutOptions.Center,
                ScaleX = 1
            };
            InitButton.Clicked += OnInitButtonClicked;

            ContentPage InitPage = new ContentPage
            {
                Content = new StackLayout
                {
                    VerticalOptions = LayoutOptions.Center,
                    Children =
                    {
                        IpEntry,
                        SubIdEntry,
                        InitButton
                    }
                }
            };

            MainPage = InitPage;
        }

        public ContentPage BlockInit()
        {
            label = new Label
            {
                Text = "Block: " + (numBlocks - trials.numberBlocksLeft + 1),
                HorizontalOptions = LayoutOptions.Center,
            };

            InitBlockButton = new Button
            {
                Text = "Start New Block",
                BackgroundColor = Color.Blue,
                HorizontalOptions = LayoutOptions.Center
            };
            InitBlockButton.Clicked += OnBlockButtonClicked;

            ContentPage BlockPage = new ContentPage
            {
                Content = new StackLayout
                {
                    VerticalOptions = LayoutOptions.Center,
                    Children =
                    {
                        label,
                        InitBlockButton
                    }
                }
            };
            return BlockPage;
        }

        public ContentPage TrialInit()
        {
            TrialInitButton = new Button
            {
                WidthRequest = 360,
                HeightRequest = 360,
                CornerRadius = 180,
                BackgroundColor = Color.Black,
                TextColor = Color.White,
                Text = "TAP TO START",
                HorizontalOptions = LayoutOptions.Center,
                Scale = 1
            };
            TrialInitButton.Clicked += OnTrialInitButtonClicked;

            ContentPage TrialInitPage = new ContentPage
            {
                Content = new StackLayout
                {
                    VerticalOptions = LayoutOptions.Center,
                    Children = { TrialInitButton }
                }
            };
            return TrialInitPage;
        }

        public ContentPage OnTrialWait()
        {
            label = new Label
            {
                Text = "·",
                TextColor = Color.White,
                HorizontalOptions = LayoutOptions.Center,
                FontAttributes = FontAttributes.Bold,
                Scale = 3
            };

            ContentPage OnTrialWaitPage = new ContentPage
            {
                Content = new StackLayout
                {
                    VerticalOptions = LayoutOptions.Center,
                    Children = { label }
                }
            };
            return OnTrialWaitPage;

        }

        public ContentPage OnTrial()
        {
            int targetNum = trials.trials[0].targetNum;
            int finger = trials.trials[0].finger;
            Point target = getTargetPoint(targetNum);

            Stimuli = new ImageButton
            {
                WidthRequest = 120,
                HeightRequest = 120,
                CornerRadius = 60,
                BackgroundColor = Color.White,
                Source = ImageFilePath + "hand"+ (finger+1) +"_1.png",
                Aspect = Aspect.Fill,
            };

            AbsoluteLayout absoluteLayout = new AbsoluteLayout {};
            absoluteLayout.Children.Add(Stimuli, target);
            ContentPage OnTrialPage = new ContentPage { Content = absoluteLayout };

            TapGestureRecognizer backgroundTap = new TapGestureRecognizer();
            backgroundTap.Tapped += OnTrialBackgroundClicked;
            absoluteLayout.GestureRecognizers.Add(backgroundTap);

            TapGestureRecognizer stimuliTap = new TapGestureRecognizer();
            stimuliTap.Tapped += OnTrialFinishedButtonClicked;
            Stimuli.GestureRecognizers.Add(stimuliTap);

            return OnTrialPage;
        }

        public ContentPage Absent()
        {
            AbsoluteLayout absoluteLayout = new AbsoluteLayout { };
            ContentPage absentPage = new ContentPage { Content = absoluteLayout };

            return absentPage;
        }

        public ContentPage TrialEnd()
        {
            label = new Label
            {
                Text = "END",
                HorizontalOptions = LayoutOptions.Center,
            };

            ContentPage TrialEndPage = new ContentPage
            {
                Content = new StackLayout
                {
                    VerticalOptions = LayoutOptions.Center,
                    Children =
                    {
                        label
                    }
                }
            };
            return TrialEndPage;
        }

        Point getTargetPoint(int targetNum)
        {
            double diagLength = 84.85;
            switch (targetNum)
            {
                case 0: return new Point(120, 120);
                case 1: return new Point(120, 0);
                case 2: return new Point(120 + diagLength, 120 - diagLength);
                case 3: return new Point(240, 120);
                case 4: return new Point(120 + diagLength, 120 + diagLength);
                case 5: return new Point(120, 240);
                case 6: return new Point(120 - diagLength, 120 + diagLength);
                case 7: return new Point(0, 120);
                case 8: return new Point(120 - diagLength, 120 - diagLength);
            }
            return new Point(999, 999);
        }

        void OnInitButtonClicked(object s, EventArgs e)
        {
            Global.IP_ADDRESS = IpEntry.Text;
            Global.SubId = Convert.ToInt32(SubIdEntry.Text);
            Global.logMessage("IP: " + Global.IP_ADDRESS + "/ Sub: " + Global.SubId);
            tcpStreamer = new TcpStreamer();
            tcpStreamer.sendInitMsg(Utilities.leftPad(Convert.ToString(Global.SubId), 4)+DateTime.UtcNow.Day+DateTime.UtcNow.Millisecond);
            trials.initTrialLog();
            MainPage = BlockInit();
        }

        void OnBlockButtonClicked(object s, EventArgs e)
        {
            if (!trials.initTrials(numPostures, numTargets, numFingers, numRepsBlock))
            { Global.logMessage("No blocks in the study. Ending"); }
            Global.logMessage("Trials generated: " + Convert.ToString(trials.trials.Count()));
            MainPage = TrialInit();
        }

        async void OnTrialInitButtonClicked(object s, EventArgs e)
        {
            trials.trials[0].startTime = DateTime.UtcNow.Ticks;
            var validator = trials.trials[0].startTime;
            tcpStreamer.startStreaming();
            player.play();
            MainPage = OnTrialWait();
            await Task.Delay(TIME_FIX);
            MainPage = OnTrial();
            await Task.Delay(TIME_TRIAL);

            if (trials.trials.Count() != 0)
                if (trials.trials[0].touchDownTime == -1 && validator == trials.trials[0].startTime )
                {
                    Global.logMessage("TIMEOUT CALLED");
                    trialFinish();
                }
        }

        void OnTrialFinishedButtonClicked(object s, EventArgs e)
        {
            MainPage = Absent();
            trials.trials[0].correctDown = true;
            trialFinish();
        }

        void OnTrialBackgroundClicked(object s, EventArgs e)
        {
            MainPage = Absent();
            trialFinish();
        }

        void trialFinish()
        {
            trials.trials[0].touchDownTime = DateTime.UtcNow.Ticks;
            Task.Delay(500).Wait();
            player.stop();
            tcpStreamer.stop();
            trials.trials[0].endTime = DateTime.UtcNow.Ticks;
            Task.Delay(500).Wait();

            tcpStreamer.transferData();
            Global.logMessage(trials.trials[0].correctDown + " | t: " + (trials.trials[0].touchDownTime - trials.trials[0].startTime) / 10000);
            tcpStreamer.sendBlockMsg(trials.trials[0]);
            TcpStreamer.indexer += 1;
            trials.trialsDone.Add(trials.trials[0]);
            trials.trials.RemoveAt(0);

            Global.logMessage("TrialsDone: " + trials.trialsDone.Count() + ", Left: " + trials.trials.Count());

            if (trials.trials.Count() == 0)
            {
                Global.logMessage("Block Left: " + trials.numberBlocksLeft);
                if (trials.numberBlocksLeft == 0)
                    MainPage = TrialEnd();
                else
                    MainPage = BlockInit();
            }
            else
                MainPage = TrialInit();
        }

        protected override void OnStart()
        {
            seq = soundGenerator.generateZCSeq(63, 127, 1024);
            dur = (int)Math.Ceiling((float)(TIME_FIX + TIME_TRIAL + TIME_PAUSE) / 21.33);
            wave = soundGenerator.generateCarrier(48000, 20250, dur, 5000, seq);
            player = new dataPlayer(wave);
            Global.logMessage("Player prepared");
            trials = new Trials();
            trials.numberBlocksLeft = numBlocks;
        }
        protected override void OnSleep()  { Global.logMessage("onsleep called"); }
        protected override void OnResume() { Global.logMessage("OnResume called"); }
    }
}
