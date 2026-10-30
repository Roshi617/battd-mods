using System;
using System.IO;
using System.Text;
using HarmonyLib;
using Il2CppAssets.Scripts.Models;
using Il2CppAssets.Scripts.Models.Bloons;
using Il2CppAssets.Scripts.Simulation;
using Il2CppAssets.Scripts.Simulation.Bloons;
using Il2CppAssets.Scripts.Simulation.Track.RoundManagers;
using Il2CppAssets.Scripts.Unity;
using Il2CppAssets.Scripts.Unity.Rounds;
using Il2CppAssets.Scripts.Unity.UI_New.GameOver;
using Il2CppAssets.Scripts.Unity.UI_New.InGame;
using Il2CppInterop.Runtime;
using MelonLoader;
using UnityEngine;

[assembly: MelonInfo(typeof(freeplayBATTD.Core), "freeplayBATTD", "2.0.0", "xenia617", null)]
[assembly: MelonGame("Ninja Kiwi", "Bloons Adventure Time TD")]

namespace freeplayBATTD
{
    public class Core : MelonMod
    {
        private static MelonPreferences_Entry<string> roundSource;
        private static MelonPreferences_Entry<int> loopWindow;
        private static MelonPreferences_Entry<string> themeName;
        private static MelonPreferences_Entry<string> roundSetDataName;
        private static MelonPreferences_Entry<float> healthPerRound;
        private static MelonPreferences_Entry<float> moabHealthPerRound;
        private static MelonPreferences_Entry<float> speedPerRound;
        private static MelonPreferences_Entry<float> maxSpeedMultiplier;

        // normal mode: number of rounds in the map's round set (75), captured in CreateRound
        private static int normalRoundCount;
        private static int lastLoggedRound = -1;

        // true from the moment freeplay is chosen until the next StartMatch (new game/restart)
        private static bool freeplayActive;
        private static bool loopNormalRounds;
        private static bool clickHandled;

        public override void OnInitializeMelon()
        {
            MelonPreferences_Category cat = MelonPreferences.CreateCategory("freeplayBATTD");
            roundSource = cat.CreateEntry("RoundSource", "Martian",
                description: "Martian = procedural Martian Trials freeplay, Normal = play the rest of the map's 75 rounds, then loop the last ones");
            loopWindow = cat.CreateEntry("LoopWindow", 10,
                description: "Normal mode: how many of the map's final rounds to loop after the last one");
            themeName = cat.CreateEntry("ThemeName", "",
                description: "Martian mode: FreeplayTheme asset name (blank = game default). Press F7 in game to list names");
            roundSetDataName = cat.CreateEntry("RoundSetDataName", "",
                description: "Martian mode: RoundSetData asset name for difficulty mods (blank = game default)");

            healthPerRound = cat.CreateEntry("HealthPerRound", 0.05f,
                description: "Normal mode: extra bloon health per round past the map's last round (0.05 = +5%/round, linear)");
            moabHealthPerRound = cat.CreateEntry("MoabHealthPerRound", 0.08f,
                description: "Normal mode: extra MOAB-class health per round past the last round");
            speedPerRound = cat.CreateEntry("SpeedPerRound", 0.01f,
                description: "Normal mode: extra bloon speed per round past the last round");
            maxSpeedMultiplier = cat.CreateEntry("MaxSpeedMultiplier", 2.0f,
                description: "Normal mode: cap on the extra speed multiplier (very fast bloons skip past towers)");

            LoggerInstance.Msg($"freeplayBATTD loaded. RoundSource={roundSource.Value}. Freeplay button appears on the victory screen. F7 = list round assets.");
        }

        public override void OnUpdate()
        {
            if (Input.GetKeyDown(KeyCode.F7))
            {
                DumpRoundAssets();
            }
        }

