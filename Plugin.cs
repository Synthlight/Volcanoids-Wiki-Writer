using System.IO;
using Base_Mod;
using JetBrains.Annotations;
using UnityEngine;

namespace Wiki_Writer {
    [UsedImplicitly]
    public class Plugin : BaseGameMod {
        public const string NO_NAME_NAME     = "{no name}";
        public const string BASE_OUTPUT_PATH = @"R:\Games\Volcanoids\Mods\_Wiki\";

        public static string GetHeader() {
            return $"For Volcanoids v{GetVersion()}\n---\n\n";
        }

        public static string GetVersion() {
            return File.ReadAllLines(Application.dataPath + "/../version.txt")[0];
        }

        public static string GetName(string input) {
            var name             = input.Trim();
            if (name == "") name = NO_NAME_NAME;
            return name;
        }
    }
}