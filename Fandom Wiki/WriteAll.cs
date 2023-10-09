using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Base_Mod;
using Base_Mod.Models;
using JetBrains.Annotations;
using UnityEngine;

namespace Wiki_Writer.Fandom_Wiki {
    public static class WriteAll {
        private static readonly Regex RECIPE_CATEGORY_REGEX = new("(Production|Refinement|Research|Scrap)Tier(\\d)");
        private static readonly GUID  COPPER_LEVERS         = GUID.Parse("3b42ca843c8036b4087c1584eee1e406");

        [OnIslandSceneLoaded]
        [UsedImplicitly]
        public static void Go() {
            Debug.Log("Writing Fandom wiki stuff.");
            // Build a list of what crafters produce what categories.
            var crafters = new Dictionary<string /*category*/, List<ItemDefinition> /*crafter*/>();
            foreach (var item in RuntimeAssetDatabase.Get<ItemDefinition>()) {
                if (item.Prefabs?.Length > 0 && item.Prefabs[0].TryGetComponent<Producer>(out var producer)) {
                    var categories = producer.Categories.Select(cat => cat.name).ToList();
                    foreach (var category in categories) {
                        if (crafters.ContainsKey(category) && !crafters[category].Contains(item)) {
                            crafters[category].Add(item);
                        } else {
                            crafters[category] = new() {item};
                        }
                    }
                }
            }
            Write(crafters);
        }

