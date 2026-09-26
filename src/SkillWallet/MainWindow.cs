using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Effects;
using System.Windows.Threading;
using Microsoft.Win32;

namespace SkillWallet {
    public class CardView {
        public int Index; public Skill Skill; public Border Root, Shade; public SolidColorBrush Rim; public ScaleTransform Scale; public RotateTransform Rotate; public TranslateTransform Move; public Button More, Select; public TextBlock Action; public double VisualSlot, Hover, HoverVelocity, Press; public bool Hovered; public Grid Face; public Border Info,Picture,Stroke; public bool? SelectionState;
    }
    public partial class MainWindow : Window {
        readonly string dataPath; readonly bool testMode;
        List<Skill> skills; List<Skill> filtered = new List<Skill>(); List<CardView> cards = new List<CardView>();
        int selected = 2; string category = "全部"; bool favorites; string query = "";
        Grid root, main, overlay; Canvas stage; StackPanel filters, dots;
        TextBox search; TextBlock counter, collectionCount, status, toastText, emptyText;
        Border toast; Button prev, next, favoriteFilter; DispatcherTimer toastTimer;
        Skill downSkill; Point downPoint; bool dragging, caughtMotion;
        readonly CarouselMotion motion=new CarouselMotion();
        readonly System.Diagnostics.Stopwatch motionClock=System.Diagnostics.Stopwatch.StartNew();
        bool rendering; double frameTime, wheelDeadline; TimeSpan lastRenderingTime;
        public int CopyCount { get; private set; } public string LastCopied { get; private set; }
        public string ScreenshotPath { get; set; }
        public MainWindow(string file, bool testing) {
            dataPath=file; testMode=testing;preferences=Preferences.Read(Path.Combine(Path.GetDirectoryName(dataPath),"preferences.json"));MotionFx.Enabled=!testing;
            Title="Skill Wallet · AI 技能钱包"; Width=1280;Height=860;MinWidth=640;MinHeight=520;
            WindowStartupLocation=WindowStartupLocation.CenterScreen; Background=UI.Paper;FontFamily=UI.Typeface;
            UseLayoutRounding=true; SnapsToDevicePixels=true;Icon=Branding.Load("app-icon.png");TextOptions.SetTextFormattingMode(this,TextFormattingMode.Ideal);TextOptions.SetTextRenderingMode(this,TextRenderingMode.Grayscale);
            skills=Load();
            Build(); RebuildFilters(); Refresh(false);StartLed();InitializeInteraction();InitializeResponsive();
            PreviewKeyDown+=Keys;
            Closing+=delegate(object sender, System.ComponentModel.CancelEventArgs e) { if (!CanCloseOverlay()) e.Cancel=true; };
            Closed+=delegate {StopRendering();toastTimer.Stop();if(ledTimer!=null)ledTimer.Stop();DisposeInteraction();};
            SourceInitialized+=delegate {DarkWindowFrame.Apply(this);};
            Deactivated+=delegate {CancelHold();if(manageReorderCandidate)CancelReorder();if(downSkill!=null){downSkill=null;dragging=false;motion.Release(motionClock.Elapsed.TotalSeconds,false);stage.ReleaseMouseCapture();StartRendering();}};
        }
        List<Skill> Load() {
            if (!File.Exists(dataPath)) {
                if (File.Exists(dataPath+".bak")) { var recovered=Packs.Read(dataPath+".bak");Packs.Write(dataPath,recovered);return recovered; }
                var sample=Samples.Create(); Packs.Write(dataPath,sample);return sample;
            }
            try { return Packs.Read(dataPath); }
            catch (Exception original) {
                if (File.Exists(dataPath+".bak")) {
                    try {
                        var backup=Packs.Read(dataPath+".bak");
                        if (MessageBox.Show("本地技能库无法读取。找到上一份备份，是否恢复？原文件会保留。", "恢复技能库", MessageBoxButton.YesNo, MessageBoxImage.Warning)==MessageBoxResult.Yes) {
                            File.Copy(dataPath,dataPath+".damaged-"+DateTime.Now.ToString("yyyyMMddHHmmss"));Packs.Write(dataPath,backup);return backup;
                        }
                    } catch { }
                }
                throw new InvalidDataException("无法读取本地技能库，原文件未被覆盖。\n"+dataPath+"\n"+original.Message);
            }
        }
        void Build() {
            root=new Grid();Content=root;AddBackdrop();
            main=new Grid {Margin=new Thickness(38,24,38,18)};root.Children.Add(main);
            for(int i=0;i<6;i++)main.RowDefinitions.Add(new RowDefinition {Height=i==3?new GridLength(1,GridUnitType.Star):GridLength.Auto});
            header=new DockPanel {LastChildFill=false,Margin=new Thickness(0,0,0,26)};main.Children.Add(header);
            var logo=UI.Stack(true);logo.VerticalAlignment=VerticalAlignment.Center;
            var icon=new Image {Source=Branding.Load("logo-mark.png"),Width=40,Height=40,VerticalAlignment=VerticalAlignment.Center,Stretch=Stretch.Uniform};RenderOptions.SetBitmapScalingMode(icon,BitmapScalingMode.HighQuality);logo.Children.Add(icon);
            brand=UI.Text("Skill Wallet",21,UI.Ink,false);brand.Margin=new Thickness(11,0,0,0);brand.VerticalAlignment=VerticalAlignment.Center;brand.TextWrapping=TextWrapping.NoWrap;logo.Children.Add(brand);DockPanel.SetDock(logo,Dock.Left);header.Children.Add(logo);
            var actions=UI.Stack(true);DockPanel.SetDock(actions,Dock.Right);header.Children.Add(actions);
            searchWrap=new Grid {Width=210,Margin=new Thickness(0,0,12,0)};
            search=new TextBox {Height=36,Padding=new Thickness(14,7,12,7),ToolTip="搜索名称、描述、标签或正文"};
            var placeholder=UI.Text("⌕  搜索 Skill",12,UI.Muted,false);placeholder.IsHitTestVisible=false;placeholder.Margin=new Thickness(13,0,0,0);placeholder.VerticalAlignment=VerticalAlignment.Center;
            searchWrap.Children.Add(search);searchWrap.Children.Add(placeholder);actions.Children.Add(searchWrap);
            search.TextChanged+=delegate {placeholder.Visibility=String.IsNullOrEmpty(search.Text)?Visibility.Visible:Visibility.Collapsed;query=search.Text.Trim();selected=0;Refresh(true);};
            importButton=UI.Button("导入",Import);importButton.Height=36;importButton.Margin=new Thickness(0,0,8,0);actions.Children.Add(importButton);
            exportButton=UI.Button("导出",Export);exportButton.Height=36;exportButton.Margin=new Thickness(0,0,12,0);actions.Children.Add(exportButton);
            addButton=UI.Button("＋ 新建",delegate {Edit(null);},true);addButton.Height=34;addButton.Padding=new Thickness(17,6,17,6);actions.Children.Add(addButton);BuildManagementActions(actions);BuildCompactActions(actions);
            intro=UI.Stack(false);intro.Margin=new Thickness(0,0,0,18);Grid.SetRow(intro,1);main.Children.Add(intro);
            intro.Children.Add(UI.Text("我的技能",18,UI.Ink,false));
            collectionCount=UI.Text("",11,UI.Muted,false);collectionCount.Margin=new Thickness(0,7,0,0);intro.Children.Add(collectionCount);
            filterScroll=new ScrollViewer {HorizontalScrollBarVisibility=ScrollBarVisibility.Auto,VerticalScrollBarVisibility=ScrollBarVisibility.Disabled,Margin=new Thickness(0,0,0,2)};
            filters=UI.Stack(true);filterScroll.Content=filters;var filterArea=UI.Stack(false);filterArea.Children.Add(filterScroll);categoryButton=UI.Button("全部  ⌄",ShowCategoryMenu);categoryButton.HorizontalAlignment=HorizontalAlignment.Left;categoryButton.Visibility=Visibility.Collapsed;filterArea.Children.Add(categoryButton);BuildManagementBar(filterArea);Grid.SetRow(filterArea,2);main.Children.Add(filterArea);
            stage=new Canvas {Background=Brushes.Transparent,ClipToBounds=true,MinHeight=160};Grid.SetRow(stage,3);main.Children.Add(stage);
            stage.SizeChanged+=delegate {ResizeStage();};
            stage.PreviewMouseLeftButtonDown+=PointerDown;stage.PreviewMouseMove+=PointerMove;stage.PreviewMouseLeftButtonUp+=PointerUp;
            stage.LostMouseCapture+=delegate {CancelHold();if(manageReorderCandidate&&!stage.IsMouseCaptured)CancelReorder();if(downSkill!=null){downSkill=null;dragging=false;motion.Release(motionClock.Elapsed.TotalSeconds,false);StartRendering();}};
            stage.PreviewMouseWheel+=delegate(object sender,MouseWheelEventArgs e){if(overlay!=null||downSkill!=null||manageReorderCandidate||filtered.Count==0)return;e.Handled=true;ScrollCards(e.Delta);};
            nav=UI.Stack(false);nav.HorizontalAlignment=HorizontalAlignment.Center;nav.Margin=new Thickness(0,8,0,22);Grid.SetRow(nav,4);main.Children.Add(nav);
            controls=UI.Stack(true);nav.Children.Add(controls);
            prev=UI.Button("‹",delegate {Navigate(-1);});prev.Width=37;prev.Height=35;prev.FontSize=23;prev.Padding=new Thickness(0,-2,0,0);controls.Children.Add(prev);
            counter=UI.Text("",12,UI.Muted,false);counter.Width=94;counter.TextAlignment=TextAlignment.Center;counter.VerticalAlignment=VerticalAlignment.Center;controls.Children.Add(counter);
            next=UI.Button("›",delegate {Navigate(1);});next.Width=37;next.Height=35;next.FontSize=23;next.Padding=new Thickness(0,-2,0,0);controls.Children.Add(next);
            dots=UI.Stack(true);dots.HorizontalAlignment=HorizontalAlignment.Center;dots.Margin=new Thickness(0,12,0,0);nav.Children.Add(dots);
            footer=new DockPanel {LastChildFill=false};Grid.SetRow(footer,5);main.Children.Add(footer);
            status=UI.Text("▱  仅保存在本机",10,UI.Muted,false);status.ToolTip=dataPath;DockPanel.SetDock(status,Dock.Left);footer.Children.Add(status);
            footerControls=UI.Stack(true);DockPanel.SetDock(footerControls,Dock.Right);footer.Children.Add(footerControls);BuildFeedbackControls(footerControls);
            var hint=UI.Text("拖动切换 · 点击复制 · ⋯ 详情",10,UI.Muted,false);browseHint=hint;hint.Margin=new Thickness(18,0,0,0);hint.VerticalAlignment=VerticalAlignment.Center;footerControls.Children.Insert(0,hint);
            toastText=UI.Text("",13,UI.Ink,false);toast=new Border {Background=UI.B("#F0242424"),BorderBrush=UI.Line,BorderThickness=new Thickness(1),CornerRadius=new CornerRadius(10),Padding=new Thickness(23,14,23,14),Child=toastText,HorizontalAlignment=HorizontalAlignment.Center,VerticalAlignment=VerticalAlignment.Bottom,Margin=new Thickness(0,0,0,110),Visibility=Visibility.Collapsed,IsHitTestVisible=false};
            Panel.SetZIndex(toast,500);root.Children.Add(toast);
            toastTimer=new DispatcherTimer {Interval=TimeSpan.FromSeconds(2.2)};toastTimer.Tick+=delegate {toastTimer.Stop();MotionFx.Fade(toast,0,.18,()=>{if(!toastTimer.IsEnabled)toast.Visibility=Visibility.Collapsed;});};
        }

