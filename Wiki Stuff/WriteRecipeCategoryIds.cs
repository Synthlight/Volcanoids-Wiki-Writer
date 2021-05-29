using System.IO;
using System.Linq;
using System.Text;
using Base_Mod.Models;
using JetBrains.Annotations;

namespace Wiki_Writer.Wiki_Stuff {
    public static class WriteRecipeCategoryIds {
        [OnIslandSceneLoaded]
        [UsedImplicitly]
        public static void Go() {
            var msg = new StringBuilder(Plugin.GetHeader());

            var categoryNameList = (from recipe in GameResources.Instance.Recipes
                                    where recipe.Categories != null && recipe.Categories.Length > 0
                                    from category in recipe.Categories
                                    where !string.IsNullOrEmpty(category.name)
                                    orderby category.name
                                    select category.name).Distinct();

            foreach (var name in categoryNameList) {
                msg.Append($" - {Plugin.GetName(name)}\r\n");
            }

            File.WriteAllText(Plugin.BASE_OUTPUT_PATH + "Recipe Category Ids.txt", msg.ToString());
        }
    }
}