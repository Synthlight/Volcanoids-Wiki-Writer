using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Base_Mod.Models;
using JetBrains.Annotations;
using Newtonsoft.Json;

namespace Wiki_Writer.Reference_Wiki {
    public static class WriteAll {
        [OnIslandSceneLoaded]
        [UsedImplicitly]
        public static void Go() {
            var wikiPages = new List<WikiPage>();

            // Build a list of what crafters produce what categories.
            var crafters = new Dictionary<string /*category*/, List<ItemDefinition> /*crafter*/>();
            foreach (var item in RuntimeAssetDatabase.Get<ItemDefinition>()) {
                if (item.Prefabs?.Length > 0 && item.Prefabs[0].TryGetComponent<Producer>(out var producer)) {
                    var categories = producer.Categories.Select(cat => cat.name).ToList();
                    foreach (var category in categories) {
                        if (crafters.ContainsKey(category) && !crafters[category].Contains(item)) {
                            crafters[category].Add(item);
                        } else {
                            crafters[category] = new List<ItemDefinition> {item};
                        }
                    }
                }
            }

            WriteAllThingPage(WriteItems.Write(wikiPages), "items", "Items");
            WriteAllThingPage(WriteRecipes.Write(wikiPages, crafters), "recipes", "Recipes");

            var json = JsonConvert.SerializeObject(wikiPages, Formatting.None, new JsonSerializerSettings {
                NullValueHandling = NullValueHandling.Ignore
            });
            File.WriteAllText($@"{Plugin.REFERENCE_WIKI_OUTPUT_PATH}\pages.json", json);
        }

        private static void WriteAllThingPage(List<string> pages, string @namespace, string allWhat) {
            // Not optimized, but IDGAF.
            pages.Sort((s1, s2) => {
                var a = s1.Split('|')[1];
                var b = s2.Split('|')[1];
                return string.Compare(a, b, StringComparison.Ordinal);
            });

            using (var writer = new StreamWriter($@"{Plugin.REFERENCE_WIKI_OUTPUT_PATH}\{@namespace}\All_{allWhat}.txt", false, Encoding.UTF8)) {
                writer.WriteLine($"====== All {allWhat} ====");

                foreach (var page in pages) {
                    writer.WriteLine(page);
                }

                writer.WriteLine();
                writer.Write(Plugin.GetFooter());
            }
        }

        public static void EraseAndCreateDir(string path) {
            //if (Directory.Exists(path)) Directory.Delete(path, true);
            Directory.CreateDirectory(path);
        }
    }
}