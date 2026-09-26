using System;
using System.ComponentModel;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Interop;
using System.Windows.Threading;

namespace SkillWallet {
    public static class NativeClipboard {
        [DllImport("user32.dll",SetLastError=true)] static extern bool OpenClipboard(IntPtr owner);
        [DllImport("user32.dll",SetLastError=true)] static extern bool EmptyClipboard();
        [DllImport("user32.dll",SetLastError=true)] static extern IntPtr SetClipboardData(uint format,IntPtr handle);
        [DllImport("user32.dll")] static extern bool CloseClipboard();
        [DllImport("kernel32.dll",SetLastError=true)] static extern IntPtr GlobalAlloc(uint flags,UIntPtr size);
        [DllImport("kernel32.dll",SetLastError=true)] static extern IntPtr GlobalLock(IntPtr handle);
        [DllImport("kernel32.dll")] static extern bool GlobalUnlock(IntPtr handle);
        [DllImport("kernel32.dll")] static extern IntPtr GlobalFree(IntPtr handle);
        public static void Write(IntPtr owner,string text) {
            byte[] bytes=Encoding.Unicode.GetBytes(text+"\0");IntPtr handle=GlobalAlloc(0x42,(UIntPtr)bytes.Length);if(handle==IntPtr.Zero)throw new Win32Exception(Marshal.GetLastWin32Error());
            bool opened=false;try {
                IntPtr ptr=GlobalLock(handle);if(ptr==IntPtr.Zero)throw new Win32Exception(Marshal.GetLastWin32Error());
                try{Marshal.Copy(bytes,0,ptr,bytes.Length);}finally{GlobalUnlock(handle);}
                if(!OpenClipboard(owner))throw new Win32Exception(Marshal.GetLastWin32Error());opened=true;
                if(!EmptyClipboard()||SetClipboardData(13,handle)==IntPtr.Zero)throw new Win32Exception(Marshal.GetLastWin32Error());handle=IntPtr.Zero;
            }finally{if(opened)CloseClipboard();if(handle!=IntPtr.Zero)GlobalFree(handle);}
        }
    }
    public sealed class CopyAttempt : IDisposable {
        readonly Action write;readonly Action<Exception,int> complete;readonly DispatcherTimer timer=new DispatcherTimer();int attempts;bool canceled;
        public CopyAttempt(Action writer,Action<Exception,int> completion){write=writer;complete=completion;timer.Tick+=delegate{timer.Stop();Try();};}
        public void Start(){Try();}
        void Try(){if(canceled)return;attempts++;try{write();complete(null,attempts);}catch(Exception ex){if((ex is Win32Exception||ex is ExternalException)&&attempts<7){timer.Interval=TimeSpan.FromMilliseconds(Math.Min(280,45*attempts));timer.Start();}else complete(ex,attempts);}}
        public void Dispose(){canceled=true;timer.Stop();}
    }
    public partial class MainWindow {
        CopyAttempt copyAttempt;Window manualCopyWindow;
        void CopyWithRetry(Skill skill) {
            if(copyAttempt!=null)copyAttempt.Dispose();PulseCard(skill.id);
            if(testMode){LastCopied=skill.content;CopyCount++;ShowCopied(skill);return;}
            copyAttempt=new CopyAttempt(()=>NativeClipboard.Write(new WindowInteropHelper(this).Handle,skill.content),(error,tries)=>{
                if(error==null){LastCopied=skill.content;CopyCount++;ShowCopied(skill);if(manualCopyWindow!=null)manualCopyWindow.Close();}
                else {LogCopyError(error,tries);Notify("复制未成功，可手动选择正文或保存为文本。");ShowManualCopy(skill,error);}
            });copyAttempt.Start();
        }
        void LogCopyError(Exception error,int tries){try{string path=System.IO.Path.Combine(System.IO.Path.GetDirectoryName(dataPath),"clipboard-errors.log");if(File.Exists(path)&&new FileInfo(path).Length>64000)File.Delete(path);var win=error as Win32Exception;File.AppendAllText(path,DateTime.UtcNow.ToString("o")+" type="+error.GetType().Name+" hresult="+error.HResult.ToString("X8")+" native="+(win==null?"n/a":win.NativeErrorCode.ToString())+" attempts="+tries+Environment.NewLine);}catch{}}
        void ShowManualCopy(Skill skill,Exception error) {
            if(manualCopyWindow!=null){manualCopyWindow.Close();manualCopyWindow=null;}
            var window=new Window {Owner=this,Title="复制正文",Width=620,Height=510,WindowStartupLocation=WindowStartupLocation.CenterOwner,Background=UI.Paper,ShowInTaskbar=false};manualCopyWindow=window;
            var layout=new DockPanel {Margin=new Thickness(24)};window.Content=layout;
            var tip=UI.Text("多次尝试后仍无法写入剪贴板。可选中文字手动复制，或保存为 TXT。\n错误码："+error.HResult.ToString("X8"),13,UI.Muted,false);tip.Margin=new Thickness(0,0,0,15);DockPanel.SetDock(tip,Dock.Top);layout.Children.Add(tip);
            var buttons=UI.Stack(true);buttons.HorizontalAlignment=HorizontalAlignment.Right;buttons.Margin=new Thickness(0,15,0,0);DockPanel.SetDock(buttons,Dock.Bottom);layout.Children.Add(buttons);
            var body=new TextBox {Text=skill.content,IsReadOnly=true,AcceptsReturn=true,TextWrapping=TextWrapping.Wrap,VerticalScrollBarVisibility=ScrollBarVisibility.Auto};layout.Children.Add(body);
            buttons.Children.Add(UI.Button("保存 TXT",delegate{var dialog=new Microsoft.Win32.SaveFileDialog {Filter="文本文件|*.txt",FileName="Skill.txt"};if(dialog.ShowDialog(window)==true)try{File.WriteAllText(dialog.FileName,skill.content,new UTF8Encoding(false));}catch(Exception ex){Dialogs.Show(window,ex.Message,"保存失败");}}));
            buttons.Children.Add(UI.Button("全选正文",delegate{body.Focus();body.SelectAll();}));buttons.Children.Add(UI.Button("重试复制",delegate{CopyWithRetry(skill);},true));
            window.Closed+=delegate{if(manualCopyWindow==window)manualCopyWindow=null;};window.SourceInitialized+=delegate{DarkWindowFrame.Apply(window);};window.Show();body.Focus();body.SelectAll();
        }
    }
}
