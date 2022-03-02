
using System;
using System.Text;
using System.Threading;
using System.Net;
using System.Net.Sockets;


using Tizen.Multimedia;

namespace FingerID
{
    public class TcpStreamer
    {
        static RecorderAudioDevice device = RecorderAudioDevice.Mic;
        private static AudioChannel CHANNEL = AudioChannel.Mono;   // use stereo to get top and bottom mics (one/channel)
        private static AudioSampleType FORMAT = AudioSampleType.S16Le;  // standard encoding
        private static int RECORDING_RATE = 48000;
        public static Encoding u8 = Encoding.UTF8;

        // the minimum buffer size needed for audio recording - 7680 for 48000, STEREO(2), PCM_16BIT, or 1/25 of a second (40ms).
        private static int BUFFER_SIZE = new AudioCapture(RECORDING_RATE, CHANNEL, FORMAT).GetBufferSize();

        public static String ip = Global.IP_ADDRESS;
        public static int duration;
        public static int id;
        public static int indexer = 0;

        static AudioRecorder audioRecorder = new AudioRecorder(RecorderAudioCodec.Pcm, RecorderFileFormat.Wav);


        IPAddress serverIP;
        IPEndPoint ipEnd;
        Socket clientSock;
        Thread streamThread;

        public void startStreaming()
        {   
            streamThread = new Thread(new ThreadStart(record));
            streamThread.Start();

        }

        public static void record()
        {
            try
            {
                //audioRecorder.Prepare();
                audioRecorder.Start("/home/owner/media/Sounds/test.wav");
            }
            catch (Exception e)
            {
                Tizen.Log.Info("LOG_TAG", "TCP Streamer RECORD Error: " + Convert.ToString(e));
            }

        }

        public void stop()
        {
            try
            {
                audioRecorder.Commit();
                //audioRecorder.Unprepare();
            }
            catch (Exception e)
            {
                Tizen.Log.Info("LOG_TAG", "TCP Streamer STOP Error: " + Convert.ToString(e));
            }
        }

        private static void OnStateChanged(object sender, RecorderStateChangedEventArgs e)
        {
            if (e.PreviousState == RecorderState.Recording)
                Tizen.Log.Info("LOG_TAG", $"Recorder Previous state = {e.PreviousState }, Current State = {e.CurrentState}");
        }

        private static void OnRecordingLimitReached(object sender, RecordingLimitReachedEventArgs e)
        {
            Tizen.Log.Info("LOG_TAG", $"Recorder Limit type = {e.Type}");
            audioRecorder.Commit();
            //audioRecorder.Unprepare();
        }

        public void transferData()
        {
            try
            {
                clientSock = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);

                string fileName = "test.wav";
                string filePath = "/home/owner/media/Sounds/";
                
                byte[] msg = Encoding.UTF8.GetBytes("SOUND:" + Utilities.leftPad(Convert.ToString(indexer), 4));
                byte[] endmsg = Encoding.UTF8.GetBytes("END" + Utilities.leftPad(Convert.ToString(indexer), 4));
                clientSock.Connect(ipEnd);
                clientSock.Send(msg);
                clientSock.SendFile(filePath + fileName);

                Global.logMessage("File:" + fileName + "has been sent.");
                clientSock.Close();
            }

            catch (Exception e)
            {
                Global.logMessage("Failed to connect" + Convert.ToString(e));
            }
        }

        public void receiveData()
        {
            try
            {
                clientSock = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                byte[] msg = Encoding.UTF8.GetBytes("RECEV:" + Utilities.leftPad(Convert.ToString(indexer), 4));
                clientSock.Connect(ipEnd);
                clientSock.Send(msg);
                byte[] bytes = new byte[256];
                clientSock.Receive(bytes);
                string finger = Encoding.UTF8.GetString(bytes);
                Global.CurrentFinger = finger;
                Global.logMessage(finger);
                clientSock.Close();
            }

            catch (Exception e)
            {
                Global.logMessage("Failed to receive" + Convert.ToString(e));
            }
        }

        public void sendInitMsg(String s)
        {
            try
            {
                clientSock = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                clientSock.Connect(ipEnd);
                byte[] msg = Encoding.UTF8.GetBytes("SUBID:" + s);
                clientSock.Send(msg);
                Global.logMessage("SUB_ID: " + s + " has been sent.");
                clientSock.Close();
            }
            catch (Exception e)
            { Global.logMessage("Failed to connect" + Convert.ToString(e)); }
        }

        public void sendBlockMsg(Trial t)
        {
            try
            {
                clientSock = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                clientSock.Connect(ipEnd);
                String s = indexer + "," + t.posture + "," + t.targetNum + ","
                    + t.finger + "," + t.startTime + "," + t.touchDownTime + "," + t.endTime + ","
                    + t.correctDown;

                int Npts = t.pts.Count;
                String pts = "";
                foreach (PointTime pt in t.pts)
                {
                    pts += pt.x + " " + pt.y + " " + pt.t + " ";
                }

                if (Npts >= 2)
                    s += "," + t.pts[0].x + "," + t.pts[0].y + "," + t.pts[0].t + ","
                    + t.pts[Npts-1].x + "," + t.pts[Npts-1].y + "," + t.pts[Npts-1].t + "," + pts;

                byte[] msg = Encoding.UTF8.GetBytes("BLOCK:" + s);
                clientSock.Send(msg);
                Global.logMessage("SUB_ID: " + s + " has been sent.");
                clientSock.Close();
            }
            catch (Exception e)
            { Global.logMessage("Failed to connect" + Convert.ToString(e)); }
        }

        public TcpStreamer()
        {
            audioRecorder.AudioDevice = device;
            audioRecorder.AudioBitRate = 128000;
            audioRecorder.AudioChannels = 1; // 1 for MONO, 2 for STEREO
            audioRecorder.AudioSampleRate = RECORDING_RATE;
            audioRecorder.SizeLimit = BUFFER_SIZE * 10;
            audioRecorder.TimeLimit = 5;
            audioRecorder.StateChanged += OnStateChanged;
            audioRecorder.RecordingLimitReached += OnRecordingLimitReached;
            audioRecorder.Prepare();
            serverIP = IPAddress.Parse(Global.IP_ADDRESS);
            ipEnd = new IPEndPoint(serverIP, Global.PORT);
            Global.logMessage("TCP Streamer prepared");
        }
    }
}