        public static void Write(Dictionary<string, List<ItemDefinition>> crafters) {
            const string path = Plugin.FANDOM_WIKI_OUTPUT_PATH + Plugin.ITEMS_NAMESPACE;
            Reference_Wiki.WriteAll.EraseAndCreateDir(path);

            foreach (var item in RuntimeAssetDatabase.Get<ItemDefinition>()) {
                var localizedName = item.GetLocalizedName();
                var localizedDesc = item.GetLocalizedDesc();

                // Currently only happens for melee weapon ammo.
                if (string.IsNullOrEmpty(localizedName)) continue;

                using var writer = new StreamWriter($@"{path}\{item.GetSafeName()}.txt", false, Encoding.UTF8);

                Debug.Log($"Writing item: {item.GetName()}");

                var properties = new Dictionary<string, object> {
                    {"name", localizedName},
                    {"image", $"{item.GetSafeName()}.png"},
                    {"description", localizedDesc.Replace("\n", "<br>")},
                    {"stackSize", item.MaxStack},
                };

                AmmoDefinition[]        ammo     = null;
                WeaponStatsModification statMods = null;

                if (item is ToolItemDefinition toolItemDef) {
                    if (toolItemDef.TryGetComponent(out WeaponReloaderAmmo reloader)) {
                        properties["ammo_capacity"]   = reloader.AmmoCapacity;
                        properties["reload_duration"] = reloader.ReloadDuration;

                        ammo = reloader.Ammunition.ToArray().Cast<AmmoDefinition>().ToArray();
                    }

                    if (toolItemDef.TryGetComponent(out WeaponReloaderBattery reloaderBattery)) {
                        properties["ammo_capacity"]   = reloaderBattery.AmmoCapacity;
                        properties["reload_duration"] = reloaderBattery.ReloadDuration;
                    }

                    if (toolItemDef.TryGetComponent(out statMods)) {
                        // Moved to a table.
                        //properties["damage_multiplier"]                      = statMods.DamageMultiplier;
                        //properties["spread_multiplier"]                      = statMods.SpreadMultiplier;
                        //properties["recoil_vertical_multiplier"]             = statMods.RecoilVerticalMultiplier;
                        //properties["recoil_horizontal_multiplier"]           = statMods.RecoilHorizontalMultiplier;
                        //properties["recoil_first_shot_multiplier"]           = statMods.RecoilFirstShotMultiplier;
                        //properties["recoil_minimum_burst_length_multiplier"] = statMods.RecoilMinimumBurstLengthMultiplier;
                        //properties["projectile_count_multiplier"]            = statMods.ProjectileCountMultiplier;
                        //properties["rate_of_fire_multiplier"]                = statMods.RateOfFireMultiplier;
                        //properties["effective_range_multiplier"]             = statMods.EffectiveRangeMultiplier;
                        //properties["range_multiplier"]                       = statMods.RangeMultiplier;
                        //properties["muzzle_velocity_multiplier"]             = statMods.MuzzleVelocityMultiplier;
                        //properties["gravity_factor_multiplier"]              = statMods.GravityFactorMultiplier;
                        //properties["noise_multiplier"]                       = statMods.NoiseMultiplier;
                        //properties["hip_cone_multiplier"]                    = statMods.HipConeMultiplier;
                        //properties["aim_cone_multiplier"]                    = statMods.AimConeMultiplier;
                    }
                }

                if (item.TryGetComponent(out PowerPlant powerPlant)) {
                    properties["energy"]          = powerPlant.EnergyPerSecond;
                    properties["fuel_efficiency"] = powerPlant.FuelEfficiency;
                }

                if (item.TryGetComponent(out EnergyConsumer energyConsumer)) {
                    properties["energy"] = -energyConsumer.EnergyPerSecond;
                }

                if (item.TryGetComponent(out HeatRibsModule geothermal)) {
                    properties["energy"] = geothermal.EnergyOutput;
                }

                if (item.TryGetComponent(out PackableModule packableModule)) {
                    properties["corePoints"] = packableModule.CoreSlotCount;
                }

                if (item.TryGetComponent(out PackableModuleHealth packableModuleHealth)) {
                    properties["health"]      = packableModuleHealth.MaxHP;
                    properties["closedArmor"] = $"{packableModuleHealth.Armor * 100}%";
                }

                if (item.TryGetComponent(out DrillshipObjectHealth drillshipObjectHealth)) {
                    properties["health"] = drillshipObjectHealth.MaxHP;
                }

                if (item.TryGetComponent(out Inventory inventory)) {
                    properties["inventorySlots"] = inventory.Capacity;
                }

                if (item.TryGetComponent(out GridModule gridModule)) {
                    properties["size"] = gridModule.ModuleCount;
                }

                try {
                    foreach (var stat in item.Stats.Items) {
                        var statName  = stat.Property.Name;
                        var statValue = stat.GetStatValue().ToString();
                        if (statName == "Energy" && float.TryParse(statValue, out var v) && v == 0) continue;

                        // ReSharper disable once ConvertSwitchStatementToSwitchExpression
                        switch (statName) {
                            case "Placement":
                                properties["placement"] = statValue;
                                break;
                        }
                    }
                } catch (Exception) {
                    Debug.LogError("Error parsing stats for item, skipping stats.");
                }

                FillRecipeProperties(crafters, item, properties);

                var categories = GetCategories(item);
                if (categories.Any()) {
                    foreach (var category in categories) {
                        writer.WriteLine($"[[Category:{category}]]");
                    }

                    for (var i = 0; i < 1; i++) {
                        writer.WriteLine();
                    }
                }

                // Write info box.
                writer.WriteLine($"{{{{Infobox {GetInfoBoxType(item)}");
                foreach (var (key, value) in properties) {
                    writer.WriteLine($"| {key} = {value}".Trim());
                }
                writer.WriteLine("}}");

                WriteModifiedAmmoStatTable(writer, ammo, statMods);

                WriteStatModifierTable(writer, statMods);
            }
        }

