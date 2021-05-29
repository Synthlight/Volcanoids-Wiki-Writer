using System.IO;
using System.Linq;
using System.Text;
using Base_Mod.Models;
using JetBrains.Annotations;

namespace Wiki_Writer.Wiki_Stuff {
    public static class WriteRecipeCategories {
        [OnIslandSceneLoaded]
        [UsedImplicitly]
        public static void Go() {
            var msg = new StringBuilder(Plugin.GetHeader())
                      .Append("Name | Category (s)\r\n")
                      .Append("--- | ---\r\n");

            foreach (var recipe in GameResources.Instance.Recipes.OrderBy(i => i.name)) {
                var categoryNames    = recipe.Categories.Select(category => category.name);
                var categoryListText = categoryNames == null || categoryNames.Length == 0 ? "{null}" : categoryNames.JoinString();

                msg.Append($"{Plugin.GetName(recipe.name)} | {categoryListText}\r\n");
            }

            File.WriteAllText(Plugin.BASE_OUTPUT_PATH + "Recipe Categories.txt", msg.ToString());
        }
    }
}