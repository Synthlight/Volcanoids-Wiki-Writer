using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using Base_Mod.Models;
using JetBrains.Annotations;
using Newtonsoft.Json;
using UnityEngine;

namespace Wiki_Writer.Reference_Wiki {
    public static class WriteRecipes {
        private const string ITEMS_NAMESPACE   = "items";
        private const string RECIPES_NAMESPACE = "recipes";

        private static Dictionary<string /*category*/, List<ItemDefinition> /*crafter*/> crafters;

        [OnIslandSceneLoaded]
        [UsedImplicitly]
        public static void Go() {
            var wikiPages = new List<WikiPage>();

            // Build a list of what crafters produce what categories.
            crafters = new Dictionary<string, List<ItemDefinition>>();
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

            WriteAllThingPage(DumpItems(wikiPages), "items", "Items");
            WriteAllThingPage(DumpRecipes(wikiPages), "recipes", "Recipes");

            var json = JsonConvert.SerializeObject(wikiPages, Formatting.None, new JsonSerializerSettings {
                NullValueHandling = NullValueHandling.Ignore
            });
            File.WriteAllText($@"{Plugin.REFERENCE_WIKI_OUTPUT_PATH}\pages.json", json);
        }

        private static List<string> DumpItems(ICollection<WikiPage> wikiPages) {
            const string path = Plugin.REFERENCE_WIKI_OUTPUT_PATH + ITEMS_NAMESPACE;
            EraseAndCreateDir(path);
            const string imgPath = Plugin.REFERENCE_WIKI_OUTPUT_PATH + @"\media\items";
            EraseAndCreateDir(imgPath);

            var pages = new List<string>();

            foreach (var item in RuntimeAssetDatabase.Get<ItemDefinition>()) {
                var itemName      = Plugin.GetName(item.name);
                var itemSafeName  = itemName.Replace(' ', '_');
                var localizedName = item.GetLocalizedName();
                var localizedDesc = item.GetLocalizedDesc().Trim();

                // Currently only happens for melee weapon ammo.
                if (string.IsNullOrEmpty(localizedName)) continue;

                WriteTexture(item.Icon, imgPath, itemSafeName);

                using (var writer = new StreamWriter($@"{path}\{itemSafeName}.txt", false, Encoding.UTF8)) {
                    var wikiPage = new WikiPage {
                        name        = localizedName,
                        description = localizedDesc,
                        type        = "item",
                        path        = $"{ITEMS_NAMESPACE}:{itemName}",
                        imagePath   = $"{ITEMS_NAMESPACE}:{itemName}.png"
                    };

                    Debug.Log($"Writing item: {itemName}");

                    writer.WriteLine($"{{{{ {ITEMS_NAMESPACE}:{itemName}.png?200}}}}");
                    writer.WriteLine($"====== {localizedName} ====");
                    writer.WriteLine($"| Internal name | {itemName} |");
                    writer.WriteLine($"| AssetId | {item.AssetId} |");
                    writer.WriteLine($"| Type | {item.GetType()} |");

                    writer.WriteLine();
                    writer.WriteLine("==== Description ====");
                    writer.WriteLine(localizedDesc);

                    WriteStats(writer, item, wikiPage);

                    writer.WriteLine();
                    writer.WriteLine("==== Recipes Outputting This Item ====");

                    foreach (var recipe in RuntimeAssetDatabase.Get<Recipe>()
                                                               .Where(recipe => recipe.Output.Item.name == item.name)) {
                        var recipeName          = Plugin.GetName(recipe.name);
                        var recipeLocalizedName = recipe.Output.Item.GetLocalizedName();
                        writer.WriteLine($"  * [[{RECIPES_NAMESPACE}:{recipeName}|{recipeLocalizedName} Recipe]] ({recipeName})");
                    }

                    writer.WriteLine();
                    writer.WriteLine("==== Recipes Using This Item As Input ====");

                    foreach (var recipe in from recipe in RuntimeAssetDatabase.Get<Recipe>()
                                           from input in recipe.Inputs
                                           where input.Item.name == item.name
                                           select recipe) {
                        var recipeName          = Plugin.GetName(recipe.name);
                        var recipeLocalizedName = recipe.Output.Item.GetLocalizedName();
                        writer.WriteLine($"  * [[{RECIPES_NAMESPACE}:{recipeName}|{recipeLocalizedName} Recipe]] ({recipeName})");
                    }

                    writer.WriteLine();
                    writer.WriteLine("==== Recipes Requiring This Item ====");

                    foreach (var recipe in from recipe in RuntimeAssetDatabase.Get<Recipe>()
                                           from requirement in recipe.RequiredUpgrades
                                           where requirement.name == item.name
                                           select recipe) {
                        var recipeName          = Plugin.GetName(recipe.name);
                        var recipeLocalizedName = recipe.Output.Item.GetLocalizedName();
                        writer.WriteLine($"  * [[{RECIPES_NAMESPACE}:{recipeName}|{recipeLocalizedName} Recipe]] ({recipeName})");
                    }

                    writer.WriteLine();
                    writer.Write(Plugin.GetFooter());

                    pages.Add($"  * [[{ITEMS_NAMESPACE}:{itemName}|{localizedName}]]");
                    wikiPages.Add(wikiPage);
                }
            }

            return pages;
        }

