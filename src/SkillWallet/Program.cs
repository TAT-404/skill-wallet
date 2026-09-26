using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using System.Security.Cryptography;

namespace SkillWallet {
    public static class Program {
        static string Option(string[] args,string name) {int i=Array.IndexOf(args,name);return i>=0&&i+1<args.Length?args[i+1]:null;}
        [STAThread]
        public static int Main(string[] args) {
            string tests=Option(args,"--self-test"),render=Option(args,"--render"),uiChecks=Option(args,"--ui-check"),motionCheck=Option(args,"--motion-check"),errorFile=Option(args,"--error-file");
            try {
                if(tests!=null){SelfTests.Run(tests);return 0;}
                string managementCheck=Option(args,"--management-check"),pictureCheck=Option(args,"--picture-check"),polishCheck=Option(args,"--polish-check"),designCheck=Option(args,"--design-check");
                string interactionCheck=Option(args,"--interaction-check");
                string coverCheck=Option(args,"--cover-check"),importCheck=Option(args,"--import-check");
                bool testing=importCheck!=null||coverCheck!=null||interactionCheck!=null||render!=null||uiChecks!=null||motionCheck!=null||managementCheck!=null||pictureCheck!=null||polishCheck!=null||designCheck!=null;
                string baseDir=AppDomain.CurrentDomain.BaseDirectory;
                string dataDir=Option(args,"--data-dir") ?? (File.Exists(Path.Combine(baseDir,"portable.mode"))?Path.Combine(baseDir,"data"):Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),"SkillWallet"));
                if(testing&&Option(args,"--data-dir")==null)throw new ArgumentException("预览和 UI 检查必须指定独立的 --data-dir。");
                string dataPath=Path.Combine(Path.GetFullPath(dataDir),"我的技能库.skillpack");
                string hash;using(var sha=SHA256.Create())hash=BitConverter.ToString(sha.ComputeHash(System.Text.Encoding.UTF8.GetBytes(dataPath.ToUpperInvariant()))).Replace("-","");
                bool created;
                using(var mutex=new Mutex(true,"Local\\SkillWallet-"+hash,out created)) {
                    if(!created){SingleInstance.Wake(hash);return 0;}
                    var app=new Application();UI.Install(app);
                    if(testing)System.Windows.Media.RenderOptions.ProcessRenderMode=System.Windows.Interop.RenderMode.SoftwareOnly;
                    var window=new MainWindow(dataPath,testing);SingleInstance.Attach(window,hash);
                    if(testing&&args.Contains("--compact")){window.Width=940;window.Height=740;}
                    if(testing&&args.Contains("--tiny")){window.Width=640;window.Height=520;}
                    if(testing&&args.Contains("--short")){window.Width=1500;window.Height=540;}
                    if(testing&&args.Contains("--narrow")){window.Width=660;window.Height=900;}
                    if(testing&&args.Contains("--wide")){window.Width=2100;window.Height=860;}
                    if(testing) {
                        window.ShowActivated=false;window.ShowInTaskbar=false;window.WindowStartupLocation=WindowStartupLocation.Manual;window.Left=-16000;window.Top=-16000;
                        window.Loaded+=delegate {
                            var timer=new DispatcherTimer {Interval=TimeSpan.FromMilliseconds(600)};
                            timer.Tick+=delegate {timer.Stop();try {
                                if(uiChecks!=null)window.RunUiChecks(uiChecks);if(designCheck!=null)window.RunDesignChecks(designCheck);
                                if(pictureCheck!=null)window.RunPictureUiChecks(pictureCheck);
                                if(coverCheck!=null)window.RunCoverChecks(Option(args,"--cover-image"),coverCheck);
                                if(importCheck!=null)window.RunImportUiChecks(importCheck);
                                if(args.Contains("--showcase"))window.ShowcasePreview();
                                if(args.Contains("--many"))window.ManyPreview();
                                if(args.Contains("--tools-preview"))window.ToolsPreview();
                                if(args.Contains("--new"))window.ShowNewPreview();
                                if(args.Contains("--manage"))window.ShowManagePreview();
                                if(args.Contains("--edit-preview"))window.ShowEditPreview();
                                if(render!=null)window.RenderPreview(render,args.Contains("--detail"),args.Contains("--copy-preview"));
                                if(interactionCheck!=null)window.RunInteractionChecks(interactionCheck,ex=>{if(ex!=null&&errorFile!=null)File.WriteAllText(errorFile,ex.ToString());app.Shutdown(ex==null?0:1);});
                                else if(polishCheck!=null)window.RunPolishChecks(polishCheck,ex=>{if(ex!=null&&errorFile!=null)File.WriteAllText(errorFile,ex.ToString());app.Shutdown(ex==null?0:1);});
                                else if(managementCheck!=null)window.RunManagementChecks(managementCheck,ex=>{if(ex!=null&&errorFile!=null)File.WriteAllText(errorFile,ex.ToString());app.Shutdown(ex==null?0:1);});
                                else if(motionCheck!=null)window.RunMotionCheck(motionCheck,ex=>{if(ex!=null&&errorFile!=null)File.WriteAllText(errorFile,ex.ToString());app.Shutdown(ex==null?0:1);});
                                else app.Shutdown(0);
                            }catch(Exception ex){if(errorFile!=null)File.WriteAllText(errorFile,ex.ToString());app.Shutdown(1);}};timer.Start();
                        };
                    }
                    return app.Run(window);
                }
            }catch(Exception ex) {
                if(errorFile!=null)File.WriteAllText(errorFile,ex.ToString());
                else MessageBox.Show(ex.Message,"Skill Wallet 无法启动",MessageBoxButton.OK,MessageBoxImage.Error);
                return 1;
            }
        }
    }
    public partial class MainWindow {
        public void ShowcasePreview() {
            // Preview-only collection; never saved over the user's library.
            var samples=Samples.Create();var product=LocalDraft.Create(FeatureTests.ProductPrompt);product.name="电商产品精修";product.description="还原真实结构，呈现专业质感。";
            skills=new System.Collections.Generic.List<Skill>{samples[4],samples[2],product,samples[1],samples[5],samples[0]};category="全部";favorites=false;query="";selected=2;RebuildFilters();Refresh(false);
        }
        public void ManyPreview(){var list=Samples.Create();skills=Enumerable.Range(0,13).Select(i=>{var skill=list[i%6].Clone();skill.id=Guid.NewGuid().ToString();return skill;}).ToList();selected=6;RebuildFilters();Refresh(false);}
        public void ToolsPreview(){ShowToolsMenu();}
        public void ShowNewPreview(){Edit(null);}
        public void ShowEditPreview(){Edit(filtered[selected]);}
        public void RenderPreview(string path,bool detail,bool copied=false) {
            toast.Visibility=Visibility.Collapsed;
            if(detail&&filtered.Count>0)OpenDetail(filtered[selected]);
            compactPhase=shortLayout?1:0;ApplyCompactFrame();UpdateLayout();layoutWidth=stage.ActualWidth;layoutHeight=stage.ActualHeight;ambientReady=false;Position(false);root.UpdateLayout();
            if(copied&&filtered.Count>0){ShowCopied(filtered[selected]);toast.Opacity=1;root.UpdateLayout();}
            int width=(int)root.ActualWidth,height=(int)root.ActualHeight;
            var bitmap=new RenderTargetBitmap(width,height,96,96,PixelFormats.Pbgra32);bitmap.Render(root);
            var encoder=new PngBitmapEncoder();encoder.Frames.Add(BitmapFrame.Create(bitmap));using(var stream=File.Create(path))encoder.Save(stream);
        }
    }
}
