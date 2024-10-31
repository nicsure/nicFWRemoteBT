using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace nicFWRemoteBT
{
    public interface IPlatformManager
    {        
        void SetOrientation(int orientation);
    }

    public static class PlatformManager
    {
        public static IPlatformManager? Instance { get; set; } = null;
        public static void SetOrientation(int orientation)
        {
            Instance?.SetOrientation(orientation);
        }
    }
}
