using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;

namespace Wiki_Writer {
    [UsedImplicitly]
    public static class Extensions {
        public static string GetLocalizedName(this ItemDefinition item) {
            return RuntimeAssetDatabase.Get<ItemDefinition>().First(def => def.AssetId == item.AssetId).NameLocalization;
        }

        public static string GetLocalizedDesc(this ItemDefinition item) {
            return RuntimeAssetDatabase.Get<ItemDefinition>().First(def => def.AssetId == item.AssetId).DescriptionLocalization;
        }

        public static Dictionary<K2, V> GetOrCreate<K1, K2, V>(this Dictionary<K1, Dictionary<K2, V>> dict, K1 key) {
            if (dict.ContainsKey(key)) return dict[key];
            dict[key] = new Dictionary<K2, V>();
            return dict[key];
        }

        public static List<V> GetOrCreate<K, V>(this Dictionary<K, List<V>> dict, K key) {
            if (dict.ContainsKey(key)) return dict[key];
            dict[key] = new List<V>();
            return dict[key];
        }

        public static bool TryGetComponent<T>(this ItemDefinition item, out T component) {
            component = default;
            return item.Prefabs?.Length > 0 && item.Prefabs[0].TryGetComponent<T>(out component);
        }

        public static bool TryGetComponent<T>(this ToolItemDefinition item, out T component) {
            component = default;
            return item.Prefab?.TryGetComponent(out component) == true;
        }
    }
}