using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Windows.Controls;

namespace SkillWallet {
    public partial class MainWindow {
        public void RunImportUiChecks(string report) {
            var lines=new List<string>();Action<bool,string> check=(ok,label)=>{if(!ok)throw new Exception(label);lines.Add("PASS "+label);};
            var before=File.ReadAllBytes(dataPath);bool backup=File.Exists(dataPath+".bak");var roots=cards.Select(c=>c.Root).ToArray();var source=Samples.Create();
            for(int i=0;i<3;i++)ImportItems(source);
            check(skills.Count==6&&cards.Select(c=>c.Root).SequenceEqual(roots),"repeated duplicate imports do not add or rebuild cards");
            check(File.ReadAllBytes(dataPath).SequenceEqual(before)&&File.Exists(dataPath+".bak")==backup,"duplicate-only import leaves library bytes and backup untouched");
            check(toastText.Text.Contains("已跳过 6 张"),"duplicate-only import explains the number of skipped cards");
            var dialog=new ImportDialog(1,2,3);var options=FindChildren<RadioButton>((System.Windows.DependencyObject)dialog.Content).ToArray();
            check(options.Single(r=>r.IsChecked==true).Content.ToString()=="冲突时保留本机版本","real conflict defaults to preserving local cards instead of making extra copies");dialog.Close();
            File.WriteAllLines(report,lines);
        }
    }
    public static class ImportTests {
        public static void Run(Action<bool,string> check) {
            var local=Samples.Create();var independent=Samples.Create();
            string before=Packs.Json().Serialize(local),incomingBefore=Packs.Json().Serialize(independent);
            var plan=Packs.PlanImport(local,independent);
            check(plan.Added==0&&plan.Conflicts==0&&plan.Same==6,"independently created default libraries deduplicate despite different IDs and times");
            foreach(int mode in new[]{0,1,2})check(plan.Apply(local,mode).Count==6,"exact semantic duplicates skipped for merge mode "+mode);
            check(Packs.Json().Serialize(local)==before&&Packs.Json().Serialize(independent)==incomingBefore,"import preview and apply leave source collections unchanged");
            var favorite=local[0].Clone();favorite.favorite=true;favorite.createdAt="2020-01-01T00:00:00Z";favorite.updatedAt=Packs.Now();favorite.tags=favorite.tags.Reverse().ToArray();favorite.content=favorite.content.Replace("\n","\r\n");
            plan=Packs.PlanImport(local,new List<Skill>{favorite});check(plan.Same==1&&plan.Conflicts==0&&!plan.Apply(local,1)[0].favorite,"favorite, timestamps, tag order and platform line endings do not create duplicate cards");
            var newCard=local[0].Clone();newCard.id=Guid.NewGuid().ToString();newCard.name="真正的新卡牌";var duplicate=newCard.Clone();duplicate.id=Guid.NewGuid().ToString();
            plan=Packs.PlanImport(local,new List<Skill>{newCard,duplicate});check(plan.Added==1&&plan.Same==1&&plan.Apply(local,2).Count==7,"duplicate items within one imported archive are inserted only once");
            var mixed=independent.Concat(new[]{newCard,duplicate}).ToList();plan=Packs.PlanImport(local,mixed);
            check(plan.Added==1&&plan.Same==7&&plan.Conflicts==0,"preview reports exact added and skipped counts for a mixed archive");
            var merged=plan.Apply(local,0);plan=Packs.PlanImport(merged,mixed);check(plan.Added==0&&plan.Conflicts==0&&plan.Same==8,"reimporting the same mixed archive is idempotent");
            var edited=local[0].Clone();edited.content+="\n新要求";plan=Packs.PlanImport(local,new List<Skill>{edited});
            check(plan.Conflicts==1&&plan.Same==0,"same-ID changed prompt stays a real version conflict");
            var kept=plan.Apply(local,0);var replaced=plan.Apply(local,1);var both=plan.Apply(local,2);
            check(kept.Count==6&&kept[0].content==local[0].content&&replaced.Count==6&&replaced[0].content==edited.content&&both.Count==7,"real conflict supports keep local, replace and retain both");
            check(Packs.PlanImport(both,new List<Skill>{edited}).Same==1&&Packs.Merge(both,new List<Skill>{edited},2).Count==7,"reimporting an already retained conflict copy does not add another copy");
            var metadata=local[0].Clone();metadata.id=Guid.NewGuid().ToString();metadata.description="更新后的介绍";plan=Packs.PlanImport(local,new List<Skill>{metadata});
            check(plan.Conflicts==1&&plan.Added==0,"same name and prompt with meaningful metadata changes is a conflict even with a new ID");
            replaced=plan.Apply(local,1);check(replaced.Count==6&&replaced[0].id==local[0].id&&replaced[0].description==metadata.description,"cross-ID replacement preserves local card identity");
            var other=local[0].Clone();other.id=Guid.NewGuid().ToString();other.content+="\n独立用途";check(Packs.PlanImport(local,new List<Skill>{other}).Added==1,"same title alone never discards a distinct prompt");
            other=local[0].Clone();other.id=Guid.NewGuid().ToString();other.name="另一个用途";check(Packs.PlanImport(local,new List<Skill>{other}).Added==1,"same prompt with a distinct title remains an intentional separate skill");
            other=local[0].Clone();other.content=" "+other.content;check(Packs.PlanImport(local,new List<Skill>{other}).Conflicts==1,"prompt whitespace is not erased while detecting equality");
            var duplicatedLocal=local.Select(s=>s.Clone()).ToList();var existingCopy=local[0].Clone();existingCopy.id=Guid.NewGuid().ToString();duplicatedLocal.Add(existingCopy);
            check(Packs.Merge(duplicatedLocal,independent,0).Count==7,"existing local duplicates are not silently deleted");
            var longName=local[0].Clone();longName.name=new string('长',120);var longEdited=longName.Clone();longEdited.content+=" changed";
            both=Packs.Merge(new List<Skill>{longName},new List<Skill>{longEdited},2);
            check(both.Count==2&&Packs.Merge(both,new List<Skill>{longEdited},2).Count==2,"long names remain valid and retained copies also deduplicate");
        }
    }
}
