using System.IO;
using System.Linq;
using System.Text;
using Base_Mod.Models;
using JetBrains.Annotations;

namespace Wiki_Writer.Wiki_Stuff {
    public static class WriteAmmoStats {
        [OnIslandSceneLoaded]
        [UsedImplicitly]
        public static void Go() {
            var msg = new StringBuilder(Plugin.GetHeader())
                      .AppendLine("Name | AssetId (GUID) | Damage | Range")
                      .AppendLine("--- | --- | --- | ---");

            var ammoList = from item in RuntimeAssetDatabase.Get<AmmoDefinition>()
                           orderby item.name
                           select item;

            foreach (var ammo in ammoList) {
                var stats = ammo.AmmoStats;
                msg.AppendLine($"{ammo.GetName()} | {ammo.AssetId} | {stats.Damage} | {stats.Range}");
            }

            File.WriteAllText(Plugin.BASE_OUTPUT_PATH + "Ammo Stats.txt", msg.ToString());
        }
    }
}