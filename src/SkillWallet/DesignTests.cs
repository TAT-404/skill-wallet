using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace SkillWallet {
    public partial class MainWindow {
        public void RunDesignChecks(string path) {
            var lines=new List<string>();Action<bool,string> check=(ok,label)=>{if(!ok)throw new Exception(label);lines.Add("PASS "+label);};
            var mono=new byte[32*32*4];for(int i=0;i<mono.Length;i+=4){mono[i]=mono[i+1]=mono[i+2]=170;mono[i+3]=255;}
            var neutral=Palette.Extract(BitmapSource.Create(32,32,96,96,PixelFormats.Bgra32,null,mono,128));check(neutral.R==neutral.G&&neutral.G==neutral.B,"grayscale covers do not invent colored lighting");
            var vivid=Palette.Vivid(Color.FromRgb(160,70,30));check(Math.Max(vivid.R,Math.Max(vivid.G,vivid.B))==255&&Palette.Saturation(vivid)>.8,"colored covers retain saturated bright accent");
            var panel=(LinearGradientBrush)Palette.Panel(skills[0]);check(panel.GradientStops.All(g=>g.Color.A==255),"card info is opaque across its complete height");
            check(panel.GradientStops.Where(g=>g.Offset<=.68).All(g=>(1.05/(Palette.Luminance(g.Color)+.05))>=7),"title and description sit over dark high-contrast stops");
            check(Branding.Load("logo-mark.png").PixelWidth>256&&Branding.Load("app-icon.png").PixelWidth>256,"both logo assets embedded and decoded");
            Width=640;Height=520;UpdateLayout();UpdateResponsive();compactPhase=1;ApplyCompactFrame();UpdateLayout();layoutWidth=stage.ActualWidth;layoutHeight=stage.ActualHeight;motion.Reset(filtered.Count,2);foreach(var c in cards)c.VisualSlot=c.Index;Position(false);
            check(cards.Count(c=>c.Root.Opacity>.1)==3,"minimum browsing window reveals exactly three cards");
            check(controls.Height<1&&dots.Children.Count>0&&dots.IsVisible,"short window preserves dots while collapsing numeric controls");
            check(importButton.Visibility==Visibility.Collapsed&&categoryButton.Visibility==Visibility.Visible&&searchButton.Visibility==Visibility.Visible,"narrow toolbar exposes compact search and category entries");
            check(stage.ActualHeight>250,"collapsed chrome leaves useful card height");
            var focus=filtered[selected].id;SetManagement(true);managePhase=1;ApplyManagementPhase();UpdateLayout();layoutWidth=stage.ActualWidth;layoutHeight=stage.ActualHeight;Position(false);selectedIds.Add(filtered[0].id);selectedIds.Add(filtered[2].id);UpdateManagementBar();
            check(deleteSelected.IsEnabled&&manageButton.IsVisible&&managementBar.Height>0,"minimum management window retains delete and done");
            Width=1900;Height=860;UpdateLayout();UpdateResponsive();compactPhase=0;ApplyCompactFrame();UpdateLayout();layoutWidth=stage.ActualWidth;layoutHeight=stage.ActualHeight;Position(false);
            check(selectedIds.Count==2&&filtered[selected].id==focus,"resizing preserves focus and selection");
            var roots=cards.ToDictionary(c=>c.Skill.id,c=>c.Root);BeginReorder(filtered[0],new Point(stage.ActualWidth/2,180));MoveReorder(new Point(stage.ActualWidth-5,180));EndReorder();
            check(cards.Where(c=>roots.ContainsKey(c.Skill.id)).All(c=>Object.ReferenceEquals(c.Root,roots[c.Skill.id]))&&stage.Children.OfType<Image>().Count()==0,"drop reuses card visuals with no full-stage snapshot overlay");
            check(Packs.Read(dataPath).Select(s=>s.id).SequenceEqual(skills.Select(s=>s.id)),"drop visual reuse still persists ordered library");
            SetManagement(false);managePhase=0;ApplyManagementPhase();foreach(var c in cards)c.VisualSlot=c.Index;motion.Reset(filtered.Count,0);Position(false);
            check(cards.Where(c=>Math.Abs(c.VisualSlot-motion.Position)>=4.2).All(c=>Math.Abs(c.Rotate.Angle)<.01),"distant browsing cards gradually return to upright");
            check(railSwitches.Count==1&&preferences.feedback,"motion always enabled with only one sound switch");
            railSwitches[0].SetValue(true);var track=FindChildren<Border>(railSwitches[0]).First(b=>b.Background is SolidColorBrush&&b.CornerRadius.TopLeft==9);var before=((SolidColorBrush)track.Background).Color;railSwitches[0].Tint(Colors.Red);check(before==((SolidColorBrush)track.Background).Color&&before.R==before.G&&before.G==before.B,"switch never acquires artwork color");railSwitches[0].SetValue(preferences.sound);
            Width=1280;Height=860;UpdateLayout();UpdateResponsive();ShowToolsMenu();check(toolsButton.Visibility==Visibility.Collapsed&&menuLayer==null,"full window has no redundant tools entry");
            Width=1500;Height=540;UpdateLayout();UpdateResponsive();ShowToolsMenu();check(menuLayer!=null&&FindChildren<RailSwitch>(menuShell).Count()==1&&FindChildren<Button>(menuShell).Count(b=>!(b is RailSwitch))==0,"wide short window menu contains only hidden sound control");CloseMenu();
            Width=660;Height=900;UpdateLayout();UpdateResponsive();ShowToolsMenu();check(menuLayer!=null&&FindChildren<RailSwitch>(menuShell).Count()==0&&FindChildren<Button>(menuShell).Count()==2,"narrow tall window menu contains import and export only");CloseMenu();
            Width=640;Height=520;UpdateLayout();UpdateResponsive();ShowToolsMenu();check(menuLayer!=null&&!main.IsEnabled&&FindChildren<RailSwitch>(menuShell).Count()==1&&FindChildren<Button>(menuShell).Count()==3,"small window collects all three hidden functions without duplicates");CloseMenu();check(menuLayer==null&&main.IsEnabled,"closing menu restores main interaction");
            File.WriteAllLines(path,lines);
        }
    }
}
