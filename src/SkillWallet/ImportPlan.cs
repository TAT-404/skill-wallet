using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Security.Cryptography;

namespace SkillWallet {
    public sealed class ImportPlan {
        internal sealed class Entry {internal Skill Skill;internal string Target;}
        internal readonly List<Entry> Entries=new List<Entry>();
        public int Added {get;internal set;}
        public int Same {get;internal set;}
        public int Conflicts {get;internal set;}
        public List<Skill> Apply(List<Skill> local,int mode) {
            if(mode<0||mode>2)throw new ArgumentOutOfRangeException("mode");
            var result=local.Select(s=>s.Clone()).ToList();var indices=new Dictionary<string,int>();
            for(int i=0;i<result.Count;i++)indices.Add(result[i].id,i);
            foreach(var entry in Entries) {
                if(entry.Target==null){indices.Add(entry.Skill.id,result.Count);result.Add(entry.Skill.Clone());}
                else if(mode==1){int index=indices[entry.Target];var copy=entry.Skill.Clone();copy.id=result[index].id;copy.favorite=result[index].favorite;copy.createdAt=result[index].createdAt;result[index]=copy;}
                else if(mode==2){var copy=entry.Skill.Clone();copy.id=Guid.NewGuid().ToString();if(copy.name.Length<=110)copy.name+="（导入副本）";result.Add(copy);}
            }
            Packs.Validate(result);return result;
        }
    }
    public static partial class Packs {
        static string ImportText(string text){return (text??"").Replace("\r\n","\n").Replace('\r','\n');}
        static string ImportName(string name){string value=(name??"").Trim();const string suffix="（导入副本）";while(value.EndsWith(suffix,StringComparison.Ordinal))value=value.Substring(0,value.Length-suffix.Length);return value;}
        static string ImportHash(object value){using(var sha=SHA256.Create())return Convert.ToBase64String(sha.ComputeHash(Encoding.UTF8.GetBytes(Json().Serialize(value))));}
        static string ImportIdentity(Skill s){return ImportHash(new{ name=ImportName(s.name),content=ImportText(s.content)});}
        // Ignore machine-generated IDs/times and local favorite state; retain all
        // meaningful content and image differences. Do not trim prompt whitespace.
        static string ImportSignature(Skill s){return ImportHash(new{
            name=ImportName(s.name),content=ImportText(s.content),description=ImportText(s.description),s.category,s.version,
            tags=(s.tags??new string[0]).Distinct().OrderBy(t=>t,StringComparer.Ordinal).ToArray(),
            theme=String.IsNullOrEmpty(s.coverId)?s.theme:null,s.coverId,
            pictures=(s.pictures??new List<SkillImage>()).Select(p=>p.id).ToArray()
        });}
        public static ImportPlan PlanImport(List<Skill> local,List<Skill> incoming) {
            // Work on copies: validation and preview must not mutate either library.
            var existing=local.Select(s=>s.Clone()).ToList();var source=incoming.Select(s=>s.Clone()).ToList();Validate(existing);Validate(source);
            var byId=existing.ToDictionary(s=>s.id,s=>s.id);var byIdentity=new Dictionary<string,string>();var known=new HashSet<string>();
            foreach(var skill in existing){string identity=ImportIdentity(skill);if(!byIdentity.ContainsKey(identity))byIdentity.Add(identity,skill.id);known.Add(ImportSignature(skill));}
            var plan=new ImportPlan();
            foreach(var skill in source) {
                if(!known.Add(ImportSignature(skill))){plan.Same++;continue;}
                string identity=ImportIdentity(skill),target;
                if(byId.TryGetValue(skill.id,out target)||byIdentity.TryGetValue(identity,out target)){
                    plan.Conflicts++;plan.Entries.Add(new ImportPlan.Entry {Skill=skill,Target=target});
                    if(!byIdentity.ContainsKey(identity))byIdentity.Add(identity,target);
                } else {
                    plan.Added++;plan.Entries.Add(new ImportPlan.Entry {Skill=skill});byId.Add(skill.id,skill.id);byIdentity.Add(identity,skill.id);
                }
            }
            return plan;
        }
    }
}
