using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace SkillWallet {
    public static class Dialogs {
        public static MessageBoxResult Show(Window owner,string message,string caption,MessageBoxButton buttons=MessageBoxButton.OK,MessageBoxImage image=MessageBoxImage.None) {
            var result=MessageBoxResult.Cancel;bool ask=buttons==MessageBoxButton.YesNo||buttons==MessageBoxButton.YesNoCancel||buttons==MessageBoxButton.OKCancel;
            var window=new Window {Owner=owner,Title=caption,Width=Math.Min(440,Math.Max(320,owner.ActualWidth-32)),SizeToContent=SizeToContent.Height,MaxHeight=Math.Max(300,owner.ActualHeight-24),ResizeMode=ResizeMode.NoResize,WindowStartupLocation=WindowStartupLocation.CenterOwner,Background=UI.Paper,FontFamily=UI.Typeface,ShowInTaskbar=false};
            var layout=new DockPanel {Margin=new Thickness(25)};window.Content=layout;
            var title=UI.Text(caption,20,UI.Ink,true);title.Margin=new Thickness(0,0,0,16);DockPanel.SetDock(title,Dock.Top);layout.Children.Add(title);
            var actions=UI.Stack(true);actions.HorizontalAlignment=HorizontalAlignment.Right;actions.Margin=new Thickness(0,22,0,0);DockPanel.SetDock(actions,Dock.Bottom);layout.Children.Add(actions);
            if(ask){var cancel=UI.Button("取消",()=>{result=MessageBoxResult.No;window.Close();});cancel.IsCancel=true;cancel.Margin=new Thickness(0,0,10,0);actions.Children.Add(cancel);}
            string label=ask?(caption.Contains("删除")?"删除":caption.Contains("未保存")?"放弃修改":"确认"):"知道了";
            var confirm=UI.Button(label,()=>{result=ask?MessageBoxResult.Yes:MessageBoxResult.OK;window.Close();},true);confirm.IsDefault=!ask;actions.Children.Add(confirm);
            var body=UI.Text(message,14,UI.Muted,false);body.LineHeight=24;layout.Children.Add(new ScrollViewer {Content=body,VerticalScrollBarVisibility=ScrollBarVisibility.Auto,MaxHeight=Math.Max(100,owner.ActualHeight-200)});
            window.SourceInitialized+=delegate{DarkWindowFrame.Apply(window);};window.Loaded+=delegate{MotionFx.Fade(layout,1,.18);};window.ShowDialog();return result;
        }
    }
}