        // victory screen button
        [HarmonyPatch(typeof(VictoryScreen), nameof(VictoryScreen.SetupScreen))]
        public static class SetupScreenPatch
        {
            public static void Postfix(VictoryScreen __instance)
            {
                var button = __instance.freeplayButton;
                if (button == null)
                {
                    MelonLogger.Warning("[freeplay] VictoryScreen.freeplayButton is null; can't show it.");
                    return;
                }

                clickHandled = false;
                VictoryScreen screen = __instance;

                // RemoveAllListeners only drops runtime listeners; any prefab-wired FreeplayClick
                // stays, and is a no-op because the game passes a null onFreeplayClick delegate.
                button.onClick.RemoveAllListeners();
                button.onClick.AddListener((UnityEngine.Events.UnityAction)(() => OnFreeplayChosen(screen)));
                button.gameObject.SetActive(true);
            }
        }

        private static void OnFreeplayChosen(VictoryScreen screen)
        {
            if (clickHandled) return;   // guard double clicks while the scene transitions
            clickHandled = true;

            InGame inGame = InGame.instance;
            if (inGame == null)
            {
                MelonLogger.Error("[freeplay] InGame.instance is null on the victory screen.");
                return;
            }

            // Switch the round source BEFORE the game returns to the in-game scene
            if (!EnterFreeplay())
            {
                clickHandled = false;
                return;
            }

            
            inGame.OnVictoryFreeplayButton(screen);
        }

        // switching round source

        private static bool EnterFreeplay()
        {
            Game game = Game.instance;
            if (game == null || game.UnityToSimulation == null)
            {
                MelonLogger.Error("[freeplay] Game / UnityToSimulation not available.");
                return false;
            }

            Simulation sim = game.UnityToSimulation.simulation;
            if (sim == null)
            {
                MelonLogger.Error("[freeplay] Simulation not available.");
                return false;
            }

            string mode = roundSource.Value == null ? "" : roundSource.Value.Trim();

            if (mode.Equals("Normal", StringComparison.OrdinalIgnoreCase))
            {
                loopNormalRounds = true;
                freeplayActive = true;
                MelonLogger.Msg($"[freeplay] Normal freeplay from round {sim.CurrentRound}: continuing through the map's full round list, " +
                                $"then looping its last {loopWindow.Value} rounds.");
                return true;
            }

            FreeplayTheme theme = FindByName<FreeplayTheme>(themeName.Value);
            if (theme == null) theme = game.freeplayTheme;
            RoundSetData mods = FindByName<RoundSetData>(roundSetDataName.Value);
            if (mods == null) mods = game.freeplayDifficultyMods;

            if (theme == null)
            {
                MelonLogger.Error("[freeplay] No FreeplayTheme found (Game.freeplayTheme is null).");
                return false;
            }

            GameModel model = sim.model;
            int seed = model.freeplayDefaultSeed;
            float difficulty = game.model.freeplayDifficulty;

            int roundBefore = sim.CurrentRound;
            sim.InitialiseFreeplay(model, seed, difficulty, theme, mods);
            if (sim.CurrentRound != roundBefore)
            {
                sim.CurrentRound = roundBefore;
            }

            loopNormalRounds = false;
            freeplayActive = true;
            MelonLogger.Msg($"[freeplay] Martian freeplay: theme={theme.name}, mods={(mods == null ? "none" : mods.name)}, " +
                            $"seed={seed}, difficulty={difficulty}, continuing from round {roundBefore}.");
            return true;
        }

        [HarmonyPatch(typeof(Simulation), nameof(Simulation.HasFinishedGame))]
        public static class HasFinishedGamePatch
        {
            public static bool Prefix(ref bool __result)
            {
                if (!freeplayActive) return true;
                __result = false;
                return false;
            }
        }

