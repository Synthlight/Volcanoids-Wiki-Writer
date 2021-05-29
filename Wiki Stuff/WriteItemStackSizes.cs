﻿using System.IO;
using System.Linq;
using Base_Mod.Models;
using JetBrains.Annotations;

namespace Wiki_Writer.Wiki_Stuff {
    public static class WriteItemStackSizes {
        [OnIslandSceneLoaded]
        [UsedImplicitly]
        public static void Go() {
            var msg = Plugin.GetHeader() +
                      "Name | Stack Size\r\n" +
                      "--- | ---\r\n";
            foreach (var item in GameResources.Instance.Items.OrderBy(i => i.name)) {
                msg += $"{Plugin.GetName(item.name)} | {item.MaxStack}\r\n";
            }

            File.WriteAllText(Plugin.BASE_OUTPUT_PATH + "Item Stack Sizes.txt", msg);
        }
    }
}