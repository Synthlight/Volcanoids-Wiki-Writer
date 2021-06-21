using System.IO;
using System.Linq;
using System.Text;
using Base_Mod.Models;
using JetBrains.Annotations;

namespace Wiki_Writer.Wiki_Stuff {
    public static class WriteItemCategoryIds {
        [OnIslandSceneLoaded]
        [UsedImplicitly]
        public static void Go() {
            var msg = new StringBuilder(Plugin.GetHeader())
                      .AppendLine("Name | AssetId (GUID)")
                      .AppendLine("--- | ---");

            var categoryList = from category in GameResources.Instance.ItemCategoryLookup.Keys
                               orderby category.name
                               select category;

            foreach (var category in categoryList) {
                msg.AppendLine($"{Plugin.GetName(category.name)} | {category.AssetId}");
            }

            File.WriteAllText(Plugin.BASE_OUTPUT_PATH + "Item Category Ids.txt", msg.ToString());
        }
    }
}