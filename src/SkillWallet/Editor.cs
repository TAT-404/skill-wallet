using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Effects;

namespace SkillWallet {
    public partial class MainWindow {
        Func<bool> isDirty;
        Grid panelBody;StackPanel panelFooter;
        bool CanCloseOverlay() {
            return isDirty==null||!isDirty()||Dialogs.Show(this,"当前修改尚未保存，确定放弃修改吗？","未保存的修改",MessageBoxButton.YesNo,MessageBoxImage.Question)==MessageBoxResult.Yes;
        }
        bool CloseOverlay() {
            if(!CanCloseOverlay())return false;
            if(overlay!=null){var old=overlay;overlay=null;old.IsHitTestVisible=false;var shell=old.Children.OfType<Border>().FirstOrDefault();if(shell!=null){var move=shell.RenderTransform as TranslateTransform;if(move!=null)MotionFx.To(move,TranslateTransform.XProperty,32,.18);}MotionFx.Fade(old,0,.18,()=>root.Children.Remove(old));}
            main.IsEnabled=true;isDirty=null;return true;
        }
        void ShowPanel(string label) {
            FreezeMotion();
            overlay=new Grid {Background=UI.B("#8807090D")};PanelSetZ(overlay,200);root.Children.Add(overlay);main.IsEnabled=false;
            overlay.MouseLeftButtonDown+=delegate(object sender,MouseButtonEventArgs e){if(e.OriginalSource==overlay)CloseOverlay();};
            var shell=new Border {Width=Math.Min(510,ActualWidth-28),HorizontalAlignment=HorizontalAlignment.Right,Margin=new Thickness(12),CornerRadius=new CornerRadius(19),Background=UI.B("#191919"),BorderBrush=UI.B("#383838"),BorderThickness=new Thickness(1),Padding=new Thickness(28,23,28,23),Effect=new DropShadowEffect {BlurRadius=35,Opacity=.2,ShadowDepth=0}};overlay.Children.Add(shell);shell.RenderTransform=new TranslateTransform(30,0);MotionFx.To((TranslateTransform)shell.RenderTransform,TranslateTransform.XProperty,0,.28);overlay.Opacity=0;MotionFx.Fade(overlay,1,.22);
            var layout=new Grid();shell.Child=layout;layout.RowDefinitions.Add(new RowDefinition {Height=GridLength.Auto});layout.RowDefinitions.Add(new RowDefinition {Height=new GridLength(1,GridUnitType.Star)});layout.RowDefinitions.Add(new RowDefinition {Height=GridLength.Auto});
            var top=new DockPanel {LastChildFill=false,Margin=new Thickness(0,0,0,20)};layout.Children.Add(top);
            var title=UI.Text(label,11,UI.Muted,true);title.VerticalAlignment=VerticalAlignment.Center;DockPanel.SetDock(title,Dock.Left);top.Children.Add(title);
            var close=UI.Button("✕",delegate {CloseOverlay();});close.Width=33;close.Height=32;close.Padding=new Thickness(0);close.BorderThickness=new Thickness(0);close.ToolTip="关闭（Esc）";DockPanel.SetDock(close,Dock.Right);top.Children.Add(close);
            var scroll=new ScrollViewer {VerticalScrollBarVisibility=ScrollBarVisibility.Auto,HorizontalScrollBarVisibility=ScrollBarVisibility.Disabled,Padding=new Thickness(0,0,9,0)};Grid.SetRow(scroll,1);layout.Children.Add(scroll);
            panelBody=new Grid();scroll.Content=panelBody;
            panelFooter=UI.Stack(true);panelFooter.Margin=new Thickness(0,20,0,0);panelFooter.HorizontalAlignment=HorizontalAlignment.Right;Grid.SetRow(panelFooter,2);layout.Children.Add(panelFooter);
        }
        static void PanelSetZ(UIElement element,int value){System.Windows.Controls.Panel.SetZIndex(element,value);}
        void OpenDetail(Skill skill) {
            if(!CloseOverlay())return;ShowPanel("S K I L L   /   了解详情");
            var stack=UI.Stack(false);panelBody.Children.Add(stack);
            var badge=new Border {Background=UI.B("#303030"),CornerRadius=new CornerRadius(8),Padding=new Thickness(10,5,10,5),HorizontalAlignment=HorizontalAlignment.Left,Child=UI.Text(skill.category+"  ·  v"+skill.version,11,UI.Ink,true)};stack.Children.Add(badge);
            var title=UI.Text(skill.name,29,UI.Ink,true);title.Margin=new Thickness(0,16,0,8);stack.Children.Add(title);
            var desc=UI.Text(skill.description,14,UI.Muted,false);desc.LineHeight=24;stack.Children.Add(desc);
            var tags=UI.Text(String.Join("   ",skill.tags.Select(t=>"#"+t)),12,UI.Muted,false);tags.Margin=new Thickness(0,15,0,23);stack.Children.Add(tags);AddPictureGallery(stack,skill);stack.Children.Add(UI.Rule());
            var bodyLabel=UI.Text("SKILL 正文",10,UI.Muted,true);bodyLabel.Margin=new Thickness(0,21,0,12);stack.Children.Add(bodyLabel);
            var body=new TextBox {Text=skill.content,IsReadOnly=true,AcceptsReturn=true,TextWrapping=TextWrapping.Wrap,Background=Brushes.Transparent,BorderThickness=new Thickness(0),Padding=new Thickness(0),FontSize=14,MinHeight=180};stack.Children.Add(body);
            var time=UI.Text("创建于 "+LocalDate(skill.createdAt)+"\n最后修改 "+LocalDate(skill.updatedAt),11,UI.Muted,false);time.LineHeight=21;time.Margin=new Thickness(0,23,0,12);stack.Children.Add(time);
            var delete=UI.Button("删除此 Skill",delegate {
                if(Dialogs.Show(this,"确定删除「"+skill.name+"」吗？","删除卡牌",MessageBoxButton.YesNo,MessageBoxImage.Warning)!=MessageBoxResult.Yes)return;
                var before=skills.Select(s=>s.Clone()).ToList();if(Commit(skills.Where(s=>s.id!=skill.id).ToList(),"已删除卡牌",null)){SetUndo(before,"删除");CloseOverlay();}
            });delete.Foreground=UI.Ink;delete.BorderThickness=new Thickness(0);delete.Padding=new Thickness(0,8,0,8);delete.HorizontalAlignment=HorizontalAlignment.Left;stack.Children.Add(delete);
            var favorite=UI.Button(skill.favorite?"★ 已收藏":"☆ 收藏",delegate {
                var changed=skill.Clone();changed.favorite=!changed.favorite;changed.updatedAt=Packs.Now();var list=skills.Select(s=>s.id==skill.id?changed:s).ToList();
                if(Commit(list,changed.favorite?"已加入收藏":"已取消收藏",skill.id))OpenDetail(changed);
            });favorite.Margin=new Thickness(0,0,8,0);panelFooter.Children.Add(favorite);
            var edit=UI.Button("编辑",delegate {Edit(skill);});edit.Margin=new Thickness(0,0,8,0);panelFooter.Children.Add(edit);
            panelFooter.Children.Add(UI.Button("复制正文  ↗",delegate {Copy(skill);},true));
        }
        static string LocalDate(string text){DateTime d;return DateTime.TryParse(text,out d)?d.ToLocalTime().ToString("yyyy-MM-dd HH:mm"):text;}
        static TextBox Field(StackPanel stack,string label,string value,int max,int height) {
            var caption=UI.Text(label,12,UI.Muted,true);caption.Margin=new Thickness(0,0,0,7);stack.Children.Add(caption);
            var field=new TextBox {Text=value??"",MaxLength=max,Margin=new Thickness(0,0,0,16),MinHeight=height};stack.Children.Add(field);return field;
        }
        void Edit(Skill original) {
            if(original==null){QuickCreate();return;}
            if(!CloseOverlay())return;ShowPanel(original==null?"N E W   /   新建技能卡牌":"E D I T   /   编辑技能卡牌");
            var stack=UI.Stack(false);panelBody.Children.Add(stack);
            var title=UI.Text(original==null?"装入一种新能力。":"让这张卡牌更好用。",26,UI.Ink,true);title.Margin=new Thickness(0,0,0,23);stack.Children.Add(title);
            var name=Field(stack,"名称 *",original==null?"":original.name,120,42);
            var desc=Field(stack,"一句话描述",original==null?"":original.description,500,65);desc.TextWrapping=TextWrapping.Wrap;desc.AcceptsReturn=true;
            var row=new Grid();row.ColumnDefinitions.Add(new ColumnDefinition());row.ColumnDefinitions.Add(new ColumnDefinition());stack.Children.Add(row);
            var left=UI.Stack(false);left.Margin=new Thickness(0,0,12,0);row.Children.Add(left);var right=UI.Stack(false);Grid.SetColumn(right,1);row.Children.Add(right);
            var cat=Field(left,"分类（可自由输入）",original==null?"未分类":original.category,40,42);
            var version=Field(right,"版本",original==null?"1.0":original.version,30,42);
            var tags=Field(stack,"标签 · 用逗号分隔",original==null?"":String.Join(", ",original.tags),650,42);
            var content=Field(stack,"Skill 正文 * · 点击卡牌时复制的内容",original==null?"":original.content,1000000,230);content.AcceptsReturn=true;content.TextWrapping=TextWrapping.Wrap;content.VerticalScrollBarVisibility=ScrollBarVisibility.Auto;content.Height=250;content.VerticalContentAlignment=VerticalAlignment.Top;
            var pictures=new PictureEditor(this,original);stack.Children.Add(pictures);
            string theme=original.theme;
            var colorNote=UI.Text("卡牌与氛围光随封面自动配色",11,UI.Muted,false);colorNote.Margin=new Thickness(0,0,0,17);stack.Children.Add(colorNote);
            var favorite=new CheckBox {Content="加入我的收藏",IsChecked=original!=null&&original.favorite,FontSize=13,Foreground=UI.Ink,Margin=new Thickness(0,0,0,16)};stack.Children.Add(favorite);
            var error=UI.Text("",12,UI.Ink,false);error.Margin=new Thickness(0,3,0,0);stack.Children.Add(error);
            Func<string> signature=()=>Packs.Json().Serialize(new[]{name.Text,desc.Text,cat.Text,version.Text,tags.Text,content.Text,theme,favorite.IsChecked.ToString(),pictures.Signature()});
            string baseline=signature();isDirty=()=>signature()!=baseline;
            var cancel=UI.Button("取消",delegate {CloseOverlay();});cancel.Margin=new Thickness(0,0,8,0);panelFooter.Children.Add(cancel);
            panelFooter.Children.Add(UI.Button("保存卡牌",delegate {
                var changed=original==null?new Skill {id=Guid.NewGuid().ToString(),createdAt=Packs.Now()}:original.Clone();
                changed.name=name.Text.Trim();changed.description=desc.Text.Trim();changed.category=cat.Text.Trim();changed.version=version.Text.Trim();
                changed.tags=tags.Text.Split(new[]{',','，',';','；','\n'},StringSplitOptions.RemoveEmptyEntries).Select(t=>t.Trim()).Where(t=>t.Length>0).Distinct().ToArray();
                changed.content=content.Text;changed.theme=theme;changed.favorite=favorite.IsChecked==true;changed.updatedAt=Packs.Now();pictures.Apply(changed);
                try {Packs.Validate(new List<Skill>{changed});}catch(Exception ex){error.Text=ex.Message;return;}
                var updated=skills.Select(s=>s.id==changed.id?changed:s).ToList();if(original==null)updated.Add(changed);
                if(Commit(updated,"✓  卡牌已保存",changed.id)) {
                    isDirty=null;CloseOverlay();
                    if(!filtered.Any(s=>s.id==changed.id)){category="全部";favorites=false;query="";search.Text="";RebuildFilters();Refresh(false);selected=filtered.FindIndex(s=>s.id==changed.id);Refresh(false);}
                }
            },true));
            name.Focus();
        }
        void QuickCreate() {
            if(!CloseOverlay())return;ShowPanel("N E W   /   新建技能卡牌");
            var stack=UI.Stack(false);panelBody.Children.Add(stack);
            var title=UI.Text("粘贴一次，自动成卡。",26,UI.Ink,true);title.Margin=new Thickness(0,0,0,13);stack.Children.Add(title);
            var subtitle=UI.Text("放入整段提示词，自动整理名称、简介、分类和标签。",14,UI.Muted,false);subtitle.LineHeight=24;subtitle.Margin=new Thickness(0,0,0,23);stack.Children.Add(subtitle);
            var label=UI.Text("你的提示词",12,UI.Muted,true);label.Margin=new Thickness(0,0,0,9);stack.Children.Add(label);
            var wrap=new Grid();stack.Children.Add(wrap);
            var input=new TextBox {Name="QuickPrompt",Height=Math.Max(200,Math.Min(280,Height-480)),AcceptsReturn=true,TextWrapping=TextWrapping.Wrap,VerticalScrollBarVisibility=ScrollBarVisibility.Auto,VerticalContentAlignment=VerticalAlignment.Top,Padding=new Thickness(15),FontSize=14};wrap.Children.Add(input);
            var placeholder=UI.Text("例如：请以我上传的实物照片为依据，制作一张专业电商白底产品精修图……\n\n直接粘贴完整内容即可。",14,UI.Muted,false);placeholder.Margin=new Thickness(16);placeholder.LineHeight=25;placeholder.IsHitTestVisible=false;wrap.Children.Add(placeholder);
            var count=UI.Text("0 字",11,UI.Muted,false);count.HorizontalAlignment=HorizontalAlignment.Right;count.Margin=new Thickness(0,7,0,19);stack.Children.Add(count);
            var pictures=new PictureEditor(this,null);stack.Children.Add(pictures);
            stack.Children.Add(UI.Text("本地整理 · 正文完整保留",12,UI.Muted,true));
            var note=UI.Text("生成后直接保存，需要微调时点卡牌上的 ⋯。",12,UI.Muted,false);note.Margin=new Thickness(0,8,0,0);stack.Children.Add(note);
            var error=UI.Text("",12,UI.Ink,false);error.Margin=new Thickness(0,14,0,0);stack.Children.Add(error);
            isDirty=()=>input.Text.Length>0||pictures.Items.Count>0;
            var cancel=UI.Button("取消",delegate {CloseOverlay();});cancel.Margin=new Thickness(0,0,8,0);panelFooter.Children.Add(cancel);
            var generate=UI.Button("生成卡牌  ↗",null,true);generate.IsEnabled=false;panelFooter.Children.Add(generate);
            input.TextChanged+=delegate {placeholder.Visibility=input.Text.Length==0?Visibility.Visible:Visibility.Collapsed;count.Text=input.Text.Length.ToString("N0")+" 字";generate.IsEnabled=!String.IsNullOrWhiteSpace(input.Text);error.Text="";};
            generate.Click+=delegate {
                try {
                    var created=LocalDraft.Create(input.Text);pictures.Apply(created);var updated=skills.ToList();updated.Add(created);
                    if(!Commit(updated,"✓  已生成「"+created.name+"」，可通过 ⋯ 修改",created.id))return;
                    isDirty=null;CloseOverlay();category="全部";favorites=false;query="";search.Text="";RebuildFilters();Refresh(false);selected=filtered.FindIndex(s=>s.id==created.id);Refresh(false);
                } catch(Exception ex){error.Text=ex.Message;}
            };
            input.Focus();
        }
    }
    public class ImportDialog : Window {
        public int Mode {get;private set;}
        public ImportDialog(int added,int same,int conflict) {
            Title="导入技能库";Width=490;SizeToContent=SizeToContent.Height;ResizeMode=ResizeMode.NoResize;WindowStartupLocation=WindowStartupLocation.CenterOwner;Background=UI.Paper;FontFamily=UI.Typeface;ShowInTaskbar=false;
            var stack=UI.Stack(false);stack.Margin=new Thickness(28);Content=stack;
            stack.Children.Add(UI.Text("把新的能力，装进钱包。",24,UI.Ink,true));
            var text=UI.Text(String.Format("新增 {0} 张 · 完全相同 {1} 张 · 版本冲突 {2} 张",added,same,conflict),14,UI.Muted,false);text.Margin=new Thickness(0,18,0,15);stack.Children.Add(text);
            var explanation=UI.Text(conflict>0?"相同卡牌会自动跳过。对于有冲突的卡牌，请选择处理方式：":"相同卡牌会自动跳过，已有的其他卡牌会保留。",13,UI.Muted,false);explanation.Margin=new Thickness(0,0,0,18);stack.Children.Add(explanation);
            RadioButton local=null,imported=null,both=null;
            if(conflict>0) {
                both=new RadioButton {Content="两份都保留（导入版作为新卡牌）",Margin=new Thickness(0,0,0,13),FontSize=14};stack.Children.Add(both);
                local=new RadioButton {Content="冲突时保留本机版本",IsChecked=true,Margin=new Thickness(0,0,0,13),FontSize=14};stack.Children.Add(local);
                imported=new RadioButton {Content="冲突时使用导入版本",Margin=new Thickness(0,0,0,17),FontSize=14};stack.Children.Add(imported);
            }
            var buttons=UI.Stack(true);buttons.HorizontalAlignment=HorizontalAlignment.Right;buttons.Margin=new Thickness(0,12,0,0);stack.Children.Add(buttons);
            var cancel=UI.Button("取消",delegate {DialogResult=false;});cancel.Margin=new Thickness(0,0,9,0);buttons.Children.Add(cancel);
            buttons.Children.Add(UI.Button("确认导入",delegate {Mode=conflict==0?0:both.IsChecked==true?2:imported.IsChecked==true?1:0;DialogResult=true;},true));
        }
    }
}
