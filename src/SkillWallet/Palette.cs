using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace SkillWallet {
    public static class Palette {
        static readonly Dictionary<string,Color[]> colors=new Dictionary<string,Color[]>();
        public static Color[] ColorsFor(Skill skill) {
            var photo=(skill.pictures??new List<SkillImage>()).FirstOrDefault(p=>p.id==skill.coverId);
            string key=photo==null?"builtin:"+skill.theme:photo.id;Color[] result;
            if(colors.TryGetValue(key,out result))return result;
            BitmapSource bitmap;
            if(photo!=null)bitmap=Pictures.Bitmap(photo,64);
            else {var brush=(ImageBrush)CardArt.Cover(skill.theme);var source=(BitmapSource)brush.ImageSource;var rect=brush.Viewbox;bitmap=new CroppedBitmap(source,new Int32Rect((int)(rect.X*source.PixelWidth),(int)(rect.Y*source.PixelHeight),(int)(rect.Width*source.PixelWidth),(int)(rect.Height*source.PixelHeight)));bitmap=new TransformedBitmap(bitmap,new ScaleTransform(64.0/bitmap.PixelWidth,64.0/bitmap.PixelHeight));}
            result=ExtractColors(bitmap);if(colors.Count>256)colors.Clear();colors[key]=result;return result;
        }
        public static Color Accent(Skill skill){return ColorsFor(skill)[0];}
        public static Color Extract(BitmapSource bitmap){return ExtractColors(bitmap)[0];}
        public static double Saturation(Color c){double hi=Math.Max(c.R,Math.Max(c.G,c.B)),lo=Math.Min(c.R,Math.Min(c.G,c.B));return hi==0?0:(hi-lo)/hi;}
        public static Color Vivid(Color c) {
            double hi=Math.Max(c.R,Math.Max(c.G,c.B)),lo=Math.Min(c.R,Math.Min(c.G,c.B));
            if(hi==0||Saturation(c)<.18)return Color.FromRgb(133,133,133);
            double floor=lo*.22,range=hi-floor;
            return Color.FromRgb((byte)(255*(c.R-floor)/range),(byte)(255*(c.G-floor)/range),(byte)(255*(c.B-floor)/range));
        }
        static Color[] ExtractColors(BitmapSource bitmap) {
            var converted=new FormatConvertedBitmap(bitmap,PixelFormats.Bgra32,null,0);int stride=converted.PixelWidth*4;
            var pixels=new byte[stride*converted.PixelHeight];converted.CopyPixels(pixels,stride,0);
            var bins=new Dictionary<int,double[]>();
            for(int i=0;i<pixels.Length;i+=4) {
                if(pixels[i+3]<180)continue;double r=pixels[i+2]/255.0,g=pixels[i+1]/255.0,b=pixels[i]/255.0;
                double hi=Math.Max(r,Math.Max(g,b)),lo=Math.Min(r,Math.Min(g,b)),sat=hi==0?0:(hi-lo)/hi;
                if(hi<.12||sat<.23)continue;
                // Combine shades in one hue family so a small bright accent cannot
                // outrank a large blue/green subject split over many RGB bins.
                double delta=hi-lo,hue=hi==r?((g-b)/delta)%6:hi==g?(b-r)/delta+2:(r-g)/delta+4;
                if(hue<0)hue+=6;int key=(int)(hue*2)%12;double[] bin;
                if(!bins.TryGetValue(key,out bin)){bin=new double[5];bins.Add(key,bin);}
                double weight=Math.Pow(sat,.7)*Math.Sqrt(hi);
                bin[0]+=r*weight;bin[1]+=g*weight;bin[2]+=b*weight;bin[3]+=weight;bin[4]+=weight;
            }
            if(bins.Count==0)return new[]{Color.FromRgb(133,133,133),Color.FromRgb(96,96,96)};
            var ranked=bins.OrderByDescending(v=>v.Value[4]).ToList();
            Func<double[],Color> color=v=>Vivid(Color.FromRgb((byte)(v[0]/v[3]*255),(byte)(v[1]/v[3]*255),(byte)(v[2]/v[3]*255)));
            Color first=color(ranked[0].Value);
            Color second=ranked.Where(v=>Math.Min(Math.Abs(v.Key-ranked[0].Key),12-Math.Abs(v.Key-ranked[0].Key))>=2&&v.Value[4]>=ranked[0].Value[4]*.04).Select(v=>color(v.Value)).FirstOrDefault();
            if(second.A==0)second=first;return new[]{first,second};
        }
        static double Linear(byte c){double n=c/255.0;return n<=.04045?n/12.92:Math.Pow((n+.055)/1.055,2.4);}
        public static double Luminance(Color c){return .2126*Linear(c.R)+.7152*Linear(c.G)+.0722*Linear(c.B);}
        public static Color Readable(Color c){while(Luminance(c)>.16)c=CardArt.Mix(c,Colors.Black,.06);c.A=255;return c;}
        public static Brush Panel(Skill skill) {
            var color=Accent(skill);var dark=CardArt.Mix(color,Colors.Black,.94);
            return new LinearGradientBrush(new GradientStopCollection {
                new GradientStop(CardArt.Mix(color,Colors.Black,.88),0),
                new GradientStop(dark,.24),new GradientStop(dark,.68),
                new GradientStop(CardArt.Mix(color,Colors.Black,.80),.83),
                new GradientStop(CardArt.Mix(color,Colors.Black,.28),.95),new GradientStop(color,1)
            },90);
        }
    }
}
