using System.IO;
using System.Linq;
using Base_Mod.Models;
using JetBrains.Annotations;

namespace Wiki_Writer.Wiki_Stuff;

public static class WriteRecipeProductionTimes {
    [OnIslandSceneLoaded]
    [UsedImplicitly]
    public static void Go() {
        var msg = Plugin.GetHeader() +
                  "Name | Production Time (s)\r\n" +
                  "--- | ---\r\n";
        // ReSharper disable once LoopCanBeConvertedToQuery
        foreach (var recipe in RuntimeAssetDatabase.Get<Recipe>().OrderBy(i => i.name)) {
            msg += $"{recipe.GetName()} | {recipe.ProductionTime}\r\n";
        }

        File.WriteAllText(Plugin.BASE_OUTPUT_PATH + "Recipe Production Times.txt", msg);
    }
}