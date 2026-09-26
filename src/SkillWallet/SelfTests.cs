using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;

namespace SkillWallet {
    public static class SelfTests {
        public static void Run(string directory) {
            Directory.CreateDirectory(directory);var report=new List<string>();
            Action<bool,string> check=(ok,label)=>{if(!ok)throw new Exception("FAIL "+label);report.Add("PASS "+label);};
            var skills=Samples.Create();skills[0].favorite=true;
            string file=Path.Combine(directory,"roundtrip.skillpack");Packs.Write(file,skills);
            var read=Packs.Read(file);check(Packs.Json().Serialize(skills)==Packs.Json().Serialize(read),"archive round-trip preserves all Unicode fields and favorite state");
            using(var zip=ZipFile.OpenRead(file))check(zip.GetEntry("manifest.json")!=null&&zip.GetEntry("skills.json")!=null,"portable archive structure");
            skills[0].name="商业分析师 · 更新";Packs.Write(file,skills);check(Packs.Read(file+".bak")[0].name==read[0].name,"atomic replacement preserves previous backup");
            var incoming=skills.Select(s=>s.Clone()).ToList();incoming[0].content+="\n补充内容";var extra=incoming[0].Clone();extra.id=Guid.NewGuid().ToString();extra.name="新增";incoming.Add(extra);
            var keep=Packs.Merge(skills,incoming,0);check(keep.Count==7&&keep[0].content==skills[0].content,"merge keep-local adds new and skips identical");
            var replace=Packs.Merge(skills,incoming,1);check(replace.Count==7&&replace[0].content==incoming[0].content,"merge use-imported replaces conflict");
            var both=Packs.Merge(skills,incoming,2);check(both.Count==8&&both.Select(s=>s.id).Distinct().Count()==8&&both.Any(s=>s.name.EndsWith("（导入副本）")),"merge keep-both creates unique ID");
            check(skills.Count==6&&skills[0].content!=incoming[0].content,"merge leaves original collection unchanged");
            var bad=skills.Select(s=>s.Clone()).ToList();bad[0].content="";bool rejected=false;try{Packs.Write(file,bad);}catch(InvalidDataException){rejected=true;}check(rejected&&Packs.Read(file)[0].content==skills[0].content,"invalid save cannot overwrite existing data");
            bad=skills.Select(s=>s.Clone()).ToList();bad[1].id=bad[0].id;rejected=false;try{Packs.Validate(bad);}catch(InvalidDataException){rejected=true;}check(rejected,"duplicate IDs rejected");
            string unknown=Path.Combine(directory,"unknown.skillpack");using(var output=File.Create(unknown))using(var zip=new ZipArchive(output,ZipArchiveMode.Create)){
                using(var w=new StreamWriter(zip.CreateEntry("manifest.json").Open()))w.Write("{\"formatVersion\":999}");
                using(var w=new StreamWriter(zip.CreateEntry("skills.json").Open()))w.Write("[]");
            }
            rejected=false;try{Packs.Read(unknown);}catch(InvalidDataException){rejected=true;}check(rejected,"unsupported format rejected");
            string oversized=Path.Combine(directory,"oversized.skillpack");using(var output=File.Create(oversized))using(var zip=new ZipArchive(output,ZipArchiveMode.Create)){
                using(var w=new StreamWriter(zip.CreateEntry("manifest.json").Open()))w.Write("{\"formatVersion\":1}");
                using(var w=new StreamWriter(zip.CreateEntry("skills.json").Open()))w.Write(new string(' ',Packs.MaxJson+1));
            }
            rejected=false;try{Packs.Read(oversized);}catch(InvalidDataException){rejected=true;}check(rejected,"oversized compressed payload rejected");
            string empty=Path.Combine(directory,"empty.skillpack");Packs.Write(empty,new List<Skill>());check(Packs.Read(empty).Count==0,"empty collection round-trip");
            string broken=Path.Combine(directory,"broken.skillpack");File.WriteAllText(broken,"not a zip");rejected=false;try{Packs.Read(broken);}catch(InvalidDataException){rejected=true;}check(rejected,"corrupt import rejected");
            FeatureTests.Run(check);
            ImportTests.Run(check);
            ManagementTests.Run(directory,check);
            File.WriteAllLines(Path.Combine(directory,"data-tests.txt"),report);
        }
    }
}
