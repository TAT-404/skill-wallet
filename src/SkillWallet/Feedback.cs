using System;
using System.IO;
using System.Media;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;

namespace SkillWallet {
    public class Preferences {
        public bool feedback {get;set;}
        public bool sound {get;set;}
        public static Preferences Read(string path){try{if(File.Exists(path))return Packs.Json().Deserialize<Preferences>(File.ReadAllText(path))??new Preferences {feedback=true};}catch{}return new Preferences {feedback=true,sound=false};}
        public void Save(string path){Directory.CreateDirectory(System.IO.Path.GetDirectoryName(path));string temp=path+".tmp";File.WriteAllText(temp,Packs.Json().Serialize(this));if(File.Exists(path))File.Replace(temp,path,null);else File.Move(temp,path);}
    }
    public static class MotionFx {
        public static bool Enabled=true;
        public static void To(System.Windows.Media.Animation.Animatable target,DependencyProperty property,double value,double seconds,Action done=null) {
            double current=(double)target.GetValue(property);target.BeginAnimation(property,null);target.SetValue(property,value);
            if(!Enabled||!SystemParameters.ClientAreaAnimation){if(done!=null)done();return;}
            var animation=new DoubleAnimation(current,value,TimeSpan.FromSeconds(seconds)){EasingFunction=new CubicEase {EasingMode=EasingMode.EaseOut},FillBehavior=FillBehavior.Stop};
            if(done!=null)animation.Completed+=delegate {done();};target.BeginAnimation(property,animation,HandoffBehavior.SnapshotAndReplace);
        }
        public static void Fade(UIElement element,double value,double seconds,Action done=null) {
            double current=element.Opacity;element.BeginAnimation(UIElement.OpacityProperty,null);element.Opacity=value;
            if(!Enabled||!SystemParameters.ClientAreaAnimation){if(done!=null)done();return;}
            var anim=new DoubleAnimation(current,value,TimeSpan.FromSeconds(seconds)){EasingFunction=new CubicEase {EasingMode=EasingMode.EaseOut},FillBehavior=FillBehavior.Stop};if(done!=null)anim.Completed+=delegate{done();};element.BeginAnimation(UIElement.OpacityProperty,anim);
        }
        public static void Width(FrameworkElement element,double value,double seconds){double current=Double.IsNaN(element.Width)?element.ActualWidth:element.Width;element.BeginAnimation(FrameworkElement.WidthProperty,null);element.Width=value;if(Enabled&&SystemParameters.ClientAreaAnimation)element.BeginAnimation(FrameworkElement.WidthProperty,new DoubleAnimation(current,value,TimeSpan.FromSeconds(seconds)){EasingFunction=new CubicEase {EasingMode=EasingMode.EaseOut},FillBehavior=FillBehavior.Stop});}
        public static void Height(FrameworkElement element,double value,double seconds){double current=Double.IsNaN(element.Height)?element.ActualHeight:element.Height;element.BeginAnimation(FrameworkElement.HeightProperty,null);element.Height=value;if(Enabled&&SystemParameters.ClientAreaAnimation)element.BeginAnimation(FrameworkElement.HeightProperty,new DoubleAnimation(current,value,TimeSpan.FromSeconds(seconds)){EasingFunction=new CubicEase {EasingMode=EasingMode.EaseOut},FillBehavior=FillBehavior.Stop});}
        public static void Press(Button button) {
            var scale=new ScaleTransform(1,1);button.RenderTransformOrigin=new Point(.5,.5);button.RenderTransform=scale;
            button.PreviewMouseLeftButtonDown+=delegate {To(scale,ScaleTransform.ScaleXProperty,.96,.08);To(scale,ScaleTransform.ScaleYProperty,.96,.08);};
            Action release=()=>{To(scale,ScaleTransform.ScaleXProperty,1,.18);To(scale,ScaleTransform.ScaleYProperty,1,.18);};
            button.PreviewMouseLeftButtonUp+=delegate {release();};button.LostMouseCapture+=delegate {release();};button.MouseLeave+=delegate{release();};
        }
    }
    public class RailSwitch : Button {
        public bool Value {get;private set;} readonly Border track;readonly TranslateTransform knobMove=new TranslateTransform();Color accent=Color.FromRgb(132,132,132);
        public RailSwitch(string label,bool value,Action<bool> change) {
            Style=Application.Current.TryFindResource(typeof(Button)) as Style;MotionFx.Press(this);
            BorderThickness=new Thickness(0);Padding=new Thickness(4,3,4,3);Background=Brushes.Transparent;Margin=new Thickness(9,0,0,0);ToolTip=label;Value=value;
            var row=UI.Stack(true);var text=UI.Text(label,10,UI.Muted,false);text.VerticalAlignment=VerticalAlignment.Center;text.Margin=new Thickness(0,0,7,0);row.Children.Add(text);
            var inside=new Grid {Width=30,Height=17};track=new Border {CornerRadius=new CornerRadius(9),Background=new SolidColorBrush()};inside.Children.Add(track);
            inside.Children.Add(new Ellipse {Width=11,Height=11,Fill=UI.Ink,HorizontalAlignment=HorizontalAlignment.Left,Margin=new Thickness(3,0,0,0),RenderTransform=knobMove});row.Children.Add(inside);Content=row;
            Click+=delegate{Value=!Value;Draw();change(Value);};Draw();
        }
        public void Tint(Color color){}
        public void SetValue(bool value){Value=value;Draw();}
        void Draw(){MotionFx.To(knobMove,TranslateTransform.XProperty,Value?13:0,.2);var fill=(SolidColorBrush)track.Background;Color target=Value?accent:Color.FromRgb(48,48,48),current=fill.Color;fill.Color=target;if(MotionFx.Enabled&&SystemParameters.ClientAreaAnimation)fill.BeginAnimation(SolidColorBrush.ColorProperty,new ColorAnimation(current,target,TimeSpan.FromSeconds(.2)){FillBehavior=FillBehavior.Stop});System.Windows.Automation.AutomationProperties.SetName(this,(ToolTip??"")+"，"+(Value?"已开启":"已关闭"));}
    }
}
