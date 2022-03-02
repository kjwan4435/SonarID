using System;
using System.Text;

namespace SoundTest
{
    public class Utilities
    {
        public static String leftPad(String result, int padNum)
        {
            
            StringBuilder sb = new StringBuilder();
            int rest = padNum - result.Length;
            for (int i = 0; i < rest; i++)
            {
                sb.Append("0");
            }
            sb.Append(result);
            return sb.ToString();
        }

        public Utilities()
        {
        }
    }
}
