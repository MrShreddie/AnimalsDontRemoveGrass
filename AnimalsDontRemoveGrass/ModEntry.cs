using System;
using HarmonyLib;
using Microsoft.Xna.Framework;
using StardewModdingAPI;
using StardewValley;
using StardewValley.TerrainFeatures;

namespace AnimalsDontRemoveGrass
{
    [HarmonyPatch(typeof(FarmAnimal))]
    internal sealed class ModEntry : Mod
    {
        private static IMonitor modMonitor = null!;

        public override void Entry(IModHelper helper)
        {
            modMonitor = this.Monitor;

            new Harmony(this.ModManifest.UniqueID).PatchAll(typeof(ModEntry).Assembly);
        }

        // Use Harmony to overwrite the Eat() method in StardewValley.FarmAnimal
        [HarmonyPatch(nameof(FarmAnimal.Eat))]
        private static bool Prefix(FarmAnimal __instance, GameLocation location)
        {
            // Try to run new logic with tile removal event taken out. Don't run original logic if successful
            try
            {
                Vector2 tile = __instance.Tile;
                __instance.isEating.Value = true;
                int num = 1;
                if (location.terrainFeatures.TryGetValue(tile, out var value) && value is Grass grass)
                {
                    num = grass.grassType.Value;
                    // Here is where the original logic lies to remove the eaten grass tile. Every other event happens except the tile removal.
                }

                __instance.Sprite.loop = false;
                __instance.fullness.Value = 255;
                if ((int)__instance.moodMessage != 5 && (int)__instance.moodMessage != 6 && !location.IsRainingHere())
                {
                    __instance.happiness.Value = 255;
                    __instance.friendshipTowardFarmer.Value = Math.Min(1000, __instance.friendshipTowardFarmer.Value + ((num == 7) ? 16 : 8));
                }

                return false;
            }
            // Run original logic and output error to SMAPI if failed
            catch (Exception ex)
            {
                modMonitor.Log($"failed in {nameof(Prefix)}:\n{ex}", LogLevel.Error);
                return true;
            }
        }
    }


}

