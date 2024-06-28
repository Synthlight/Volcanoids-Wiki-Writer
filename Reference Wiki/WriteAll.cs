using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Base_Mod;
using Base_Mod.Models;
using JetBrains.Annotations;
using Newtonsoft.Json;

namespace Wiki_Writer.Reference_Wiki;

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
                        crafters[category] = [item];
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
        writer.WriteLine(@"This does not take range, spread or other RNG into account.\\");
        writer.WriteLine(@"Damage and RoF here are pre-calculated from ammo damage * weapon damage multiplier * projectile count. This is to save column space.\\");
        writer.WriteLine("(Weapons have damage and RoF multipliers, and the ammo has the base damage / RoF values.)");
        writer.WriteLine();
        writer.WriteLine("Formula used: (trueDamage * trueRateOfFire) * (firingTime / (firingTime + reloadDuration))");
        writer.WriteLine();
        writer.WriteLine("| Weapon | Ammo | Damage | RoF | Reload Speed | Capacity | DPS |");

        foreach (var weapon in RuntimeAssetDatabase.Get<ToolItemDefinition>()) {
            if (!weapon.TryGetComponent(out WeaponReloaderAmmo reloader)) continue;
            weapon.TryGetComponent(out WeaponStatsModification statMods);
            var reloadDuration = reloader.ReloadCooldown + reloader.ReloadDuration;
            var ammoCapacity   = reloader.AmmoCapacity;

            foreach (var ammo in reloader.Ammunition.ToArray().Cast<AmmoDefinition>()) {
                var ammoStats            = ammo.AmmoStats;
                var projectileCount      = ammoStats.ProjectileCount;
                var damage               = ammoStats.Damage;
                var damageMultiplier     = statMods == null ? 1 : statMods.DamageMultiplier;
                var trueDamage           = damage * damageMultiplier * projectileCount;
                var rateOfFire           = ammoStats.RateOfFire;
                var rateOfFireMultiplier = statMods == null ? 1 : statMods.RateOfFireMultiplier;
                var trueRateOfFire       = rateOfFire * rateOfFireMultiplier;
                var firingTime           = ammoCapacity / trueRateOfFire;
                var dps                  = (trueDamage * trueRateOfFire) * (firingTime / (firingTime + reloadDuration));
                writer.WriteLine($"| {weapon.CreateWikiLink(false)} | {ammo.CreateWikiLink(false)} | {trueDamage} | {trueRateOfFire} | {reloadDuration} | {ammoCapacity} | {dps} |");
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