        private static void WriteStats(TextWriter writer, ItemDefinition item, WikiPage wikiPage) {
            writer.WriteLine();
            writer.WriteLine("==== Stats ====");
            AddStat(writer, wikiPage.stats, "Max Stack", item.MaxStack);
            AddStatsFromPropertySet(writer, wikiPage.stats, item.Stats);

            if (item.TryGetComponent(out PowerPlant powerPlant)) {
                AddStat(writer, wikiPage.stats, "Energy Per Second", powerPlant.EnergyPerSecond);
                AddStat(writer, wikiPage.stats, "Fuel Efficiency", powerPlant.FuelEfficiency);
            }

            if (item.TryGetComponent(out EnergyConsumer energyConsumer)) {
                AddStat(writer, wikiPage.stats, "Energy Per Second", -energyConsumer.EnergyPerSecond);
            }

            if (item.TryGetComponent(out HeatRibsModule geothermal)) {
                AddStat(writer, wikiPage.stats, "Energy Per Second", geothermal.EnergyOutput);
            }

            if (item.TryGetComponent(out PackableModule packableModule)) {
                AddStat(writer, wikiPage.stats, "Core Slot Cost", packableModule.CoreSlotCount);
            }

            if (item.TryGetComponent(out ProductionModule prodModule)) {
                AddStat(writer, wikiPage.stats, $"{prodModule.FactoryType.name} Points", prodModule.Points);
            }

            if (item.TryGetComponent(out Inventory inventory)) {
                AddStat(writer, wikiPage.stats, "Inventory Size", inventory.Capacity);
            }

            AmmoDefinition[] ammo = null;

            switch (item) {
                case AmmoDefinition ammoDef:
                    var ammoStats = ammoDef.AmmoStats;
                    AddAmmoStats(writer, wikiPage, ammoStats);

                    writer.WriteLine();
                    AddStat(writer, "Aim Accuracy", ammoStats.AimAccuracy);
                    AddStat(writer, "Hip Accuracy", ammoStats.AimAccuracy);
                    AddStat(writer, "Recoil", ammoStats.Recoil);
                    break;
                case ToolItemDefinition toolItemDef:
                    if (toolItemDef.TryGetComponent(out WeaponReloaderAmmo reloader)) {
                        AddStat(writer, wikiPage.stats, "Ammo Capacity", reloader.AmmoCapacity);
                        AddStat(writer, wikiPage.stats, "Reload Cooldown", reloader.ReloadCooldown);
                        AddStat(writer, wikiPage.stats, "Reload Duration", reloader.ReloadDuration);
                        ammo = reloader.Ammunition;
                    }
                    if (toolItemDef.TryGetComponent(out WeaponReloaderNoAmmo reloaderNoAmmo)) {
                        AddAmmoStats(writer, wikiPage, reloaderNoAmmo.LoadedAmmo);
                    }

                    var components = (from component in toolItemDef.Prefab.GetComponents<Component>()
                                      select component.GetType().ToString()).Distinct();
                    Debug.Log("---- Components: " + string.Join(", ", components));
                    break;
            }

            if (ammo != null && ammo.Length > 0) {
                writer.WriteLine();
                writer.WriteLine("==== Ammo Types ====");
                foreach (var ammoDef in ammo) {
                    var name          = Plugin.GetName(ammoDef.name);
                    var localizedName = ammoDef.GetLocalizedName();
                    writer.WriteLine($"  * [[{ITEMS_NAMESPACE}:{name}|{localizedName}]]");
                }
            }

            if (item.Prefabs?.Length > 0) {
                var components = (from component in item.Prefabs[0].GetComponents<Component>()
                                  select component.GetType().ToString()).Distinct();
                Debug.Log("---- Components: " + string.Join(", ", components));
            }
        }

