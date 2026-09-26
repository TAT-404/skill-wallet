using System;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
namespace SkillWallet {
    static class DarkWindowFrame {
        [DllImport("dwmapi.dll")] static extern int DwmSetWindowAttribute(IntPtr hwnd,int attribute,ref int value,int size);
        public static void Apply(Window window) {
            try {var handle=new WindowInteropHelper(window).Handle;int dark=1;DwmSetWindowAttribute(handle,20,ref dark,4);}catch(DllNotFoundException){}catch(EntryPointNotFoundException){}
        }
    }
}
