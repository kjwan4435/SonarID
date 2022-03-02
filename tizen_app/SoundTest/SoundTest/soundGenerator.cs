using System;
using static FftSharp.Transform;

namespace SoundTest
{
    public class soundGenerator
    {
        public static System.Numerics.Complex[] computeDft(System.Numerics.Complex[] seq)
        {
            int n = seq.Length;
            System.Numerics.Complex[] outseq = new System.Numerics.Complex[n];
            for (int k = 0; k < n; k++)
            {  // For each output element
                double sumreal = 0;
                double sumimag = 0;
                for (int t = 0; t < n; t++)
                {  // For each input element
                    double angle = 2 * Math.PI * t * k / n;
                    sumreal +=  seq[t].Real * Math.Cos(angle) + seq[t].Imaginary * Math.Sin(angle);
                    sumimag += -seq[t].Real * Math.Sin(angle) + seq[t].Imaginary * Math.Cos(angle);
                }
                outseq[k] = new System.Numerics.Complex(sumreal, sumimag);
            }

            return outseq;
        }

        public static System.Numerics.Complex[] zcsequence(int u, int seqLen)
        {
            System.Numerics.Complex[] ret = new System.Numerics.Complex[seqLen];
            for (int i = 0; i < seqLen; i++)
            {
                ret[i] = System.Numerics.Complex.Exp(
                    System.Numerics.Complex.Divide(
                        System.Numerics.Complex.Multiply(
                            (new System.Numerics.Complex(0, -1)),
                            (Math.PI * (double)u * (double)i * (double)(i + 1))),
                        (double)seqLen));
            }
            return ret;
        }

        /*
         * Generate a padded (filtered) ZC seq
         */
        public static System.Numerics.Complex[] generateZCSeq(int u, int len, int paddedSize)
        {
            if (paddedSize < len)
            {
                Global.logMessage("Padded size too short");
                return null;
            }

            try
            {
                // generate a zc sequence
                System.Numerics.Complex[] zcOrgTrunc = zcsequence(u, len); // checked and matches python

                // compute its fft. Apache only includes a DFT, so needed to use a custom imp. checked and matches python
                System.Numerics.Complex[] zcFFT = computeDft(zcOrgTrunc);

                // pad the center up to a size of paddedSize samples
                System.Numerics.Complex[] zcFFTPadded = new System.Numerics.Complex[paddedSize];
                int half = (int)Math.Ceiling((float)len / 2.0);    // Do we have an odd/even problem? CHECK THIS. ONLY CHECKED FOR ODD SEQ LENS
                for (int i = 0; i < half; i++) zcFFTPadded[i] = zcFFT[i];                                      // works
                for (int i = half; i <= zcFFTPadded.Length - half; i++) zcFFTPadded[i] = new System.Numerics.Complex(0, 0);                   // works 
                for (int i = half; i < zcFFT.Length; i++) zcFFTPadded[i + (zcFFTPadded.Length - len)] = zcFFT[i];                 // works

                System.Numerics.Complex[] zcseqUpscaled = new System.Numerics.Complex[paddedSize];

                FftSharp.Complex[] zcFFTPaddedForFftSharp = new FftSharp.Complex[paddedSize];
                for (int i = 0; i < paddedSize; i++)
                {
                    zcFFTPaddedForFftSharp[i].Real = zcFFTPadded[i].Real;
                    zcFFTPaddedForFftSharp[i].Imaginary = zcFFTPadded[i].Imaginary;
                }
          
                IFFT(zcFFTPaddedForFftSharp);

                for (int i = 0; i < paddedSize; i++)
                {
                    zcseqUpscaled[i] = new System.Numerics.Complex(zcFFTPaddedForFftSharp[i].Real,zcFFTPaddedForFftSharp[i].Imaginary);
                }
                return  zcseqUpscaled;
            }
            catch (Exception e)
            {
                Global.logMessage(Convert.ToString(e));
            }

            return null;
        }

        public static double[] generateCarrier(double fileFreq, double carrierFreq, double sampleDur, double volumeConst, System.Numerics.Complex[] data)
        {
            if (sampleDur < 1) sampleDur = 1;
            sampleDur = sampleDur * data.Length;
            double[] y = new double[(int)sampleDur];

            for (int i = 0; i < sampleDur; i++)
                y[i] = volumeConst * ((Math.Cos(2 * Math.PI * carrierFreq * i / fileFreq) * data[i % data.Length].Real)
                                    - (Math.Sin(2 * Math.PI * carrierFreq * i / fileFreq) * data[i % data.Length].Imaginary));

            return y;
        }

        public soundGenerator()
        {
        }
    }
}
