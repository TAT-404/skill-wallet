using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;

namespace SkillWallet {
    public partial class MainWindow {
        DockPanel header,footer;StackPanel intro,nav,controls,footerControls;
        Grid searchWrap;TextBlock brand;ScrollViewer filterScroll;
        Button importButton,exportButton,addButton,toolsButton,searchButton,categoryButton;
        bool shortLayout,narrowLayout,tinyLayout;double compactPhase;
        Action finishMenu;Grid menuLayer;Border menuShell;Button menuAnchor;Point menuStart;double menuStartWidth,menuStartHeight;
        string copiedId;double copiedUntil;bool preserveOrderVisuals;
        void InitializeResponsive(){SizeChanged+=delegate{UpdateResponsive();};UpdateResponsive();}
        static bool Threshold(bool state,double size,double point){return state?size<point+22:size<point-22;}
        void BuildCompactActions(StackPanel actions) {
            searchButton=UI.Button("⌕",()=>ShowSearchMenu());searchButton.Margin=new Thickness(0,0,8,0);searchButton.Width=36;searchButton.Height=36;searchButton.Padding=new Thickness(0);searchButton.FontSize=22;searchButton.ToolTip="搜索技能";searchButton.Visibility=Visibility.Collapsed;actions.Children.Insert(0,searchButton);
            toolsButton=UI.Button("⚙",ShowToolsMenu);toolsButton.Width=36;toolsButton.Height=34;toolsButton.FontSize=16;toolsButton.Padding=new Thickness(0);toolsButton.Margin=new Thickness(8,0,0,0);toolsButton.ToolTip="工具与音效";actions.Children.Add(toolsButton);
        }
        void UpdateResponsive() {
            if(stage==null)return;
            shortLayout=Threshold(shortLayout,ActualHeight>0?ActualHeight:Height,740);
            narrowLayout=Threshold(narrowLayout,ActualWidth>0?ActualWidth:Width,1000);
            tinyLayout=Threshold(tinyLayout,ActualWidth>0?ActualWidth:Width,800);
            importButton.Visibility=exportButton.Visibility=narrowLayout?Visibility.Collapsed:Visibility.Visible;
            searchWrap.Visibility=narrowLayout?Visibility.Collapsed:Visibility.Visible;searchButton.Visibility=narrowLayout?Visibility.Visible:Visibility.Collapsed;
            brand.Visibility=tinyLayout?Visibility.Collapsed:Visibility.Visible;
            addButton.Content=tinyLayout?"＋":"＋ 新建";addButton.ToolTip="新建技能";
            filterScroll.Visibility=tinyLayout?Visibility.Collapsed:Visibility.Visible;categoryButton.Visibility=tinyLayout?Visibility.Visible:Visibility.Collapsed;
            browseHint.Visibility=narrowLayout||shortLayout?Visibility.Collapsed:Visibility.Visible;
            status.Visibility=narrowLayout||shortLayout?Visibility.Collapsed:Visibility.Visible;
            foreach(var toggle in railSwitches)toggle.Visibility=shortLayout?Visibility.Collapsed:Visibility.Visible;
            toolsButton.Visibility=HasOverflowTools?Visibility.Visible:Visibility.Collapsed;
            main.Margin=new Thickness(tinyLayout?20:38,shortLayout?14:24,tinyLayout?20:38,shortLayout?8:18);
            if(menuLayer!=null)CloseMenu();
            if(!IsLoaded||!SystemParameters.ClientAreaAnimation){compactPhase=shortLayout?1:0;ApplyCompactFrame();}else StartRendering();
        }
        void AdvanceResponsive(double dt){double target=shortLayout?1:0;if(Math.Abs(compactPhase-target)>.001){compactPhase+=(target-compactPhase)*(1-Math.Exp(-dt*17));interactionMoving=true;}else compactPhase=target;ApplyCompactFrame();}
        void ApplyCompactFrame() {
            double t=compactPhase;
            intro.ClipToBounds=true;intro.Height=56*(1-t);intro.Opacity=1-t;intro.Margin=new Thickness(0,0,0,18*(1-t));
            header.Margin=new Thickness(0,0,0,26-14*t);
            controls.Height=35*(1-t);controls.Opacity=1-t;controls.ClipToBounds=true;controls.IsHitTestVisible=t<.5;
            dots.Margin=new Thickness(0,12-8*t,0,0);nav.Margin=new Thickness(0,8-4*t,0,22-15*t);
            footer.Margin=new Thickness(0,0,0,0);
        }
        void CloseMenu() {
            if(menuLayer==null)return;var old=menuLayer;var shell=menuShell;menuLayer=null;menuShell=null;menuAnchor=null;main.IsEnabled=overlay==null;
            old.IsHitTestVisible=false;var transform=shell.RenderTransform as TranslateTransform;if(transform!=null){MotionFx.To(transform,TranslateTransform.XProperty,menuStart.X,.18);MotionFx.To(transform,TranslateTransform.YProperty,menuStart.Y,.18);}MotionFx.Width(shell,menuStartWidth,.18);MotionFx.Height(shell,menuStartHeight,.18);MotionFx.Fade(old,0,.16,()=>root.Children.Remove(old));
        }
        StackPanel OpenMenu(Button anchor,double width) {
            if(menuLayer!=null&&menuAnchor==anchor){CloseMenu();return null;}CloseMenu();
            menuAnchor=anchor;main.IsEnabled=false;menuLayer=new Grid {Background=Brushes.Transparent};Panel.SetZIndex(menuLayer,600);root.Children.Add(menuLayer);
            var layer=menuLayer;layer.MouseLeftButtonDown+=delegate(object sender,MouseButtonEventArgs e){if(e.OriginalSource==layer){CloseMenu();e.Handled=true;}};
            var origin=anchor.TranslatePoint(new Point(0,anchor.ActualHeight),root);double x=Math.Max(12,Math.Min(root.ActualWidth-width-12,origin.X));
            var rows=UI.Stack(false);menuShell=new Border {Width=width,HorizontalAlignment=HorizontalAlignment.Left,VerticalAlignment=VerticalAlignment.Top,Margin=new Thickness(x,origin.Y+8,0,0),Padding=new Thickness(8),Background=UI.B("#F51C1C1C"),BorderBrush=UI.B("#494949"),BorderThickness=new Thickness(.7),CornerRadius=new CornerRadius(13),Child=rows,RenderTransformOrigin=new Point(Math.Max(0,Math.Min(1,(origin.X-x+anchor.ActualWidth/2)/width)),0)};
            menuStart=new Point(origin.X-x,-anchor.ActualHeight-8);menuStartWidth=Math.Max(36,anchor.ActualWidth);menuStartHeight=Math.Max(32,anchor.ActualHeight);menuShell.RenderTransform=new TranslateTransform(menuStart.X,menuStart.Y);menuShell.ClipToBounds=true;var shell=menuShell;shell.Width=menuStartWidth;shell.Height=menuStartHeight;layer.Children.Add(shell);layer.Opacity=0;finishMenu=delegate {rows.Measure(new Size(width-18,Double.PositiveInfinity));double height=rows.DesiredSize.Height+18;MotionFx.Width(shell,width,.25);MotionFx.Height(shell,height,.29);MotionFx.To((TranslateTransform)shell.RenderTransform,TranslateTransform.XProperty,0,.25);MotionFx.To((TranslateTransform)shell.RenderTransform,TranslateTransform.YProperty,0,.29);MotionFx.Fade(layer,1,.17);};
            return rows;
        }
        void FinishMenu(){if(finishMenu!=null){var finish=finishMenu;finishMenu=null;finish();}}
        void MenuItem(StackPanel rows,string text,Action action,bool active=false) {var button=UI.Button(text,()=>{CloseMenu();action();});button.HorizontalContentAlignment=HorizontalAlignment.Left;button.Padding=new Thickness(12,9,12,9);button.Margin=new Thickness(0,2,0,2);button.BorderThickness=new Thickness(0);button.Background=active?UI.B("#373737"):Brushes.Transparent;rows.Children.Add(button);}
        bool ImportHidden {get{return importButton.Visibility!=Visibility.Visible;}}
        bool ExportHidden {get{return exportButton.Visibility!=Visibility.Visible;}}
        bool SoundHidden {get{return railSwitches.Count>0&&railSwitches.All(t=>t.Visibility!=Visibility.Visible);}}
        bool HasOverflowTools {get{return ImportHidden||ExportHidden||SoundHidden;}}
        void ShowToolsMenu() {
            if(!HasOverflowTools)return;
            var rows=OpenMenu(toolsButton,222);if(rows==null)return;
            if(ImportHidden)MenuItem(rows,"导入技能库",Import);
            if(ExportHidden)MenuItem(rows,"导出技能库",Export);
            if(SoundHidden){
                if(rows.Children.Count>0)rows.Children.Add(new Border {Height=1,Margin=new Thickness(9,7,9,7),Background=UI.B("#363636")});
                var sound=new RailSwitch("滑动音效",preferences.sound,value=>{preferences.sound=value;SavePreferences();foreach(var toggle in railSwitches)toggle.SetValue(value);if(value){PrepareSound();}else if(soundPlayer!=null){soundPlayer.Dispose();soundPlayer=null;}});sound.Margin=new Thickness(8);rows.Children.Add(sound);
            }
            FinishMenu();
        }
        void ShowCategoryMenu() {
            var rows=OpenMenu(categoryButton,230);if(rows==null)return;
            var list=UI.Stack(false);var scroll=new ScrollViewer {MaxHeight=Math.Max(120,root.ActualHeight-260),Content=list,VerticalScrollBarVisibility=ScrollBarVisibility.Auto};rows.Children.Add(scroll);
            foreach(var name in new[]{"全部"}.Concat(skills.Select(s=>s.category).Distinct())){var capture=name;MenuItem(list,name,()=>{SelectCategory(capture);},category==name);}
            rows.Children.Add(UI.Rule());MenuItem(rows,favorites?"✓ 仅看收藏":"☆ 仅看收藏",()=>{favorites=!favorites;selected=0;RebuildFilters();Refresh(true);},favorites);FinishMenu();
        }
        void ShowSearchMenu() {
            var rows=OpenMenu(searchButton,Math.Min(340,root.ActualWidth-40));if(rows==null)return;
            var field=new TextBox {Text=search.Text,Padding=new Thickness(12),ToolTip="搜索名称、标签或正文"};rows.Children.Add(field);field.TextChanged+=delegate{search.Text=field.Text;};field.Loaded+=delegate{field.Focus();field.SelectAll();};FinishMenu();
        }
        void ShowCopied(Skill skill){copiedId=skill.id;copiedUntil=motionClock.Elapsed.TotalSeconds+1.6;Position(false);NotifyCopied();var timer=new System.Windows.Threading.DispatcherTimer {Interval=TimeSpan.FromSeconds(1.65)};timer.Tick+=delegate{timer.Stop();Position(false);};timer.Start();}
        bool CommitOrder(List<Skill> updated,string message) {
            try {Packs.Write(dataPath,updated);ClearUndo();skills=updated;var ids=new HashSet<string>(filtered.Select(s=>s.id));filtered=skills.Where(s=>ids.Contains(s.id)).ToList();foreach(var c in cards)c.Index=filtered.FindIndex(s=>s.id==c.Skill.id);UpdateManagementBar();UpdateNav();status.Text="● 已保存 · "+DateTime.Now.ToString("HH:mm");Notify(message);return true;}
            catch(Exception ex){Dialogs.Show(this,"未能保存，修改尚未写入磁盘。\n"+ex.Message,"保存失败");return false;}
        }
    }
}
