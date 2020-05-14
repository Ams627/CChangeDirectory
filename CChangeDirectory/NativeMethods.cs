using System;
using System.Runtime.InteropServices;
using System.Text;

namespace CChangeDirectory
{

    internal class NativeMethods
    {
        [Flags]
        public enum DefineDosDeviceFlags
        {
            DDD_RAW_TARGET_PATH = 1,
            DDD_REMOVE_DEFINITION = 2,
            DDD_EXACT_MATCH_ON_REMOVE = 4,
            DDD_NO_BROADCAST_SYSTEM = 8
        };


        [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        public static extern int DefineDosDevice(int flags, string devname, string path);
        [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        public static extern int QueryDosDevice(string devname, StringBuilder buffer, int bufSize);
    }
}