        private static List<string> GetCategories(ItemDefinition item) {
            var categories = new List<string>();
            var itemName   = item.GetName();

            // Ammo
            if (item is AmmoDefinition)
                categories.Add("Ammo");

            // Components
            // Basically, no prefab, and not one of the other categories.
            if (item.Prefabs.Length == 0
                && !itemName.ContainsIgnoreCase("Parts")
                && !itemName.ContainsIgnoreCase("Intel")
                && !(itemName.ContainsIgnoreCase(" Ore") || itemName.ContainsIgnoreCase("Raw")) // Space before 'ore' to exclude 'core'.
                && !itemName.ContainsIgnoreCase("Schematic")
                && !(itemName.ContainsIgnoreCase("Lever") && item.AssetId != COPPER_LEVERS) // Exclude the non-component levers.
                && item is not (AmmoDefinition
                    or TrainCoreItemDefinition
                    or TrainDrillItemDefinition
                    or TrainEngineItemDefinition
                    or TrainHullItemDefinition
                    or TrainSegmentDefinition
                    or TrainTracksItemDefinition)
                && !item.HasAny(typeof(ToolFirstPersonData),
                                typeof(WeaponReloaderAmmo),
                                typeof(WeaponReloaderBattery),
                                typeof(WeaponMining)))
                categories.Add("Components");

            // Devices
            if ((item.HasAny(typeof(GridModule))
                 || itemName.ContainsIgnoreCase("Lever"))
                && !(item.HasAny(typeof(PaintGridModule),
                                 typeof(PackableModule))
                     || item.AssetId == COPPER_LEVERS)) // Don't match the lever component.
                categories.Add("Devices");

            // Drillship Parts
            if (itemName.ContainsIgnoreCase("Parts"))
                categories.Add("Drillship Parts");

            // Intel
            if (itemName.ContainsIgnoreCase("Intel"))
                categories.Add("Intel");

            // Materials
            if (itemName.ContainsIgnoreCase(" Ore") // Space before 'ore' to exclude 'core'.
                || itemName.ContainsIgnoreCase("Raw"))
                categories.Add("Materials");

            // Modules
            if (item.HasAny(typeof(PaintGridModule),
                            typeof(PackableModule)))
                categories.Add("Modules");

            // Schematics
            if (itemName.ContainsIgnoreCase("Schematic"))
                categories.Add("Schematics");

            // Structures
            if (item.HasAny(typeof(Subpart)))
                categories.Add("Structures");

            // Tools
            if (item.HasAny(typeof(ToolFirstPersonData))
                && !item.HasAny(typeof(WeaponReloaderAmmo), // If 1st person and *not* a weapon.
                                typeof(WeaponReloaderBattery),
                                typeof(WeaponMining)))
                categories.Add("Tools");

            // Turrets
            if (item.HasAny(typeof(Turret))
                && item.HasAny(typeof(PackableModule)))
                categories.Add("Turrets");

            // Upgrades
            if (item is TrainCoreItemDefinition
                or TrainDrillItemDefinition
                or TrainEngineItemDefinition
                or TrainHullItemDefinition
                or TrainSegmentDefinition
                or TrainTracksItemDefinition)
                categories.Add("Upgrades");

            // Weapons
            if (item.HasAny(typeof(WeaponReloaderAmmo),
                            typeof(WeaponReloaderBattery),
                            typeof(WeaponMining)))
                categories.Add("Weapons");

            return categories;
        }

        private static string GetInfoBoxType(ItemDefinition item) {
            var type                                                                         = "material";
            if (item.HasAny(typeof(WeaponReloaderAmmo), typeof(WeaponReloaderBattery))) type = "weapon";
            if (item.HasAny(typeof(WeaponMining))) type                                      = "melee";
            if (item.HasAny(typeof(PaintGridModule), typeof(PackableModule))) type           = "module";
            return type;
        }

