using System;
using System.Threading;

using Tizen.Multimedia;

namespace SoundTest
{
    public class dataPlayer
    {
        AudioPlayback audioPlayback;
        byte[] generatedTone;
        double[] sample = null;    // sound as double vals

        public void play()
        {
            Thread playStreamThread = new Thread(new ThreadStart(playThread));
            playStreamThread.Start();
        }

        void OnBufferAvailable(object sender, AudioPlaybackBufferAvailableEventArgs args)
        {
            if (args.Length > 0)
            {
                try
                {
                    /// Copy the audio data from the local buffer
                    /// to the internal output buffer (starts playback)
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
            audioPlayback.Prepare();
            //File.WriteAllBytes("/home/owner/media/Sounds/generatedTone.bin", generatedTone);
        }

        public void stop()
        {
            audioPlayback.Unprepare();
        }


        void setData(double[] dataIn)
        {
                sample = new double[dataIn.Length];
                for (int i = 0; i < dataIn.Length; i++) sample[i] = dataIn[i];
        }

        public byte[] genTone(double[] sample)
        {
            byte[] generatedSnd = new byte[2 * sample.Length];
            Global.logMessage("genTone called");
            int idx = 0;
            foreach (double dVal in sample)
            {
                try
                {
                    short val = (short)((dVal * 25.0));  // in 16 bit wav PCM, first byte is the low order byte
                    generatedSnd[idx++] = (byte)(val & 0x00ff);
                    generatedSnd[idx++] = (byte)((uint)(val & 0xff00) >> 8);
                }
                catch(Exception e)
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