        void SelectCategory(string value) {
            if(String.Equals(category,value,StringComparison.Ordinal))return;
            category=value;selected=0;RebuildFilters();Refresh(true);
        }
        void RebuildFilters() {
            double oldFilterX=filterX;filters.Children.Clear();
            var categories=new[]{"全部"}.Concat(skills.Select(s=>s.category).Distinct()).ToList();
            if(!categories.Contains(category))category="全部";
            foreach(var name in categories) { var capture=name;var b=UI.Button(name,delegate {SelectCategory(capture);});b.Padding=new Thickness(13,7,13,7);b.Margin=new Thickness(0,0,7,0);b.BorderThickness=new Thickness(0);b.Background=category==name?UI.B("#333333"):Brushes.Transparent;b.Foreground=category==name?Brushes.White:UI.Muted;filters.Children.Add(b); }
            var sep=new Border {Width=1,Height=20,Background=UI.Line,Margin=new Thickness(8,0,15,0)};filters.Children.Add(sep);
            favoriteFilter=UI.Button(favorites?"★ 已收藏":"☆ 收藏",delegate {favorites=!favorites;selected=0;RebuildFilters();Refresh(true);});favoriteFilter.BorderThickness=new Thickness(0);favoriteFilter.Foreground=favorites?UI.Ink:UI.Muted;favoriteFilter.Background=favorites?UI.B("#333333"):Brushes.Transparent;favoriteFilter.Padding=new Thickness(12,8,12,8);filters.Children.Add(favoriteFilter);AnimateFilterSelection(oldFilterX);if(categoryButton!=null)categoryButton.Content=category+(favorites?" · 收藏":"")+"  ⌄";
        }
        void Refresh(bool animate) {
            CancelHold();var oldView=CaptureStage(animate);var oldCards=cards.ToDictionary(c=>c.Skill.id);
            filtered=skills.Where(s=>(category=="全部"||s.category==category)&&(!favorites||s.favorite)&&(query.Length==0||(s.name+" "+s.description+" "+s.content+" "+String.Join(" ",s.tags)).IndexOf(query,StringComparison.OrdinalIgnoreCase)>=0)).ToList();
            selectedIds.IntersectWith(filtered.Select(s=>s.id));UpdateManagementBar();
            selected=Math.Max(0,Math.Min(selected,filtered.Count-1));
            StopRendering();wheelDeadline=0;motion.Reset(filtered.Count,selected);
            collectionCount.Text="把好用的能力，收进钱包。  ·  "+skills.Count+" 张卡牌";
            stage.Children.Clear();cards.Clear();
            // Materialize only nearby cards so large imported libraries stay responsive.
            for(int i=Math.Max(0,selected-4);i<Math.Min(filtered.Count,selected+5);i++) {var card=CreateCard(filtered[i],i);cards.Add(card);stage.Children.Add(card.Root);}
            if(filtered.Count==0) {
                var empty=UI.Stack(false);empty.Width=400;
                empty.Children.Add(UI.Text("✧",45,UI.Muted,false));
                emptyText=UI.Text(skills.Count==0?"第一张卡牌，从你的灵感开始":"暂时没有匹配的卡牌",21,UI.Ink,true);emptyText.Margin=new Thickness(0,10,0,10);empty.Children.Add(emptyText);
                empty.Children.Add(UI.Text("新建一个 Skill，或调整搜索与分类。",13,UI.Muted,false));
                foreach(var tb in empty.Children.OfType<TextBlock>())tb.TextAlignment=TextAlignment.Center;
                stage.Children.Add(empty);Canvas.SetLeft(empty,Math.Max(0,(stage.ActualWidth-400)/2));Canvas.SetTop(empty,100);
            }
            Position(animate);UpdateNav();ApplyRefreshTransition(oldView,oldCards,animate);if(ManageChanging)StartRendering();
        }
        CardView CreateCard(Skill skill,int index) {
            var v=new CardView {Index=index,VisualSlot=index,Skill=skill,Scale=new ScaleTransform(1,1),Rotate=new RotateTransform(0),Move=new TranslateTransform(0,0)};
            var transforms=new TransformGroup();transforms.Children.Add(v.Scale);transforms.Children.Add(v.Rotate);transforms.Children.Add(v.Move);
            var card=new Border {Width=324,Height=470,CornerRadius=new CornerRadius(22),Background=Brushes.Transparent,BorderBrush=Brushes.Transparent,BorderThickness=new Thickness(0),Cursor=Cursors.Hand,RenderTransformOrigin=new Point(.5,.5),RenderTransform=transforms,Focusable=true,Tag=skill};v.Root=card;
            v.Rim=new SolidColorBrush(Color.FromArgb(110,176,189,204));card.UseLayoutRounding=false;card.SnapsToDevicePixels=false;card.Clip=new RectangleGeometry(new Rect(0,0,324,470),22,22);
            card.MouseEnter+=delegate{v.Hovered=true;StartRendering();};card.MouseLeave+=delegate{v.Hovered=false;StartRendering();};
            System.Windows.Automation.AutomationProperties.SetName(card,skill.name+"，点击复制正文");
            card.KeyDown+=delegate(object sender,KeyEventArgs e){if(e.Key==Key.Enter||e.Key==Key.Space){ActivateCard(skill);e.Handled=true;}};
            var face=new Grid {Background=Pictures.CoverBackdrop(skill),UseLayoutRounding=false,SnapsToDevicePixels=false};card.Child=face;v.Face=face;
            v.Picture=new Border {Height=Pictures.HasCover(skill)?270:288,VerticalAlignment=VerticalAlignment.Top,Background=Pictures.Cover(skill)};face.Children.Add(v.Picture);
            var info=new Border {VerticalAlignment=VerticalAlignment.Bottom,Height=200,CornerRadius=new CornerRadius(18,18,0,0),Background=Palette.Panel(skill),BorderThickness=new Thickness(0),Padding=new Thickness(23,16,23,31)};face.Children.Add(info);v.Info=info;
            info.Clip=new RectangleGeometry(new Rect(0,0,324,200),18,18);var panelLayers=new Grid();info.Child=panelLayers;var accent=Palette.ColorsFor(skill);var glint=new RadialGradientBrush {Center=new Point(0,0),GradientOrigin=new Point(0,0),RadiusX=.5,RadiusY=.42};glint.GradientStops.Add(new GradientStop(Color.FromArgb(130,accent[0].R,accent[0].G,accent[0].B),0));glint.GradientStops.Add(new GradientStop(Color.FromArgb(0,accent[0].R,accent[0].G,accent[0].B),1));panelLayers.Children.Add(new Border {Background=glint,Margin=new Thickness(-23,-16,-23,-31),IsHitTestVisible=false});var content=new Grid();panelLayers.Children.Add(content);
            content.RowDefinitions.Add(new RowDefinition {Height=GridLength.Auto});content.RowDefinitions.Add(new RowDefinition {Height=new GridLength(42)});content.RowDefinitions.Add(new RowDefinition {Height=new GridLength(30)});content.RowDefinitions.Add(new RowDefinition());
            var title=UI.Text(skill.name,23,Brushes.White,true);title.LineHeight=28;title.MaxHeight=56;title.Margin=new Thickness(0,0,0,6);title.TextWrapping=TextWrapping.Wrap;title.TextTrimming=TextTrimming.CharacterEllipsis;title.ToolTip=skill.name;content.Children.Add(title);
            var desc=UI.Text(skill.description.Replace("\n",""),13,UI.B("#BDBDBD"),false);desc.LineHeight=21;desc.MaxHeight=42;desc.TextTrimming=TextTrimming.CharacterEllipsis;Grid.SetRow(desc,1);content.Children.Add(desc);
            var tags=UI.Stack(true);Grid.SetRow(tags,2);content.Children.Add(tags);
            foreach(var tag in skill.tags.Take(3)){
                var label=UI.Text("#"+tag,10,UI.B("#E5FFFFFF"),false);label.MaxWidth=69;label.TextWrapping=TextWrapping.NoWrap;label.TextTrimming=TextTrimming.CharacterEllipsis;
                tags.Children.Add(new Border {Child=label,Background=UI.B("#0BFFFFFF"),BorderBrush=UI.B("#13FFFFFF"),BorderThickness=new Thickness(.6),CornerRadius=new CornerRadius(7),Padding=new Thickness(7,3,7,3),Margin=new Thickness(0,2,6,8)});
            }
            var bottom=new DockPanel {LastChildFill=false,VerticalAlignment=VerticalAlignment.Bottom};Grid.SetRow(bottom,3);content.Children.Add(bottom);
            var version=UI.Text("v"+skill.version,11,UI.B("#BFFFFFFF"),false);version.MaxWidth=100;version.TextTrimming=TextTrimming.CharacterEllipsis;version.TextWrapping=TextWrapping.NoWrap;DockPanel.SetDock(version,Dock.Left);bottom.Children.Add(version);
            var copy=UI.Text("点击复制 ↗",12,Brushes.White,true);DockPanel.SetDock(copy,Dock.Right);bottom.Children.Add(copy);v.Action=copy;
            var top=new DockPanel {LastChildFill=false,VerticalAlignment=VerticalAlignment.Top,Margin=new Thickness(20,17,17,0)};face.Children.Add(top);
            var categoryText=UI.Text(skill.category,11,Brushes.White,true);categoryText.MaxWidth=150;categoryText.TextWrapping=TextWrapping.NoWrap;categoryText.TextTrimming=TextTrimming.CharacterEllipsis;
            var pill=new Border {Background=UI.B("#80111111"),CornerRadius=new CornerRadius(8),Padding=new Thickness(10,5,10,5),Child=categoryText};DockPanel.SetDock(pill,Dock.Left);top.Children.Add(pill);
            var more=UI.Button("⋯",delegate {OpenDetail(skill);});more.Width=32;more.Height=29;more.FontSize=23;more.Padding=new Thickness(0,-4,0,0);more.BorderThickness=new Thickness(0);more.Background=UI.B("#70111111");more.Foreground=Brushes.White;more.ToolTip="了解详情";System.Windows.Automation.AutomationProperties.SetName(more,"了解详情："+skill.name);DockPanel.SetDock(more,Dock.Right);top.Children.Add(more);v.More=more;
            var select=CreateSelectButton(delegate {ToggleSelection(skill);});select.ToolTip="选择卡牌";DockPanel.SetDock(select,Dock.Right);top.Children.Add(select);v.Select=select;
            if(skill.favorite){var star=UI.Text("★",14,Brushes.White,false);star.Margin=new Thickness(0,4,8,0);DockPanel.SetDock(star,Dock.Right);top.Children.Add(star);}
            v.Stroke=new Border {BorderBrush=v.Rim,BorderThickness=new Thickness(1),CornerRadius=new CornerRadius(21.5),Margin=new Thickness(.5),IsHitTestVisible=false};face.Children.Add(v.Stroke);
            var shade=new Border {Background=Brushes.Black,Opacity=0,IsHitTestVisible=false};face.Children.Add(shade);v.Shade=shade;
            return v;
        }

