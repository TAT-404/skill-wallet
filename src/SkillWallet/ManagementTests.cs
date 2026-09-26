using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;

namespace SkillWallet {
    public static class ManagementTests {
        public static void Run(string directory,Action<bool,string> check) {
            var all=Samples.Create();var ids=all.Select(s=>s.id).ToList();
            var moving=new HashSet<string>{ids[1],ids[3]};
            var moved=CardOrder.Move(all,ids,moving,6);
            check(moved.Select(s=>s.id).SequenceEqual(new[]{ids[0],ids[2],ids[4],ids[5],ids[1],ids[3]}),"group drag preserves internal order across nonadjacent selection");
            var front=CardOrder.Move(all,ids,moving,0);
            check(front.Take(2).Select(s=>s.id).SequenceEqual(new[]{ids[1],ids[3]})&&all.Select(s=>s.id).SequenceEqual(ids),"move to start leaves source unmodified");
            var subset=new List<string>{ids[0],ids[2],ids[4]};
            var filtered=CardOrder.Move(all,subset,new HashSet<string>{ids[4]},0);
            check(filtered.Select(s=>s.id).SequenceEqual(new[]{ids[4],ids[1],ids[0],ids[3],ids[2],ids[5]}),"filtered ordering leaves hidden cards in original slots");
            check(CardOrder.Move(all,ids,new HashSet<string>(ids),3).Select(s=>s.id).SequenceEqual(ids),"moving all cards is a stable no-op");
            string imageFile=Path.Combine(directory,"fixture.png");
            var pixels=new byte[16*16*4];for(int i=0;i<pixels.Length;i+=4){pixels[i]=80;pixels[i+1]=140;pixels[i+2]=220;pixels[i+3]=255;}
            var bitmap=BitmapSource.Create(16,16,96,96,PixelFormats.Bgra32,null,pixels,64);var encoder=new PngBitmapEncoder();encoder.Frames.Add(BitmapFrame.Create(bitmap));using(var stream=File.Create(imageFile))encoder.Save(stream);
            var photo=Pictures.Load(imageFile);all[0].pictures=new List<SkillImage>{photo};all[0].coverId=photo.id;all[1].pictures=new List<SkillImage>{photo.Clone()};all[1].coverId=photo.id;
            string file=Path.Combine(directory,"photos.skillpack");Packs.Write(file,all);File.Delete(imageFile);var loaded=Packs.Read(file);
            check(loaded[0].pictures[0].data.SequenceEqual(photo.data)&&loaded[0].coverId==photo.id&&Pictures.Bitmap(loaded[0].pictures[0],720).PixelWidth==720,"image archive survives deletion of source and renders offline");
            using(var zip=ZipFile.OpenRead(file)){check(zip.Entries.Count(e=>e.FullName.StartsWith("assets/"))==1,"identical images are stored once in archive");using(var reader=new StreamReader(zip.GetEntry("manifest.json").Open()))check(Packs.Json().Deserialize<Manifest>(reader.ReadToEnd()).formatVersion==2,"image archives are versioned to prevent old-reader data loss");}
            var clone=loaded[0].Clone();clone.pictures.Clear();check(loaded[0].pictures.Count==1,"editing image list cannot mutate original card before save");
            check(Pictures.Cover(loaded[0]) is ImageBrush&&Object.ReferenceEquals(Pictures.Cover(loaded[2]),CardArt.Cover(loaded[2].theme)),"custom cover and default fallback both render");
            var incoming=loaded.Select(s=>s.Clone()).ToList();incoming[0].coverId=null;
            check(Packs.Merge(loaded,incoming,0)[0].coverId==photo.id&&Packs.Merge(loaded,incoming,1)[0].coverId==null,"picture metadata participates in import conflict handling");
            var renamedPhoto=loaded[0].Clone();renamedPhoto.id=Guid.NewGuid().ToString();renamedPhoto.pictures[0].name="重命名图片.png";
            check(Packs.PlanImport(loaded,new List<Skill>{renamedPhoto}).Same==1,"identical picture bytes with another filename still deduplicate");
            renamedPhoto.coverId=null;
            check(Packs.PlanImport(loaded,new List<Skill>{renamedPhoto}).Conflicts==1,"different cover selection with a new card ID is not silently discarded");
            renamedPhoto.pictures.Clear();
            check(Packs.PlanImport(loaded,new List<Skill>{renamedPhoto}).Conflicts==1,"different image gallery with a new card ID remains a conflict");
            var invalid=loaded.Select(s=>s.Clone()).ToList();invalid[0].pictures[0].data=new byte[]{0,1};bool rejected=false;try{Packs.Write(file,invalid);}catch(InvalidDataException){rejected=true;}
            check(rejected&&Packs.Read(file)[0].pictures[0].data.SequenceEqual(photo.data),"invalid picture save preserves existing library");
            string missing=Path.Combine(directory,"missing-picture.skillpack");File.Copy(file,missing,true);using(var zip=ZipFile.Open(missing,ZipArchiveMode.Update))zip.GetEntry("assets/"+photo.id).Delete();
            rejected=false;try{Packs.Read(missing);}catch(InvalidDataException){rejected=true;}check(rejected,"missing archive image rejects import");
            invalid=loaded.Select(s=>s.Clone()).ToList();invalid[0].coverId=new string('a',64);rejected=false;try{Packs.Validate(invalid);}catch(InvalidDataException){rejected=true;}check(rejected,"unresolved cover reference is rejected");
            var plain=Samples.Create();Packs.Validate(plain);check(plain.All(s=>s.pictures!=null&&s.pictures.Count==0),"older skills without picture fields normalize safely");
            Packs.Write(file,loaded);check(Packs.Read(file+".bak")[0].pictures[0].data.SequenceEqual(photo.data),"automatic backup includes image assets");
        }
    }
    public partial class MainWindow {
        public void RunPictureUiChecks(string report) {
            var lines=new List<string>();Action<bool,string> check=(ok,label)=>{if(!ok)throw new Exception(label);lines.Add("PASS "+label);};
            byte[] data;using(var stream=System.Reflection.Assembly.GetExecutingAssembly().GetManifestResourceStream("SkillWallet.covers.png"))using(var output=new MemoryStream()){stream.CopyTo(output);data=output.ToArray();}
            var picture=new SkillImage {id=Pictures.Hash(data),name="测试效果图.png",data=data};
            var target=skills[0];Edit(target);UpdateLayout();var editor=FindChildren<PictureEditor>(panelBody).Single();editor.Items.Add(picture);editor.CoverId=picture.id;
            check(isDirty(),"adding picture marks editor dirty");
            panelFooter.Children.OfType<Button>().Last().RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
            check(overlay==null&&skills[0].coverId==picture.id&&Packs.Read(dataPath)[0].pictures.Count==1,"editor saves picture and cover in local library");
            OpenDetail(skills[0]);UpdateLayout();check(FindChildren<Image>(panelBody).Count()==1,"detail shows clickable effect gallery");CloseOverlay();
            Edit(skills[0]);UpdateLayout();editor=FindChildren<PictureEditor>(panelBody).Single();editor.Items.Clear();editor.CoverId=null;
            check(skills[0].pictures.Count==1,"unsaved image removal leaves library unchanged");
            panelFooter.Children.OfType<Button>().Last().RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
            check(skills[0].pictures.Count==0&&Object.ReferenceEquals(Pictures.Cover(skills[0]),CardArt.Cover(skills[0].theme)),"saving image removal restores default cover");
            QuickCreate();UpdateLayout();editor=FindChildren<PictureEditor>(panelBody).Single();editor.Items.Add(picture);editor.CoverId=picture.id;FindChildren<TextBox>(panelBody).Single().Text=FeatureTests.ProductPrompt;
            panelFooter.Children.OfType<Button>().Last().RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
            check(overlay==null&&skills.Last().pictures.Count==1&&skills.Last().content==FeatureTests.ProductPrompt,"single-input creator retains prompt with selected picture");
            File.WriteAllLines(report,lines);
        }
        public void RunManagementChecks(string report,Action<Exception> done) {
            var results=new List<string>();Action<bool,string> check=(ok,label)=>{if(!ok)throw new Exception(label);results.Add("PASS "+label);};
            var phases=new List<double>();var colors=new List<byte>();var drifts=new List<double>();
            var watch=System.Diagnostics.Stopwatch.StartNew();int step=0;double mark=0;double mid=0;
            SetManagement(true);
            var timer=new DispatcherTimer {Interval=TimeSpan.FromMilliseconds(20)};
            timer.Tick+=delegate {
                try {
                    double time=watch.Elapsed.TotalSeconds;
                    phases.Add(managePhase);colors.Add(coreColor.Color.A);drifts.Add(topLed.Center.X);
                    if(step==0&&time>1.15) {
                        check(managePhase==1&&!rendering,"entry animation settles and releases frame subscription");
                        check(!SystemParameters.ClientAreaAnimation||phases.Where(p=>p>0&&p<1).Distinct().Count()>4,"entry contains continuous intermediate frames");
                        check(cards.All(c=>c.Shade.Opacity==0&&c.Rotate.Angle==0&&c.Move.Y==0),"management cards are bright straight and aligned in one row");
                        var sorted=cards.OrderBy(c=>c.Index).ToList();check(sorted.Zip(sorted.Skip(1),(a,b)=>b.Move.X-a.Move.X-324*a.Scale.ScaleX).All(g=>g>0),"management cards have positive horizontal gaps without overlap");
                        int before=CopyCount;ActivateCard(filtered[1]);ActivateCard(filtered[3]);check(CopyCount==before&&selectedIds.Count==2,"management clicks select without copying");
                        var original=filtered.Select(s=>s.id).ToList();
                        BeginReorder(filtered[1],new Point(stage.ActualWidth/2,200));MoveReorder(new Point(stage.ActualWidth/2+Spacing()*3.6,205));EndReorder();
                        check(skills.Select(s=>s.id).SequenceEqual(new[]{original[0],original[2],original[4],original[5],original[1],original[3]}),"same drag gesture handlers move selected group to end");
                        check(Packs.Read(dataPath).Select(s=>s.id).SequenceEqual(skills.Select(s=>s.id)),"group order persists to local archive");
                        check(selectedIds.Count==2&&CopyCount==before,"selection retained after drag without copying");
                        BeginReorder(filtered[0],new Point(stage.ActualWidth/2,200));MoveReorder(new Point(stage.ActualWidth-1,210));double old=motion.Position;AdvanceManagement(.2);check(motion.Position>old,"drag near right edge scrolls across long row");CancelReorder();
                        SetManagement(false);step=1;mark=time;
                    }else if(step==1&&time-mark>.18) {
                        mid=managePhase;SetManagement(true);check(managePhase==mid,"reversing exit does not reset transition position");step=2;mark=time;
                    }else if(step==2&&time-mark>.8) {
                        check(managePhase==1,"reversed transition settles in management");
                        selectAll.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));check(selectedIds.Count==skills.Count,"select all spans offscreen cards in current filter");
                        selectAll.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));check(selectedIds.Count==0,"cancel select all clears selection");
                        ActivateCard(filtered[0]);int count=skills.Count;check(RemoveSelection()&&skills.Count==count-1&&Packs.Read(dataPath).Count==count-1,"batch deletion persists selected removal");
                        SetManagement(false);step=3;mark=time;
                    }else if(step==3&&time-mark>1.15) {
                        check(managePhase==0&&!rendering,"exit returns to coverflow and stops frame loop");
                        check(cards.Any(c=>c.Shade.Opacity>.3)&&cards.Any(c=>Math.Abs(c.Rotate.Angle)>1),"browsing restores side dimming and overlap angles");
                        check(!SystemParameters.ClientAreaAnimation||colors.Distinct().Count()>2&&drifts.Distinct().Count()>5,"ambient LEDs breathe and drift across both modes including idle");
                        timer.Stop();File.WriteAllLines(report,results);done(null);
                    }
                }catch(Exception ex){timer.Stop();done(ex);}
            };timer.Start();
        }
    }
}
