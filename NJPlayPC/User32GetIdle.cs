using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.InteropServices;
using System.ComponentModel;

namespace NJPlayPC
{
    public static class User32GetIdle
    {

        public static TimeSpan GetLastInput()
        {
            var plii = new LASTINPUTINFO();
            plii.cbSize = (uint)Marshal.SizeOf(plii);

            if (GetLastInputInfo(ref plii))
            {
                int idleTime = unchecked(Environment.TickCount - (int)plii.dwTime);
                return TimeSpan.FromMilliseconds(idleTime);
            }
            else
                throw new Win32Exception(Marshal.GetLastWin32Error());
        }

        [DllImport("user32.dll", SetLastError = true)]
        static extern bool GetLastInputInfo(ref LASTINPUTINFO plii);

        struct LASTINPUTINFO
        {
            public uint cbSize;
            public uint dwTime;
        }
    }
}