        private static void WriteStatModifierTable(TextWriter writer, WeaponStatsModification statMods) {
            if (statMods != null) {
                // Break before writing.
                for (var i = 0; i < 10; i++) {
                    writer.WriteLine();
                }

                writer.WriteLine("==Modifiers==");
                writer.WriteLine("{|class=\"article-table mw-collapsible\"");
                writer.WriteLine("!Stat");
                writer.WriteLine("!Value\u2800\u2800\u2800\u2800");

                AddModifierStat(writer, "Damage Multiplier", statMods.DamageMultiplier);
                AddModifierStat(writer, "Spread Multiplier", statMods.SpreadMultiplier);
                AddModifierStat(writer, "Recoil Vertical Multiplier", statMods.RecoilVerticalMultiplier);
                AddModifierStat(writer, "Recoil Horizontal Multiplier", statMods.RecoilHorizontalMultipler);
                AddModifierStat(writer, "Recoil First-Shot Multiplier", statMods.RecoilFirstShotMultiplier);
                AddModifierStat(writer, "Recoil Minimum Burst Length Multiplier", statMods.RecoilMinimumBurstLengthMultiplier);
                AddModifierStat(writer, "Projectile Count Multiplier", statMods.ProjectileCountMultiplier);
                AddModifierStat(writer, "Rate of Fire Multiplier", statMods.RateOfFireMultiplier);
                AddModifierStat(writer, "Effective Range Multiplier", statMods.EffectiveRangeMultiplier);
                AddModifierStat(writer, "Range Multiplier", statMods.RangeMultiplier);
                AddModifierStat(writer, "Muzzle Velocity Multiplier", statMods.MuzzleVelocityMultiplier);
                AddModifierStat(writer, "Gravity Factor Multiplier", statMods.GravityFactorMultiplier);
                AddModifierStat(writer, "Noise Multiplier", statMods.NoiseMultiplier);
                AddModifierStat(writer, "Hip-Cone Multiplier", statMods.HipConeMultiplier);
                AddModifierStat(writer, "Aim-Cone Multiplier", statMods.AimConeMultiplier);

                writer.WriteLine("|}");
            }
        }

        private static void AddModifierStat(TextWriter writer, string statName, object value) {
            writer.WriteLine("|-");
            writer.WriteLine($"!{statName}");
            writer.WriteLine($"|{value}");
        }

        private static void WriteModifiedAmmoStatTable(TextWriter writer, AmmoDefinition[] ammo, [CanBeNull] WeaponStatsModification statMods) {
            var ammoDefinitions = new List<AmmoDefinition>();

            if (ammo is {Length: > 0}) {
                ammoDefinitions.AddRange(ammo.Select(ammoDef => RuntimeAssetDatabase.Get<AmmoDefinition>().First(def => def.AssetId == ammoDef.AssetId)));
            }

            if (ammoDefinitions.Any()) {
                // Break before writing.
                for (var i = 0; i < 10; i++) {
                    writer.WriteLine();
                }

                writer.WriteLine("==Ammo==");
                writer.WriteLine("{|class=\"article-table mw-collapsible\"");
                writer.WriteLine("!Stat");

                for (var i = 0; i < ammoDefinitions.Count; i++) {
                    var header                                 = $"![[{ammoDefinitions[i].GetLocalizedName()}]]";
                    if (i + 1 == ammoDefinitions.Count) header += "\u2800\u2800\u2800\u2800";
                    writer.WriteLine(header);
                }

                AddAmmoStat(writer, ammoDefinitions, "Damage", def => def.Damage * (statMods?.DamageMultiplier ?? 1f));
                AddAmmoStat(writer, ammoDefinitions, "Damage at Range Multiplier", def => def.DamageAtRangeMult);
                AddAmmoStat(writer, ammoDefinitions, "Damage Type", def => def.DamageType);
                AddAmmoStat(writer, ammoDefinitions, "Effective Range", def => def.EffectiveRange * (statMods?.EffectiveRangeMultiplier ?? 1f));
                AddAmmoStat(writer, ammoDefinitions, "Gravity Factor", def => def.GravityFactor * (statMods?.GravityFactorMultiplier ?? 1f));
                AddAmmoStat(writer, ammoDefinitions, "Muzzle Velocity", def => def.MuzzleVelocity * (statMods?.MuzzleVelocityMultiplier ?? 1f));
                AddAmmoStat(writer, ammoDefinitions, "Noise", def => def.Noise * (statMods?.NoiseMultiplier ?? 1f));
                AddAmmoStat(writer, ammoDefinitions, "Projectile Count", def => def.ProjectileCount * (statMods?.ProjectileCountMultiplier ?? 1f));
                AddAmmoStat(writer, ammoDefinitions, "Range", def => def.Range * (statMods?.RangeMultiplier ?? 1f));
                AddAmmoStat(writer, ammoDefinitions, "Rate of Fire", def => def.RateOfFire * (statMods?.RateOfFireMultiplier ?? 1f));
                AddAmmoStat(writer, ammoDefinitions, "Spread", def => def.Spread * (statMods?.SpreadMultiplier ?? 1f));
                AddAmmoStat(writer, ammoDefinitions, "Aim Accuracy", def => def.AimAccuracy.Cone * (statMods?.AimConeMultiplier ?? 1f));
                AddAmmoStat(writer, ammoDefinitions, "Hip Accuracy", def => def.HipAccuracy.Cone * (statMods?.HipConeMultiplier ?? 1f));
                AddAmmoStat(writer, ammoDefinitions, "Recoil", def => {
                    var vertical = def.Recoil.Vertical * (statMods?.RecoilVerticalMultiplier ?? 1f);
                    var horizontal = (def.Recoil.HorizontalMinMax.x * (statMods?.RecoilHorizontalMultipler ?? 1f)
                                      + def.Recoil.HorizontalMinMax.y * (statMods?.RecoilHorizontalMultipler ?? 1f))
                                     / 2;
                    return (vertical + horizontal) / 2;
                });

                writer.WriteLine("|}");
            }
        }

