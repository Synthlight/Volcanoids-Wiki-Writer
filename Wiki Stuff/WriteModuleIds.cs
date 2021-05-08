using System.IO;
using System.Linq;

namespace Wiki_Writer.Wiki_Stuff {
    public static class WriteModuleIds {
        public static void Go() {
            var msg = Plugin.GetHeader() +
                      "Name | AssetId (GUID)\r\n" +
                      "--- | ---\r\n";
            foreach (var module in GameResources.Instance.Modules.OrderBy(i => i.name)) {
                msg += $"{Plugin.GetName(module.name)} | {module.AssetId}\r\n";
            }

            File.WriteAllText(Plugin.BASE_OUTPUT_PATH + "Module Ids.txt", msg);
        }
    }
}