        private static void AddAmmoStats(TextWriter writer, WikiPage wikiPage, AmmoStats ammoStats) {
            AddStat(writer, wikiPage.stats, "Damage", ammoStats.Damage);
            AddStat(writer, wikiPage.stats, "Damage At Range Multiplier", ammoStats.DamageAtRangeMult);
            AddStat(writer, null, "Damage Type", ammoStats.DamageType);
            AddStat(writer, wikiPage.stats, "Effective Range", ammoStats.EffectiveRange);
            AddStat(writer, wikiPage.stats, "Gravity Factor", ammoStats.GravityFactor);
            AddStat(writer, wikiPage.stats, "Muzzle Velocity", ammoStats.MuzzleVelocity);
            AddStat(writer, null, "Noise", ammoStats.Noise);
            AddStat(writer, wikiPage.stats, "Projectile Count", ammoStats.ProjectileCount);
            AddStat(writer, wikiPage.stats, "Range", ammoStats.Range);
            AddStat(writer, wikiPage.stats, "Rate Of Fire", ammoStats.RateOfFire);
            AddStat(writer, wikiPage.stats, "Spread", ammoStats.RateOfFire);
        }

        private static void AddStatsFromPropertySet(TextWriter writer, IDictionary<string, string> statDict, PropertySet properties) {
            foreach (var stat in properties.Items) {
                var statName  = stat.Property.Name;
                var statValue = stat.GetStatValue().ToString();
                if (statName == "Energy" && float.TryParse(statValue, out var v) && v == 0) continue;
                AddStat(writer, statDict, statName, statValue);
            }
        }

        private static void AddStat<T>(TextWriter writer, IDictionary<string, string> statDict, string statName, T stat) {
            var value = stat.ToString();
            writer.WriteLine($"| {statName} | {value} |");
            if (statDict != null) {
                statDict[statName] = value;
            }
        }

        private static void AddStat(TextWriter writer, string statName, AccuracyData obj) {
            writer.WriteLine($"| {statName} | Bloom | {obj.Bloom} | {GetTooltipText(obj, nameof(obj.Bloom))} |");
            writer.WriteLine($"| ::: | Cone | {obj.Cone} | {GetTooltipText(obj, nameof(obj.Cone))} |");
            writer.WriteLine($"| ::: | Cone (Moving) | {obj.ConeMoving} | {GetTooltipText(obj, nameof(obj.ConeMoving))} |");
            writer.WriteLine($"| ::: | Cone (Crouched) | {obj.ConeCrouch} | {GetTooltipText(obj, nameof(obj.ConeCrouch))} |");
            writer.WriteLine($"| ::: | Cone (Crouched, Moving) | {obj.ConeCrouchMoving} | {GetTooltipText(obj, nameof(obj.ConeCrouchMoving))} |");
            writer.WriteLine($"| ::: | Max Cone Angle | {obj.MaxConeAngle} | {GetTooltipText(obj, nameof(obj.MaxConeAngle))} |");
        }

        private static void AddStat(TextWriter writer, string statName, RecoilData obj) {
            writer.WriteLine($"| {statName} | Angle Min/Max | {obj.AngleMinMax} | {GetTooltipText(obj, nameof(obj.AngleMinMax))} |");
            writer.WriteLine($"| ::: | First Shot Multiplier | {obj.FirstShotMultiplier} | {GetTooltipText(obj, nameof(obj.FirstShotMultiplier))} |");
            writer.WriteLine($"| ::: | Horizontal Min/Max | {obj.HorizontalMinMax} | {GetTooltipText(obj, nameof(obj.HorizontalMinMax))} |");
            writer.WriteLine($"| ::: | Horizontal Tolerance | {obj.HorizontalTolerance} | {GetTooltipText(obj, nameof(obj.HorizontalTolerance))} |");
            writer.WriteLine($"| ::: | Minimum Burst Length | {obj.MinimumBurstLength} | {GetTooltipText(obj, nameof(obj.MinimumBurstLength))} |");
            writer.WriteLine($"| ::: | Vertical | {obj.Vertical} | {GetTooltipText(obj, nameof(obj.Vertical))} |");
        }

