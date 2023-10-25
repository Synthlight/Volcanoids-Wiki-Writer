using System.IO;
using Base_Mod;
using JetBrains.Annotations;
using UnityEngine;

namespace Wiki_Writer {
    [UsedImplicitly]
    public class Plugin : BaseGameMod {
        public const string NO_NAME_NAME               = "{no name}";
        public const string BASE_OUTPUT_PATH           = @"R:\Games\Volcanoids\Mods\_Wiki\";
        public const string REFERENCE_WIKI_OUTPUT_PATH = BASE_OUTPUT_PATH + @"_ReferenceWiki\";
        public const string FANDOM_WIKI_OUTPUT_PATH    = BASE_OUTPUT_PATH + @"_FandomWiki\";
        public const string ITEMS_NAMESPACE            = "items";
        public const string MAPS_NAMESPACE             = "maps";
        public const string RECIPES_NAMESPACE          = "recipes";

        public static string GetHeader() {
            return $"For Volcanoids v{GetVersion()}\n---\n\n";
        }

        public static string GetFooter() {
            return $"For Volcanoids v{GetVersion()}";
        }

        public static string GetVersion() {
            return File.ReadAllLines(Application.dataPath + "/../version.txt")[0];
        }
    }
}