using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace Wiki_Writer.Reference_Wiki {
    [SuppressMessage("ReSharper", "CollectionNeverQueried.Global")]
    [SuppressMessage("ReSharper", "NotAccessedField.Global")]
    [SuppressMessage("ReSharper", "UnusedMember.Global")]
    public class WikiPage {
        public          string                     guid;
        public          string                     name;
        public          string                     type;
        public          string                     path;
        public          string                     imagePath;
        public          string                     description;
        public readonly List<string>               requiredUpgrades = new List<string>();
        public readonly List<string>               requiredItems    = new List<string>();
        public readonly List<string>               craftedIn        = new List<string>();
        public readonly Dictionary<string, string> stats            = new Dictionary<string, string>();
    }
}