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
        private static readonly Regex RECIPE_CATEGORY_REGEX = new Regex("(Production|Refinement|Research|Scrap)Tier(\\d)");

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
                            crafters[category] = new List<ItemDefinition> {item};
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
                    {"description", localizedDesc},
                    {"ammo_capacity", item.MaxStack},
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
                        properties["damage_multiplier"]                      = statMods.DamageMultiplier;
                        properties["spread_multiplier"]                      = statMods.SpreadMultiplier;
                        properties["recoil_vertical_multiplier"]             = statMods.RecoilVerticalMultiplier;
                        properties["recoil_horizontal_multiplier"]           = statMods.RecoilHorizontalMultipler;
                        properties["recoil_first_shot_multiplier"]           = statMods.RecoilFirstShotMultiplier;
                        properties["recoil_minimum_burst_length_multiplier"] = statMods.RecoilMinimumBurstLengthMultiplier;
                        properties["projectile_count_multiplier"]            = statMods.ProjectileCountMultiplier;
                        properties["rate_of_fire_multiplier"]                = statMods.RateOfFireMultiplier;
                        properties["effective_range_multiplier"]             = statMods.EffectiveRangeMultiplier;
                        properties["range_multiplier"]                       = statMods.RangeMultiplier;
                        properties["muzzle_velocity_multiplier"]             = statMods.MuzzleVelocityMultiplier;
                        properties["gravity_factor_multiplier"]              = statMods.GravityFactorMultiplier;
                        properties["noise_multiplier"]                       = statMods.NoiseMultiplier;
                        properties["hip_cone_multiplier"]                    = statMods.HipConeMultiplier;
                        properties["aim_cone_multiplier"]                    = statMods.AimConeMultiplier;
                    }
                }

                var recipe = RuntimeAssetDatabase.Get<Recipe>()
                                                 .Where(recipe => recipe.Output.Item.name == item.name)
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

                // Write info box.
                // ReSharper disable once StringLiteralTypo
                writer.WriteLine("{{Infobox gun");
                foreach (var (key, value) in properties) {
                    writer.WriteLine($"| {key} = {value}".Trim());
                }
                writer.WriteLine("}}");

                // Write modified ammo stats.
                var ammoDefinitions = new List<AmmoDefinition>();

                if (ammo != null && ammo.Length > 0) {
                    ammoDefinitions.AddRange(ammo.Select(ammoDef => RuntimeAssetDatabase.Get<AmmoDefinition>().First(def => def.AssetId == ammoDef.AssetId)));
                }

                if (ammoDefinitions.Any() && statMods != null) {
                    // Break before writing ammo stats.
                    for (var i = 0; i < 10; i++) {
                        writer.WriteLine();
                    }

                    writer.WriteLine("==Ammo==");
                    writer.WriteLine("{|class=\"article-table\"");
                    writer.WriteLine("!Stat");

                    foreach (var ammoDef in ammoDefinitions) {
                        writer.WriteLine($"![[{ammoDef.GetLocalizedName()}]]");
                    }

                    AddStat(writer, ammoDefinitions, "Damage", def => def.Damage * statMods.DamageMultiplier);
                    AddStat(writer, ammoDefinitions, "Damage at Range Multiplier", def => def.DamageAtRangeMult);
                    AddStat(writer, ammoDefinitions, "Damage Type", def => def.DamageType);
                    AddStat(writer, ammoDefinitions, "Effective Range", def => def.EffectiveRange * statMods.EffectiveRangeMultiplier);
                    AddStat(writer, ammoDefinitions, "Gravity Factor", def => def.GravityFactor * statMods.GravityFactorMultiplier);
                    AddStat(writer, ammoDefinitions, "Muzzle Velocity", def => def.MuzzleVelocity * statMods.MuzzleVelocityMultiplier);
                    AddStat(writer, ammoDefinitions, "Noise", def => def.Noise * statMods.NoiseMultiplier);
                    AddStat(writer, ammoDefinitions, "Projectile Count", def => def.ProjectileCount * statMods.ProjectileCountMultiplier);
                    AddStat(writer, ammoDefinitions, "Range", def => def.Range * statMods.RangeMultiplier);
                    AddStat(writer, ammoDefinitions, "Rate of Fire", def => def.RateOfFire * statMods.RateOfFireMultiplier);
                    AddStat(writer, ammoDefinitions, "Spread", def => def.Spread * statMods.SpreadMultiplier);
                    AddStat(writer, ammoDefinitions, "Aim Accuracy", def => def.AimAccuracy.Cone * statMods.AimConeMultiplier);
                    AddStat(writer, ammoDefinitions, "Hip Accuracy", def => def.HipAccuracy.Cone * statMods.HipConeMultiplier);
                    AddStat(writer, ammoDefinitions, "Recoil", def => {
                        var vertical = def.Recoil.Vertical * statMods.RecoilVerticalMultiplier;
                        var horizontal = (def.Recoil.HorizontalMinMax.x * statMods.RecoilHorizontalMultipler
                                          + def.Recoil.HorizontalMinMax.y * statMods.RecoilHorizontalMultipler)
                                         / 2;
                        return (vertical + horizontal) / 2;
                    });

                    writer.WriteLine("|}");
                }
            }
        }

        private static void AddStat(TextWriter writer, List<AmmoDefinition> ammoDefinitions, string statName, Func<AmmoStats, object> getValue) {
            writer.WriteLine("|-");
            writer.WriteLine($"!{statName}");
            foreach (var ammoDef in ammoDefinitions) {
                var value = getValue(ammoDef.AmmoStats).ToString();
                writer.WriteLine($"|{value}");
            }
        }
    }
}