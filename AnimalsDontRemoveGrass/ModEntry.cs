using System;
using System.Runtime.CompilerServices;
using GenericModConfigMenu;
using HarmonyLib;
using Microsoft.Xna.Framework;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;
using StardewValley.TerrainFeatures;

namespace AnimalsDontRemoveGrass
{
    public sealed class ModConfig
    {
        public bool BoolBlue { get; set; } = true;
        public bool BoolRegular { get; set; } = true;
    }

    [HarmonyPatch(typeof(FarmAnimal))]
    internal sealed class ModEntry : Mod
    {
        private static IMonitor modMonitor = null!;
        private static ModConfig Config =null!;

        public override void Entry(IModHelper helper)
        {
            modMonitor = this.Monitor;

            new Harmony(this.ModManifest.UniqueID).PatchAll(typeof(ModEntry).Assembly);

            Config = this.Helper.ReadConfig<ModConfig>();
            bool blueBool = Config.BoolBlue;
            bool regularBool = Config.BoolRegular;

            helper.Events.GameLoop.GameLaunched += onGameLaunched;

        }

        private void onGameLaunched(object? sender, GameLaunchedEventArgs e)
        {
            // get Generic Mod Config Menu's API (if it's installed)
            var configMenu = this.Helper.ModRegistry.GetApi<IGenericModConfigMenuApi>("spacechase0.GenericModConfigMenu");
            if (configMenu is null)
                return;

            // register mod
            configMenu.Register(
                mod: this.ModManifest,
                reset: () => Config = new ModConfig(),
                save: () => this.Helper.WriteConfig(Config)
            );

            // add some config options
            configMenu.AddBoolOption(
                mod: this.ModManifest,
                name: () => "Don't remove blue grass",
                tooltip: () => "Disabling this will cause blue grass tiles to be removed by animals when eaten.",
                getValue: () => Config.BoolBlue,
                setValue: value => Config.BoolBlue = value
            );

            configMenu.AddBoolOption(
                mod: this.ModManifest,
                name: () => "Don't remove regular grass",
                tooltip: () => "Disabling this will cause regular grass tiles to be removed by animals when eaten.",
                getValue: () => Config.BoolRegular,
                setValue: value => Config.BoolRegular = value
            );
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
                    // Remove grass tile if config set to off (depending on grass type)
                    int number = __instance.GetAnimalData()?.GrassEatAmount ?? 2;

                    if (!Config.BoolBlue && grass.grassType.Value == 7)
                    {
                        if (grass.reduceBy(number, location.Equals(Game1.currentLocation)))
                        {
                            location.terrainFeatures.Remove(tile);
                        }
                    }
                    if (!Config.BoolRegular && grass.grassType.Value != 7)
                    {
                        if (grass.reduceBy(number, location.Equals(Game1.currentLocation)))
                        {
                            location.terrainFeatures.Remove(tile);
                        }
                    }
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