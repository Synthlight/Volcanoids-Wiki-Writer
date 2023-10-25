using System;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;

namespace Wiki_Writer {
    [UsedImplicitly]
    public static class Extensions {
        public static string GetLocalizedName(this ItemDefinition item) {
            return RuntimeAssetDatabase.Get<ItemDefinition>().First(def => def.AssetId == item.AssetId).NameLocalization.Text.Trim();
        }

        public static string GetLocalizedName(this LandingSite item) {
            return item.NameLocalization.Text.Trim();
        }

        public static string GetLocalizedDesc(this ItemDefinition item) {
            return RuntimeAssetDatabase.Get<ItemDefinition>().First(def => def.AssetId == item.AssetId).DescriptionLocalization.Text.Trim();
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

        public static string GetName<T>(this T item) where T : UnityEngine.Object {
            var name             = item.name.Trim();
            if (name == "") name = Plugin.NO_NAME_NAME;
            return name.Trim();
        }

        public static string GetSafeName<T>(this T item) where T : UnityEngine.Object {
            return item.GetName().Replace(' ', '_').ToLower();
        }

        public static string GetSafeName(this string item) {
            return item.Replace(' ', '_').ToLower();
        }

        public static string CreateWikiLink<T>(this T item, bool includeNameInParens) where T : ItemDefinition {
            var name                          = item.GetName();
            var safeName                      = name.Replace(' ', '_');
            var localizedName                 = item.GetLocalizedName();
            var wikiLink                      = $"[[{Plugin.ITEMS_NAMESPACE}:{safeName}|{localizedName}]]";
            if (includeNameInParens) wikiLink += $" ({name})";
            return wikiLink;
        }

        public static string CreateWikiLink(this Recipe recipe, bool includeNameInParens) {
            var name                          = recipe.GetName();
            var safeName                      = name.Replace(' ', '_');
            var localizedName                 = recipe.Output.Item.GetLocalizedName();
            var wikiLink                      = $"[[{Plugin.RECIPES_NAMESPACE}:{safeName}|{localizedName} Recipe]]";
            if (includeNameInParens) wikiLink += $" ({name})";
            return wikiLink;
        }

        public static string GetWikiPath<T>(this T definition) where T : Definition {
            string @namespace;
            if (definition.GetType().Is(typeof(ItemDefinition))) @namespace = Plugin.ITEMS_NAMESPACE;
            else if (definition.GetType().Is(typeof(Recipe))) @namespace    = Plugin.RECIPES_NAMESPACE;
            else throw new Exception($"No known namespace for: {definition.GetType()}");
            return $"{@namespace}:{definition.GetSafeName()}";
        }

        public static bool Is(this Type source, params Type[] types) {
            return types.Any(type => type.IsAssignableFrom(source));
        }
    }
}