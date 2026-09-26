using System;
using System.Linq;
using System.IO;

namespace SkillWallet {
    public static class FeatureTests {
        public const string ProductPrompt="请以我上传的实物照片为依据，制作一张专业电商白底产品精修图。\r\n参考图用途：\r\n图一是实物依据，决定产品结构、比例、颜色、零件和文字。\r\n如提供图二，仅参考其精修质感、光线和构图。两图冲突时，结构与文字以图一为准。\r\n一、产品结构必须准确\r\n保留实物的外轮廓、长宽比例、零件数量、连接关系、孔洞、凹槽、厚度和边缘。将产品调整为正面平视展示，正面朝向镜头，避免左右倾斜和透视变形。角度调整不得改变产品真实结构。文字要求继续保留：倒置刻字仍保持倒置，不得因调整为正面而自动转正、镜像或改字。\r\n二、表面达到专业商业精修效果\r\n去除灰尘、污渍、指纹、非设计性的划痕、麻点、氧化斑和杂乱环境倒影。\r\n呈现全新、干净、做工精良的产品状态，不要照搬实物照片上的使用痕迹或拍摄瑕疵。\r\n保持原有材质属性：亮面保持亮面，哑光保持哑光，拉丝保留细腻且有规律的拉丝。\r\n金属使用干净、连续、受控的棚拍渐变反光，体现真实金属光泽和体积感。\r\n避免大片死黑、过曝白斑、脏污反光、塑料感和过度磨皮。\r\n三、白底与构图\r\n采用正面平视构图，产品完整居中，左右对称摆正，边缘清晰自然，四周留白均衡，不裁切产品。不凭空补全无法从实物确认的结构。\r\n最终效果：\r\n结构忠于实物，表面干净精致，光线柔和且层次清晰，达到专业电商产品摄影后期精修效果。\r\n生成前先核对结构、文字朝向及材质，再一次性输出1张成品。\r\n若关键结构或文字确实看不清，先指出具体问题，暂不生成，不要猜测。\r\n未经我同意，不要自动生成多个版本或反复重试。";
        public static void Run(Action<bool,string> check) {
            var skill=LocalDraft.Create(ProductPrompt);
            check(skill.name=="电商白底产品精修"&&skill.category=="图像处理","product photo prompt classified and titled");
            check(skill.content==ProductPrompt&&skill.tags.Contains("白底图")&&skill.version=="1.0","original instructions and newlines preserved with useful tags");
            check(LocalDraft.Create("请审查这段 Python 代码，检查函数的边界条件，定位调试问题。").category=="开发","development prompt classified");
            check(LocalDraft.Create("你是一名资深研究分析师。检索研究文献，标明证据和来源。").name=="研究分析师","role extracted from natural language");
            check(LocalDraft.Create("# 我的自定义标题\n请帮助我整理研究资料。").name=="我的自定义标题","explicit heading respected");
            check(LocalDraft.Create("帮我想想今晚吃什么").category=="未分类","unknown topics fall back without invented category");
            check(LocalDraft.Create("Please translate this text and provide a bilingual translation.").category=="翻译","English keywords supported");
            check(LocalDraft.Create("  整理一些文字。\n\n保留空格 \t 和换行。  ").content=="  整理一些文字。\n\n保留空格 \t 和换行。  ","whitespace is not rewritten");
            bool rejected=false;try{LocalDraft.Create(" \r\n ");}catch(InvalidDataException){rejected=true;}check(rejected,"blank draft rejected");
            rejected=false;try{LocalDraft.Create(new string('字',1000001));}catch(InvalidDataException){rejected=true;}check(rejected,"oversized draft rejected without truncation");
            var a=new CarouselMotion();a.Reset(20,2);a.BeginDrag(0);a.Drag(.4,.1);check(Math.Abs(a.Position-2.4)<.00001,"drag follows continuous pointer position");
            a.Drag(.9,.16);a.Release(.16,true);check(a.Target>3,"fast release carries inertia across multiple cards");
            for(int i=0;i<240;i++)a.Step(1.0/60);check(!a.Moving&&a.Position==a.Target,"spring settles precisely on card");
            a.Reset(6,0);a.BeginDrag(0);a.Drag(-8,.1);check(a.Position<0&&a.Position>-.65,"edge drag has bounded rubber resistance");a.Release(.3,true);for(int i=0;i<240;i++)a.Step(1.0/60);check(a.Position==0,"edge rebounds without escaping collection");
            a.Reset(6,2);a.MoveTo(4);a.Step(.05);double before=a.Position;a.MoveTo(0);check(a.Position==before,"reversal preserves current position without jump");
            for(int i=0;i<240;i++)a.Step(1.0/60);check(a.Position==0,"reversal settles at new target");
            a.Reset(6,2);a.BeginDrag(0);a.Drag(.3,.08);a.Release(.4,true);check(a.Target==2,"holding before release cancels stale inertia");
            var b=new CarouselMotion();a.Reset(10,1);b.Reset(10,1);a.MoveTo(7);b.MoveTo(7);for(int i=0;i<30;i++)a.Step(1.0/60);for(int i=0;i<60;i++)b.Step(1.0/120);check(Math.Abs(a.Position-b.Position)<.01,"60 and 120 Hz trajectories agree");
            a.Reset(0,0);a.MoveTo(10);a.Step(.016);check(a.Position==0,"empty library motion remains stable");
        }
    }
}
