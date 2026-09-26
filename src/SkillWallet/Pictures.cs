using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Security.Cryptography;
using System.Text.RegularExpressions;
using System.Web.Script.Serialization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Microsoft.Win32;

namespace SkillWallet {
    public class SkillImage {
        public string id {get;set;}
        public string name {get;set;}
        [ScriptIgnore] public byte[] data {get;set;}
        public SkillImage Clone(){return (SkillImage)MemberwiseClone();}
    }
    public static class Pictures {
        public const int MaxImage=12*1024*1024,MaxTotal=128*1024*1024;
        static readonly Dictionary<string,BitmapSource> cache=new Dictionary<string,BitmapSource>();
        public static string Hash(byte[] data){using(var sha=SHA256.Create())return BitConverter.ToString(sha.ComputeHash(data)).Replace("-","").ToLowerInvariant();}
        public static void Check(byte[] data) {
            if(data==null||data.Length<8||data.Length>MaxImage)throw new InvalidDataException("单张图片须小于 12 MB。");
            bool png=data[0]==137&&data[1]==80&&data[2]==78&&data[3]==71, jpg=data[0]==255&&data[1]==216&&data[2]==255;
            if(!png&&!jpg)throw new InvalidDataException("请选择 PNG 或 JPG 图片。");
            using(var stream=new MemoryStream(data,false)) {
                var decoder=BitmapDecoder.Create(stream,BitmapCreateOptions.DelayCreation,BitmapCacheOption.None);
                var frame=decoder.Frames[0];
                if(frame.PixelWidth<1||frame.PixelHeight<1||frame.PixelWidth>16384||frame.PixelHeight>16384||(long)frame.PixelWidth*frame.PixelHeight>40000000)throw new InvalidDataException("图片尺寸过大，请使用 4000 万像素以内的图片。");
            }
        }
        public static SkillImage Load(string path) {
            if(new FileInfo(path).Length>MaxImage)throw new InvalidDataException("单张图片须小于 12 MB。");
            var data=File.ReadAllBytes(path);Check(data);var p=new SkillImage {id=Hash(data),name=Path.GetFileName(path),data=data};Bitmap(p,720);return p;
        }
        public static void Validate(List<Skill> skills) {
            long total=0;var seen=new Dictionary<string,byte[]>();
            foreach(var s in skills) {
                if(s.pictures==null)s.pictures=new List<SkillImage>();
                if(s.pictures.Count>12)throw new InvalidDataException("每张 Skill 最多添加 12 张图片。");
                var ids=new HashSet<string>();
                foreach(var p in s.pictures) {
                    if(p==null||!Regex.IsMatch(p.id??"","\\A[a-f0-9]{64}\\z")||!ids.Add(p.id)||String.IsNullOrEmpty(p.name)||p.name.Length>260)throw new InvalidDataException("图片信息不正确。");
                    byte[] previous;
                    if(seen.TryGetValue(p.id,out previous)&&Object.ReferenceEquals(previous,p.data))continue;
                    Check(p.data);if(Hash(p.data)!=p.id)throw new InvalidDataException("图片内容校验失败。");
                    if(!seen.ContainsKey(p.id)){total+=p.data.Length;seen.Add(p.id,p.data);}
                    if(total>MaxTotal||seen.Count>2048)throw new InvalidDataException("技能库图片总量不能超过 128 MB / 2048 张。");
                }
                if(String.IsNullOrEmpty(s.coverId))s.coverId=null;
                else if(!ids.Contains(s.coverId))throw new InvalidDataException("封面图片不存在。");
            }
        }
        public static void ReadAssets(ZipArchive zip,List<Skill> skills) {
            if(skills==null||skills.Count>5000||skills.Any(s=>s==null))throw new InvalidDataException("技能库内容无效。");
            var loaded=new Dictionary<string,byte[]>();long total=0;
            foreach(var s in skills) {
                if(s.pictures==null)continue;
                if(s.pictures.Count>12)throw new InvalidDataException("卡牌图片过多。");
                foreach(var p in s.pictures) {
                    if(p==null||!Regex.IsMatch(p.id??"","\\A[a-f0-9]{64}\\z"))throw new InvalidDataException("图片 ID 无效。");
                    byte[] data;
                    if(!loaded.TryGetValue(p.id,out data)) {
                        var matches=zip.Entries.Where(e=>e.FullName=="assets/"+p.id).ToArray();
                        if(matches.Length!=1||matches[0].Length>MaxImage)throw new InvalidDataException("图片缺失、重复或体积过大。");
                        if(total+matches[0].Length>MaxTotal)throw new InvalidDataException("技能库图片总量过大。");
                        using(var input=matches[0].Open())using(var output=new MemoryStream()) {
                            var buffer=new byte[8192];int n;
                            while((n=input.Read(buffer,0,buffer.Length))>0){if(output.Length+n>MaxImage)throw new InvalidDataException("图片体积过大。");output.Write(buffer,0,n);}
                            data=output.ToArray();
                        }
                        total+=data.Length;if(total>MaxTotal)throw new InvalidDataException("图片总量过大。");loaded.Add(p.id,data);
                    }
                    p.data=data;
                }
            }
        }
        public static void WriteAssets(ZipArchive zip,List<Skill> skills) {
            foreach(var p in skills.SelectMany(s=>s.pictures).GroupBy(p=>p.id).Select(g=>g.First()))
                using(var stream=zip.CreateEntry("assets/"+p.id,CompressionLevel.NoCompression).Open())stream.Write(p.data,0,p.data.Length);
        }
        public static BitmapSource Bitmap(SkillImage p,int width) {
            BitmapSource found;if(width==720&&cache.TryGetValue(p.id,out found))return found;
            using(var stream=new MemoryStream(p.data,false)) {
                var bitmap=new BitmapImage();bitmap.BeginInit();bitmap.CacheOption=BitmapCacheOption.OnLoad;bitmap.DecodePixelWidth=width;bitmap.StreamSource=stream;bitmap.EndInit();bitmap.Freeze();
                if(width==720){if(cache.Count>=24)cache.Clear();cache[p.id]=bitmap;}return bitmap;
            }
        }
        public static Brush Cover(Skill s) {
            var p=(s.pictures??new List<SkillImage>()).FirstOrDefault(x=>x.id==s.coverId);
            if(p==null)return CardArt.Cover(s.theme);
            var brush=new ImageBrush(Bitmap(p,720)){Stretch=Stretch.Uniform};brush.Freeze();return brush;
        }
        public static bool HasCover(Skill s){return (s.pictures??new List<SkillImage>()).Any(p=>p.id==s.coverId);}
        public static Brush CoverBackdrop(Skill s) {
            var p=(s.pictures??new List<SkillImage>()).FirstOrDefault(x=>x.id==s.coverId);if(p==null)return Brushes.Black;
            var bitmap=new FormatConvertedBitmap(Bitmap(p,32),PixelFormats.Bgra32,null,0);int w=bitmap.PixelWidth,h=bitmap.PixelHeight,stride=w*4;var pixels=new byte[stride*h];bitmap.CopyPixels(pixels,stride,0);
            double r=0,g=0,b=0;int count=0;
            for(int y=0;y<h;y++)for(int x=0;x<w;x++)if((x<2||x>=w-2)&&(y<2||y>=h-2)){int i=y*stride+x*4;double alpha=pixels[i+3]/255.0;r+=pixels[i+2]*alpha+12*(1-alpha);g+=pixels[i+1]*alpha+14*(1-alpha);b+=pixels[i]*alpha+16*(1-alpha);count++;}
            var brush=new SolidColorBrush(Color.FromRgb((byte)(r/count),(byte)(g/count),(byte)(b/count)));brush.Freeze();return brush;
        }
        public static void View(Window owner,SkillImage p,FrameworkElement origin=null) {
            var wallet=owner as MainWindow;if(wallet!=null){wallet.ViewPicture(p,origin);return;}
            var window=new Window {Owner=owner,Title=p.name,Width=900,Height=700,MinWidth=400,MinHeight=300,WindowStartupLocation=WindowStartupLocation.CenterOwner,Background=UI.Paper,ShowInTaskbar=false};
            var layout=new DockPanel {Margin=new Thickness(20)};window.Content=layout;
            var footer=UI.Stack(true);footer.HorizontalAlignment=HorizontalAlignment.Right;DockPanel.SetDock(footer,Dock.Bottom);layout.Children.Add(footer);
            var image=new Image {Source=Bitmap(p,0),Stretch=Stretch.Uniform};var scroll=new ScrollViewer {HorizontalScrollBarVisibility=ScrollBarVisibility.Auto,VerticalScrollBarVisibility=ScrollBarVisibility.Auto,Content=image};
            Action fit=()=>{image.Width=Math.Max(100,window.ActualWidth-62);image.Height=Math.Max(100,window.ActualHeight-115);};
            footer.Children.Add(UI.Button("适应窗口",delegate {fit();}));footer.Children.Add(UI.Button("原始尺寸",delegate {var b=(BitmapSource)image.Source;image.Width=b.PixelWidth;image.Height=b.PixelHeight;}));footer.Children.Add(UI.Button("关闭",delegate {window.Close();}));
            layout.Children.Add(scroll);window.Loaded+=delegate {fit();};window.PreviewKeyDown+=delegate(object sender,System.Windows.Input.KeyEventArgs e){if(e.Key==System.Windows.Input.Key.Escape)window.Close();};window.SourceInitialized+=delegate {DarkWindowFrame.Apply(window);};window.ShowDialog();
        }
    }
    public class PictureEditor : StackPanel {
        public List<SkillImage> Items; public string CoverId; readonly Window owner;readonly WrapPanel thumbs=new WrapPanel();readonly TextBlock error=UI.Text("",11,UI.B("#EEEEEE"),false);
        public PictureEditor(Window window,Skill skill) {
            owner=window;Items=(skill==null?new List<SkillImage>():skill.pictures??new List<SkillImage>()).Select(p=>p.Clone()).ToList();CoverId=skill==null?null:skill.coverId;
            Margin=new Thickness(0,16,0,18);Children.Add(UI.Text("效果图片（可选）",12,UI.Muted,true));
            var hint=UI.Text("PNG / JPG · 最多 12 张 · 不设置封面时使用默认图",11,UI.Muted,false);hint.Margin=new Thickness(0,7,0,10);Children.Add(hint);Children.Add(thumbs);
            var add=UI.Button("＋ 添加图片",delegate {Add();});add.HorizontalAlignment=HorizontalAlignment.Left;Children.Add(add);Children.Add(error);Draw();
        }
        public string Signature(){return String.Join(",",Items.Select(p=>p.id))+"|"+CoverId;}
        public void Apply(Skill s){s.pictures=Items.Select(p=>p.Clone()).ToList();s.coverId=CoverId;}
        void Add() {
            var dialog=new OpenFileDialog {Filter="图片 PNG / JPG|*.png;*.jpg;*.jpeg",Multiselect=true};if(dialog.ShowDialog(owner)!=true)return;
            try {
                var incoming=dialog.FileNames.Select(Pictures.Load).ToList();var merged=Items.Concat(incoming).GroupBy(p=>p.id).Select(g=>g.First()).ToList();
                if(merged.Count>12)throw new InvalidDataException("每张 Skill 最多添加 12 张图片。");
                if(Items.Count==0&&incoming.Count>0)CoverId=incoming[0].id;Items=merged;error.Text="";Draw();
            }catch(Exception ex){error.Text=ex.Message;}
        }
        void Draw() {
            thumbs.Children.Clear();foreach(var p in Items) {
                var card=UI.Stack(false);card.Width=124;card.Margin=new Thickness(0,0,8,12);thumbs.Children.Add(card);
                Button preview=null;preview=UI.Button("",delegate {Pictures.View(owner,p,preview);});preview.Padding=new Thickness(0);preview.Content=new Image {Source=Pictures.Bitmap(p,720),Height=83,Stretch=Stretch.UniformToFill};preview.ToolTip=p.name;card.Children.Add(preview);
                var cover=UI.Button(CoverId==p.id?"✓ 封面 · 取消":"设为封面",delegate {CoverId=CoverId==p.id?null:p.id;Draw();});cover.FontSize=10;cover.Padding=new Thickness(4,5,4,5);card.Children.Add(cover);
                var remove=UI.Button("移除",delegate {Items.Remove(p);if(CoverId==p.id)CoverId=null;Draw();});remove.FontSize=10;remove.Padding=new Thickness(4,4,4,4);remove.BorderThickness=new Thickness(0);card.Children.Add(remove);
            }
        }
    }
}
