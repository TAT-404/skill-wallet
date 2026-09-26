using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Web.Script.Serialization;

namespace SkillWallet {
    public class Skill {
        public string id { get; set; }
        public string name { get; set; }
        public string description { get; set; }
        public string category { get; set; }
        public string[] tags { get; set; }
        public string version { get; set; }
        public string content { get; set; }
        public bool favorite { get; set; }
        public string createdAt { get; set; }
        public string updatedAt { get; set; }
        public string theme { get; set; }
        public List<SkillImage> pictures { get; set; }
        public string coverId { get; set; }
        public Skill Clone() { var copy=(Skill)MemberwiseClone();copy.tags=tags==null?null:(string[])tags.Clone();copy.pictures=(pictures??new List<SkillImage>()).Select(p=>p.Clone()).ToList();return copy; }
    }
    public class Manifest {
        public int formatVersion { get; set; }
        public string application { get; set; }
        public string exportedAt { get; set; }
    }
    public static partial class Packs {
        public const int MaxJson = 16 * 1024 * 1024;
        public static JavaScriptSerializer Json() { return new JavaScriptSerializer { MaxJsonLength = MaxJson, RecursionLimit = 32 }; }
        public static string Now() { return DateTime.UtcNow.ToString("o"); }
        public static void Validate(List<Skill> skills) {
            if (skills == null || skills.Count > 5000) throw new InvalidDataException("技能库为空或超过 5000 张卡牌上限。");
            var ids = new HashSet<string>();
            foreach (var s in skills) {
                Guid parsed; DateTime created, updated;
                if (s == null || !Guid.TryParse(s.id, out parsed)) throw new InvalidDataException("技能 ID 无效。");
                s.id=parsed.ToString();
                if (!ids.Add(s.id)) throw new InvalidDataException("技能 ID 重复。");
                if (String.IsNullOrWhiteSpace(s.name) || s.name.Length > 120 || String.IsNullOrWhiteSpace(s.content) || s.content.Length > 1000000)
                    throw new InvalidDataException("技能名称或正文无效（名称最多 120 字，正文最多 100 万字）。");
                if ((s.description ?? "").Length > 500 || (s.category ?? "").Length > 40 || (s.version ?? "").Length > 30)
                    throw new InvalidDataException("技能描述、分类或版本过长。");
                if (s.tags == null || s.tags.Length > 20 || s.tags.Any(t => String.IsNullOrWhiteSpace(t) || t.Length > 30))
                    throw new InvalidDataException("标签格式不正确（最多 20 个，每个最多 30 字）。");
                if (!DateTime.TryParse(s.createdAt, out created) || !DateTime.TryParse(s.updatedAt, out updated))
                    throw new InvalidDataException("技能时间格式不正确。");
                s.description = s.description ?? ""; s.category = String.IsNullOrWhiteSpace(s.category) ? "未分类" : s.category;
                s.version = String.IsNullOrWhiteSpace(s.version) ? "1.0" : s.version;
                if (!new[] {"lime", "purple", "blue", "peach", "pink", "mint"}.Contains(s.theme)) s.theme = "lime";
            }
            Pictures.Validate(skills);
        }
        static string ReadEntry(ZipArchive zip, string name) {
            var matches = zip.Entries.Where(e => e.FullName == name).ToArray();
            if (matches.Length != 1 || matches[0].Length > MaxJson) throw new InvalidDataException("技能库缺少必要文件、包含重复文件或体积过大。");
            using (var input = matches[0].Open()) using (var output = new MemoryStream()) {
                byte[] buffer = new byte[8192]; int n;
                while ((n = input.Read(buffer, 0, buffer.Length)) > 0) {
                    if (output.Length + n > MaxJson) throw new InvalidDataException("解压后的技能库过大。");
                    output.Write(buffer, 0, n);
                }
                return new UTF8Encoding(false, true).GetString(output.ToArray());
            }
        }
        public static List<Skill> Read(string path) {
            if (new FileInfo(path).Length > 160 * 1024 * 1024) throw new InvalidDataException("技能库文件不能超过 160 MB。");
            using (var file = File.OpenRead(path)) using (var zip = new ZipArchive(file, ZipArchiveMode.Read)) {
                if (zip.Entries.Count > 2050) throw new InvalidDataException("技能库包含过多文件。");
                var manifest = Json().Deserialize<Manifest>(ReadEntry(zip, "manifest.json"));
                if (manifest == null || (manifest.formatVersion != 1 && manifest.formatVersion != 2)) throw new InvalidDataException("暂不支持此技能库格式版本。");
                var skills = Json().Deserialize<List<Skill>>(ReadEntry(zip, "skills.json"));
                Pictures.ReadAssets(zip,skills);
                Validate(skills); return skills;
            }
        }
        public static void Write(string path, List<Skill> skills) {
            Validate(skills);
            string json = Json().Serialize(skills);
            if (Encoding.UTF8.GetByteCount(json) > MaxJson) throw new InvalidDataException("技能库超过 16 MB 内容上限。");
            var full = Path.GetFullPath(path); Directory.CreateDirectory(Path.GetDirectoryName(full));
            var temp = full + "." + Guid.NewGuid().ToString("N") + ".tmp";
            try {
                using (var file = new FileStream(temp, FileMode.CreateNew, FileAccess.Write, FileShare.None)) {
                    using (var zip = new ZipArchive(file, ZipArchiveMode.Create, true)) {
                        WriteEntry(zip, "manifest.json", Json().Serialize(new Manifest { formatVersion = skills.Any(s=>s.pictures.Count>0)?2:1, application = "Skill Wallet", exportedAt = Now() }));
                        WriteEntry(zip, "skills.json", json);
                        Pictures.WriteAssets(zip,skills);
                    }
                    file.Flush(true);
                }
                if (File.Exists(full)) File.Replace(temp, full, full + ".bak", true);
                else File.Move(temp, full);
            } finally { if (File.Exists(temp)) File.Delete(temp); }
        }
        static void WriteEntry(ZipArchive zip, string name, string text) {
            using (var writer = new StreamWriter(zip.CreateEntry(name, CompressionLevel.Optimal).Open(), new UTF8Encoding(false))) writer.Write(text);
        }
        public static bool Same(Skill a, Skill b) { return Json().Serialize(a) == Json().Serialize(b); }
        // 0: keep local, 1: use imported version, 2: retain both versions.
        public static List<Skill> Merge(List<Skill> local, List<Skill> incoming, int mode) {
            return PlanImport(local,incoming).Apply(local,mode);
        }
    }
    public static class Samples {
        public static List<Skill> Create() {
            return new List<Skill> {
                Make("商业分析师", "商业", "拆解市场与商业模式，\n让每一个判断更有依据。", "市场,竞品,商业模式", "purple", "你是一名商业分析师。请先确认分析对象、目标市场和业务阶段，再从用户需求、市场规模、竞争格局、商业模式与风险五个维度展开分析。\n\n要求：\n1. 区分已知事实、假设与待验证问题。\n2. 没有可靠来源时，不编造市场数据。\n3. 对关键判断给出依据和反例。\n4. 最后列出三个可执行的下一步及其验证指标。"),
                Make("灵感写作搭子", "写作", "把零散的想法，\n变成有温度的表达。", "创作,文案,表达", "peach", "你是一位耐心的写作伙伴。请根据我的素材，先确认读者、目的、语气和发布场景，然后给出三个不同方向的写作提纲。\n\n选定方向后，完成初稿。保留我的观点和个人表达，使用具体事例，避免套话与夸张。没有提供的经历和数据不要虚构。最后提出精简、增强说服力和调整语气的建议。"),
                Make("深度研究专家", "研究分析", "穿过信息的噪音，\n找到问题背后的答案。", "研究,分析,报告", "lime", "你是一名专业研究分析师。你的任务是帮助我理解复杂问题，并输出有依据的结构化结论。\n\n工作步骤：\n1. 明确问题、研究范围和目标。\n2. 拆解关键因素，列出待验证的假设。\n3. 在具备检索工具时查找可靠资料；没有检索工具时说明限制。\n4. 交叉核对信息，区分事实、推断与观点。\n5. 输出结论、证据、局限性和下一步建议。\n\n回答要求：\n- 保持客观，说明不同观点。\n- 标注不确定信息和资料日期。\n- 不编造数据、引用或来源。\n- 先给核心结论，再展开分析。"),
                Make("产品思考伙伴", "产品设计", "从真实需求出发，\n把好想法变成好产品。", "需求,体验,MVP", "blue", "你是一名产品经理。请协助我把模糊的想法整理为可验证的产品方案。\n\n依次讨论：目标用户、使用场景、核心问题、现有替代方案、最小可行产品和成功指标。优先追问会影响方案的关键未知条件。\n\n输出：用户故事、核心流程、功能优先级、验收标准、风险及验证计划。避免为未经验证的需求过早设计复杂功能。"),
                Make("代码审查助手", "开发", "多一双细致的眼睛，\n让代码更稳，也更清晰。", "代码,审查,质量", "mint", "你是一名代码审查伙伴。请先理解代码的目标和调用环境，再检查逻辑错误、边界条件、资源管理、安全风险和维护成本。\n\n每条发现应包含：位置、触发条件、实际影响、修复建议和验证方法。按影响程度排序。不确定的地方明确说明，不把个人风格偏好当作缺陷。如果没有实质问题，直接说明并列出尚未验证的部分。"),
                Make("学习拆解教练", "学习", "把复杂知识拆小，\n让每一次理解都更扎实。", "学习,拆解,练习", "pink", "你是一名学习教练。先了解我的已有基础、学习目标和可用时间，再将主题拆分为循序渐进的小单元。\n\n每个单元包含：核心概念、通俗解释、具体例子、一个练习和自检标准。通过提问确认我的理解，再决定是否进入下一步。发现误解时指出具体原因，用新的例子帮助我修正。")
            };
        }
        static Skill Make(string name, string category, string description, string tags, string theme, string content) {
            return new Skill { id = Guid.NewGuid().ToString(), name = name, description = description, category = category, tags = tags.Split(','), theme = theme, content = content, favorite = false, version = "1.0", createdAt = Packs.Now(), updatedAt = Packs.Now() };
        }
    }
}
