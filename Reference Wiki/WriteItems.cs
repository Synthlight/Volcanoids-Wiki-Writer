using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEngine;

namespace Wiki_Writer.Reference_Wiki {
    public static class WriteItems {
        public static List<string> Write(ICollection<WikiPage> wikiPages) {
            const string path = Plugin.REFERENCE_WIKI_OUTPUT_PATH + Plugin.ITEMS_NAMESPACE;
            WriteAll.EraseAndCreateDir(path);
            const string imgPath = Plugin.REFERENCE_WIKI_OUTPUT_PATH + @"\media\items";
            WriteAll.EraseAndCreateDir(imgPath);

            var pages = new List<string>();

            foreach (var item in RuntimeAssetDatabase.Get<ItemDefinition>()) {
                var localizedName = item.GetLocalizedName();
                var localizedDesc = item.GetLocalizedDesc();

                // Currently only happens for melee weapon ammo.
                if (string.IsNullOrEmpty(localizedName)) continue;

                WriteTexture(item.Icon, imgPath, item.GetSafeName());

                using (var writer = new StreamWriter($@"{path}\{item.GetSafeName()}.txt", false, Encoding.UTF8)) {
                    var wikiPage = new WikiPage {
                        name        = localizedName,
                        description = localizedDesc,
                        type        = "item",
                        path        = item.GetWikiPath(),
                        imagePath   = $"{item.GetWikiPath()}.png"
                    };

                    Debug.Log($"Writing item: {item.GetName()}");

                    writer.WriteLine($"{{{{ {item.GetWikiPath()}.png?200}}}}"); // Leave the space, it aligns the image.
                    writer.WriteLine($"====== {localizedName} ====");
                    writer.WriteLine($"| Internal name | {item.GetName()} |");
                    writer.WriteLine($"| AssetId | {item.AssetId} |");
                    writer.WriteLine($"| Type | {item.GetType()} |");

                    writer.WriteLine();
                    writer.WriteLine("==== Description ====");
                    writer.WriteLine(localizedDesc);

                    WriteStats.Write(writer, item, wikiPage);

                    writer.WriteLine();
                    writer.WriteLine("==== Recipes Outputting This Item ====");

                    foreach (var recipe in RuntimeAssetDatabase.Get<Recipe>().Where(recipe => recipe.Output.Item.name == item.name)) {
                        writer.WriteLine($"  * {recipe.CreateWikiLink(true)}");
                    }

                    writer.WriteLine();
                    writer.WriteLine("==== Recipes Using This Item As Input ====");

                    foreach (var recipe in from recipe in RuntimeAssetDatabase.Get<Recipe>()
                                           from input in recipe.Inputs
                                           where input.Item.name == item.name
                                           select recipe) {
                        writer.WriteLine($"  * {recipe.CreateWikiLink(true)}");
                    }

                    writer.WriteLine();
                    writer.WriteLine("==== Recipes Requiring This Item ====");

                    foreach (var recipe in from recipe in RuntimeAssetDatabase.Get<Recipe>()
                                           from requirement in recipe.RequiredUpgrades
                                           where requirement.name == item.name
                                           select recipe) {
                        writer.WriteLine($"  * {recipe.CreateWikiLink(true)}");
                    }

                    writer.WriteLine();
                    writer.Write(Plugin.GetFooter());

                    pages.Add($"  * {item.CreateWikiLink(false)}");
                    wikiPages.Add(wikiPage);
                }
            }

            return pages;
        }

        private static void WriteTexture(Sprite sprite, string path, string itemSafeName) {
            var tex = sprite.texture;
            if (!tex.isReadable) {
                var target = new RenderTexture(tex.width, tex.height, 0, RenderTextureFormat.ARGB32);
                Graphics.Blit(tex, target);
                var copy = new Texture2D(target.width, target.height, target.graphicsFormat, target.mipmapCount, 0);
                copy.ReadPixels(target);
                tex = copy;
            }
            File.WriteAllBytes($@"{path}\{itemSafeName}.png", tex.EncodeToPNG());
        }
    }
}