using System.IO;
using System.Linq;
using Base_Mod.Models;
using JetBrains.Annotations;

namespace Wiki_Writer.Wiki_Stuff {
    public static class WriteItemIds {
        [OnIslandSceneLoaded]
        [UsedImplicitly]
        public static void Go() {
            var msg = Plugin.GetHeader() +
                      "Name | AssetId (GUID) | Type\r\n" +
                      "--- | --- | ---\r\n";
            foreach (var item in RuntimeAssetDatabase.Get<ItemDefinition>().OrderBy(i => i.name)) {
                msg += $"{Plugin.GetName(item.name)} | {item.AssetId} | {item.GetType()}\r\n";
            }

            File.WriteAllText(Plugin.BASE_OUTPUT_PATH + "Item Ids.txt", msg);
        }
    }
}