        [HarmonyPatch(typeof(RoundSetRoundManager), nameof(RoundSetRoundManager.CreateRound))]
        public static class CreateRoundPatch
        {
            public static void Prefix(RoundSetRoundManager __instance, ref int roundNumber)
            {
                if (!freeplayActive || !loopNormalRounds) return;

                var set = __instance.roundSet;
                if (set == null || set.rounds == null) return;
                int total = set.rounds.Length;
                normalRoundCount = total;
                if (total <= 0 || roundNumber <= total) return;

                if (roundNumber != lastLoggedRound)
                {
                    lastLoggedRound = roundNumber;
                    int past = roundNumber - total;
                    MelonLogger.Msg($"[freeplay] Round {roundNumber} ({past} past the end): " +
                                    $"health x{HealthFactor(past, false):F2}, MOAB health x{HealthFactor(past, true):F2}, " +
                                    $"speed x{SpeedFactor(past):F2}");
                }

                int window = Math.Max(1, Math.Min(loopWindow.Value, total));
                int loopFirst = total - window + 1;   // first round number in the loop
                roundNumber = loopFirst + ((roundNumber - total - 1) % window);
            }
        }

        // loop difficulty scaling

        private static int RoundsPastEnd(Bloon bloon)
        {
            if (!freeplayActive || !loopNormalRounds || normalRoundCount <= 0) return 0;
            Simulation sim = bloon.Sim;
            if (sim == null) return 0;
            int past = sim.CurrentRound - normalRoundCount;
            return past > 0 ? past : 0;
        }

        private static float HealthFactor(int past, bool moab)
        {
            float per = moab ? moabHealthPerRound.Value : healthPerRound.Value;
            return Math.Max(1f, 1f + per * past);
        }

        private static float SpeedFactor(int past)
        {
            return Math.Min(Math.Max(1f, maxSpeedMultiplier.Value), Math.Max(1f, 1f + speedPerRound.Value * past));
        }

        [HarmonyPatch(typeof(Bloon), nameof(Bloon.ResetTotalHealth))]
        public static class ResetTotalHealthPatch
        {
            public static void Postfix(Bloon __instance)
            {
                int past = RoundsPastEnd(__instance);
                if (past <= 0) return;
                BloonModel model = __instance.bloonModel;
                bool moab = model != null && model.UpdateMoab(false);
                __instance.totalHealth *= HealthFactor(past, moab);
            }
        }

        [HarmonyPatch(typeof(Bloon), nameof(Bloon.Initialise))]
        public static class BloonInitialisePatch
        {
            public static void Postfix(Bloon __instance)
            {
                int past = RoundsPastEnd(__instance);
                if (past <= 0) return;
                __instance.speedMultiplier *= SpeedFactor(past);
            }
        }

        // leaving freeplay
        [HarmonyPatch(typeof(InGame), nameof(InGame.StartMatch))]
        public static class StartMatchPatch
        {
            public static void Postfix()
            {
                if (freeplayActive) MelonLogger.Msg("[freeplay] New match: freeplay off.");
                freeplayActive = false;
                loopNormalRounds = false;
                normalRoundCount = 0;
                lastLoggedRound = -1;
            }
        }

        // asset discovery

        private static T FindByName<T>(string name) where T : UnityEngine.Object
        {
            if (string.IsNullOrWhiteSpace(name)) return null;
            var all = Resources.FindObjectsOfTypeAll(Il2CppType.Of<T>());
            for (int i = 0; i < all.Length; i++)
            {
                UnityEngine.Object obj = all[i];
                if (obj != null && obj.name == name) return obj.Cast<T>();
            }
            MelonLogger.Warning($"[freeplay] No loaded {typeof(T).Name} named '{name}', using default.");
            return null;
        }

