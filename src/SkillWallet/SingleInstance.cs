using System;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows;
using System.Windows.Interop;

namespace SkillWallet {
    // Identify the window by the same per-library key as the mutex. Never match
    // by title: separate libraries may legitimately be open at the same time.
    static class SingleInstance {
        delegate bool EnumProc(IntPtr hwnd,IntPtr state);
        [DllImport("user32.dll")] static extern bool EnumWindows(EnumProc callback,IntPtr state);
        [DllImport("user32.dll",CharSet=CharSet.Unicode)] static extern IntPtr GetProp(IntPtr hwnd,string name);
        [DllImport("user32.dll",CharSet=CharSet.Unicode)] static extern bool SetProp(IntPtr hwnd,string name,IntPtr value);
        [DllImport("user32.dll",CharSet=CharSet.Unicode)] static extern IntPtr RemoveProp(IntPtr hwnd,string name);
        [DllImport("user32.dll",CharSet=CharSet.Unicode)] static extern uint RegisterWindowMessage(string name);
        [DllImport("user32.dll")] static extern bool PostMessage(IntPtr hwnd,uint message,IntPtr wParam,IntPtr lParam);
        [DllImport("user32.dll")] static extern uint GetWindowThreadProcessId(IntPtr hwnd,out uint processId);
        [DllImport("user32.dll")] static extern bool AllowSetForegroundWindow(uint processId);
        [DllImport("user32.dll")] static extern bool SetForegroundWindow(IntPtr hwnd);
        [DllImport("user32.dll")] static extern bool IsIconic(IntPtr hwnd);
        [DllImport("user32.dll")] static extern bool ShowWindow(IntPtr hwnd,int command);
        const string MessageName="SkillWallet.ShowExistingWindow.v1";
        static string Property(string key){return "SkillWallet.Library."+key;}
        internal static bool Wake(string key) {
            string property=Property(key);uint message=RegisterWindowMessage(MessageName);
            for(int attempt=0;attempt<40;attempt++){
                IntPtr found=IntPtr.Zero;EnumWindows((hwnd,state)=>{if(GetProp(hwnd,property)!=IntPtr.Zero){found=hwnd;return false;}return true;},IntPtr.Zero);
                if(found!=IntPtr.Zero){uint pid;GetWindowThreadProcessId(found,out pid);AllowSetForegroundWindow(pid);return PostMessage(found,message,IntPtr.Zero,IntPtr.Zero);}
                Thread.Sleep(50);
            }
            return false;
        }
        internal static void Attach(Window window,string key) {
            window.SourceInitialized+=delegate {
                var handle=new WindowInteropHelper(window).Handle;var source=HwndSource.FromHwnd(handle);string property=Property(key);uint message=RegisterWindowMessage(MessageName);
                SetProp(handle,property,new IntPtr(1));
                HwndSourceHook hook=delegate(IntPtr hwnd,int msg,IntPtr wParam,IntPtr lParam,ref bool handled){
                    if((uint)msg==message){handled=true;window.Dispatcher.BeginInvoke(new Action(()=>{
                        if(IsIconic(handle))ShowWindow(handle,9);if(!window.IsVisible)window.Show();
                        var focus=window.OwnedWindows.Cast<Window>().LastOrDefault(w=>w.IsVisible)??window;
                        focus.Activate();SetForegroundWindow(new WindowInteropHelper(focus).Handle);
                    }));}return IntPtr.Zero;
                };
                source.AddHook(hook);window.Closed+=delegate{RemoveProp(handle,property);source.RemoveHook(hook);};
            };
        }
    }
}
