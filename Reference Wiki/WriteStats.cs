using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Base_Mod;
using UnityEngine;

namespace Wiki_Writer.Reference_Wiki {
    public static class WriteStats {
        public static void Write(TextWriter writer, ItemDefinition item, WikiPage wikiPage) {
            writer.WriteLine();
            writer.WriteLine("==== Stats ====");
            AddStat(writer, wikiPage.stats, "Max Stack", item.MaxStack);
            try {
                AddStatsFromPropertySet(writer, wikiPage.stats, item.Stats);
            } catch (Exception e) {
                Debug.LogError($"Error writing stats for: `{item.Name}`, \"{e.Message}\".");
            }

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

            if (item.TryGetComponent(out PackableModuleHealth packableModuleHealth)) {
                AddStat(writer, wikiPage.stats, "Module Health", packableModuleHealth.MaxHP);
                AddStat(writer, wikiPage.stats, "Closed Armor", $"{packableModuleHealth.Armor * 100}%");
            }

            if (item.TryGetComponent(out DrillshipObjectHealth drillshipObjectHealth)) {
                AddStat(writer, wikiPage.stats, "Object Health", drillshipObjectHealth.MaxHP);
            }

            if (item.TryGetComponent(out GridModule gridModule)) {
                AddStat(writer, wikiPage.stats, "Size", gridModule.ModuleCount);
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
                        ammo = reloader.Ammunition.ToArray().Cast<AmmoDefinition>().ToArray();
                    }
                    if (toolItemDef.TryGetComponent(out WeaponReloaderBattery reloaderBattery)) {
                        AddStat(writer, wikiPage.stats, "Ammo (Battery) Capacity", reloaderBattery.AmmoCapacity);
                        AddStat(writer, wikiPage.stats, "Reload Cooldown", reloaderBattery.ReloadCooldown);
                        AddStat(writer, wikiPage.stats, "Reload Duration", reloaderBattery.ReloadDuration);
                    }
                    if (toolItemDef.TryGetComponent(out WeaponReloaderNoAmmo reloaderNoAmmo)) {
                        AddAmmoStats(writer, wikiPage, reloaderNoAmmo.LoadedAmmo);
                    }

                    var components = (from component in toolItemDef.Prefab.GetComponents<Component>()
                                      select component.GetType().ToString()).Distinct();
                    Debug.Log("---- Components: " + string.Join(", ", components));
                    break;
            }

            // Weapon modifiers weapons have.
            if ((item as ToolItemDefinition)?.TryGetComponent(out WeaponStatsModification statMods) == true) {
                writer.WriteLine();
                writer.WriteLine("==== Stats Modifiers ====");
                AddStat(writer, wikiPage.stats, "Damage Multiplier", statMods.DamageMultiplier);
                AddStat(writer, wikiPage.stats, "Spread Multiplier", statMods.SpreadMultiplier);
                AddStat(writer, wikiPage.stats, "Recoil Vertical Multiplier", statMods.RecoilVerticalMultiplier);
                AddStat(writer, wikiPage.stats, "Recoil Horizontal Multiplier", statMods.RecoilHorizontalMultipler);
                AddStat(writer, wikiPage.stats, "Recoil First-Shot Multiplier", statMods.RecoilFirstShotMultiplier);
                AddStat(writer, wikiPage.stats, "Recoil Minimum Burst Length Multiplier", statMods.RecoilMinimumBurstLengthMultiplier);
                AddStat(writer, wikiPage.stats, "Projectile Count Multiplier", statMods.ProjectileCountMultiplier);
                AddStat(writer, wikiPage.stats, "Rate of Fire Multiplier", statMods.RateOfFireMultiplier);
                AddStat(writer, wikiPage.stats, "Effective Range Multiplier", statMods.EffectiveRangeMultiplier);
                AddStat(writer, wikiPage.stats, "Range Multiplier", statMods.RangeMultiplier);
                AddStat(writer, wikiPage.stats, "Muzzle Velocity Multiplier", statMods.MuzzleVelocityMultiplier);
                AddStat(writer, wikiPage.stats, "Gravity Factor Multiplier", statMods.GravityFactorMultiplier);
                AddStat(writer, wikiPage.stats, "Noise Multiplier", statMods.NoiseMultiplier);
                AddStat(writer, wikiPage.stats, "Hip-Cone Multiplier", statMods.HipConeMultiplier);
                AddStat(writer, wikiPage.stats, "Aim-Cone Multiplier", statMods.AimConeMultiplier);
            }

            if (ammo != null && ammo.Length > 0) {
                writer.WriteLine();
                writer.WriteLine("==== Ammo Types ====");

                var ammoTypes = new List<string>(ammo.Length);
                foreach (var ammoDef in ammo) {
                    writer.WriteLine($"  * {ammoDef.CreateWikiLink(false)}");
                    ammoTypes.Add(ammoDef.GetLocalizedName());
                }

                wikiPage.stats["Ammo Types"] = string.Join(", ", ammoTypes);
            }

            if (item.Prefabs?.Length > 0) {
                var components = (from component in item.Prefabs[0].GetComponents<Component>()
                                  select component.GetType().ToString()).Distinct();
                Debug.Log("---- Components: " + string.Join(", ", components));
            }
        }

        private static void AddAmmoStats(TextWriter writer, WikiPage wikiPage, AmmoStats ammoStats) {
            AddStat(writer, wikiPage.stats, "Damage", ammoStats.Damage);
            AddStat(writer, wikiPage.stats, "Damage at Range Multiplier", ammoStats.DamageAtRangeMult);
            AddStat(writer, null, "Damage Type", ammoStats.DamageType);
            AddStat(writer, wikiPage.stats, "Effective Range", ammoStats.EffectiveRange);
            AddStat(writer, wikiPage.stats, "Gravity Factor", ammoStats.GravityFactor);
            AddStat(writer, wikiPage.stats, "Muzzle Velocity", ammoStats.MuzzleVelocity);
            AddStat(writer, null, "Noise", ammoStats.Noise);
            AddStat(writer, wikiPage.stats, "Projectile Count", ammoStats.ProjectileCount);
            AddStat(writer, wikiPage.stats, "Range", ammoStats.Range);
            AddStat(writer, wikiPage.stats, "Rate of Fire", ammoStats.RateOfFire);
            AddStat(writer, wikiPage.stats, "Spread", ammoStats.RateOfFire);
        }

        private static void AddStatsFromPropertySet(TextWriter writer, IDictionary<string, string> statDict, PropertySet properties) {
            try {
                foreach (var stat in properties.Items) {
                    var statName  = stat.Property.Name;
                    var statValue = stat.GetStatValue().ToString();
                    if (statName == "Energy" && float.TryParse(statValue, out var v) && v == 0) continue;
                    AddStat(writer, statDict, statName, statValue);
                }
            } catch (Exception) {
                Debug.LogError("Error parsing stats for item, skipping stats.");
            }
        }

        public static void AddStat<T>(TextWriter writer, IDictionary<string, string> statDict, string statName, T stat) {
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