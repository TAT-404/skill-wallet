using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;

namespace SkillWallet {
    public partial class MainWindow {
        public void RunInteractionChecks(string report,Action<Exception> done) {
            var results=new List<string>();Action<bool,string> check=(ok,label)=>{if(!ok)throw new Exception(label);results.Add("PASS "+label);};
            preferences.sound=true;railSwitches[0].SetValue(true);MotionFx.Enabled=true;
            var roots=cards.Select(c=>c.Root).ToArray();double position=motion.Position;int focus=selected;var filter=filters.Children.OfType<Button>().First(b=>(string)b.Content==category);
            for(int i=0;i<30;i++)filter.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
            check(cards.Select(c=>c.Root).SequenceEqual(roots)&&stage.Children.OfType<Image>().Count()==0,"repeated active category retains every card visual without screenshot overlays");
            check(selected==focus&&motion.Position==position&&filters.Children.Contains(filter),"repeated active category keeps viewport and filter button identities");
            SelectCategory("开发");check(filtered.Count==1&&filtered[0].category=="开发","different category still filters normally");
            var one=cards[0].Root;for(int i=0;i<20;i++)SelectCategory("开发");check(Object.ReferenceEquals(one,cards[0].Root),"repeated non-All category also does not rebuild");
            SelectCategory("全部");motion.Reset(filtered.Count,2);Position(false);StopRendering();int before=slideSoundRequests;
            UI.Button("普通按钮",delegate{}).RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
            railSwitches[0].RaiseEvent(new RoutedEventArgs(Button.ClickEvent));railSwitches[0].RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
            check(slideSoundRequests==before,"normal buttons and sound toggle are silent");
            StartCardGesture(filtered[selected],new Point(600,180));MoveCardGesture(new Point(600-Spacing()*1.1,180));
            check(slideSoundRequests==before+1,"dragging across a card boundary requests one mechanical cue");FinishGesture(-Spacing()*1.1,0);FreezeMotion();
            check(!IsActive,"slide cue is also requested when the visible window is inactive");
            before=slideSoundRequests;ScrollCards(-120);for(int frame=0;frame<120;frame++){motion.Step(1.0/60);Position(false);}FreezeMotion();
            check(!IsActive&&slideSoundRequests>before,"wheel scrolling an inactive window produces a card slide cue without activation");
            before=slideSoundRequests;WindowState=WindowState.Minimized;PlaySlideSound();WindowState=WindowState.Normal;
            check(slideSoundRequests==before,"minimized window remains silent");
            preferences.sound=false;before=slideSoundRequests;StartCardGesture(filtered[selected],new Point(600,180));MoveCardGesture(new Point(600+Spacing()*1.1,180));FinishGesture(Spacing()*1.1,0);FreezeMotion();
            check(slideSoundRequests==before,"sound switch suppresses card slide cue");preferences.sound=true;
            SetManagement(true);managePhase=1;ApplyManagementPhase();StopRendering();before=slideSoundRequests;ToggleSelection(filtered[0]);ToggleSelection(filtered[0]);
            StartCardGesture(filtered[0],new Point(400,180));ActivateHold();EndReorder();StopRendering();
            check(slideSoundRequests==before,"selection, long press and order completion stay silent");
            SetManagement(false);managePhase=0;ApplyManagementPhase();StopRendering();before=slideSoundRequests;Copy(filtered[selected]);
            check(slideSoundRequests==before,"successful copy stays silent");
            check(toast.Visibility==Visibility.Visible&&toastText.Text=="✓  已复制","copy in browsing mode shows bottom success toast");
            check(toastText.FontSize==12&&toast.Padding.Top==9&&toast.Padding.Left==16&&toast.CornerRadius.TopLeft==12,"copy toast uses restrained dimensions and rounded monochrome shell");
            Notify("普通提示");check(toastText.FontSize==13&&toast.Padding.Top==14,"compact copy style does not leak into other messages");
            byte[] wave=SoftSound.CreateWave();check(wave.Length==4630&&BitConverter.ToInt32(wave,24)==44100&&BitConverter.ToInt16(wave,22)==1,"embedded reference cue is 52ms mono 44100Hz PCM");
            check(Math.Abs(BitConverter.ToInt16(wave,44))<10&&Math.Abs(BitConverter.ToInt16(wave,wave.Length-2))<10,"cue boundaries fade to silence without a cut click");
            ShowCopied(filtered[selected]);var watch=System.Diagnostics.Stopwatch.StartNew();bool repeated=false;var timer=new DispatcherTimer {Interval=TimeSpan.FromMilliseconds(30)};
            timer.Tick+=delegate{try{
                if(!repeated&&watch.Elapsed.TotalSeconds>.7){ShowCopied(filtered[selected]);repeated=true;}
                if(watch.Elapsed.TotalSeconds>1.65&&watch.Elapsed.TotalSeconds<1.95)check(toast.Visibility==Visibility.Visible,"repeat copy renews the same toast rather than hiding early");
                if(watch.Elapsed.TotalSeconds>2.5){check(toast.Visibility==Visibility.Collapsed,"copy success toast fades away automatically");timer.Stop();preferences.sound=false;MotionFx.Enabled=false;File.WriteAllLines(report,results.Distinct());done(null);}
            }catch(Exception ex){timer.Stop();MotionFx.Enabled=false;done(ex);}};timer.Start();
        }
    }
}
