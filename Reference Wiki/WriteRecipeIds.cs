using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Text;
using Base_Mod.Models;
using JetBrains.Annotations;
using Newtonsoft.Json;

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
            //const string imgPath = path + "\\media";
            //EraseAndCreateDir(imgPath);

            var pages = new List<string>();

            foreach (var item in RuntimeAssetDatabase.Get<ItemDefinition>()) {
                var itemName      = Plugin.GetName(item.name);
                var itemSafeName  = itemName.Replace(' ', '_');
                var localizedName = item.GetLocalizedName();
                var localizedDesc = item.GetLocalizedDesc();

                // Currently only happens for melee weapon ammo.
                if (string.IsNullOrEmpty(localizedName)) continue;

                //var png = item.Icon.texture.EncodeToPNG();
                //File.WriteAllBytes($@"{path}\{itemSafeName}.txt", png);

                using (var writer = new StreamWriter($@"{path}\{itemSafeName}.txt", false, Encoding.UTF8)) {
                    var wikiPage = new WikiPage {
                        name        = localizedName,
                        description = localizedDesc,
                        type        = "item",
                        path        = $"{ITEMS_NAMESPACE}:{itemName}"
                    };

                    writer.WriteLine($"====== {localizedName} ====");
                    writer.WriteLine($"| Internal name | {itemName} |");
                    writer.WriteLine($"| AssetId | {item.AssetId} |");
                    writer.WriteLine($"| Type | {item.GetType()} |");

                    writer.WriteLine();
                    writer.WriteLine("==== Description ====");
                    writer.WriteLine(localizedDesc);
                    writer.WriteLine();

                    // TODO: List various item parameters, its type, etc.

                    writer.WriteLine();
                    writer.WriteLine("==== Recipes Outputting This Item ====");
                    writer.WriteLine();

                    foreach (var recipe in RuntimeAssetDatabase.Get<Recipe>()
                                                               .Where(recipe => recipe.Output.Item.name == item.name)) {
                        var recipeName          = Plugin.GetName(recipe.name);
                        var recipeLocalizedName = recipe.Output.Item.GetLocalizedName();
                        writer.WriteLine($"  * [[{RECIPES_NAMESPACE}:{recipeName}|{recipeLocalizedName} Recipe]] ({recipeName})");
                    }

                    writer.WriteLine();
                    writer.WriteLine("==== Recipes Using This Item As Input ====");
                    writer.WriteLine();

                    foreach (var recipe in from recipe in RuntimeAssetDatabase.Get<Recipe>()
                                           from input in recipe.Inputs
                                           where input.Item.name == item.name
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

        private static List<string> DumpRecipes(ICollection<WikiPage> wikiPages) {
            const string path = Plugin.REFERENCE_WIKI_OUTPUT_PATH + RECIPES_NAMESPACE;
            EraseAndCreateDir(path);

            var scrapRecipes = new Dictionary<string, List<Recipe>>();
            var pages        = new List<string>();

            foreach (var recipe in RuntimeAssetDatabase.Get<Recipe>()) {
                var recipeName    = Plugin.GetName(recipe.name);
                var localizedName = recipe.Output.Item.GetLocalizedName();
                var localizedDesc = recipe.Output.Item.GetLocalizedDesc();
                var isScrap       = false;

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
                        path        = $"{RECIPES_NAMESPACE}:{recipeName}"
                    };

                    writer.WriteLine($"====== {localizedName} Recipe ====");
                    writer.WriteLine($"| Internal name | {recipeName} |");
                    writer.WriteLine($"| AssetId | {recipe.AssetId} |");
                    writer.WriteLine($"| Output | [[{ITEMS_NAMESPACE}:{Plugin.GetName(recipe.Output.Item.name)}|{localizedName}]] |");

                    writer.WriteLine();
                    writer.WriteLine("==== Description ====");
                    writer.WriteLine(localizedDesc);
                    writer.WriteLine();

                    writer.WriteLine();
                    writer.WriteLine("==== Required Schematics ====");
                    writer.WriteLine();

                    foreach (var requirement in recipe.RequiredUpgrades) {
                        var itemName          = Plugin.GetName(requirement.name);
                        var itemLocalizedName = Plugin.GetName(requirement.GetLocalizedName());
                        writer.WriteLine($"  * [[{ITEMS_NAMESPACE}:{itemName}|{itemLocalizedName}]]");
                        wikiPage.requiredUpgrades.Add(itemLocalizedName);
                    }

                    writer.WriteLine();
                    writer.WriteLine("==== Required Items [Quantity] ====");
                    writer.WriteLine();

                    foreach (var input in recipe.Inputs) {
                        var itemName          = Plugin.GetName(input.Item.name);
                        var itemLocalizedName = Plugin.GetName(input.Item.GetLocalizedName());
                        writer.WriteLine($"  * [[{ITEMS_NAMESPACE}:{itemName}|{itemLocalizedName}]] [{input.Amount}]");
                        wikiPage.requiredItems.Add(itemLocalizedName);
                    }

                    writer.WriteLine();
                    writer.WriteLine("==== Can Be Crafted In ====");
                    writer.WriteLine();

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
                    writer.WriteLine();

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
                var recipes    = scrapRecipes[localizedName];
                var metaName   = localizedName + " Scrap Recipes";
                var safeName   = metaName.Replace(' ', '_');
                var outputItem = recipes[0].Output.Item;

                using (var writer = new StreamWriter($@"{path}\{safeName}.txt", false, Encoding.UTF8)) {
                    var wikiPage = new WikiPage {
                        name        = metaName,
                        description = outputItem.GetLocalizedDesc(),
                        type        = "recipe",
                        path        = $"{RECIPES_NAMESPACE}:{safeName}"
                    };

                    writer.WriteLine($"====== {metaName} ====");
                    writer.WriteLine($"This is an meta page to link to all the scrap recipes for {localizedName}.\\\\");
                    writer.WriteLine("See the individual recipes for AssetIds and such.\\\\");
                    writer.WriteLine($"Output: [[{ITEMS_NAMESPACE}:{Plugin.GetName(outputItem.name)}|{localizedName}]]");
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

        private static void EraseAndCreateDir(string path) {
            //if (Directory.Exists(path)) Directory.Delete(path, true);
            Directory.CreateDirectory(path);
        }

        [SuppressMessage("ReSharper", "CollectionNeverQueried.Global")]
        [SuppressMessage("ReSharper", "NotAccessedField.Global")]
        public class WikiPage {
            public          string       name;
            public          string       type;
            public          string       path;
            public          string       description;
            public readonly List<string> requiredUpgrades = new List<string>();
            public readonly List<string> requiredItems    = new List<string>();
            public readonly List<string> craftedIn        = new List<string>();
        }
    }
}