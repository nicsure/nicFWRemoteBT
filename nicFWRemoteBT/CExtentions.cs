using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace nicFWRemoteBT
{
    public static class CExtentions
    {
        public static int IfZero(this int i, int if0)
            => i == 0 ? if0 : i;

        public static int Clamp(this int i, int min, int max)
            => i < min ? min : i > max ? max : i;

        public static string ToHexBleAddress(this Guid id)
            => id.ToString("N")[20..].ToUpperInvariant();

        public static byte[] Copy(this byte[] src)
        {
            byte[] dest = new byte[src.Length];
            Array.Copy(src, 0, dest, 0, src.Length);
            return dest;
        }

        public static bool IsEqual(this byte[] a, byte[] b)
        {
            if(a.Length != b.Length) return false;
            for(int i=0; i<a.Length; i++)
            {
                if(a[i] != b[i])
                    return false;
            }
            return true;
        }
        
    }
}
