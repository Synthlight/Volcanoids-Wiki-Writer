using System.IO;
using System.Linq;

namespace Wiki_Writer.Wiki_Stuff {
    public static class WriteRecipeProductionTimes {
        public static void Go() {
            var msg = Plugin.GetHeader() +
                      "Name | Production Time (s)\r\n" +
                      "--- | ---\r\n";
            foreach (var recipe in GameResources.Instance.Recipes.OrderBy(i => i.name)) {
                msg += $"{Plugin.GetName(recipe.name)} | {recipe.ProductionTime}\r\n";
            }

            File.WriteAllText(Plugin.BASE_OUTPUT_PATH + "Recipe Production Times.txt", msg);
        }
    }
}