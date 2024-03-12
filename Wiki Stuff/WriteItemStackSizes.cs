using System.IO;
using System.Linq;
using Base_Mod.Models;
using JetBrains.Annotations;

namespace Wiki_Writer.Wiki_Stuff;

public static class WriteItemStackSizes {
    [OnIslandSceneLoaded]
    [UsedImplicitly]
    public static void Go() {
        var msg = Plugin.GetHeader() +
                  "Name | Stack Size\r\n" +
                  "--- | ---\r\n";
        // ReSharper disable once LoopCanBeConvertedToQuery
        foreach (var item in RuntimeAssetDatabase.Get<ItemDefinition>().OrderBy(i => i.name)) {
            msg += $"{item.GetName()} | {item.MaxStack}\r\n";
        }

        File.WriteAllText(Plugin.BASE_OUTPUT_PATH + "Item Stack Sizes.txt", msg);
    }
}