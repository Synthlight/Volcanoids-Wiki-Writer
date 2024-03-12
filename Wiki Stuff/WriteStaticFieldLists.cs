using System.IO;
using System.Linq;
using Base_Mod.Models;
using JetBrains.Annotations;

namespace Wiki_Writer.Wiki_Stuff;

public static class WriteStaticFieldLists {
    [OnIslandSceneLoaded]
    [UsedImplicitly]
    public static void Go() {
        var msg = Plugin.GetHeader() +
                  "Table of Contents\r\n" +
                  "---\r\n\r\n" +
                  "- [Item Id Fields](#Item-Id-Fields)\r\n" +
                  "- [Module Id Fields](#Module-Id-Fields)\r\n" +
                  "- [Recipe Id Fields](#Recipe-Id-Fields)\r\n" +
                  "\r\n";

        msg += "Item Id Fields:\r\n" +
               "---\r\n\r\n" +
               "```\r\n";
        foreach (var item in RuntimeAssetDatabase.Get<ItemDefinition>().OrderBy(i => i.name)) {
            var itemName                 = item.name.Trim();
            if (itemName == "") itemName = Plugin.NO_NAME_NAME;

            msg += MakeFieldForNameAndId(itemName, item.AssetId) + "\r\n";
        }
        msg += "```\r\n";

        msg += "Module Id Fields:\r\n" +
               "---\r\n\r\n" +
               "```\r\n";
        foreach (var module in RuntimeAssetDatabase.Get<ModuleItemDefinition>().OrderBy(i => i.name)) {
            var moduleName                   = module.name.Trim();
            if (moduleName == "") moduleName = Plugin.NO_NAME_NAME;

            msg += MakeFieldForNameAndId(moduleName, module.AssetId) + "\r\n";
        }
        msg += "```\r\n";

        msg += "Recipe Id Fields:\r\n" +
               "---\r\n\r\n" +
               "```\r\n";
        foreach (var recipe in RuntimeAssetDatabase.Get<Recipe>().OrderBy(i => i.name)) {
            var recipeName                   = recipe.name.Trim();
            if (recipeName == "") recipeName = Plugin.NO_NAME_NAME;

            msg += MakeFieldForNameAndId(recipeName, recipe.AssetId) + "\r\n";
        }
        msg += "```\r\n";

        File.WriteAllText(Plugin.BASE_OUTPUT_PATH + "GUID Fields (C#).txt", msg);
    }

    private static string MakeFieldForNameAndId(string name, GUID id) {
        return $"private static readonly GUID {name} = GUID.Parse(\"{id}\");";
    }
}