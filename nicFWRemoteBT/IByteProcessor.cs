﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace nicFWRemoteBT
{
    public interface IByteProcessor
    {
        void ProcessByte(int byt);

        bool IsIdle { get; }
    }
}
