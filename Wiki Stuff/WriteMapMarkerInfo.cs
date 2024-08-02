using System;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using Base_Mod.Models;
using JetBrains.Annotations;
using Newtonsoft.Json;
using UnityEngine;
using Wiki_Writer.Reference_Wiki;

namespace Wiki_Writer.Wiki_Stuff;

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
        var markers = Resources.FindObjectsOfTypeAll<MapMarker>();

        var list = markers.Select(marker => new MarkerInfo {
                              position = new(marker.Data.Position),
                              tooltip  = marker.Data.Tooltip,
                              level    = marker.Data.IslandLevel,
                              surface  = marker.Data.Surface,
                              icon     = marker.Data.Icon.name
                          })
                          .ToList();

        var icons = markers.Select(marker => marker.Icon).Distinct();

        Debug.Log($"Landing site count (LandingSite.Instances): {LandingSite.Instances.Count}");
        Debug.Log($"Landing site count (Resources.FindObjectsOfTypeAll<LandingSite>()): {Resources.FindObjectsOfTypeAll<LandingSite>().Length}");

        list.AddRange(LandingSite.Instances.Select(landingSite => new MarkerInfo {
            position      = new(landingSite.TravelPosition.x, 0, landingSite.TravelPosition.y),
            tooltip       = landingSite.NameLocalization.Text,
            level         = landingSite.IslandLevel,
            icon          = "LandingSite_Icon",
            isLandingSite = true
        }));

        var songs = Resources.FindObjectsOfTypeAll<PhonographCylinder>();
        Debug.Log($"Songs count (Resources.FindObjectsOfTypeAll<PhonographCylinder>()): {songs.Length}");

        list.AddRange(songs.Select(song => new MarkerInfo {
            position         = new(song.gameObject.transform.position.x, song.gameObject.transform.position.y, song.gameObject.transform.position.z),
            tooltip          = song.Item.GetLocalizedName(),
            level            = song.gameObject.layer,
            icon             = "Phonograph_Icon",
            isPhonographSong = true,
            surface          = true
        }));

        var basePath = $"{Plugin.BASE_OUTPUT_PATH}Map Markers";
        if (!Directory.Exists(basePath)) Directory.CreateDirectory(basePath);

        foreach (var icon in icons) {
            try {
                WriteItems.WriteTexture(icon, basePath, icon.name);
            } catch (Exception e) {
                Debug.LogError($"Error saving {icon.name} texture: " + e.Message);
            }
        }

        var json = JsonConvert.SerializeObject(list, Formatting.Indented);
        File.WriteAllText($@"{basePath}\Map Marker Info.json", json);
    }

    private struct MarkerInfo {
        public Vec    position;
        public string tooltip;
        public int    level;
        public bool   surface;
        public string icon;
        public bool   isLandingSite;
        public bool   isPhonographSong;
    }

    [SuppressMessage("ReSharper", "MemberCanBePrivate.Local")]
    private struct Vec {
        public float x;
        public float y;
        public float z;

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