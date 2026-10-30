using HarmonyLib;
using Il2CppAssets.Scripts.Models;
using Il2CppAssets.Scripts.Simulation;
using Il2CppAssets.Scripts.Unity;
using Il2CppAssets.Scripts.Unity.Rounds;
using MelonLoader;

[assembly: MelonInfo(typeof(freeplayBATTD.Core), "freeplayBATTD", "1.0.0", "xenia617", null)]
[assembly: MelonGame("Ninja Kiwi", "Bloons Adventure Time TD")]

namespace freeplayBATTD
{
    public class Core : MelonMod
    {
        public static void StartFreeplay(Simulation __instance)
        {
            int seed = __instance.model.freeplayDefaultSeed;
            float diff = Game.instance.model.freeplayDifficulty;
            FreeplayTheme freeplayTheme = Game.instance.freeplayTheme;
            GameModel gameModel = __instance.model;
            RoundSetData roundData = Game.instance.freeplayDifficultyMods;

            __instance.InitialiseFreeplay(gameModel, seed, diff, freeplayTheme, roundData);

            MelonLogger.Msg("Freeplay initialized!");
        }
        public override void OnInitializeMelon()
        {
            LoggerInstance.Msg("freeplaymod loaded!");
        }

        [HarmonyPatch(typeof(Simulation), "InitialiseMap")]
        public class InGamePatch
        {
            [HarmonyPostfix]
            public static void Postfix(Simulation __instance)
            {
                StartFreeplay(__instance);
            }

        }
    }
}