        // writes every round set's per-difficulty window and modifiers to UserData/freeplayBATTD_roundsets.txt
        private static void DumpDifficultyBoundsToFile(Il2CppInterop.Runtime.InteropTypes.Arrays.Il2CppReferenceArray<UnityEngine.Object> sets)
        {
            var sb = new StringBuilder(64 * 1024);
            for (int i = 0; i < sets.Length; i++)
            {
                RoundSetData data = sets[i].Cast<RoundSetData>();
                int rounds = (data.roundSet != null && data.roundSet.rounds != null) ? data.roundSet.rounds.Count : -1;
                sb.AppendLine($"=== {data.name}  rounds={rounds}");
                if (data.difficultyBounds == null) continue;

                for (int b = 0; b < data.difficultyBounds.Count; b++)
                {
                    DifficultyBoundData bound = data.difficultyBounds[b];
                    if (bound == null) continue;
                    int modCount = bound.difficultyModifiers != null ? bound.difficultyModifiers.Count : 0;
                    sb.AppendLine($"  {bound.difficulty,-11} rounds {bound.start}-{bound.end} (StartRound={bound.start}, EndRound={bound.end + 1}) " +
                                  $"cash={bound.startingCash} hp={bound.startingHealth} speed={bound.speedMultiplier} " +
                                  $"sell={bound.sellMultiplier} roundCash={bound.roundCash}x{bound.roundCashMultiple} " +
                                  $"allowFreeplay={bound.allowFreeplay} modifiers={modCount}");

                    for (int m = 0; m < modCount; m++)
                    {
                        DifficultyModifierData mod = bound.difficultyModifiers[m];
                        if (mod == null) continue;
                        int groups = mod.bloonGroups != null ? mod.bloonGroups.Count : 0;
                        sb.AppendLine($"      mod idx {mod.startRound}-{mod.endRound} skip={mod.skipRound} wave={(mod.allWaves ? "all" : mod.waveIndex.ToString())} " +
                                      $"fort={mod.fortifyRoundChance} camo={mod.camoRoundChance} regen={mod.regenRoundChance} " +
                                      $"promote={mod.promoteBloonsChance} zombie={mod.zombieBloonChance} ghost={mod.ghostBloonChance} " +
                                      $"ghostMoab={mod.ghostMoabChance} shield={mod.shieldBloonChance}/{mod.shieldHp}hp " +
                                      $"replaceGroups={mod.replaceGroups} groups={groups}");
                    }
                }
            }

            string path = Path.Combine(MelonLoader.Utils.MelonEnvironment.UserDataDirectory, "freeplayBATTD_roundsets.txt");
            File.WriteAllText(path, sb.ToString());
            MelonLogger.Msg($"[freeplay] Difficulty windows for {sets.Length} round sets written to {path}");
        }

        private static void DumpRoundAssets()
        {
            var themes = Resources.FindObjectsOfTypeAll(Il2CppType.Of<FreeplayTheme>());
            MelonLogger.Msg($"[freeplay] FreeplayTheme assets loaded: {themes.Length}");
            for (int i = 0; i < themes.Length; i++)
            {
                MelonLogger.Msg($"  theme '{themes[i].name}'");
            }

            var sets = Resources.FindObjectsOfTypeAll(Il2CppType.Of<RoundSetData>());
            MelonLogger.Msg($"[freeplay] RoundSetData assets loaded: {sets.Length}");
            for (int i = 0; i < sets.Length; i++)
            {
                RoundSetData data = sets[i].Cast<RoundSetData>();
                int rounds = (data.roundSet != null && data.roundSet.rounds != null) ? data.roundSet.rounds.Count : -1;
                int bounds = data.difficultyBounds != null ? data.difficultyBounds.Count : -1;
                MelonLogger.Msg($"  roundSetData '{data.name}' rounds={rounds} difficultyBounds={bounds}");
            }

            DumpDifficultyBoundsToFile(sets);

            Game game = Game.instance;
            if (game != null)
            {
                MelonLogger.Msg($"[freeplay] Game defaults: theme='{(game.freeplayTheme == null ? "null" : game.freeplayTheme.name)}' " +
                                $"mods='{(game.freeplayDifficultyMods == null ? "null" : game.freeplayDifficultyMods.name)}'");
            }
        }
    }
}
