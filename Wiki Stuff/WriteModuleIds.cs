﻿using System.IO;
using System.Linq;
using Base_Mod.Models;
using JetBrains.Annotations;

namespace Wiki_Writer.Wiki_Stuff {
    public static class WriteModuleIds {
        [OnIslandSceneLoaded]
        [UsedImplicitly]
        public static void Go() {
            var msg = Plugin.GetHeader() +
                      "Name | AssetId (GUID)\r\n" +
                      "--- | ---\r\n";
            foreach (var module in RuntimeAssetDatabase.Get<ModuleItemDefinition>().OrderBy(i => i.name)) {
                msg += $"{Plugin.GetName(module.name)} | {module.AssetId}\r\n";
            }

            File.WriteAllText(Plugin.BASE_OUTPUT_PATH + "Module Ids.txt", msg);
        }
    }
}