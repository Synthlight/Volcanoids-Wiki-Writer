using System;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Text;
using Base_Mod.Models;
using JetBrains.Annotations;
using UnityEngine;

namespace Wiki_Writer.Fandom_Wiki {
    public static class WriteMapMarkerInfo {
        [OnIslandSceneLoaded]
        [UsedImplicitly]
        public static void Go() {
            TrackingHandler<Player>.Subscribe(new PlayerCallback(), true);
        }

        private class PlayerCallback : ITrackingHandlerCallback<Player> {
            public void OnAdded(Player player) {
                DumpInfo();
            }

            public void OnRemoved(Player instance) {
            }
        }

        private static void DumpInfo() {
            const string path     = Plugin.FANDOM_WIKI_OUTPUT_PATH + Plugin.MAPS_NAMESPACE;
            const string iconPath = $@"{path}\Icons";

            Reference_Wiki.WriteAll.EraseAndCreateDir(path);
            Reference_Wiki.WriteAll.EraseAndCreateDir(iconPath);

            Debug.Log("Writing Fandom wiki map info.");

            var markers = Resources.FindObjectsOfTypeAll<MapMarker>();
            var icons   = markers.Select(marker => marker.Icon).Distinct();

            var list = markers.Select(marker => new MarkerInfo {
                                  name     = marker.GetName(),
                                  position = new(marker.Data.Position),
                                  tooltip  = marker.Data.Tooltip,
                                  level    = marker.Data.IslandLevel,
                                  surface  = marker.Data.Surface,
                                  icon     = $"{marker.Data.Icon.name}.png"
                              })
                              .ToList();

            Debug.Log($"Landing site count (LandingSite.Instances): {LandingSite.Instances.Count}");
            Debug.Log($"Landing site count (Resources.FindObjectsOfTypeAll<LandingSite>()): {Resources.FindObjectsOfTypeAll<LandingSite>().Length}");

            list.AddRange(LandingSite.Instances.Select(landingSite => {
                var localizedName = landingSite.GetLocalizedName();
                return new MarkerInfo {
                    name          = localizedName,
                    position      = new(landingSite.TravelPosition.x, 0, landingSite.TravelPosition.y),
                    tooltip       = localizedName,
                    level         = landingSite.IslandLevel,
                    icon          = "LandingSite_Icon.png",
                    isLandingSite = true
                };
            }));

            foreach (var icon in icons) {
                try {
                    Reference_Wiki.WriteItems.WriteTexture(icon, iconPath, icon.name);
                } catch (Exception e) {
                    Debug.LogError($"Error saving {icon.name} texture: " + e.Message);
                }
            }

            Directory.CreateDirectory($@"{path}\Landing Sites\");
            Directory.CreateDirectory($@"{path}\Everything Else\");

            foreach (var entry in list) {
                var safeName = entry.name.GetSafeName();
                var outFile  = entry.isLandingSite ? $@"{path}\Landing Sites\{safeName}.txt" : $@"{path}\Everything Else\{safeName}.txt";

                using var writer = new StreamWriter(outFile, false, Encoding.UTF8);

                // ReSharper disable once ConvertIfStatementToConditionalTernaryExpression
                if (entry.isLandingSite) {
                    writer.WriteLine("[[Category:Landing site]]\r\n");
                } else {
                    writer.WriteLine("[[Category:Location]]\r\n");
                }

                // Write info box.
                writer.WriteLine("{{Infobox location");
                writer.WriteLine($"| name = {entry.name}".Trim());
                writer.WriteLine($"| tooltip = {entry.tooltip.Replace("\n", " ").Replace("  ", " ")}".Trim());
                writer.WriteLine($"| icon = {entry.icon}".Trim());
                writer.WriteLine($"| coordinates = \"x\": {entry.position.x}<br>\"y\": {entry.position.y}<br>\"z\": {entry.position.z}");
                writer.WriteLine($"| level = {entry.level}".Trim());
                writer.WriteLine($"| surface = {entry.surface}".Trim());
                writer.WriteLine($"| locationType = {entry.isLandingSite}".Trim());
                writer.WriteLine("}}");
            }
        }

        private struct MarkerInfo {
            public string name;
            public Vec    position;
            public string tooltip;
            public int    level;
            public bool   surface;
            public string icon;
            public bool   isLandingSite;
        }

        [SuppressMessage("ReSharper", "MemberCanBePrivate.Local")]
        private readonly struct Vec {
            public readonly float x;
            public readonly float y;
            public readonly float z;

            public Vec(float x, float y, float z) {
                this.x = x;
                this.y = y;
                this.z = z;
            }

            public Vec(Vector3 vec) {
                x = vec.x;
                y = vec.y;
                z = vec.z;
            }
        }
    }
}