        private static void AddAmmoStat(TextWriter writer, List<AmmoDefinition> ammoDefinitions, string statName, Func<AmmoStats, object> getValue) {
            writer.WriteLine("|-");
            writer.WriteLine($"!{statName}");
            foreach (var ammoDef in ammoDefinitions) {
                var value = getValue(ammoDef.AmmoStats).ToString();
                writer.WriteLine($"|{value}");
            }
        }

        private static void FillRecipeProperties(IReadOnlyDictionary<string, List<ItemDefinition>> crafters, Definition item, IDictionary<string, object> properties) {
            var recipe = RuntimeAssetDatabase.Get<Recipe>()
                                             .Where(recipe => recipe.Output.Item.AssetId == item.AssetId)
                                             .FirstOrDefault(recipe => !recipe.GetName().Contains("Worktable"));

            if (recipe != null) {
                var requirements = "";
                var i            = 0;

                foreach (var category in recipe.Categories) {
                    var categoryName = category.name;
                    if (!crafters.ContainsKey(categoryName)) continue;
                    foreach (var crafter in crafters[categoryName]) {
                        if (i > 0) requirements += "<br>";
                        requirements += $"[[{crafter.GetLocalizedName()}]]";
                        i++;
                    }
                }

                properties["craftedIn"] = requirements;

                if (recipe.RequiredUpgrades.Any()) {
                    var schematics = "";
                    i = 0;

                    foreach (var requirement in recipe.RequiredUpgrades) {
                        if (i > 0) schematics += "<br>";
                        schematics += $"[[{requirement.GetLocalizedName()}]]";
                        i++;
                    }

                    properties["schematics"] = schematics;
                }

                foreach (var category in recipe.Categories) {
                    var match = RECIPE_CATEGORY_REGEX.Match(category.GetName());
                    if (match.Success) {
                        if (match.Groups[2].Value == "0") continue;
                        var moduleName = match.Groups[1].Value.Replace("Refinement", "Refinery");
                        properties["production_req"] = $"{moduleName} Module Tier {match.Groups[2].Value}";
                    }
                }

                i = 1;
                foreach (var input in recipe.Inputs) {
                    properties[$"ingredient{i}"] = input.Item.GetLocalizedName();
                    properties[$"quantity{i}"]   = input.Amount;
                    i++;
                }
            }
        }
    }
}