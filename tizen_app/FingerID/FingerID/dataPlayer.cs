using System;
using System.Threading;

using Tizen.Multimedia;

namespace FingerID
{
    public class dataPlayer
    {
        AudioPlayback audioPlayback;
        byte[] generatedTone;
        double[] sample = null;    // sound as double vals
        Thread playStreamThread;

        public void play()
        {
            //playThread();
            try
            {
                playStreamThread = new Thread(new ThreadStart(playThread));
                playStreamThread.Start();
            }
            catch (Exception e)
            {
                Global.logMessage("Failed to write. " + e);
            }
        }

        void OnBufferAvailable(object sender, AudioPlaybackBufferAvailableEventArgs args)
        {
            if (args.Length > 0)
            {
                try
                {
                    audioPlayback.Write(generatedTone);
                }
                catch (Exception e)
                {
                    Global.logMessage("Failed to write. " + e);
                }
            }
        }

        public void playThread()
        {
            try
            {
                audioPlayback.Prepare();
            }
            catch (Exception e)
            {
                Global.logMessage("Failed to write. " + e);
            }
            //File.WriteAllBytes("/home/owner/media/Sounds/generatedTone.bin", generatedTone);
        }

        public void stop()
        {
            try
            {
                audioPlayback.Unprepare();
            }
            catch (Exception e)
            {
                Global.logMessage("Failed to stop player. " + e);
            }
        }


        void setData(double[] dataIn)
        {
            sample = new double[dataIn.Length];
            for (int i = 0; i < dataIn.Length; i++) sample[i] = dataIn[i];
        }

        public byte[] genTone(double[] sample)
        {
            byte[] generatedSnd = new byte[2 * sample.Length];
            int idx = 0;
            foreach (double dVal in sample)
            {
                try
                {
                    short val = (short)((dVal * 25.0));  // in 16 bit wav PCM, first byte is the low order byte
                    generatedSnd[idx++] = (byte)(val & 0x00ff);
                    generatedSnd[idx++] = (byte)(short)((ushort)(val & 0xff00) >> 8);
                }
                catch (Exception e)
                {
                    Global.logMessage("at for each error: " + Convert.ToString(e));
                }

            }
            return generatedSnd;
        }

        public dataPlayer(double[] dataIn)
        {
            audioPlayback = new AudioPlayback(48000, AudioChannel.Mono, AudioSampleType.S16Le);
            audioPlayback.BufferAvailable += OnBufferAvailable;
            setData(dataIn);
            generatedTone = genTone(sample);
            Global.logMessage("Tone generated");
            
        }

        public dataPlayer()
        {

        }

    }
}
