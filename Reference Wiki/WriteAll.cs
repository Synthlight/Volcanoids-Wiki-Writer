using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Base_Mod;
using Base_Mod.Models;
using JetBrains.Annotations;
using Newtonsoft.Json;

namespace Wiki_Writer.Reference_Wiki {
    public static class WriteAll {
        [OnIslandSceneLoaded]
        [UsedImplicitly]
        public static void Go() {
            var wikiPages = new List<WikiPage>();

            // Build a list of what crafters produce what categories.
            var crafters = new Dictionary<string /*category*/, List<ItemDefinition> /*crafter*/>();
            foreach (var item in RuntimeAssetDatabase.Get<ItemDefinition>()) {
                if (item.Prefabs?.Length > 0 && item.Prefabs[0].TryGetComponent<Producer>(out var producer)) {
                    var categories = producer.Categories.Select(cat => cat.name).ToList();
                    foreach (var category in categories) {
                        if (crafters.ContainsKey(category) && !crafters[category].Contains(item)) {
                            crafters[category].Add(item);
                        } else {
                            crafters[category] = new List<ItemDefinition> {item};
                        }
                    }
                }
            }

            WriteAllThingPage(WriteItems.Write(wikiPages), "items", "Items");
            WriteAllThingPage(WriteRecipes.Write(wikiPages, crafters), "recipes", "Recipes");
            WriteDpsCalculations();

            var json = JsonConvert.SerializeObject(wikiPages, Formatting.None, new JsonSerializerSettings {
                NullValueHandling = NullValueHandling.Ignore
            });
            File.WriteAllText($@"{Plugin.REFERENCE_WIKI_OUTPUT_PATH}\pages.json", json);
        }

        private static void WriteAllThingPage(List<string> pages, string @namespace, string allWhat) {
            // Not optimized, but IDGAF.
            pages.Sort((s1, s2) => {
                var a = s1.Split('|')[1];
                var b = s2.Split('|')[1];
                return string.Compare(a, b, StringComparison.Ordinal);
            });

            using var writer = new StreamWriter($@"{Plugin.REFERENCE_WIKI_OUTPUT_PATH}\{@namespace}\all_{allWhat.ToLower()}.txt", false, Encoding.UTF8);
            writer.WriteLine($"====== All {allWhat} ====");

            foreach (var page in pages) {
                writer.WriteLine(page);
            }

            writer.WriteLine();
            writer.Write(Plugin.GetFooter());
        }

        private static void WriteDpsCalculations() {
            using var writer = new StreamWriter($@"{Plugin.REFERENCE_WIKI_OUTPUT_PATH}\dps_calculations.txt", false, Encoding.UTF8);
            writer.WriteLine("====== DPS Calculations ====");
            writer.WriteLine();
            writer.WriteLine("This does not take range, reloading, spread or anything else into account. Just RoF (Rate-of-Fire) and damage.");
            writer.WriteLine();
            writer.WriteLine("| Weapon | Ammo | Damage | Damage Multiplier | RoF | RoF Multiplier | DPS |");

            foreach (var weapon in RuntimeAssetDatabase.Get<ToolItemDefinition>()) {
                if (!weapon.TryGetComponent(out WeaponReloaderAmmo reloader)) continue;
                weapon.TryGetComponent(out WeaponStatsModification statMods);

                foreach (var ammo in reloader.Ammunition.ToArray().Cast<AmmoDefinition>()) {
                    var ammoStats            = ammo.AmmoStats;
                    var damage               = ammoStats.Damage;
                    var damageMultiplier     = statMods == null ? 1 : statMods.DamageMultiplier;
                    var rateOfFire           = ammoStats.RateOfFire;
                    var rateOfFireMultiplier = statMods == null ? 1 : statMods.RateOfFireMultiplier;
                    var dps                  = (damage * damageMultiplier) * (rateOfFire * rateOfFireMultiplier);
                    writer.WriteLine($"| {weapon.CreateWikiLink(false)} | {ammo.CreateWikiLink(false)} | {damage} | {damageMultiplier} | {rateOfFire} | {rateOfFireMultiplier} | {dps} |");
                }
            }

            writer.WriteLine();
            writer.Write(Plugin.GetFooter());
        }

        public static void EraseAndCreateDir(string path) {
            //if (Directory.Exists(path)) Directory.Delete(path, true);
            Directory.CreateDirectory(path);
        }
    }
}