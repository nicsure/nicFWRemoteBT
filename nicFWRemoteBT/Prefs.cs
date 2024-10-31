using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace nicFWRemoteBT
{
    public static class Prefs
    {

        public static bool Bool(string key)
        {
            return bool.TryParse(Preferences.Get(key, "False"), out bool b) ? b : false; 
        }

        public static void Bool(string key, bool val)
        {
            Preferences.Set(key, val.ToString());
        }

        public static string Str(string key)
        {
            return Preferences.Get(key, string.Empty);
        }

        public static void Str(string key, string val)
        {
            Preferences.Set(key, val);
        }

        public static double Double(string key)
        {
            return double.TryParse(Preferences.Get(key, "0"), out double d) ? d : 0;
        }

        public static void Double(string key, double val)
        {
            Preferences.Set(key, val.ToString());
        }

        public static int Int(string key)
        {
            return int.TryParse(Preferences.Get(key, "0"), out int i) ? i : 0;
        }

        public static void Int(string key, int val)
        {
            Preferences.Set(key, val.ToString());
        }

    }
}
