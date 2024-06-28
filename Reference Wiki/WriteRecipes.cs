using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace Wiki_Writer.Reference_Wiki;

public static class WriteRecipes {
    public static List<string> Write(ICollection<WikiPage> wikiPages, Dictionary<string, List<ItemDefinition>> crafters) {
        const string path = Plugin.REFERENCE_WIKI_OUTPUT_PATH + Plugin.RECIPES_NAMESPACE;
        WriteAll.EraseAndCreateDir(path);

        var scrapRecipes = new Dictionary<string, List<Recipe>>();
        var pages        = new List<string>();

        // Build a list of what items need to be unlocked.
        var unlockGroupByRecipe = new Dictionary<Recipe, List<ItemDefinition>>();
        foreach (var unlockGroup in RuntimeAssetDatabase.Get<RecipeUnlockGroup>()) {
            foreach (var recipe in unlockGroup.Recipes) {
                if (!unlockGroupByRecipe.ContainsKey(recipe)) unlockGroupByRecipe[recipe] = [];

                var itemList = unlockGroupByRecipe[recipe];

                foreach (var item in unlockGroup.Items) {
                    if (itemList.Contains(item)) continue;
                    itemList.Add(item);
                }
            }
        }

        foreach (var recipe in RuntimeAssetDatabase.Get<Recipe>()) {
            var recipeName     = recipe.GetName();
            var localizedName  = recipe.Output.Item.GetLocalizedName();
            var localizedDesc  = recipe.Output.Item.GetLocalizedDesc();
            var outputItemPath = recipe.Output.Item.GetWikiPath();
            var isScrap        = false;

            if (recipe.Categories.Any(cat => cat.name.StartsWith("Scrap"))) {
                if (scrapRecipes.TryGetValue(localizedName, out var scrapRecipe)) {
                    scrapRecipe.Add(recipe);
                } else {
                    scrapRecipes[localizedName] = [recipe];
                }
                isScrap = true;
            }

            using var writer = new StreamWriter($@"{path}\{recipe.GetSafeName()}.txt", false, Encoding.UTF8);

            var wikiPage = new WikiPage {
                guid        = recipe.AssetId.ToString(),
                name        = localizedName,
                description = localizedDesc,
                type        = "recipe",
                path        = recipe.GetWikiPath(),
                imagePath   = $"{outputItemPath}.png"
            };

            if (recipeName.Contains("Worktable") && recipeName != "WorktableRecipe") {
                if (recipeName.Contains("5x")) {
                    wikiPage.name += " (Worktable, x5)";
                } else {
                    wikiPage.name += " (Worktable)";
                }
            }

            writer.WriteLine($"{{{{ {outputItemPath}.png?200}}}}");
            writer.WriteLine($"====== {localizedName} Recipe ====");
            writer.WriteLine($"| Internal name | {recipeName} |");
            writer.WriteLine($"| AssetId | {recipe.AssetId} |");
            writer.WriteLine($"| Output | {recipe.Output.Item.CreateWikiLink(false)} |");

            writer.WriteLine();
            writer.WriteLine("==== Description ====");
            writer.WriteLine(localizedDesc);

            writer.WriteLine();
            writer.WriteLine("==== Items That Need to Be Found to Unlock ====");
            writer.WriteLine(@"Finding any listed item triggers the unlock.\\");
            writer.WriteLine("Some of these might be schematics you can research instead.");

            if (unlockGroupByRecipe.TryGetValue(recipe, out var unlockItems)) {
                foreach (var item in unlockItems) {
                    writer.WriteLine($"  * {item.CreateWikiLink(false)}");
                    wikiPage.requiredUnlockItems.Add(item.GetLocalizedName());
                }
            }

            writer.WriteLine();
            writer.WriteLine("==== Required Schematics ====");

            foreach (var requirement in recipe.RequiredUpgrades) {
                writer.WriteLine($"  * {requirement.CreateWikiLink(false)}");
                wikiPage.requiredUpgrades.Add(requirement.GetLocalizedName());
            }

            writer.WriteLine();
            writer.WriteLine("==== Required Items [Quantity] ====");

            foreach (var input in recipe.Inputs) {
                writer.WriteLine($"  * {input.Item.CreateWikiLink(false)} [{input.Amount}]");
                wikiPage.requiredItems.Add(input.Item.GetLocalizedName());
            }

            writer.WriteLine();
            writer.WriteLine("==== Can Be Crafted In ====");

            foreach (var category in recipe.Categories) {
                var categoryName = category.name;
                if (!crafters.ContainsKey(categoryName)) continue;
                foreach (var item in crafters[categoryName]) {
                    writer.WriteLine($"  * {item.CreateWikiLink(false)}");
                    wikiPage.craftedIn.Add(item.GetLocalizedName());
                }
            }

            writer.WriteLine();
            writer.WriteLine("==== Crafting Categories ====");

            foreach (var category in recipe.Categories) {
                writer.WriteLine($"  * {category.GetName()}");
            }

            writer.WriteLine();
            writer.Write(Plugin.GetFooter());

            pages.Add($"  * {recipe.CreateWikiLink(true)}");

            // Don't track individual scrap recipes in the json.
            if (!isScrap) {
                wikiPages.Add(wikiPage);
            }
        }

        // Scrap meta recipes.
        foreach (var localizedName in scrapRecipes.Keys) {
            var recipes        = scrapRecipes[localizedName];
            var metaName       = localizedName + " Scrap Recipes";
            var outputItem     = recipes[0].Output.Item;
            var outputItemPath = outputItem.GetWikiPath();

            using var writer = new StreamWriter($@"{path}\{metaName.Replace(' ', '_').ToLower()}.txt", false, Encoding.UTF8);

            var wikiPage = new WikiPage {
                name        = metaName,
                description = outputItem.GetLocalizedDesc(),
                type        = "recipe",
                path        = recipes[0].GetWikiPath(),
                imagePath   = $"{outputItemPath}.png"
            };

            writer.WriteLine($"{{{{ {outputItemPath}.png?200}}}}");
            writer.WriteLine($"====== {metaName} ====");
            writer.WriteLine($@"This is an meta page to link to all the scrap recipes for {localizedName}.\\");
            writer.WriteLine(@"See the individual recipes for AssetIds and such.\\");
            writer.WriteLine($"Output: {outputItem.CreateWikiLink(false)}");
            writer.WriteLine();

            foreach (var recipe in recipes) {
                writer.WriteLine($"  * {recipe.CreateWikiLink(true)}");
            }

            writer.WriteLine();
            writer.Write(Plugin.GetFooter());

            wikiPages.Add(wikiPage);
        }

        return pages;
    }
}