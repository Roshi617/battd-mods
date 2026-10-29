using HarmonyLib;
using Il2CppAssets.Scripts.Unity.UI_New.InGame;
using MelonLoader;
using UnityEngine;

[assembly: MelonInfo(typeof(speedhackBloonsAT.Core), "speedhackBloonsAT", "1.1.0", "xenia617", null)]
[assembly: MelonGame("Ninja Kiwi", "Bloons Adventure Time TD")]

namespace speedhackBloonsAT
{
    public class Core : MelonMod
    {
        private static readonly float[] Multipliers = { 1f, 2f, 5f, 10f };
        private static readonly KeyCode[] Keys = { KeyCode.F1, KeyCode.F2, KeyCode.F3, KeyCode.F4 };

        private const float VanillaMaxDeltaTime = 1f / 3f;
        private const float BoostedMaxDeltaTime = 0.5f;


        private static float multiplier = 1f;
        private static float gameScale = 1f;
        private static float appliedScale = 1f;
        private static bool gameOverLock;   

        private static void ResetToVanilla(string reason)
        {
            multiplier = 1f;
            Time.maximumDeltaTime = VanillaMaxDeltaTime;
            // Undo the multiplication right now, before the game-over screen loads. (prevents lag)
            if (gameScale >= 0f && !Mathf.Approximately(Time.timeScale, gameScale))
            {
                Time.timeScale = gameScale;
            }
            appliedScale = Time.timeScale;
            MelonLogger.Msg($"Speed reset to vanilla ({reason})");
        }

        private static void ReleaseLock(string reason)
        {
            if (!gameOverLock) return;
            gameOverLock = false;
            MelonLogger.Msg($"Speed keys re-enabled ({reason})");
        }

        [HarmonyPatch(typeof(InGame), nameof(InGame.GameComplete))]
        public static class GameCompletePatch
        {
            public static void Prefix()
            {
                gameOverLock = true;
                ResetToVanilla("game ended");
            }
        }

        [HarmonyPatch(typeof(InGame), nameof(InGame.StartMatch))]
        public static class StartMatchPatch
        {
            public static void Postfix() { ReleaseLock("match started"); }
        }

        [HarmonyPatch(typeof(InGame), nameof(InGame.OnVictoryFreeplayButton))]
        public static class FreeplayPatch
        {
            public static void Postfix() { ReleaseLock("freeplay"); }
        }

        [HarmonyPatch(typeof(InGame), nameof(InGame.ContinueForCoins))]
        public static class ContinuePatch
        {
            public static void Postfix() { ReleaseLock("continued"); }
        }

        public override void OnInitializeMelon()
        {
            LoggerInstance.Msg("speedhack keys: F1 = 1x, F2 = 2x, F3 = 5x, F4 = 10x (stacks with in-game FF). F8 = debug.");
        }

        public override void OnSceneWasLoaded(int buildIndex, string sceneName)
        {
            appliedScale = Time.timeScale;
            gameScale = Time.timeScale;
        }

        public override void OnUpdate()
        {
            for (int i = 0; i < Keys.Length; i++)
            {
                if (Input.GetKeyDown(Keys[i]))
                {
                    if (gameOverLock)
                    {
                        MelonLogger.Msg("Speed keys disabled on the game-over screen.");
                        break;
                    }
                    multiplier = Multipliers[i];
                    Time.maximumDeltaTime = multiplier > 1f ? BoostedMaxDeltaTime : VanillaMaxDeltaTime;
                    MelonLogger.Msg($"Speed multiplier set to {multiplier}x");
                }
            }

            float current = Time.timeScale;

            if (!Mathf.Approximately(current, appliedScale))
            {
                gameScale = current;
            }

            // only speed up actual gameplay
            bool inGame = InGame.instance != null && !gameOverLock;
            float target = (inGame && gameScale > 0f) ? gameScale * multiplier : gameScale;

            if (!Mathf.Approximately(current, target))
            {
                Time.timeScale = target;
            }
            appliedScale = target;

            if (Input.GetKeyDown(KeyCode.F8))
            {
                MelonLogger.Msg($"timeScale={Time.timeScale} gameScale={gameScale} multiplier={multiplier} locked={gameOverLock} " +
                                $"fixedDeltaTime={Time.fixedDeltaTime} maxDeltaTime={Time.maximumDeltaTime} " +
                                $"fps={1f / Time.unscaledDeltaTime:F0}");
            }
        }
    }
}
