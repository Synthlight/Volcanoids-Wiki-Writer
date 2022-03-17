using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEngine;

namespace Wiki_Writer.Reference_Wiki {
    public static class WriteStats {
        public static void Write(TextWriter writer, ItemDefinition item, WikiPage wikiPage) {
            writer.WriteLine();
            writer.WriteLine("==== Stats ====");
            AddStat(writer, wikiPage.stats, "Max Stack", item.MaxStack);
            AddStatsFromPropertySet(writer, wikiPage.stats, item.Stats);

            if (item.TryGetComponent(out PowerPlant powerPlant)) {
                AddStat(writer, wikiPage.stats, "Energy Per Second", powerPlant.EnergyPerSecond);
                AddStat(writer, wikiPage.stats, "Fuel Efficiency", powerPlant.FuelEfficiency);
            }

            if (item.TryGetComponent(out EnergyConsumer energyConsumer)) {
                AddStat(writer, wikiPage.stats, "Energy Per Second", -energyConsumer.EnergyPerSecond);
            }

            if (item.TryGetComponent(out HeatRibsModule geothermal)) {
                AddStat(writer, wikiPage.stats, "Energy Per Second", geothermal.EnergyOutput);
            }

            if (item.TryGetComponent(out PackableModule packableModule)) {
                AddStat(writer, wikiPage.stats, "Core Slot Cost", packableModule.CoreSlotCount);
            }

            if (item.TryGetComponent(out ProductionModule prodModule)) {
                AddStat(writer, wikiPage.stats, $"{prodModule.FactoryType.name} Points", prodModule.Points);
            }

            if (item.TryGetComponent(out Inventory inventory)) {
                AddStat(writer, wikiPage.stats, "Inventory Size", inventory.Capacity);
            }

            AmmoDefinition[] ammo = null;

            switch (item) {
                case AmmoDefinition ammoDef:
                    var ammoStats = ammoDef.AmmoStats;
                    AddAmmoStats(writer, wikiPage, ammoStats);

                    writer.WriteLine();
                    AddStat(writer, "Aim Accuracy", ammoStats.AimAccuracy);
                    AddStat(writer, "Hip Accuracy", ammoStats.AimAccuracy);
                    AddStat(writer, "Recoil", ammoStats.Recoil);
                    break;
                case ToolItemDefinition toolItemDef:
                    if (toolItemDef.TryGetComponent(out WeaponReloaderAmmo reloader)) {
                        AddStat(writer, wikiPage.stats, "Ammo Capacity", reloader.AmmoCapacity);
                        AddStat(writer, wikiPage.stats, "Reload Cooldown", reloader.ReloadCooldown);
                        AddStat(writer, wikiPage.stats, "Reload Duration", reloader.ReloadDuration);
                        ammo = reloader.Ammunition;
                    }
                    if (toolItemDef.TryGetComponent(out WeaponReloaderNoAmmo reloaderNoAmmo)) {
                        AddAmmoStats(writer, wikiPage, reloaderNoAmmo.LoadedAmmo);
                    }

                    var components = (from component in toolItemDef.Prefab.GetComponents<Component>()
                                      select component.GetType().ToString()).Distinct();
                    Debug.Log("---- Components: " + string.Join(", ", components));
                    break;
            }

            if (ammo != null && ammo.Length > 0) {
                writer.WriteLine();
                writer.WriteLine("==== Ammo Types ====");
                foreach (var ammoDef in ammo) {
                    writer.WriteLine($"  * {ammoDef.CreateWikiLink(false)}");
                }
            }

            if (item.Prefabs?.Length > 0) {
                var components = (from component in item.Prefabs[0].GetComponents<Component>()
                                  select component.GetType().ToString()).Distinct();
                Debug.Log("---- Components: " + string.Join(", ", components));
            }
        }