        private static string GetTooltipText(object obj, string statName) {
            try {
                if (obj.GetType().GetField(statName)?.GetCustomAttribute(typeof(TooltipAttribute)) is TooltipAttribute tooltip) {
                    return tooltip.tooltip.Replace("\r\n", " ") ?? "";
                }
                return "";
            } catch (Exception) {
                return "";
            }
        }

        private static List<string> DumpRecipes(ICollection<WikiPage> wikiPages) {
            const string path = Plugin.REFERENCE_WIKI_OUTPUT_PATH + RECIPES_NAMESPACE;
            EraseAndCreateDir(path);

            var scrapRecipes = new Dictionary<string, List<Recipe>>();
            var pages        = new List<string>();

            foreach (var recipe in RuntimeAssetDatabase.Get<Recipe>()) {
                var recipeName     = Plugin.GetName(recipe.name);
                var localizedName  = recipe.Output.Item.GetLocalizedName();
                var localizedDesc  = recipe.Output.Item.GetLocalizedDesc().Trim();
                var outputItemName = Plugin.GetName(recipe.Output.Item.name);
                var isScrap        = false;

                if (recipe.Categories.Any(cat => cat.name.StartsWith("Scrap"))) {
                    if (scrapRecipes.ContainsKey(localizedName)) {
                        scrapRecipes[localizedName].Add(recipe);
                    } else {
                        scrapRecipes[localizedName] = new List<Recipe> {recipe};
                    }
                    isScrap = true;
                }

                using (var writer = new StreamWriter($@"{path}\{recipeName.Replace(' ', '_')}.txt", false, Encoding.UTF8)) {
                    var wikiPage = new WikiPage {
                        name        = localizedName,
                        description = localizedDesc,
                        type        = "recipe",
                        path        = $"{RECIPES_NAMESPACE}:{recipeName}",
                        imagePath   = $"{ITEMS_NAMESPACE}:{outputItemName}.png"
                    };

                    if (recipeName.Contains("Worktable") && recipeName != "WorktableRecipe") {
                        if (recipeName.Contains("5x")) {
                            wikiPage.name += " (Worktable, x5)";
                        } else {
                            wikiPage.name += " (Worktable)";
                        }
                    }

                    writer.WriteLine($"{{{{ {ITEMS_NAMESPACE}:{outputItemName}.png?200}}}}");
                    writer.WriteLine($"====== {localizedName} Recipe ====");
                    writer.WriteLine($"| Internal name | {recipeName} |");
                    writer.WriteLine($"| AssetId | {recipe.AssetId} |");
                    writer.WriteLine($"| Output | [[{ITEMS_NAMESPACE}:{outputItemName}|{localizedName}]] |");

                    writer.WriteLine();
                    writer.WriteLine("==== Description ====");
                    writer.WriteLine(localizedDesc);

                    writer.WriteLine();
                    writer.WriteLine("==== Required Schematics ====");

                    foreach (var requirement in recipe.RequiredUpgrades) {
                        var itemName          = Plugin.GetName(requirement.name);
                        var itemLocalizedName = Plugin.GetName(requirement.GetLocalizedName());
                        writer.WriteLine($"  * [[{ITEMS_NAMESPACE}:{itemName}|{itemLocalizedName}]]");
                        wikiPage.requiredUpgrades.Add(itemLocalizedName);
                    }

                    writer.WriteLine();
                    writer.WriteLine("==== Required Items [Quantity] ====");

                    foreach (var input in recipe.Inputs) {
                        var itemName          = Plugin.GetName(input.Item.name);
                        var itemLocalizedName = Plugin.GetName(input.Item.GetLocalizedName());
                        writer.WriteLine($"  * [[{ITEMS_NAMESPACE}:{itemName}|{itemLocalizedName}]] [{input.Amount}]");
                        wikiPage.requiredItems.Add(itemLocalizedName);
                    }

                    writer.WriteLine();
                    writer.WriteLine("==== Can Be Crafted In ====");

                    foreach (var category in recipe.Categories) {
                        var categoryName = category.name;
                        if (!crafters.ContainsKey(categoryName)) continue;
                        foreach (var item in crafters[categoryName]) {
                            var itemName          = Plugin.GetName(item.name);
                            var itemLocalizedName = Plugin.GetName(item.GetLocalizedName());
                            writer.WriteLine($"  * [[{ITEMS_NAMESPACE}:{itemName}|{itemLocalizedName}]]");
                            wikiPage.craftedIn.Add(itemLocalizedName);
                        }
                    }

                    writer.WriteLine();
                    writer.WriteLine("==== Crafting Categories ====");

                    foreach (var category in recipe.Categories) {
                        var name = Plugin.GetName(category.name);
                        writer.WriteLine($"  * {name}");
                    }

                    writer.WriteLine();
                    writer.Write(Plugin.GetFooter());

                    pages.Add($"  * [[{RECIPES_NAMESPACE}:{recipeName}|{localizedName}]] ({recipeName})");

                    // Don't track individual scrap recipes in the json.
                    if (!isScrap) {
                        wikiPages.Add(wikiPage);
                    }
                }
            }

            // Scrap meta recipes.
            foreach (var localizedName in scrapRecipes.Keys) {
                var recipes        = scrapRecipes[localizedName];
                var metaName       = localizedName + " Scrap Recipes";
                var safeName       = metaName.Replace(' ', '_');
                var outputItem     = recipes[0].Output.Item;
                var outputItemName = Plugin.GetName(outputItem.name);

                using (var writer = new StreamWriter($@"{path}\{safeName}.txt", false, Encoding.UTF8)) {
                    var wikiPage = new WikiPage {
                        name        = metaName,
                        description = outputItem.GetLocalizedDesc(),
                        type        = "recipe",
                        path        = $"{RECIPES_NAMESPACE}:{safeName}",
                        imagePath   = $"{ITEMS_NAMESPACE}:{outputItemName}.png"
                    };

                    writer.WriteLine($"{{{{ {ITEMS_NAMESPACE}:{outputItemName}.png?200}}}}");
                    writer.WriteLine($"====== {metaName} ====");
                    writer.WriteLine($"This is an meta page to link to all the scrap recipes for {localizedName}.\\\\");
                    writer.WriteLine("See the individual recipes for AssetIds and such.\\\\");
                    writer.WriteLine($"Output: [[{ITEMS_NAMESPACE}:{outputItemName}|{localizedName}]]");
                    writer.WriteLine();

                    foreach (var recipe in recipes) {
                        var recipeName = Plugin.GetName(recipe.name);
                        writer.WriteLine($"  * [[{RECIPES_NAMESPACE}:{recipeName}|{localizedName}]] ({recipeName})");
                    }

                    writer.WriteLine();
                    writer.Write(Plugin.GetFooter());

                    wikiPages.Add(wikiPage);
                }
            }

            return pages;
        }

