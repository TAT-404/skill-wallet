using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace SkillWallet {
    public partial class MainWindow {
        public void RunCoverChecks(string imagePath,string report) {
            var lines=new List<string>();Action<bool,string> check=(ok,label)=>{if(!ok)throw new Exception(label);lines.Add("PASS "+label);};
            MotionFx.Enabled=false;FreezeMotion();
            var photo=Pictures.Load(imagePath);var sample=Samples.Create();var target=sample[3];
            target.name="Amazon产品视觉策略师";target.description="主图、附图与 A+ 页面规划。";
            target.category="图像处理";target.tags=new[]{"电商","视觉","规划"};
            skills=new List<Skill>{sample[4],sample[2],target,sample[1],sample[5]};selected=2;category="全部";favorites=false;query="";Refresh(false);RebuildFilters();
            var before=Palette.Accent(target);target=target.Clone();target.pictures=new List<SkillImage>{photo};target.coverId=photo.id;skills[2]=target;Refresh(false);UpdateLayout();ambientReady=false;Position(false);
            var colors=Palette.ColorsFor(target);var primary=colors[0];var secondary=colors[1];var card=cards.Single(c=>c.Skill.id==target.id);
            check(primary.B>primary.R*2&&primary.B>primary.G,"provided blue poster chooses blue primary instead of small orange accent");
            check(secondary.R>secondary.B*2,"orange remains a secondary accent only");
            check(before!=primary,"replacing a built-in cover invalidates its old palette");
            var panel=(LinearGradientBrush)card.Info.Background;
            check(panel.GradientStops.Last().Color==primary,"information panel lower rim uses the same image primary");
            var topLight=((Grid)card.Info.Child).Children.OfType<Border>().Select(b=>b.Background).OfType<RadialGradientBrush>().Single().GradientStops[0].Color;
            check(topLight.R==primary.R&&topLight.G==primary.G&&topLight.B==primary.B,"information panel upper glow and lower rim use the same primary hue");
            check(ambientColor==primary&&topGlow.Color.B==primary.B&&bottomGlow.Color.B==primary.B&&bottomGlow.Color.R==primary.R,"top and bottom ambient lights use the new primary");
            check(ambientSecondary==secondary,"ambient accent uses the image secondary");
            check(((ImageBrush)card.Picture.Background).Stretch==Stretch.Uniform,"custom cover is fitted without fill cropping");
            check(card.Picture.Height<=470-card.Info.Height,"custom image ends above information panel without overlap");
            check(((SolidColorBrush)card.Face.Background).Color.R>220,"letterbox gaps match the supplied white poster background");
            var original=target.Clone();target=target.Clone();target.coverId=null;skills[2]=target;Refresh(false);ambientReady=false;Position(false);
            check(Palette.Accent(target)==before&&ambientColor==before,"removing cover restores both default palette and light");
            skills[2]=original;Refresh(false);ambientReady=false;Position(false);
            check(ambientColor==primary&&((LinearGradientBrush)cards.Single(c=>c.Skill.id==target.id).Info.Background).GradientStops.Last().Color==primary,"reselecting uploaded cover restores its complete palette");
            lines.Add("Primary: "+primary+" Secondary: "+secondary);File.WriteAllLines(report,lines);
        }
    }
}