        private static void AddAmmoStats(TextWriter writer, WikiPage wikiPage, AmmoStats ammoStats) {
            AddStat(writer, wikiPage.stats, "Damage", ammoStats.Damage);
            AddStat(writer, wikiPage.stats, "Damage At Range Multiplier", ammoStats.DamageAtRangeMult);
            AddStat(writer, null, "Damage Type", ammoStats.DamageType);
            AddStat(writer, wikiPage.stats, "Effective Range", ammoStats.EffectiveRange);
            AddStat(writer, wikiPage.stats, "Gravity Factor", ammoStats.GravityFactor);
            AddStat(writer, wikiPage.stats, "Muzzle Velocity", ammoStats.MuzzleVelocity);
            AddStat(writer, null, "Noise", ammoStats.Noise);
            AddStat(writer, wikiPage.stats, "Projectile Count", ammoStats.ProjectileCount);
            AddStat(writer, wikiPage.stats, "Range", ammoStats.Range);
            AddStat(writer, wikiPage.stats, "Rate Of Fire", ammoStats.RateOfFire);
            AddStat(writer, wikiPage.stats, "Spread", ammoStats.RateOfFire);
        }

        private static void AddStatsFromPropertySet(TextWriter writer, IDictionary<string, string> statDict, PropertySet properties) {
            foreach (var stat in properties.Items) {
                var statName  = stat.Property.Name;
                var statValue = stat.GetStatValue().ToString();
                if (statName == "Energy" && float.TryParse(statValue, out var v) && v == 0) continue;
                AddStat(writer, statDict, statName, statValue);
            }
        }

        private static void AddStat<T>(TextWriter writer, IDictionary<string, string> statDict, string statName, T stat) {
            var value = stat.ToString();
            writer.WriteLine($"| {statName} | {value} |");
            if (statDict != null) {
                statDict[statName] = value;
            }
        }

        private static void AddStat(TextWriter writer, string statName, AccuracyData obj) {
            writer.WriteLine($"| {statName} | Bloom | {obj.Bloom} | {GetTooltipText(obj, nameof(obj.Bloom))} |");
            writer.WriteLine($"| ::: | Cone | {obj.Cone} | {GetTooltipText(obj, nameof(obj.Cone))} |");
            writer.WriteLine($"| ::: | Cone (Moving) | {obj.ConeMoving} | {GetTooltipText(obj, nameof(obj.ConeMoving))} |");
            writer.WriteLine($"| ::: | Cone (Crouched) | {obj.ConeCrouch} | {GetTooltipText(obj, nameof(obj.ConeCrouch))} |");
            writer.WriteLine($"| ::: | Cone (Crouched, Moving) | {obj.ConeCrouchMoving} | {GetTooltipText(obj, nameof(obj.ConeCrouchMoving))} |");
            writer.WriteLine($"| ::: | Max Cone Angle | {obj.MaxConeAngle} | {GetTooltipText(obj, nameof(obj.MaxConeAngle))} |");
        }

        private static void AddStat(TextWriter writer, string statName, RecoilData obj) {
            writer.WriteLine($"| {statName} | Angle Min/Max | {obj.AngleMinMax} | {GetTooltipText(obj, nameof(obj.AngleMinMax))} |");
            writer.WriteLine($"| ::: | First Shot Multiplier | {obj.FirstShotMultiplier} | {GetTooltipText(obj, nameof(obj.FirstShotMultiplier))} |");
            writer.WriteLine($"| ::: | Horizontal Min/Max | {obj.HorizontalMinMax} | {GetTooltipText(obj, nameof(obj.HorizontalMinMax))} |");
            writer.WriteLine($"| ::: | Horizontal Tolerance | {obj.HorizontalTolerance} | {GetTooltipText(obj, nameof(obj.HorizontalTolerance))} |");
            writer.WriteLine($"| ::: | Minimum Burst Length | {obj.MinimumBurstLength} | {GetTooltipText(obj, nameof(obj.MinimumBurstLength))} |");
            writer.WriteLine($"| ::: | Vertical | {obj.Vertical} | {GetTooltipText(obj, nameof(obj.Vertical))} |");
        }

        private static string GetTooltipText(object obj, string statName) {
            try {
                if (obj.GetType().GetField(statName)?.GetCustomAttribute(typeof(TooltipAttribute)) is TooltipAttribute tooltip) {
                    return tooltip.tooltip.Replace("\r\n", " ") ?? "";
                }
                return "";
            } catch (Exception) {
                return "";
            }
        }
    }
}