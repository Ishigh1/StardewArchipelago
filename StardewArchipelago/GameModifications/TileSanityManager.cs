using System.Text.RegularExpressions;
using HarmonyLib;
using Microsoft.Xna.Framework;
using StardewArchipelago.GameModifications.CodeInjections.Tilesanity;
using StardewModdingAPI;
using StardewValley;

namespace StardewArchipelago.GameModifications;

public class TileSanityManager
{
    private readonly Harmony _harmony;
    public const string TILESANITY_PREFIX = "Tilesanity: ";

    public static string GetTileName(int x, int y, Farmer farmer)
    {
        string map = farmer.currentLocation.DisplayName;
        if (map == $"{farmer.farmName} Farm")
        {
            map = farmer.currentLocation.Name;
            if (map == "Farm")
                map = $"{Game1.GetFarmTypeKey()} Farm";
        }

        return $"{TILESANITY_PREFIX}{map} ({x}-{y})";
    }

    public static (string, int, int) GetTileFromName(string name)
    {
        var pattern = $@"{Regex.Escape(TILESANITY_PREFIX)}([ \w]+) +\((\d+)\-(\d+)\)";

        var match = Regex.Match(name, pattern);

        var map = match.Groups[1].Value;
        var x = int.Parse(match.Groups[2].Value);
        var y = int.Parse(match.Groups[3].Value);
        
        if (map == $"{Game1.GetFarmTypeKey()} Farm")
        {
            map = "Farm";
        }

        return (map, x, y);
    }

    public TileSanityManager(Harmony harmony)
    {
        _harmony = harmony;
    }

    public void PatchWalk(IModHelper modHelper)
    {
        _harmony.Patch(
            original: AccessTools.Method(typeof(Farmer), nameof(Farmer.Update)),
            prefix: new HarmonyMethod(typeof(WalkSanityInjections),
                nameof(WalkSanityInjections.MovePosition_Update_Prefix)),
            postfix: new HarmonyMethod(typeof(WalkSanityInjections),
                nameof(WalkSanityInjections.MovePosition_Update_Postfix))
        );

        _harmony.Patch(
            original: AccessTools.Method(typeof(GameLocation), nameof(GameLocation.isCollidingPosition),
                new[]
                {
                    typeof(Rectangle), typeof(xTile.Dimensions.Rectangle), typeof(bool), typeof(int), typeof(bool),
                    typeof(Character),
                }),
            prefix: new HarmonyMethod(typeof(WalkSanityInjections),
                nameof(WalkSanityInjections.isCollidingPosition_ForbidMove_Prefix))
        );

        modHelper.Events.Display.RenderedWorld += TileUI.Render_Tiles;
    }
}