        private static void WriteAllThingPage(List<string> pages, string @namespace, string allWhat) {
            // Not optimized, but IDGAF.
            pages.Sort((s1, s2) => {
                var a = s1.Split('|')[1];
                var b = s2.Split('|')[1];
                return string.Compare(a, b, StringComparison.Ordinal);
            });

            using (var writer = new StreamWriter($@"{Plugin.REFERENCE_WIKI_OUTPUT_PATH}\{@namespace}\All_{allWhat}.txt", false, Encoding.UTF8)) {
                writer.WriteLine($"====== All {allWhat} ====");

                foreach (var page in pages) {
                    writer.WriteLine(page);
                }

                writer.WriteLine();
                writer.Write(Plugin.GetFooter());
            }
        }

        private static void WriteTexture(Sprite sprite, string path, string itemSafeName) {
            var tex = sprite.texture;
            if (!tex.isReadable) {
                var target = new RenderTexture(tex.width, tex.height, 0, RenderTextureFormat.ARGB32);
                Graphics.Blit(tex, target);
                var copy = new Texture2D(target.width, target.height, target.graphicsFormat, target.mipmapCount, 0);
                copy.ReadPixels(target);
                tex = copy;
            }
            File.WriteAllBytes($@"{path}\{itemSafeName}.png", tex.EncodeToPNG());
        }

        private static void EraseAndCreateDir(string path) {
            //if (Directory.Exists(path)) Directory.Delete(path, true);
            Directory.CreateDirectory(path);
        }

        [SuppressMessage("ReSharper", "CollectionNeverQueried.Global")]
        [SuppressMessage("ReSharper", "NotAccessedField.Global")]
        public class WikiPage {
            public          string                     name;
            public          string                     type;
            public          string                     path;
            public          string                     imagePath;
            public          string                     description;
            public readonly List<string>               requiredUpgrades = new List<string>();
            public readonly List<string>               requiredItems    = new List<string>();
            public readonly List<string>               craftedIn        = new List<string>();
            public readonly Dictionary<string, string> stats            = new Dictionary<string, string>();
        }
    }
}