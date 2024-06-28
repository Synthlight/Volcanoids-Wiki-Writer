using System.IO;
using System.Linq;
using Base_Mod.Models;
using JetBrains.Annotations;

namespace Wiki_Writer.Wiki_Stuff;

public static class WriteModuleIds {
    [OnIslandSceneLoaded]
    [UsedImplicitly]
    public static void Go() {
        var msg = Plugin.GetHeader() +
                  "Name | AssetId (GUID)\r\n" +
                  "--- | ---\r\n";
        // ReSharper disable once LoopCanBeConvertedToQuery
        foreach (var module in RuntimeAssetDatabase.Get<ItemDefinition>().OrderBy(i => i.name)) {
            msg += $"{module.GetName()} | {module.AssetId}\r\n";
        }

        File.WriteAllText(Plugin.BASE_OUTPUT_PATH + "Module Ids.txt", msg);
    }
}