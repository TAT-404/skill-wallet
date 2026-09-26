using System;
using System.IO;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace SkillWallet {
    public static class CardArt {
        static BitmapSource atlas;
        static readonly ImageBrush[] covers=new ImageBrush[6];
        public static int Index(string theme){switch(theme){case "lime":return 1;case "peach":return 2;case "mint":return 3;case "pink":return 4;case "purple":return 5;default:return 0;}}
        public static Brush Cover(string theme) {
            int index=Index(theme);if(covers[index]!=null)return covers[index];
            if(atlas==null)using(var stream=Assembly.GetExecutingAssembly().GetManifestResourceStream("SkillWallet.covers.png")) {
                if(stream==null)throw new InvalidDataException("缺少内置封面素材，请重新下载完整程序。");
                var bitmap=new BitmapImage();bitmap.BeginInit();bitmap.CacheOption=BitmapCacheOption.OnLoad;bitmap.StreamSource=stream;bitmap.EndInit();bitmap.Freeze();atlas=bitmap;
            }
            var brush=new ImageBrush(atlas) {ViewboxUnits=BrushMappingMode.RelativeToBoundingBox,Viewbox=new Rect((index%3)/3.0,(index/3)/2.0,1.0/3,1.0/2),Stretch=Stretch.UniformToFill};brush.Freeze();covers[index]=brush;return brush;
        }
        public static Color Accent(string theme){switch(theme){case "lime":return Color.FromRgb(77,165,79);case "peach":return Color.FromRgb(222,170,42);case "mint":return Color.FromRgb(49,111,225);case "pink":return Color.FromRgb(122,167,217);case "purple":return Color.FromRgb(159,97,207);default:return Color.FromRgb(235,112,52);}}
        public static Color Mix(Color a,Color b,double t){t=Math.Max(0,Math.Min(1,t));return Color.FromArgb((byte)(a.A+(b.A-a.A)*t),(byte)(a.R+(b.R-a.R)*t),(byte)(a.G+(b.G-a.G)*t),(byte)(a.B+(b.B-a.B)*t));}
        public static Brush Panel(string theme) {
            var color=Accent(theme);var top=Mix(color,Colors.Black,.40);top.A=235;
            var bottom=Mix(color,Colors.Black,.17);
            return new LinearGradientBrush(top,bottom,90);
        }
    }
    public partial class MainWindow {
        const double BottomLedGain=.8;
        GradientStop topGlow,bottomGlow;
        RadialGradientBrush topLed,bottomLed;
        GradientStop auraColor,coreColor,sideColor;RadialGradientBrush aura,core,side;
        Color ambientColor,ambientSecondary;bool ambientReady;double lastLedTime;
        System.Windows.Threading.DispatcherTimer ledTimer;
        void StartLed() {
            ledTimer=new System.Windows.Threading.DispatcherTimer {Interval=TimeSpan.FromMilliseconds(System.Windows.Media.RenderCapability.Tier>>16==0?80:40)};
            ledTimer.Tick+=delegate {if(WindowState!=WindowState.Minimized&&(IsActive||testMode))UpdateGlow();};ledTimer.Start();
        }
        void AddBackdrop() {
            root.Background=UI.B("#090A0C");
            var field=new RadialGradientBrush {Center=new Point(.5,.52),GradientOrigin=new Point(.5,.52),RadiusX=.84,RadiusY=.60};
            field.GradientStops.Add(new GradientStop(Color.FromArgb(80,46,48,52),0));field.GradientStops.Add(new GradientStop(Color.FromArgb(25,46,48,52),.62));field.GradientStops.Add(new GradientStop(Color.FromArgb(0,46,48,52),1));
            root.Children.Add(new Border {Background=field,IsHitTestVisible=false});
            topLed=Light(.48,-.09,.70,.36,out topGlow);bottomLed=Light(.53,1.09,.64,.39,out bottomGlow);
            aura=Light(.50,1.05,.38,.30,out auraColor);core=Light(.48,1.08,.24,.19,out coreColor);side=Light(.68,1.04,.25,.26,out sideColor);
            // Very low opacity monochrome texture softens quantization on dark displays.
            var noise=new byte[64*64*4];uint seed=9127;for(int i=0;i<noise.Length;i+=4){seed=seed*1664525+1013904223;noise[i+3]=(byte)((seed>>24)%4);}
            var texture=BitmapSource.Create(64,64,96,96,PixelFormats.Bgra32,null,noise,256);texture.Freeze();var brush=new ImageBrush(texture){TileMode=TileMode.Tile,ViewportUnits=BrushMappingMode.Absolute,Viewport=new Rect(0,0,64,64)};brush.Freeze();root.Children.Add(new Border {Background=brush,IsHitTestVisible=false});
        }
        RadialGradientBrush Light(double x,double y,double rx,double ry,out GradientStop stop) {
            var brush=new RadialGradientBrush {Center=new Point(x,y),GradientOrigin=new Point(x,y),RadiusX=rx,RadiusY=ry};
            foreach(double offset in new[]{0,.23,.5,.76,1})brush.GradientStops.Add(new GradientStop(Colors.Transparent,offset));
            stop=brush.GradientStops[0];root.Children.Add(new Border {Background=brush,IsHitTestVisible=false});return brush;
        }
        static void PaintLight(RadialGradientBrush brush,Color color,double alpha) {
            double[] weights={1,.72,.24,.035,0};for(int i=0;i<weights.Length;i++)brush.GradientStops[i].Color=Color.FromArgb((byte)Math.Max(0,Math.Min(255,alpha*weights[i])),color.R,color.G,color.B);
        }
        void UpdateGlow() {
            if(topGlow==null)return;
            double position=Math.Max(0,Math.Min(filtered.Count-1,motion.Position));int left=(int)Math.Floor(position),right=Math.Min(filtered.Count-1,left+1);
            double time=SystemParameters.ClientAreaAnimation?motionClock.Elapsed.TotalSeconds:0;
            var target=filtered.Count==0?Color.FromRgb(100,100,100):CardArt.Mix(Palette.ColorsFor(filtered[left])[0],Palette.ColorsFor(filtered[right])[0],position-left);
            var secondary=filtered.Count==0?target:CardArt.Mix(Palette.ColorsFor(filtered[left])[1],Palette.ColorsFor(filtered[right])[1],position-left);
            double now=motionClock.Elapsed.TotalSeconds,dt=Math.Max(0,now-lastLedTime);lastLedTime=now;
            if(manageReordering&&ambientReady){target=ambientColor;secondary=ambientSecondary;}
            if(!ambientReady||!SystemParameters.ClientAreaAnimation){ambientColor=target;ambientSecondary=secondary;ambientReady=true;}else {ambientColor=CardArt.Mix(ambientColor,target,1-Math.Exp(-dt*4));ambientSecondary=CardArt.Mix(ambientSecondary,secondary,1-Math.Exp(-dt*3.5));}
            PaintLight(topLed,ambientColor,55*(1+.09*Math.Sin(time*.48)));
            PaintLight(bottomLed,ambientColor,BottomLedGain*83*(1+.10*Math.Sin(time*.51+1.6)));
            PaintLight(aura,ambientColor,BottomLedGain*105*(1+.11*Math.Sin(time*.63+.7)));
            PaintLight(core,ambientColor,BottomLedGain*170*(1+.08*Math.Sin(time*.54+2.4)));
            PaintLight(side,ambientSecondary,BottomLedGain*65*(1+.13*Math.Sin(time*.43+3.1)));
            topLed.Center=topLed.GradientOrigin=new Point(.48+.06*Math.Sin(time*.24),-.07);
            bottomLed.Center=bottomLed.GradientOrigin=new Point(.53+.045*Math.Sin(time*.28+1),1.07);
            core.Center=core.GradientOrigin=new Point(.48+.06*Math.Sin(time*.32),1.07);
            aura.Center=aura.GradientOrigin=new Point(.51+.05*Math.Sin(time*.26+2),1.04);
            side.Center=side.GradientOrigin=new Point(.65+.07*Math.Sin(time*.27+3),1.02);
            aura.RadiusX=.38+.035*Math.Sin(time*.33);topLed.RadiusX=.70+.045*Math.Sin(time*.31+1);
        }
    }
}
