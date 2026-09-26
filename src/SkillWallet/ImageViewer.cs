using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace SkillWallet {
    public partial class MainWindow {
        Grid pictureLayer;Action closePicture,fitPicture;
        public void ViewPicture(SkillImage picture,FrameworkElement origin) {
            if(pictureLayer!=null)return;var source=Pictures.Bitmap(picture,0);
            var layer=new Grid {Background=UI.B("#F5090B0E"),Focusable=true};pictureLayer=layer;Panel.SetZIndex(layer,800);root.Children.Add(layer);
            bool mainEnabled=main.IsEnabled;main.IsEnabled=false;var previousOverlay=overlay;if(previousOverlay!=null)previousOverlay.IsEnabled=false;
            var canvas=new Canvas {ClipToBounds=true,Background=Brushes.Transparent};layer.Children.Add(canvas);
            var scale=new ScaleTransform(1,1);var move=new TranslateTransform();var transforms=new TransformGroup();transforms.Children.Add(scale);transforms.Children.Add(move);
            var image=new Image {Source=source,Width=source.PixelWidth,Height=source.PixelHeight,Stretch=Stretch.Fill,RenderTransform=transforms,Cursor=Cursors.Hand};canvas.Children.Add(image);
            var controls=UI.Stack(true);controls.HorizontalAlignment=HorizontalAlignment.Center;controls.VerticalAlignment=VerticalAlignment.Bottom;controls.Margin=new Thickness(20);layer.Children.Add(controls);
            double zoom=1;bool moving=false,closing=false;Point start=new Point(),pan=new Point();
            Action<double,double,double,bool> place=(z,x,y,animate)=>{zoom=z;if(animate){MotionFx.To(scale,ScaleTransform.ScaleXProperty,z,.24);MotionFx.To(scale,ScaleTransform.ScaleYProperty,z,.24);MotionFx.To(move,TranslateTransform.XProperty,x,.24);MotionFx.To(move,TranslateTransform.YProperty,y,.24);}else{scale.BeginAnimation(ScaleTransform.ScaleXProperty,null);scale.BeginAnimation(ScaleTransform.ScaleYProperty,null);move.BeginAnimation(TranslateTransform.XProperty,null);move.BeginAnimation(TranslateTransform.YProperty,null);scale.ScaleX=scale.ScaleY=z;move.X=x;move.Y=y;}};
            Action fit=()=>{double z=Math.Min((root.ActualWidth-100)/source.PixelWidth,(root.ActualHeight-120)/source.PixelHeight);place(z,(root.ActualWidth-source.PixelWidth*z)/2,(root.ActualHeight-65-source.PixelHeight*z)/2,true);};fitPicture=fit;
            Func<Rect> thumbnail=()=>{try{if(origin!=null&&origin.IsVisible){var point=origin.TransformToAncestor(root).Transform(new Point());return new Rect(point,new Size(origin.ActualWidth,origin.ActualHeight));}}catch{}return new Rect(root.ActualWidth*.4,root.ActualHeight*.4,root.ActualWidth*.2,root.ActualHeight*.2);};
            Rect from=thumbnail();double fromZoom=Math.Min(from.Width/source.PixelWidth,from.Height/source.PixelHeight);place(fromZoom,from.X,from.Y,false);layer.Opacity=0;MotionFx.Fade(layer,1,.22);fit();
            Action close=()=>{if(closing)return;closing=true;canvas.ReleaseMouseCapture();layer.IsHitTestVisible=false;Rect to=thumbnail();place(Math.Min(to.Width/source.PixelWidth,to.Height/source.PixelHeight),to.X,to.Y,true);MotionFx.Fade(layer,0,.22,()=>{root.Children.Remove(layer);if(pictureLayer==layer)pictureLayer=null;main.IsEnabled=mainEnabled;if(previousOverlay!=null)previousOverlay.IsEnabled=true;});};closePicture=close;
            controls.Children.Add(UI.Button("适应窗口",delegate{fit();}));controls.Children.Add(UI.Button("100%",delegate{place(1,(root.ActualWidth-source.PixelWidth)/2,(root.ActualHeight-65-source.PixelHeight)/2,true);}));controls.Children.Add(UI.Button("关闭 · Esc",delegate{close();}));
            var hint=UI.Text("滚轮缩放 · 拖动查看 · 双击适应窗口",11,UI.Muted,false);hint.HorizontalAlignment=HorizontalAlignment.Center;hint.VerticalAlignment=VerticalAlignment.Top;hint.Margin=new Thickness(20);hint.IsHitTestVisible=false;layer.Children.Add(hint);
            canvas.PreviewMouseWheel+=delegate(object sender,MouseWheelEventArgs e){Point p=e.GetPosition(canvas);double old=scale.ScaleX;double z=Math.Max(.02,Math.Min(8,zoom*Math.Pow(1.15,e.Delta/120.0)));place(z,p.X-(p.X-move.X)*z/old,p.Y-(p.Y-move.Y)*z/old,true);e.Handled=true;};
            canvas.MouseLeftButtonDown+=delegate(object sender,MouseButtonEventArgs e){if(e.ClickCount==2){fit();e.Handled=true;return;}place(scale.ScaleX,move.X,move.Y,false);moving=true;start=e.GetPosition(canvas);pan=new Point(move.X,move.Y);canvas.CaptureMouse();e.Handled=true;};
            canvas.MouseMove+=delegate(object sender,MouseEventArgs e){if(!moving)return;Point p=e.GetPosition(canvas);move.X=pan.X+p.X-start.X;move.Y=pan.Y+p.Y-start.Y;};
            canvas.MouseLeftButtonUp+=delegate{moving=false;canvas.ReleaseMouseCapture();};canvas.LostMouseCapture+=delegate{moving=false;};layer.SizeChanged+=delegate{if(!moving&&!closing)fit();};layer.Focus();
        }
    }
}
