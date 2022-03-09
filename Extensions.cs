using System.Linq;
using JetBrains.Annotations;

namespace Wiki_Writer {
    [UsedImplicitly]
    public static class Extensions {
        public static string GetLocalizedName(this ItemDefinition item) {
            return RuntimeAssetDatabase.Get<ItemDefinition>().First(def => def.AssetId == item.AssetId).NameLocalization;
        }
    }
}