using System.IO;
using System.Linq;
using Base_Mod.Models;
using JetBrains.Annotations;

namespace Wiki_Writer.Wiki_Stuff {
    public static class WriteDecalCategories {
        [OnIslandSceneLoaded]
        [UsedImplicitly]
        public static void Go() {
            var msg = Plugin.GetHeader() +
                      "Name | AssetId (GUID) | Type\r\n" +
                      "--- | --- | ---\r\n";

            foreach (var item in (from dResource in RuntimeAssetDatabase.Get<DecalResource>()
                                  select dResource.Category).Distinct()) {
                msg += $"{item.GetName()} | {item.AssetId} | {item.GetType()}\r\n";
            }

            File.WriteAllText(Plugin.BASE_OUTPUT_PATH + "Decal Category Ids.txt", msg);
        }
    }
}