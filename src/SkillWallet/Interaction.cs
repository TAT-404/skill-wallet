using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using System.Windows.Threading;

namespace SkillWallet {
    public partial class MainWindow {
        Preferences preferences;SoftSound soundPlayer;DispatcherTimer holdTimer;double layoutWidth,layoutHeight;bool interactionMoving;readonly List<RailSwitch> railSwitches=new List<RailSwitch>();
        bool slideSoundArmed;int slideSoundRequests;
        double filterX;List<Skill> undoSnapshot;string undoFocus;Button undoButton;bool suppressUndoClear;
        void InitializeInteraction(){preferences.feedback=true;Loaded+=delegate{PrepareSound();};holdTimer=new DispatcherTimer {Interval=TimeSpan.FromMilliseconds(350)};holdTimer.Tick+=delegate {ActivateHold();};}
        void DisposeInteraction(){CancelHold();if(copyAttempt!=null)copyAttempt.Dispose();if(soundPlayer!=null)soundPlayer.Dispose();}
        void BuildFeedbackControls(StackPanel host) {
            undoButton=UI.Button("撤销",delegate{UndoLast();});undoButton.FontSize=10;undoButton.Padding=new Thickness(8,3,8,3);undoButton.Visibility=Visibility.Collapsed;host.Children.Add(undoButton);
            railSwitches.Add(new RailSwitch("滑动音效",preferences.sound,value=>{preferences.sound=value;SavePreferences();if(value){PrepareSound();}else if(soundPlayer!=null){soundPlayer.Dispose();soundPlayer=null;}}));foreach(var toggle in railSwitches)host.Children.Add(toggle);
        }
        void SavePreferences(){try{preferences.Save(System.IO.Path.Combine(System.IO.Path.GetDirectoryName(dataPath),"preferences.json"));}catch{Notify("设置暂未保存，下次启动将使用原设置。");}}
        void PlaySlideSound(){if(!preferences.sound||WindowState==WindowState.Minimized)return;if(testMode){slideSoundRequests++;return;}if(soundPlayer==null)soundPlayer=new SoftSound(System.IO.Path.Combine(System.IO.Path.GetDirectoryName(dataPath),"audio-errors.log"));soundPlayer.TryPlay(motionClock.Elapsed.TotalSeconds);}
        void PrepareSound(){if(preferences.sound&&!testMode&&soundPlayer==null)soundPlayer=new SoftSound(System.IO.Path.Combine(System.IO.Path.GetDirectoryName(dataPath),"audio-errors.log"));}
        void CancelHold(){if(holdTimer!=null)holdTimer.Stop();}
        void StartCardGesture(Skill skill,Point point) {
            CancelHold();downSkill=skill;downPoint=point;dragging=false;caughtMotion=motion.Moving;wheelDeadline=0;motion.BeginDrag(motionClock.Elapsed.TotalSeconds);if(!testMode)stage.CaptureMouse();
            if(managing&&holdTimer!=null)holdTimer.Start();StartRendering();
        }
        void ActivateHold() {
            CancelHold();if(!managing||downSkill==null||dragging||overlay!=null)return;
            var skill=downSkill;var point=downPoint;downSkill=null;motion.Release(motionClock.Elapsed.TotalSeconds,false);BeginReorder(skill,point);StartHeldReorder();slideSoundArmed=false;StartRendering();
        }
        void PulseCard(string id){if(!preferences.feedback)return;var card=cards.FirstOrDefault(c=>c.Skill.id==id);if(card!=null){card.Press=1;StartRendering();}}
        void ResizeStage(){if(stage==null)return;if(layoutWidth==0||!IsLoaded||!SystemParameters.ClientAreaAnimation){layoutWidth=stage.ActualWidth;layoutHeight=stage.ActualHeight;Position(false);}else StartRendering();}
        void AdvanceInteraction(double dt) {
            interactionMoving=false;AdvanceResponsive(dt);double factor=1-Math.Exp(-dt*22);
            if(Math.Abs(layoutWidth-stage.ActualWidth)>.1||Math.Abs(layoutHeight-stage.ActualHeight)>.1){layoutWidth+=(stage.ActualWidth-layoutWidth)*factor;layoutHeight+=(stage.ActualHeight-layoutHeight)*factor;interactionMoving=true;}else{layoutWidth=stage.ActualWidth;layoutHeight=stage.ActualHeight;}
            foreach(var c in cards) {
                double target=preferences.feedback&&SystemParameters.ClientAreaAnimation&&overlay==null&&((manageReordering&&selectedIds.Contains(c.Skill.id))||(!motion.Moving&&!motion.IsDragging&&!manageReorderCandidate&&!ManageChanging&&c.Hovered))?1:0;
                // Slightly overdamped movement keeps small controls readable without oscillation.
                double remaining=dt;while(remaining>0){double h=Math.Min(remaining,1.0/240);c.HoverVelocity+=(230*(target-c.Hover)-30*c.HoverVelocity)*h;c.Hover+=c.HoverVelocity*h;remaining-=h;}
                c.Press*=Math.Exp(-dt*22);
                if(Math.Abs(c.Hover-target)<.001&&Math.Abs(c.HoverVelocity)<.01){c.Hover=target;c.HoverVelocity=0;}else interactionMoving=true;
                if(c.Press<.001)c.Press=0;else interactionMoving=true;
            }
        }
        Button CreateSelectButton(Action click) {
            var button=UI.Button("",click);button.Width=36;button.Height=36;button.Padding=new Thickness(0);button.BorderThickness=new Thickness(0);button.Background=Brushes.Transparent;
            var grid=new Grid {Width=23,Height=23};grid.Children.Add(new Ellipse {Stroke=new SolidColorBrush(Color.FromArgb(210,242,244,247)),StrokeThickness=1.2,Fill=new SolidColorBrush(Color.FromArgb(60,16,21,28))});
            var tick=UI.Text("✓",14,UI.Paper,true);tick.HorizontalAlignment=HorizontalAlignment.Center;tick.VerticalAlignment=VerticalAlignment.Center;tick.Opacity=0;grid.Children.Add(tick);button.Content=grid;return button;
        }
        void UpdateSelectionVisual(CardView card) {
            bool active=selectedIds.Contains(card.Skill.id);if(card.SelectionState==active)return;bool first=card.SelectionState==null;card.SelectionState=active;
            var grid=(Grid)card.Select.Content;var circle=(Ellipse)grid.Children[0];var tick=(TextBlock)grid.Children[1];
            Color color=active?Color.FromRgb(239,241,242):Color.FromArgb(60,16,21,28);var fill=(SolidColorBrush)circle.Fill;
            if(!first&&MotionFx.Enabled&&SystemParameters.ClientAreaAnimation){var old=fill.Color;fill.Color=color;fill.BeginAnimation(SolidColorBrush.ColorProperty,new System.Windows.Media.Animation.ColorAnimation(old,color,TimeSpan.FromSeconds(.15)){FillBehavior=System.Windows.Media.Animation.FillBehavior.Stop});MotionFx.Fade(tick,active?1:0,.14);}else{fill.Color=color;tick.Opacity=active?1:0;}
            System.Windows.Automation.AutomationProperties.SetName(card.Select,(active?"取消选择：":"选择：")+card.Skill.name);
        }
        void AnimateFilterSelection(double oldX) {
            var button=filters.Children.OfType<Button>().FirstOrDefault(b=>(string)b.Content==category);if(button==null)return;
            filters.Measure(new Size(Double.PositiveInfinity,50));double x=0;foreach(UIElement child in filters.Children){if(child==button)break;x+=child.DesiredSize.Width;}filterX=x;
            // The active background slides while labels stay in place.
            button.Loaded+=delegate {
                if(button.Content==null||!MotionFx.Enabled)return;var border=FindChildren<Border>(button).FirstOrDefault();if(border==null)return;
                var highlight=new Border {Background=UI.B("#333333"),CornerRadius=new CornerRadius(7),IsHitTestVisible=false,RenderTransform=new TranslateTransform(oldX-x,0)};
                var content=border.Child;if(content==null)return;border.Child=null;var layers=new Grid();layers.Children.Add(highlight);layers.Children.Add(content);border.Child=layers;border.Background=Brushes.Transparent;
                MotionFx.To((TranslateTransform)highlight.RenderTransform,TranslateTransform.XProperty,0,.24);
            };
        }
        Image CaptureStage(bool animate) {
            if(!animate||testMode||!SystemParameters.ClientAreaAnimation||stage==null||stage.ActualWidth<1||stage.ActualHeight<1)return null;
            var bitmap=new RenderTargetBitmap((int)Math.Ceiling(stage.ActualWidth),(int)Math.Ceiling(stage.ActualHeight),96,96,PixelFormats.Pbgra32);bitmap.Render(stage);bitmap.Freeze();
            return new Image {Source=bitmap,Width=stage.ActualWidth,Height=stage.ActualHeight,IsHitTestVisible=false};
        }
        void ApplyRefreshTransition(Image oldView,Dictionary<string,CardView> previous,bool animate) {
            if(oldView!=null){Panel.SetZIndex(oldView,25000);stage.Children.Add(oldView);MotionFx.Fade(oldView,0,.24,()=>stage.Children.Remove(oldView));}
            foreach(var c in cards){CardView old;if(previous.TryGetValue(c.Skill.id,out old)&&old.Skill.coverId!=c.Skill.coverId&&old.Skill.theme==c.Skill.theme&&MotionFx.Enabled){var picture=new Border {Background=old.Face.Background,Child=new Border {Background=old.Picture.Background},Height=old.Picture.Height,VerticalAlignment=VerticalAlignment.Top,IsHitTestVisible=false};c.Face.Children.Insert(1,picture);MotionFx.Fade(picture,0,.3,()=>c.Face.Children.Remove(picture));var before=(LinearGradientBrush)old.Info.Background;var after=(LinearGradientBrush)c.Info.Background;for(int i=0;i<after.GradientStops.Count;i++){var stop=after.GradientStops[i];stop.BeginAnimation(GradientStop.ColorProperty,new System.Windows.Media.Animation.ColorAnimation(before.GradientStops[i].Color,stop.Color,TimeSpan.FromSeconds(.3)));}}}
            if(animate&&!testMode&&SystemParameters.ClientAreaAnimation){foreach(var c in cards){CardView old;if(previous.TryGetValue(c.Skill.id,out old))c.VisualSlot=motion.Position+old.Move.X/Spacing();}Position(false);StartRendering();}
        }
        void SetUndo(List<Skill> before,string action) {undoSnapshot=before;undoFocus=before.Count==0?null:before[Math.Min(selected,before.Count-1)].id;undoButton.Content="撤销"+action;undoButton.Visibility=Visibility.Visible;undoButton.ToolTip="撤销上一次"+action+"（Ctrl+Z）";}
        void ClearUndo(){if(suppressUndoClear)return;undoSnapshot=null;if(undoButton!=null)undoButton.Visibility=Visibility.Collapsed;}
        void UndoLast(){if(undoSnapshot==null||overlay!=null)return;var restore=undoSnapshot;var focus=undoFocus;suppressUndoClear=true;try{if(Commit(restore,"✓ 已撤销上次操作",focus))ClearUndoAfterRestore();}finally{suppressUndoClear=false;}}
        void ClearUndoAfterRestore(){undoSnapshot=null;undoButton.Visibility=Visibility.Collapsed;}
    }
}
