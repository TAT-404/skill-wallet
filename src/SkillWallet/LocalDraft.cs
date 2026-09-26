using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace SkillWallet {
    // Deterministic, offline metadata extraction. The source text is never rewritten.
    public static class LocalDraft {
        class Rule {
            public string Category, Theme, Title; public string[] Words;
            public Rule(string category,string theme,string title,string words) {Category=category;Theme=theme;Title=title;Words=words.Split('|');}
        }
        static readonly Rule[] Rules = {
            new Rule("图像处理","blue","图像创作助手","精修|白底|修图|抠图|图像|图片|照片|摄影|海报|插画|渲染|绘画|image|photo|retouch"),
            new Rule("开发","mint","代码开发助手","代码|编程|函数|调试|程序|算法|单元测试|数据库|python|javascript|typescript|sql|code|debug"),
            new Rule("研究分析","lime","深度研究专家","研究|资料|文献|论文|证据|引用|来源|检索|调研|research"),
            new Rule("写作","peach","写作创作助手","写作|文案|文章|写一篇|故事|小说|文笔|读者|公众号|小红书|润色|copywriting"),
            new Rule("商业","purple","商业分析师","商业|市场|竞品|商业模式|盈利|营销|销售|创业|竞争|business"),
            new Rule("产品设计","blue","产品思考伙伴","产品经理|用户体验|用户需求|需求分析|原型|交互|用户故事|验收标准|MVP"),
            new Rule("学习","pink","学习拆解教练","学习|教学|课程|练习|学生|知识点|考试|辅导|老师|study"),
            new Rule("翻译","mint","翻译助手","翻译|译文|原文|双语|中译英|英译中|translate|translation"),
            new Rule("办公效率","purple","办公效率助手","会议|纪要|待办|周报|日报|邮件|日程|总结工作|整理表格|Excel")
        };
        static readonly string[][] Tags = {
            new[]{"电商","电商|商品"},new[]{"白底图","白底"},new[]{"精修","精修|修图|retouch"},new[]{"实物还原","实物|结构还原"},
            new[]{"抠图","抠图|去除背景"},new[]{"摄影","摄影|棚拍"},new[]{"插画","插画|绘画"},new[]{"海报","海报"},
            new[]{"代码","代码|code"},new[]{"审查","审查|review"},new[]{"调试","调试|debug"},new[]{"Python","python"},new[]{"SQL","sql"},
            new[]{"研究","研究|research"},new[]{"报告","报告"},new[]{"文献","文献|论文"},new[]{"分析","分析"},
            new[]{"文案","文案"},new[]{"写作","写作"},new[]{"润色","润色"},new[]{"故事","故事|小说"},
            new[]{"竞品","竞品"},new[]{"市场","市场"},new[]{"商业模式","商业模式"},new[]{"需求","需求"},new[]{"MVP","MVP"},
            new[]{"学习","学习"},new[]{"练习","练习"},new[]{"翻译","翻译|translate|translation"},new[]{"会议纪要","会议|纪要"},new[]{"邮件","邮件"}
        };
        static bool Has(string text,string word) {
            if(Regex.IsMatch(word,@"^[a-zA-Z]+$"))return Regex.IsMatch(text,@"(?<![a-zA-Z])"+Regex.Escape(word)+@"(?![a-zA-Z])",RegexOptions.IgnoreCase);
            return text.IndexOf(word,StringComparison.OrdinalIgnoreCase)>=0;
        }
        static string Short(string text,int limit) {
            if(text.Length<=limit)return text;
            int end=limit-1;if(Char.IsHighSurrogate(text[end-1]))end--;
            return text.Substring(0,end).TrimEnd()+"…";
        }
        static string Clean(string text) {
            text=Regex.Replace(text,@"^[\s#>*\-]+","");
            text=Regex.Replace(text,@"^(?:请你|请|帮我|帮助我|麻烦你|你现在是|你是一名|你是一个|你是|作为一名)\s*","");
            return Regex.Replace(text,@"\s+"," ").Trim(' ','：',':','。');
        }
        public static Skill Create(string content) {
            if(String.IsNullOrWhiteSpace(content))throw new InvalidDataException("请先粘贴或输入一段提示词。");
            if(content.Length>1000000)throw new InvalidDataException("正文最多支持 100 万字，请缩短后再生成。");
            // Limit classification work independently of the preserved full body.
            string body=content.Substring(0,Math.Min(content.Length,16000));
            string lead=body.Substring(0,Math.Min(body.Length,300));
            Rule best=null;double bestScore=0;
            foreach(var rule in Rules) {
                double score=rule.Words.Sum(w=>(Has(body,w)?1:0)+(Has(lead,w)?2:0));
                if(score>bestScore){best=rule;bestScore=score;}
            }
            var lines=Regex.Split(body,@"[\r\n]+").Where(s=>!String.IsNullOrWhiteSpace(s)).ToArray();
            string first=lines.First().Trim();
            string name=null;
            var named=Regex.Match(first,@"^(?:#{1,3}\s+|(?:名称|标题|Skill名称|Skill name)\s*[:：]\s*)(.{2,60})$",RegexOptions.IgnoreCase);
            if(named.Success)name=named.Groups[1].Value.Trim();
            if(name==null) {
                var role=Regex.Match(lead,@"(?:你是|担任|扮演)(?:一名|一个|我的)?\s*(?:专业的?|资深的?)?([^，。！!\r\n：:]{2,16}?(?:专家|分析师|教练|助手|顾问|编辑|翻译官|工程师|产品经理|老师))");
                if(role.Success)name=role.Groups[1].Value.Trim();
            }
            if(name==null && best!=null) {
                name=best.Title;
                if(best.Category=="图像处理") {
                    if(Has(body,"精修")||Has(body,"修图"))name=(Has(body,"电商")?"电商":"")+(Has(body,"白底")?"白底":"")+(Has(body,"产品")||Has(body,"商品")?"产品":"图片")+"精修";
                    else if(Has(lead,"抠图"))name="图片抠图助手";
                    else if(Has(lead,"海报"))name="海报设计助手";
                    else if(Has(lead,"插画"))name="插画创作助手";
                }
                if(best.Category=="开发"&&(Has(body,"审查")||Has(body,"review")))name="代码审查助手";
                if(best.Category=="办公效率"&&Has(body,"会议"))name="会议纪要整理";
            }
            if(name==null)name=Short(Clean(Regex.Split(first,@"[，。！？!?；;]")[0]),18);
            if(String.IsNullOrWhiteSpace(name))name="我的新 Skill";
            var sentences=Regex.Split(body,@"[。！？!?\r\n]+").Select(Clean).Where(s=>s.Length>3&&!Regex.IsMatch(s,@"^(参考图用途|回答要求|输出要求|工作步骤|名称|标题)[:：]?$"));
            string description=Short(sentences.FirstOrDefault()??Clean(first),78);
            var tags=Tags.Where(t=>t[1].Split('|').Any(w=>Has(body,w))).Select(t=>t[0]).Take(4).ToArray();
            var skill=new Skill {id=Guid.NewGuid().ToString(),name=Short(name,24),description=description,category=best==null?"未分类":best.Category,tags=tags,version="1.0",content=content,theme=best==null?"lime":best.Theme,favorite=false,createdAt=Packs.Now(),updatedAt=Packs.Now()};
            Packs.Validate(new List<Skill>{skill});return skill;
        }
    }
}
