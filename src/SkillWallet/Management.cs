using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace SkillWallet {
    public static class CardOrder {
        // target is a gap in the original visible sequence. Hidden cards retain their slots.
        public static List<Skill> Move(List<Skill> all,List<string> visible,HashSet<string> moving,int target) {
            var chosen=visible.Where(moving.Contains).ToList();if(chosen.Count==0)return all.ToList();
            target=Math.Max(0,Math.Min(visible.Count,target));
            int insert=visible.Take(target).Count(id=>!moving.Contains(id));
            var sequence=visible.Where(id=>!moving.Contains(id)).ToList();sequence.InsertRange(insert,chosen);
            var visibleSet=new HashSet<string>(visible);var byId=all.ToDictionary(s=>s.id);int i=0;
            return all.Select(s=>visibleSet.Contains(s.id)?byId[sequence[i++]]:s).ToList();
        }
    }
    public partial class MainWindow {
        bool managing,manageReorderCandidate,manageReordering,manageLayoutMoving;
        double managePhase;Point reorderPointer;Skill reorderSkill;
        List<string> reorderPreview;int reorderGap;bool heldReorder,reorderMoved;double edgeVelocity;
        Border insertionMarker;
        readonly HashSet<string> selectedIds=new HashSet<string>();
        Button manageButton,deleteSelected,selectAll;
        Border managementBar;TextBlock managementCount,browseHint;Border dragBadge;TextBlock dragBadgeText;
        double ManageT {get{return managePhase*managePhase*(3-2*managePhase);}}
        bool ManageChanging {get{return Math.Abs(managePhase-(managing?1:0))>.0001;}}
        void BuildManagementActions(StackPanel actions) {
            manageButton=UI.Button("⋯",delegate {SetManagement(!managing);});manageButton.ToolTip="管理卡牌 · 批量选择、删除和排序";manageButton.Width=45;manageButton.Height=34;manageButton.FontSize=21;manageButton.Padding=new Thickness(6,0,6,0);manageButton.Margin=new Thickness(8,0,0,0);actions.Children.Add(manageButton);
        }
        void BuildManagementBar(StackPanel parent) {
            managementBar=new Border {Height=0,Opacity=0,ClipToBounds=true,IsHitTestVisible=false};parent.Children.Add(managementBar);
            var bar=new DockPanel {LastChildFill=false,Margin=new Thickness(0,8,0,3)};managementBar.Child=bar;
            var right=UI.Stack(true);DockPanel.SetDock(right,Dock.Right);bar.Children.Add(right);
            deleteSelected=UI.Button("删除所选",delegate {DeleteSelection();});deleteSelected.Foreground=UI.Ink;deleteSelected.Padding=new Thickness(12,5,12,5);right.Children.Add(deleteSelected);
            var left=UI.Stack(true);DockPanel.SetDock(left,Dock.Left);bar.Children.Add(left);
            selectAll=UI.Button("全选",delegate {if(selectedIds.Count==filtered.Count)selectedIds.Clear();else foreach(var s in filtered)selectedIds.Add(s.id);UpdateManagementBar();Position(false);});selectAll.Padding=new Thickness(11,5,11,5);left.Children.Add(selectAll);
            managementCount=UI.Text("",11,UI.Muted,false);managementCount.VerticalAlignment=VerticalAlignment.Center;managementCount.Margin=new Thickness(14,0,0,0);left.Children.Add(managementCount);
        }
        void SetManagement(bool value) {
            if(overlay!=null)return;CancelHold();if(manageReorderCandidate)CancelReorder();
            downSkill=null;dragging=false;stage.ReleaseMouseCapture();wheelDeadline=0;motion.Reset(filtered.Count,motion.Position);
            slideSoundArmed=false;managing=value;manageButton.Content=value?"完成":"⋯";manageButton.FontSize=value?12:21;MotionFx.Width(manageButton,value?60:45,.25);
            manageButton.ToolTip=value?"完成管理，返回卡牌浏览":"管理卡牌 · 批量选择、删除和排序";
            if(!value)selectedIds.Clear();UpdateManagementBar();StartRendering();
        }
        void ApplyManagementPhase() {
            managementBar.Height=43*ManageT;managementBar.Opacity=ManageT;managementBar.IsHitTestVisible=managing&&!ManageChanging;
            if(browseHint!=null)browseHint.Text=managing?"短划浏览 · 长按排序 · Esc 完成":"拖动切换 · 点击复制 · ⋯ 详情";
        }
        void AdvanceManagement(double dt) {
            double target=managing?1:0;managePhase=target>managePhase?Math.Min(target,managePhase+dt/ .55):Math.Max(target,managePhase-dt/ .55);ApplyManagementPhase();
            if(manageReordering&&reorderMoved) {
                double direction=reorderPointer.X<80?-Math.Min(1,(80-reorderPointer.X)/80):reorderPointer.X>stage.ActualWidth-80?Math.Min(1,(reorderPointer.X-stage.ActualWidth+80)/80):0;
                edgeVelocity+=(direction*2.7-edgeVelocity)*(1-Math.Exp(-dt*10));
                if(Math.Abs(edgeVelocity)>.02){motion.Reset(filtered.Count,motion.Clamp(motion.Position+edgeVelocity*dt));reorderMoved=true;UpdateReorderPreview();}
                PlaceDragBadge();
            }
            manageLayoutMoving=false;
            foreach(var c in cards) {
                double slot=reorderPreview==null?c.Index:reorderPreview.IndexOf(c.Skill.id);
                double delta=slot-c.VisualSlot;if(Math.Abs(delta)<.002)c.VisualSlot=slot;else {c.VisualSlot+=delta*(1-Math.Exp(-dt*19));manageLayoutMoving=true;}
            }
        }
        void UpdateManagementBar() {
            if(managementCount==null)return;
            managementCount.Text="管理模式  ·  已选 "+selectedIds.Count+" / "+filtered.Count+" 张";
            deleteSelected.Content="删除所选"+(selectedIds.Count>0?"（"+selectedIds.Count+"）":"");deleteSelected.IsEnabled=selectedIds.Count>0;
            selectAll.Content=filtered.Count>0&&selectedIds.Count==filtered.Count?"取消全选":"全选";selectAll.IsEnabled=filtered.Count>0;
        }
        void ActivateCard(Skill s){if(ManageChanging)return;if(managing)ToggleSelection(s);else Copy(s);}
        void ToggleSelection(Skill s) {if(!managing||ManageChanging)return;if(!selectedIds.Add(s.id))selectedIds.Remove(s.id);UpdateManagementBar();Position(false);}
        void DeleteSelection() {
            if(selectedIds.Count==0)return;int count=selectedIds.Count;
            if(Dialogs.Show(this,"确定删除所选的 "+count+" 张卡牌及其效果图片吗？","批量删除",MessageBoxButton.YesNo,MessageBoxImage.Warning)!=MessageBoxResult.Yes)return;
            RemoveSelection();
        }
        bool RemoveSelection() {
            int count=selectedIds.Count;if(count==0)return false;
            var before=skills.Select(s=>s.Clone()).ToList();if(!Commit(skills.Where(s=>!selectedIds.Contains(s.id)).ToList(),"已删除 "+count+" 张卡牌",null))return false;SetUndo(before,"删除");
            selectedIds.Clear();UpdateManagementBar();Position(false);return true;
        }
        void BeginReorder(Skill s,Point point) {
            if(!managing||ManageChanging)return;
            CancelHold();downSkill=null;dragging=false;wheelDeadline=0;motion.Reset(filtered.Count,motion.Position);reorderSkill=s;downPoint=reorderPointer=point;manageReorderCandidate=true;manageReordering=false;heldReorder=false;reorderMoved=false;edgeVelocity=0;if(!testMode&&!stage.IsMouseCaptured)stage.CaptureMouse();
        }
        void StartHeldReorder(){heldReorder=true;StartReorderVisual();}
        void StartReorderVisual(){if(manageReordering)return;manageReordering=true;if(!selectedIds.Contains(reorderSkill.id)){selectedIds.Clear();selectedIds.Add(reorderSkill.id);}UpdateManagementBar();CreateDragBadge();}
        void MoveReorder(Point point) {
            reorderPointer=point;
            if(Math.Abs(point.X-downPoint.X)<8&&Math.Abs(point.Y-downPoint.Y)<8&&!reorderMoved)return;
            reorderMoved=true;StartReorderVisual();
            UpdateReorderPreview();StartRendering();
        }
        void UpdateReorderPreview() {
            reorderGap=(int)Math.Floor((reorderPointer.X-stage.ActualWidth/2)/Spacing()+motion.Position+.5);
            reorderGap=Math.Max(0,Math.Min(filtered.Count,reorderGap));
            reorderPreview=CardOrder.Move(filtered,filtered.Select(s=>s.id).ToList(),selectedIds,reorderGap).Select(s=>s.id).ToList();
            if(insertionMarker!=null){double x=stage.ActualWidth/2+(reorderGap-motion.Position-.5)*Spacing();Canvas.SetLeft(insertionMarker,x);Canvas.SetTop(insertionMarker,Math.Max(10,(stage.ActualHeight-470*BaseScale())/2+14));insertionMarker.Height=470*BaseScale()-28;}
        }
        void CreateDragBadge() {
            dragBadgeText=UI.Text("移动 "+selectedIds.Count+" 张",12,UI.Ink,true);
            dragBadge=new Border {Child=dragBadgeText,Background=UI.B("#F02B2B2B"),BorderBrush=UI.B("#BDBDBD"),BorderThickness=new Thickness(1),CornerRadius=new CornerRadius(9),Padding=new Thickness(15,10,15,10),IsHitTestVisible=false};Panel.SetZIndex(dragBadge,30000);stage.Children.Add(dragBadge);PlaceDragBadge();
            insertionMarker=new Border {Width=2,Background=UI.B("#A3DDDDDD"),CornerRadius=new CornerRadius(1),IsHitTestVisible=false};Panel.SetZIndex(insertionMarker,20000);stage.Children.Add(insertionMarker);
        }
        void PlaceDragBadge(){if(dragBadge==null)return;Canvas.SetLeft(dragBadge,Math.Max(0,Math.Min(stage.ActualWidth-155,reorderPointer.X+15)));Canvas.SetTop(dragBadge,Math.Max(0,Math.Min(stage.ActualHeight-45,reorderPointer.Y-48)));}
        void ClearReorder() {
            manageReorderCandidate=false;manageReordering=false;reorderPreview=null;if(dragBadge!=null)stage.Children.Remove(dragBadge);if(insertionMarker!=null)stage.Children.Remove(insertionMarker);insertionMarker=null;dragBadge=null;edgeVelocity=0;stage.ReleaseMouseCapture();StartRendering();
        }
        void CancelReorder(){ClearReorder();}
        void EndReorder() {
            bool moved=manageReordering&&reorderMoved;bool held=heldReorder;var clicked=reorderSkill;
            var updated=moved?CardOrder.Move(skills,filtered.Select(s=>s.id).ToList(),selectedIds,reorderGap):null;
            var positions=cards.ToDictionary(c=>c.Skill.id,c=>c.VisualSlot);ClearReorder();
            if(moved&& !updated.Select(s=>s.id).SequenceEqual(skills.Select(s=>s.id))) {
                var before=skills.Select(s=>s.Clone()).ToList();string viewport=filtered.Count==0?null:updated.Where(s=>filtered.Any(f=>f.id==s.id)).ElementAt(Math.Min(selected,filtered.Count-1)).id;
                preserveOrderVisuals=true;try {if(Commit(updated,"✓  已保存卡牌顺序",viewport)){SetUndo(before,"排序");foreach(var c in cards){double old;if(positions.TryGetValue(c.Skill.id,out old))c.VisualSlot=old;}Position(false);StartRendering();}}finally{preserveOrderVisuals=false;}
            }else if(!moved&&!held)ToggleSelection(clicked);
        }
        void AddPictureGallery(StackPanel stack,Skill skill) {
            if(skill.pictures==null||skill.pictures.Count==0)return;
            var label=UI.Text("效果图片 · 点击放大",11,UI.Muted,true);label.Margin=new Thickness(0,0,0,10);stack.Children.Add(label);
            var gallery=new WrapPanel {Margin=new Thickness(0,0,0,16)};stack.Children.Add(gallery);
            foreach(var p in skill.pictures){Button button=null;button=UI.Button("",delegate {Pictures.View(this,p,button);});button.ToolTip=p.name;button.Padding=new Thickness(0);button.Margin=new Thickness(0,0,8,8);button.Content=new Image {Source=Pictures.Bitmap(p,720),Width=126,Height=94,Stretch=Stretch.UniformToFill};gallery.Children.Add(button);}
        }
        public void ShowManagePreview(){SetManagement(true);managePhase=1;ApplyManagementPhase();selectedIds.Add(filtered[1].id);selectedIds.Add(filtered[2].id);UpdateManagementBar();Position(false);StopRendering();}
    }
}