        double BaseScale(){return Math.Min(1,Math.Max(.38,((layoutHeight>0?layoutHeight:stage.ActualHeight)-38)/470));}
        double Spacing(){return Blend(Math.Max(80,Math.Min(300,190+Math.Max(0,(layoutWidth>0?layoutWidth:stage.ActualWidth)-1100)*.09)*BaseScale()),348*BaseScale(),managePhase); }
        static double Blend(double a,double b,double fraction){double t=Math.Max(0,Math.Min(1,fraction));return a+(b-a)*t*t*(3-2*t);}
        void EnsureCards() {
            int center=(int)Math.Round(motion.Position);
            int radius=Math.Max(5,(int)Math.Ceiling(stage.ActualWidth/Math.Max(1,Spacing())/2)+2);
            for(int slot=Math.Max(0,center-radius);slot<Math.Min(filtered.Count,center+radius+1);slot++) {
                int i=reorderPreview==null?slot:filtered.FindIndex(s=>s.id==reorderPreview[slot]);
                if(!cards.Any(c=>c.Index==i)){var c=CreateCard(filtered[i],i);c.VisualSlot=slot;cards.Add(c);stage.Children.Add(c.Root);}
            }
            foreach(var c in cards.Where(c=>Math.Abs((reorderPreview==null?c.Index:reorderPreview.IndexOf(c.Skill.id))-center)>radius+1&&Math.Abs(c.VisualSlot-center)>radius+1).ToArray()){stage.Children.Remove(c.Root);cards.Remove(c);}
        }
        void Position(bool animate) {
            if(stage==null)return;
            double baseScale=BaseScale(),spacing=Spacing(),viewWidth=layoutWidth>0?layoutWidth:stage.ActualWidth,viewHeight=layoutHeight>0?layoutHeight:stage.ActualHeight;
            EnsureCards();
            foreach(var c in cards) {
                double distance=c.VisualSlot-motion.Position,abs=Math.Abs(distance),t=ManageT;
                var scale=baseScale*(abs<1?Blend(1,.88,abs):abs<2?Blend(.88,.75,abs-1):Blend(.75,.70,(abs-2)/3));
                Canvas.SetLeft(c.Root,(viewWidth-324)/2);Canvas.SetTop(c.Root,(viewHeight-470)/2-1);
                Panel.SetZIndex(c.Root,(manageReordering&&selectedIds.Contains(c.Skill.id)?15000:10000)-(int)(abs*100));c.Root.Visibility=Visibility.Visible;
                double edge=viewWidth/2+324*scale/2; c.Root.Opacity=Blend(1,0,(Math.Abs(distance*spacing)-edge+55)/70);double narrow=1-Blend(0,1,(viewWidth-670)/220);c.Root.Opacity*=1-narrow+narrow*Blend(1,0,(abs-1.02)/.75);c.Root.IsHitTestVisible=c.Root.Opacity>.05;
                c.Shade.Opacity=abs<1?Blend(0,.48,abs):abs<2?Blend(.48,.64,abs-1):.67;
                c.Shade.Opacity*=1-t;
                c.Rim.Color=managing&&selectedIds.Contains(c.Skill.id)?Color.FromRgb(220,228,235):Color.FromArgb(110,176,189,204);
                c.Select.Visibility=t>.5?Visibility.Visible:Visibility.Collapsed;c.Select.Opacity=t;UpdateSelectionVisual(c);
                c.More.Visibility=t>.5?Visibility.Collapsed:Visibility.Visible;c.Action.Text=t>.5?"长按排序":(c.Skill.id==copiedId&&motionClock.Elapsed.TotalSeconds<copiedUntil?"✓ 已复制":"点击复制 ↗");
                scale=scale+(baseScale-scale)*t;
                scale*=1+c.Hover*.018-c.Press*.015;c.Scale.ScaleX=c.Scale.ScaleY=scale;
                c.Rotate.Angle=Math.Sign(distance)*Math.Min(10,abs*7)*(1-Blend(0,1,(abs-1.5)/2.7))*(1-t);
                c.Move.X=distance*spacing;c.Move.Y=(abs<1?Blend(0,14,abs):Math.Min(30,abs*12))*(1-t)-c.Hover*(manageReordering?13:7);
            }
            UpdateGlow();
            int current=(int)motion.Clamp(Math.Round(motion.Position));if(current!=selected){selected=current;UpdateNav();if(slideSoundArmed&&!manageReorderCandidate&&!manageReordering&&!ManageChanging&&overlay==null)PlaySlideSound();}
        }
        void StopRendering(){slideSoundArmed=false;if(rendering){CompositionTarget.Rendering-=RenderFrame;rendering=false;}}
        void StartRendering() {
            if(!SystemParameters.ClientAreaAnimation&&!manageReordering){motion.Reset(filtered.Count,Math.Round(motion.Target));wheelDeadline=0;managePhase=managing?1:0;foreach(var c in cards){c.VisualSlot=c.Index;c.Hover=0;c.Press=0;}layoutWidth=stage.ActualWidth;layoutHeight=stage.ActualHeight;ApplyManagementPhase();Position(false);return;}
            if(rendering)return;rendering=true;frameTime=motionClock.Elapsed.TotalSeconds;lastRenderingTime=TimeSpan.MinValue;CompositionTarget.Rendering+=RenderFrame;
        }
        void RenderFrame(object sender,EventArgs e) {
            var args=e as RenderingEventArgs;if(args!=null){if(args.RenderingTime==lastRenderingTime)return;lastRenderingTime=args.RenderingTime;}
            double now=motionClock.Elapsed.TotalSeconds;
            if(wheelDeadline>0&&now>=wheelDeadline){wheelDeadline=0;motion.MoveTo(Math.Round(motion.Target,MidpointRounding.AwayFromZero));}
            double dt=Math.Min(.05,now-frameTime);motion.Step(dt);AdvanceManagement(dt);AdvanceInteraction(dt);frameTime=now;Position(false);
            if(!motion.Moving&&!motion.IsDragging&&wheelDeadline==0&&!ManageChanging&&!manageReordering&&!manageLayoutMoving&&!interactionMoving)StopRendering();
        }
        void ScrollCards(int delta) {
            slideSoundArmed=true;double change=-delta/120.0;
            if(Math.Sign(change)!=Math.Sign(motion.Target-motion.Position)&&motion.Moving)motion.MoveTo(motion.Position);
            motion.MoveTo(motion.Target+change);wheelDeadline=motionClock.Elapsed.TotalSeconds+.14;StartRendering();
        }
        void FreezeMotion(){StopRendering();wheelDeadline=0;motion.Reset(filtered.Count,Math.Round(motion.Position));Position(false);if(ManageChanging)StartRendering();}
        void UpdateNav() {
            counter.Text=filtered.Count==0?"00 / 00":(selected+1).ToString("00")+" / "+filtered.Count.ToString("00");
            prev.IsEnabled=selected>0;next.IsEnabled=selected<filtered.Count-1;
            dots.Children.Clear();
            int start=Math.Max(0,Math.Min(selected-4,filtered.Count-9));
            for(int i=start;i<Math.Min(filtered.Count,start+9);i++) {int target=i;bool active=i==selected;var dot=UI.Button("",delegate {slideSoundArmed=true;wheelDeadline=0;motion.MoveTo(target);StartRendering();});dot.Width=active?30:19;dot.Height=20;dot.Padding=new Thickness(0);dot.Margin=new Thickness(0);dot.Background=Brushes.Transparent;dot.BorderThickness=new Thickness(0);double size=!active&&(i==start&&start>0||i==start+8&&i<filtered.Count-1)?4:6;dot.Content=new Border {Width=active?22:size,Height=6,CornerRadius=new CornerRadius(3),Background=active?UI.Ink:UI.B("#505050")};dot.ToolTip="第 "+(i+1)+" 张，共 "+filtered.Count+" 张";System.Windows.Automation.AutomationProperties.SetName(dot,(string)dot.ToolTip);dots.Children.Add(dot);}
        }
        void Navigate(int delta) {
            if(overlay!=null||filtered.Count==0)return;
            slideSoundArmed=true;wheelDeadline=0;motion.MoveTo(Math.Round(motion.Target)+delta);StartRendering();
        }
        static T Ancestor<T>(DependencyObject item) where T:DependencyObject {
            while(item!=null) {if(item is T)return (T)item;item=VisualTreeHelper.GetParent(item);}return null;
        }
        void PointerDown(object sender,MouseButtonEventArgs e) {
            if(overlay!=null||ManageChanging||Ancestor<Button>(e.OriginalSource as DependencyObject)!=null)return;
            DependencyObject current=e.OriginalSource as DependencyObject;
            while(current!=null && current!=stage) {var b=current as Border;if(b!=null&&b.Tag is Skill){downSkill=(Skill)b.Tag;break;}current=VisualTreeHelper.GetParent(current);}
            if(downSkill==null)return;
            StartCardGesture(downSkill,e.GetPosition(stage));e.Handled=true;
        }
        void PointerMove(object sender,MouseEventArgs e) {
            if(manageReorderCandidate){MoveReorder(e.GetPosition(stage));e.Handled=true;return;}
            if(downSkill==null)return;
            MoveCardGesture(e.GetPosition(stage));if(dragging)e.Handled=true;
        }
        void MoveCardGesture(Point point) {
            if(downSkill==null)return;double dx=point.X-downPoint.X,dy=point.Y-downPoint.Y;
            if(Math.Abs(dx)>8||Math.Abs(dy)>8){dragging=true;CancelHold();}
            if(dragging){slideSoundArmed=true;motion.Drag(-dx/Spacing(),motionClock.Elapsed.TotalSeconds);Position(false);}
        }
        void FinishGesture(double dx,double dy) {
            CancelHold();
            var skill=downSkill;bool moved=dragging||Math.Abs(dx)>8||Math.Abs(dy)>8;downSkill=null;dragging=false;
            if(stage.IsMouseCaptured)stage.ReleaseMouseCapture();
            if(skill==null)return;
            motion.Release(motionClock.Elapsed.TotalSeconds,moved);
            if(!moved&&!caughtMotion)ActivateCard(skill);
            caughtMotion=false;StartRendering();
        }
        void PointerUp(object sender,MouseButtonEventArgs e) {if(manageReorderCandidate){EndReorder();e.Handled=true;return;}if(downSkill==null)return;Point p=e.GetPosition(stage);FinishGesture(p.X-downPoint.X,p.Y-downPoint.Y);e.Handled=true;}
        void Copy(Skill skill) {CopyWithRetry(skill);}
        void Notify(string text) {toastTimer.Interval=TimeSpan.FromSeconds(2.2);toastText.FontSize=13;toast.Padding=new Thickness(23,14,23,14);toast.CornerRadius=new CornerRadius(10);toast.Margin=new Thickness(0,0,0,110);toastText.Text=text;toast.Visibility=Visibility.Visible;MotionFx.Fade(toast,1,.18);var offset=toast.RenderTransform as TranslateTransform;if(offset==null){offset=new TranslateTransform(0,6);toast.RenderTransform=offset;}MotionFx.To(offset,TranslateTransform.YProperty,0,.22);toastTimer.Stop();toastTimer.Start();}
        void NotifyCopied(){
            Notify("✓  已复制");toastTimer.Interval=TimeSpan.FromSeconds(1.35);toastText.FontSize=12;toast.Padding=new Thickness(16,9,16,9);toast.CornerRadius=new CornerRadius(12);toast.Margin=new Thickness(0,0,0,shortLayout?48:18);
        }
        void Keys(object sender,KeyEventArgs e) {
            if(menuLayer!=null){if(e.Key==Key.Escape){CloseMenu();e.Handled=true;}return;}
            if(pictureLayer!=null){if(e.Key==Key.Escape&&closePicture!=null)closePicture();e.Handled=true;return;}
            if(e.Key==Key.Escape&&downSkill!=null){CancelHold();downSkill=null;dragging=false;motion.Release(motionClock.Elapsed.TotalSeconds,false);stage.ReleaseMouseCapture();e.Handled=true;return;}
            if(e.Key==Key.Escape&&manageReorderCandidate){CancelReorder();e.Handled=true;return;}
            if(e.Key==Key.Escape&&managing&&overlay==null){SetManagement(false);e.Handled=true;return;}
            if(e.Key==Key.Escape&&overlay!=null){CloseOverlay();e.Handled=true;return;}
            if(overlay!=null||Keyboard.FocusedElement is TextBox||Keyboard.FocusedElement is ComboBox)return;
            if(e.Key==Key.Z&&(Keyboard.Modifiers&ModifierKeys.Control)!=0){UndoLast();e.Handled=true;return;}
            if(e.Key==Key.Left){Navigate(-1);e.Handled=true;}else if(e.Key==Key.Right){Navigate(1);e.Handled=true;}
            else if((e.Key==Key.Enter||e.Key==Key.Space)&&!(Keyboard.FocusedElement is Button)&&filtered.Count>0){ActivateCard(filtered[selected]);e.Handled=true;}
        }
        bool Commit(List<Skill> updated,string message,string focusId) {
            if(preserveOrderVisuals)return CommitOrder(updated,message);
            try {
                if(focusId==null&&filtered.Count>0)focusId=filtered[selected].id;
                Packs.Write(dataPath,updated);ClearUndo();skills=updated;RebuildFilters();
                if(focusId!=null){var visible=skills.Where(s=>(category=="全部"||s.category==category)&&(!favorites||s.favorite)&&(query.Length==0||(s.name+" "+s.description+" "+s.content+" "+String.Join(" ",s.tags)).IndexOf(query,StringComparison.OrdinalIgnoreCase)>=0)).ToList();int index=visible.FindIndex(s=>s.id==focusId);if(index>=0)selected=index;}Refresh(true);
                status.Text="●  已保存 · "+DateTime.Now.ToString("HH:mm");Notify(message);return true;
            } catch(Exception ex) {Dialogs.Show(this,"未能保存，修改尚未写入磁盘。\n"+ex.Message,"保存失败",MessageBoxButton.OK,MessageBoxImage.Warning);return false;}
        }
        void Export() {
            var dialog=new SaveFileDialog {Filter="Skill Wallet 技能库|*.skillpack",FileName="我的AI技能库.skillpack",DefaultExt=".skillpack",AddExtension=true};
            if(dialog.ShowDialog(this)!=true)return;
            try {if(String.Equals(Path.GetFullPath(dialog.FileName),Path.GetFullPath(dataPath),StringComparison.OrdinalIgnoreCase)){Notify("这就是正在使用的技能库，无需再次导出。");return;}Packs.Write(dialog.FileName,skills);Notify("✓  已导出全部 "+skills.Count+" 张卡牌");}
            catch(Exception ex){Dialogs.Show(this,ex.Message,"导出失败",MessageBoxButton.OK,MessageBoxImage.Warning);}
        }
        void Import() {
            var dialog=new OpenFileDialog {Filter="Skill Wallet 技能库|*.skillpack",Multiselect=false};if(dialog.ShowDialog(this)!=true)return;
            try {
                ImportItems(Packs.Read(dialog.FileName));
            } catch(Exception ex){Dialogs.Show(this,"技能库未导入，本地数据未改变。\n"+ex.Message,"导入失败",MessageBoxButton.OK,MessageBoxImage.Warning);}
        }
        void ImportItems(List<Skill> incoming) {
                var plan=Packs.PlanImport(skills,incoming);
                if(plan.Added==0&&plan.Conflicts==0){Notify(plan.Same==0?"这个技能库没有卡牌。":"这些卡牌已经在库中，已跳过 "+plan.Same+" 张相同卡牌。");return;}
                var choice=new ImportDialog(plan.Added,plan.Same,plan.Conflicts){Owner=this};if(choice.ShowDialog()!=true)return;
                if(plan.Added==0&&choice.Mode==0){Notify("已保留本机卡牌，无需更新。");return;}
                var result=plan.Apply(skills,choice.Mode);Commit(result,"✓  导入完成"+(plan.Same>0?" · 已跳过 "+plan.Same+" 张相同卡牌":"")+" · 共 "+result.Count+" 张",null);
        }
        // End-to-end checks invoke the same gesture and detail handlers as normal input.
        public void RunUiChecks(string report) {
            var lines=new List<string>();Action<bool,string> check=(ok,name)=>{if(!ok)throw new Exception("UI check failed: "+name);lines.Add("PASS "+name);};
            check(filtered.Count==6,"initial sample cards");var target=filtered[selected];
            TestGesture(0,0);check(CopyCount==1&&LastCopied==target.content,"card click copies only body");
            TestGesture(-120,2);check(CopyCount==1&&selected==3,"drag changes card without copying");
            TestGesture(12,0);check(CopyCount==1&&selected==3,"small drag does not copy");
            ScrollCards(-120);double firstTarget=motion.Target;ScrollCards(-120);check(motion.Target>firstTarget,"consecutive wheel events are retained");ScrollCards(120);check(motion.Target<firstTarget,"wheel reverses destination immediately");FreezeMotion();
            var detailCard=cards.First(c=>c.Skill==filtered[selected]);detailCard.More.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));check(overlay!=null&&CopyCount==1,"ellipsis opens details without copying");CloseOverlay();
            search.Text="结构化结论";check(filtered.Count==1&&filtered[0].name=="深度研究专家","search matches body");search.Text="";
            category="开发";Refresh(false);check(filtered.Count==1&&filtered[0].category=="开发","category filter");category="全部";
            favorites=true;Refresh(false);check(filtered.Count==0,"empty favorites state");favorites=false;selected=2;RebuildFilters();Refresh(false);
            // Exercise the one-field creator, then the existing full editor and disk reload.
            Edit(null);UpdateLayout();
            var fields=FindChildren<TextBox>(panelBody).ToList();
            check(fields.Count==1,"new card requires only one input");
            check(!panelFooter.Children.OfType<Button>().Last().IsEnabled,"empty prompt cannot generate");
            fields[0].Text=FeatureTests.ProductPrompt;
            panelFooter.Children.OfType<Button>().Last().RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
            check(overlay==null&&skills.Count==7,"new card saved and editor closes");
            var created=skills.Last();check(created.name=="电商白底产品精修"&&created.category=="图像处理","local creator fills product metadata");
            check(Packs.Read(dataPath).Last().content==FeatureTests.ProductPrompt,"creator persists exact original body to disk");
            check(filtered[selected].id==created.id,"generated card becomes selected");
            Edit(created);UpdateLayout();fields=FindChildren<TextBox>(panelBody).ToList();fields[0].Text="已编辑卡牌";fields[5].Text+="\n更新";
            panelFooter.Children.OfType<Button>().Last().RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
            check(skills.Last().id==created.id&&skills.Last().name=="已编辑卡牌","edit keeps ID and updates fields");
            OpenDetail(skills.Last());panelFooter.Children.OfType<Button>().First().RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
            UpdateLayout();var detailScroll=Ancestor<ScrollViewer>(panelBody);detailScroll.ScrollToEnd();UpdateLayout();
            check(detailScroll.VerticalOffset>0,"long skill details remain scrollable with dark controls");
            check(skills.Last().favorite&&Packs.Read(dataPath).Last().favorite,"favorite persists from detail action");CloseOverlay();
            Edit(skills.Last());UpdateLayout();fields=FindChildren<TextBox>(panelBody).ToList();fields[0].Text="";
            panelFooter.Children.OfType<Button>().Last().RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
            check(overlay!=null&&skills.Last().name=="已编辑卡牌","empty name blocks save without data loss");
            isDirty=null;CloseOverlay();
            check(Commit(skills.Where(s=>s.id!=created.id).ToList(),"测试完成",null),"remove test card saves remaining collection");
            selected=2;category="全部";RebuildFilters();Refresh(false);
            File.WriteAllLines(report,lines);
        }
        void TestGesture(double dx,double dy) {
            downSkill=filtered[selected];caughtMotion=false;double now=motionClock.Elapsed.TotalSeconds;motion.BeginDrag(now-.12);
            if(Math.Abs(dx)>8||Math.Abs(dy)>8){dragging=true;motion.Drag(-dx/Spacing(),now);Position(false);}
            FinishGesture(dx,dy);StopRendering();for(int i=0;i<180;i++)motion.Step(1.0/60);Position(false);
        }
        public void RunMotionCheck(string report,Action<Exception> done) {
            var samples=new List<double>();var glowSamples=new List<Color>();var watch=System.Diagnostics.Stopwatch.StartNew();
            motion.Reset(filtered.Count,2);Position(false);Navigate(2);
            var timer=new DispatcherTimer {Interval=TimeSpan.FromMilliseconds(16)};
            timer.Tick+=delegate {
                samples.Add(motion.Position);
                glowSamples.Add(topGlow.Color);
                if(watch.Elapsed.TotalSeconds<1.6)return;
                timer.Stop();
                try {
                    if(SystemParameters.ClientAreaAnimation&&samples.Where(x=>x>2.01&&x<3.99).Distinct().Count()<4)throw new Exception("Actual render loop did not produce continuous frames.");
                    if(Math.Abs(motion.Position-4)>.005||rendering)throw new Exception("Actual render loop did not settle and unsubscribe.");
                    if(SystemParameters.ClientAreaAnimation&&glowSamples.Distinct().Count()<4)throw new Exception("Ambient color did not interpolate during motion.");
                    if(topGlow.Color.R!=bottomGlow.Color.R||topGlow.Color.G!=bottomGlow.Color.G||topGlow.Color.B!=bottomGlow.Color.B)throw new Exception("Ambient edges have different active colors.");
                    var active=cards.First(c=>c.Index==4);var neighbor=cards.First(c=>c.Index==3);if(active.Shade.Opacity>.01||neighbor.Shade.Opacity<.3)throw new Exception("Active card lighting hierarchy lost.");
                    File.WriteAllLines(report,new[]{"PASS real CompositionTarget.Rendering loop produces intermediate positions","PASS real rendering reaches target and stops when idle","PASS ambient hue changes continuously with carousel motion","PASS top and bottom ambience share active card color","PASS active card stays bright while side card is dimmed","Samples: "+samples.Count,"Intermediate samples: "+samples.Count(x=>x>2.01&&x<3.99)});done(null);
                }catch(Exception ex){done(ex);}
            };timer.Start();
        }
        static IEnumerable<T> FindChildren<T>(DependencyObject parent) where T:DependencyObject {
            for(int i=0;i<VisualTreeHelper.GetChildrenCount(parent);i++){var child=VisualTreeHelper.GetChild(parent,i);if(child is T)yield return (T)child;foreach(var nested in FindChildren<T>(child))yield return nested;}
        }
    }
}
