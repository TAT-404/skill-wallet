using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Markup;
using System.Windows.Shapes;

namespace SkillWallet {
    public static class UI {
        public static readonly FontFamily Typeface=new FontFamily("Segoe UI, Microsoft YaHei UI");
        public static Brush Ink = B("#F2F3F5"), Muted = B("#A4A4A4"), Paper = B("#111111"), Line = B("#393939");
        public static Brush B(string hex) { var brush = new SolidColorBrush((Color)ColorConverter.ConvertFromString(hex)); brush.Freeze(); return brush; }
        public static void Install(Application app) {
            app.Resources = (ResourceDictionary)XamlReader.Parse(@"<ResourceDictionary xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation' xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'>
<Style TargetType='Button'>
 <Setter Property='FontFamily' Value='Segoe UI, Microsoft YaHei UI'/><Setter Property='FontSize' Value='13'/><Setter Property='Foreground' Value='#F1F1F1'/><Setter Property='Background' Value='#222222'/><Setter Property='BorderBrush' Value='#444444'/><Setter Property='BorderThickness' Value='0.7'/><Setter Property='Padding' Value='16,9'/><Setter Property='Cursor' Value='Hand'/><Setter Property='VerticalContentAlignment' Value='Center'/><Setter Property='HorizontalContentAlignment' Value='Center'/><Setter Property='FocusVisualStyle' Value='{x:Null}'/>
 <Setter Property='Template'><Setter.Value><ControlTemplate TargetType='Button'>
 <Border x:Name='box' CornerRadius='10' Background='{TemplateBinding Background}' BorderBrush='{TemplateBinding BorderBrush}' BorderThickness='{TemplateBinding BorderThickness}'>
 <Grid><Border x:Name='hover' CornerRadius='9' Background='White' Opacity='0' IsHitTestVisible='False'/><Border x:Name='pressed' CornerRadius='9' Background='Black' Opacity='0' IsHitTestVisible='False'/><ContentPresenter Margin='{TemplateBinding Padding}' HorizontalAlignment='{TemplateBinding HorizontalContentAlignment}' VerticalAlignment='{TemplateBinding VerticalContentAlignment}'/></Grid></Border>
 <ControlTemplate.Triggers>
 <Trigger Property='IsMouseOver' Value='True'><Trigger.EnterActions><BeginStoryboard><Storyboard><DoubleAnimation Storyboard.TargetName='hover' Storyboard.TargetProperty='Opacity' To='0.07' Duration='0:0:0.14'/></Storyboard></BeginStoryboard></Trigger.EnterActions><Trigger.ExitActions><BeginStoryboard><Storyboard><DoubleAnimation Storyboard.TargetName='hover' Storyboard.TargetProperty='Opacity' To='0' Duration='0:0:0.18'/></Storyboard></BeginStoryboard></Trigger.ExitActions></Trigger>
 <Trigger Property='IsPressed' Value='True'><Setter TargetName='pressed' Property='Opacity' Value='0.17'/></Trigger>
 <Trigger Property='IsEnabled' Value='False'><Setter Property='Opacity' Value='0.32'/></Trigger>
 <Trigger Property='IsKeyboardFocused' Value='True'><Setter TargetName='box' Property='BorderBrush' Value='#EEEEEE'/></Trigger>
 </ControlTemplate.Triggers></ControlTemplate></Setter.Value></Setter>
</Style>
<Style TargetType='TextBox'><Setter Property='FontFamily' Value='Segoe UI, Microsoft YaHei UI'/><Setter Property='FontSize' Value='14'/><Setter Property='Foreground' Value='#F2F3F5'/><Setter Property='Background' Value='#202020'/><Setter Property='BorderBrush' Value='#3C3C3C'/><Setter Property='BorderThickness' Value='1'/><Setter Property='Padding' Value='12,10'/><Setter Property='VerticalContentAlignment' Value='Center'/><Setter Property='Template'><Setter.Value><ControlTemplate TargetType='TextBox'><Border x:Name='frame' CornerRadius='9' BorderBrush='{TemplateBinding BorderBrush}' BorderThickness='{TemplateBinding BorderThickness}' Background='{TemplateBinding Background}'><ScrollViewer x:Name='PART_ContentHost' Margin='{TemplateBinding Padding}'/></Border><ControlTemplate.Triggers><Trigger Property='IsKeyboardFocused' Value='True'><Setter TargetName='frame' Property='BorderBrush' Value='#B7B7B7'/></Trigger></ControlTemplate.Triggers></ControlTemplate></Setter.Value></Setter></Style>
<Style TargetType='ToolTip'><Setter Property='Background' Value='#242424'/><Setter Property='Foreground' Value='#EEEEEE'/><Setter Property='FontFamily' Value='Segoe UI, Microsoft YaHei UI'/><Setter Property='FontSize' Value='12'/><Setter Property='Padding' Value='12,8'/><Setter Property='Template'><Setter.Value><ControlTemplate TargetType='ToolTip'><Border Background='{TemplateBinding Background}' BorderBrush='#444444' BorderThickness='0.7' CornerRadius='9' Padding='{TemplateBinding Padding}'><ContentPresenter/></Border></ControlTemplate></Setter.Value></Setter></Style>
<Style TargetType='RadioButton'><Setter Property='Foreground' Value='#EEEEEE'/><Setter Property='Cursor' Value='Hand'/><Setter Property='Template'><Setter.Value><ControlTemplate TargetType='RadioButton'><StackPanel Orientation='Horizontal'><Grid Width='18' Height='18'><Ellipse Stroke='#777777' StrokeThickness='1' Fill='#242424'/><Ellipse x:Name='mark' Width='8' Height='8' Fill='#EEEEEE' Visibility='Collapsed'/></Grid><ContentPresenter Margin='9,0,0,0' VerticalAlignment='Center'/></StackPanel><ControlTemplate.Triggers><Trigger Property='IsChecked' Value='True'><Setter TargetName='mark' Property='Visibility' Value='Visible'/></Trigger><Trigger Property='IsKeyboardFocused' Value='True'><Setter Property='Foreground' Value='White'/></Trigger></ControlTemplate.Triggers></ControlTemplate></Setter.Value></Setter></Style>
<Style TargetType='CheckBox'><Setter Property='Foreground' Value='#EEEEEE'/><Setter Property='Cursor' Value='Hand'/><Setter Property='Template'><Setter.Value><ControlTemplate TargetType='CheckBox'><StackPanel Orientation='Horizontal'><Border x:Name='check' Width='18' Height='18' CornerRadius='5' BorderBrush='#707070' BorderThickness='1' Background='#272727'><TextBlock x:Name='tick' Text='✓' Foreground='#171717' FontSize='13' HorizontalAlignment='Center' VerticalAlignment='Center' Visibility='Collapsed'/></Border><ContentPresenter Margin='9,0,0,0' VerticalAlignment='Center'/></StackPanel><ControlTemplate.Triggers><Trigger Property='IsChecked' Value='True'><Setter TargetName='check' Property='Background' Value='#EEEEEE'/><Setter TargetName='tick' Property='Visibility' Value='Visible'/></Trigger><Trigger Property='IsMouseOver' Value='True'><Setter TargetName='check' Property='BorderBrush' Value='White'/></Trigger><Trigger Property='IsKeyboardFocused' Value='True'><Setter TargetName='check' Property='BorderBrush' Value='White'/></Trigger></ControlTemplate.Triggers></ControlTemplate></Setter.Value></Setter></Style>
<Style TargetType='ScrollBar'>
 <Setter Property='Width' Value='10'/><Setter Property='Background' Value='Transparent'/>
 <Setter Property='Template'><Setter.Value><ControlTemplate TargetType='ScrollBar'>
  <Grid Background='Transparent'><Track x:Name='PART_Track' Orientation='{TemplateBinding Orientation}' Minimum='{TemplateBinding Minimum}' Maximum='{TemplateBinding Maximum}' Value='{Binding Value, RelativeSource={RelativeSource TemplatedParent}, Mode=TwoWay}' ViewportSize='{TemplateBinding ViewportSize}' IsDirectionReversed='True'>
   <Track.DecreaseRepeatButton><RepeatButton x:Name='less' Command='{x:Static ScrollBar.PageUpCommand}' Opacity='0' Focusable='False'/></Track.DecreaseRepeatButton>
   <Track.Thumb><Thumb><Thumb.Template><ControlTemplate TargetType='Thumb'><Border Background='#606060' CornerRadius='4' Margin='2'/></ControlTemplate></Thumb.Template></Thumb></Track.Thumb>
   <Track.IncreaseRepeatButton><RepeatButton x:Name='more' Command='{x:Static ScrollBar.PageDownCommand}' Opacity='0' Focusable='False'/></Track.IncreaseRepeatButton>
  </Track></Grid>
  <ControlTemplate.Triggers><Trigger Property='Orientation' Value='Horizontal'><Setter TargetName='PART_Track' Property='IsDirectionReversed' Value='False'/><Setter TargetName='less' Property='Command' Value='{x:Static ScrollBar.PageLeftCommand}'/><Setter TargetName='more' Property='Command' Value='{x:Static ScrollBar.PageRightCommand}'/></Trigger></ControlTemplate.Triggers>
 </ControlTemplate></Setter.Value></Setter>
 <Style.Triggers><Trigger Property='Orientation' Value='Horizontal'><Setter Property='Width' Value='Auto'/><Setter Property='Height' Value='10'/></Trigger></Style.Triggers>
</Style>
</ResourceDictionary>");
        }
        public static TextBlock Text(string text, double size, Brush color, bool bold) { return new TextBlock { Text = text, FontFamily=Typeface, FontSize = size, Foreground = color, FontWeight = bold ? FontWeights.SemiBold : FontWeights.Normal, TextWrapping = TextWrapping.Wrap }; }
        public static TextBlock Text(string text, double size) { return Text(text, size, Ink, false); }
        public static Button Button(string label, Action click, bool primary) {
            var b = new Button { Content = label, Background = primary ? Ink : B("#222222"), Foreground = primary ? B("#17191D") : Ink, BorderBrush = primary ? Ink : Line };
            if (click != null) b.Click += delegate { click(); }; MotionFx.Press(b);return b;
        }
        public static Button Button(string label, Action click) { return Button(label, click, false); }
        public static StackPanel Stack(bool horizontal) { return new StackPanel { Orientation = horizontal ? Orientation.Horizontal : Orientation.Vertical }; }
        public static Border Rule() { return new Border { Height = 1, Background = Line }; }
        public static Brush Theme(string theme) {
            switch (theme) { case "purple": return B("#663B85"); case "blue": return B("#B65127"); case "peach": return B("#8C711C"); case "pink": return B("#526E96"); case "mint": return B("#174587"); default: return B("#285E32"); }
        }
    }
}
