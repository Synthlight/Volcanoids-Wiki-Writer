using System.IO;
using System.Linq;
using Base_Mod.Models;
using JetBrains.Annotations;

namespace Wiki_Writer.Wiki_Stuff {
    public static class WriteRecipeIds {
        [OnIslandSceneLoaded]
        [UsedImplicitly]
        public static void Go() {
            var msg = Plugin.GetHeader() +
                      "Name | Info\r\n" +
                      "--- | ---\r\n";
            foreach (var recipe in RuntimeAssetDatabase.Get<Recipe>().OrderBy(i => i.name)) {
                var requirements = GetRequirements(recipe);
                msg += $"{Plugin.GetName(recipe.name)} | <ul>" +
                       $"<li>AssetId: {recipe.AssetId}</li>" +
                       "<li>Output Item:" +
                       "<ul>" +
                       $"<li>Name: {Plugin.GetName(recipe.Output.Item.name)}</li>" +
                       $"<li>AssetId: {recipe.Output.Item.AssetId}</li>" +
                       "</ul>" +
                       "</li>";

                if (recipe.RequiredUpgrades?.Length > 0) {
                    msg += $"<li>Requirements: {requirements}</li>";
                }

                msg += "</ul>\r\n";
            }

            File.WriteAllText(Plugin.BASE_OUTPUT_PATH + "Recipe Ids.txt", msg);
        }

        private static string GetRequirements(Recipe recipe) {
            return string.Join(", ", from input in recipe.RequiredUpgrades
                                     select Plugin.GetName(input.name));
        }
    }
}