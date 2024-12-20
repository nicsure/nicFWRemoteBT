﻿using System;
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

        public static string IfNullOrEmpty(this string str, string def)
            => string.IsNullOrEmpty(str) ? def : str;
        
    }
}
