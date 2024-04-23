using System;
using System.Runtime.InteropServices;

namespace NetTally
{
    internal partial class NativeMethods
    {
        public const int HWND_BROADCAST = 0xffff;
        public static readonly int WM_NETTALLYUPDATE = RegisterWindowMessage("WM_NETTALLYUPDATE");

        [LibraryImport("user32")]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static partial bool PostMessage(IntPtr hwnd, int msg, IntPtr wparam, IntPtr lparam);

        [LibraryImport("user32", StringMarshalling = StringMarshalling.Utf16)]
        public static partial int RegisterWindowMessage(string message);
    }
}
