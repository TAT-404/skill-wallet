using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using System.Windows.Threading;

namespace SkillWallet {
    public partial class MainWindow {
        public void RunPolishChecks(string report,Action<Exception> done) {
            var results=new List<string>();Action<bool,string> check=(ok,label)=>{if(!ok)throw new Exception(label);results.Add("PASS "+label);};
            MotionFx.Enabled=true;
            var bytes=new byte[64*64*4];for(int i=0;i<4096;i++){int j=i*4;bytes[j]=bytes[j+1]=bytes[j+2]=245;bytes[j+3]=255;if(i%8==0){bytes[j]=30;bytes[j+1]=70;bytes[j+2]=205;}}
            var fixture=BitmapSource.Create(64,64,96,96,PixelFormats.Bgra32,null,bytes,256);Color accent=Palette.Extract(fixture);
            check(accent.R>accent.G*1.5&&accent.R>accent.B*2,"cover palette prioritizes colored subject over white backdrop");
            check((1.05/(Palette.Luminance(Palette.Readable(accent))+.05))>=4.5,"automatic panel colors preserve white-text contrast");
            foreach(var c in cards){check(((LinearGradientBrush)c.Info.Background).GradientStops.All(g=>g.Color.A==255),"opaque card panel prevents photograph-end color band: "+c.Index);}
            check(cards.All(c=>c.Root.BorderThickness.Left==0&&c.Root.Clip is RectangleGeometry&&c.Face.Children.IndexOf(c.Stroke)<c.Face.Children.IndexOf(c.Shade)),"outer rim is inside the same full-card clipping and dimming layers");
            var switches=FindChildren<RailSwitch>(root).ToList();check(switches.Count==1,"only monochrome sound switch remains");
            bool previousSound=preferences.sound;switches[0].RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
            check(preferences.sound!=previousSound&&Preferences.Read(System.IO.Path.Combine(System.IO.Path.GetDirectoryName(dataPath),"preferences.json")).sound==preferences.sound,"sound setting persists");switches[0].RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
            check(!preferences.sound&&SoftSound.CreateWave().Length==4630,"sound defaults off with 52ms reference mechanical cue");
            SetManagement(true);managePhase=1;ApplyManagementPhase();foreach(var s in filtered)selectedIds.Add(s.id);UpdateManagementBar();Position(false);StopRendering();
            var original=skills.Select(s=>s.id).ToList();int copies=CopyCount;
            StartCardGesture(filtered[selected],new Point(600,220));MoveCardGesture(new Point(480,220));
            check(dragging&&!holdTimer.IsEnabled&&!manageReorderCandidate,"short swipe of selected card cancels long press and pans");
            FinishGesture(-120,0);FreezeMotion();check(selectedIds.Count==filtered.Count&&CopyCount==copies&&skills.Select(s=>s.id).SequenceEqual(original),"all-selected collection remains scrollable without reordering or copying");
            selectedIds.Clear();selectedIds.Add(filtered[0].id);selectedIds.Add(filtered[2].id);UpdateManagementBar();Position(false);
            check(cards.All(c=>((Grid)c.Select.Content).Children[0] is Ellipse),"selection controls use circles rather than square tiles");
            StartCardGesture(filtered[0],new Point(stage.ActualWidth/2,200));
            var watch=System.Diagnostics.Stopwatch.StartNew();int phase=0;double mark=0;CopyAttempt retry=null;bool retryDone=false;int writes=0;bool failureDone=false,canceledCalled=false,exhaustionDone=false;
            var timer=new DispatcherTimer {Interval=TimeSpan.FromMilliseconds(25)};
            timer.Tick+=delegate {
                try {
                    double now=watch.Elapsed.TotalSeconds;
                    if(phase==0&&now>.48){
                        check(manageReordering&&heldReorder&&selectedIds.Count==2,"real hold timer activates group ordering after 350ms; active="+manageReordering+" held="+heldReorder+" selected="+selectedIds.Count+" pending="+(downSkill!=null)+" dragging="+dragging+" timer="+holdTimer.IsEnabled);
                        MoveReorder(new Point(stage.ActualWidth-2,200));EndReorder();
                        check(!skills.Select(s=>s.id).SequenceEqual(original)&&undoSnapshot!=null,"long-press group drop saves order and exposes undo");UndoLast();
                        check(skills.Select(s=>s.id).SequenceEqual(original)&&Packs.Read(dataPath).Select(s=>s.id).SequenceEqual(original),"undo sorting restores original order on disk");
                        selectedIds.Clear();foreach(var s in skills)selectedIds.Add(s.id);RemoveSelection();check(skills.Count==0,"batch delete can remove all visible cards");UndoLast();
                        check(skills.Count==original.Count&&Packs.Read(dataPath).Count==original.Count,"undo batch deletion restores entire collection");
                        selectedIds.Clear();selectedIds.Add(filtered[0].id);StartCardGesture(filtered[0],new Point(400,200));ActivateHold();EndReorder();check(skills.Select(s=>s.id).SequenceEqual(original),"hold and release without moving does not reorder");
                        SetManagement(false);managePhase=0;ApplyManagementPhase();motion.Reset(filtered.Count,2);Position(false);
                        UpdateLayout();layoutHeight=stage.ActualHeight;foreach(var c in cards)c.VisualSlot=c.Index;layoutWidth=900;Position(false);int narrow=cards.Count(c=>c.Root.Opacity>.5&&Math.Abs(c.Move.X)<layoutWidth/2);layoutWidth=2050;Position(false);int wide=cards.Count(c=>c.Root.Opacity>.5&&Math.Abs(c.Move.X)<layoutWidth/2);check(wide>narrow,"wider viewport exposes additional real cards");layoutWidth=stage.ActualWidth;
                        OpenDetail(filtered[0]);CloseOverlay();OpenDetail(filtered[1]);
                        retry=new CopyAttempt(()=>{writes++;if(writes<3)throw new Win32Exception(5);},(ex,count)=>{retryDone=ex==null&&count==3;});retry.Start();phase=1;mark=now;
                    }else if(phase==1&&now-mark>.55){
                        check(overlay!=null&&root.Children.Contains(overlay)&&!main.IsEnabled,"rapid panel close and reopen retains latest panel");
                        check(retryDone&&writes==3,"clipboard retries recover transient lock without blocking dispatcher");retry.Dispose();
                        retry=new CopyAttempt(()=>{throw new InvalidOperationException("test");},(ex,count)=>{failureDone=ex is InvalidOperationException&&count==1;});retry.Start();check(failureDone,"non-transient clipboard errors report real failure immediately");retry.Dispose();
                        retry=new CopyAttempt(()=>{throw new Win32Exception(5);},(ex,count)=>{canceledCalled=true;});retry.Start();retry.Dispose();
                        byte[] data;var encoder=new PngBitmapEncoder();encoder.Frames.Add(BitmapFrame.Create(fixture));using(var ms=new MemoryStream()){encoder.Save(ms);data=ms.ToArray();}
                        var photo=new SkillImage {id=Pictures.Hash(data),name="test.png",data=data};ViewPicture(photo,null);check(pictureLayer!=null&&previousLayerDisabled(),"image opens over details while blocking underlying interaction");fitPicture();phase=2;mark=now;
                    }else if(phase==2&&now-mark>.4){
                        check(!canceledCalled,"superseded copy request cancels queued retries");closePicture();retry=new CopyAttempt(()=>{throw new Win32Exception(5);},(ex,count)=>{exhaustionDone=ex is Win32Exception&&count==7;});retry.Start();phase=3;mark=now;
                    }else if(phase==3&&now-mark>1.5){
                        check(pictureLayer==null&&overlay!=null&&overlay.IsEnabled,"image close returns to same detail panel");CloseOverlay();
                        check(exhaustionDone,"persistent clipboard contention stops after seven attempts and reports failure");
                        check(topGlow.Color.A<bottomGlow.Color.A&&coreColor.Color.A>auraColor.Color.A,"ambient light has restrained top glow and concentrated lower core");
                        timer.Stop();if(retry!=null)retry.Dispose();File.WriteAllLines(report,results);MotionFx.Enabled=false;done(null);
                    }
                }catch(Exception ex){timer.Stop();CancelHold();if(retry!=null)retry.Dispose();MotionFx.Enabled=false;done(ex);}
            };timer.Start();
        }
        bool previousLayerDisabled(){return overlay!=null&&!overlay.IsEnabled;}
    }
}
