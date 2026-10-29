using HarmonyLib;
using Il2CppAssets.Scripts.Unity.Ads;
using Il2CppAssets.Scripts.Unity.Player;
using Il2CppAssets.Scripts.Unity.UI_New.GameOver;
using Il2CppAssets.Scripts.Unity.UI_New.InGame;
using MelonLoader;

[assembly: MelonInfo(typeof(AlwaysDoubleReward.Core), "AlwaysDoubleReward", "1.0.0", "xenia617", null)]
[assembly: MelonGame("Ninja Kiwi", "Bloons Adventure Time TD")]

namespace AlwaysDoubleReward
{
    public class Core : MelonMod
    {
        public override void OnInitializeMelon()
        {
            LoggerInstance.Msg("AlwaysDoubleReward mod loaded.");
        }

        [HarmonyPatch(typeof(NKAdHandler), nameof(NKAdHandler.CanWatchRewardAd))]
        public static class CanWatchRewardAdPatch
        {
            public static void Postfix(RewardAdvertGroup group, ref bool __result)
            {
                if (group == RewardAdvertGroup.DoubleReward && !__result)
                {
                    __result = true;
                    MelonLogger.Msg("[DoubleReward] daily limit bypassed");
                }
            }
        }

        [HarmonyPatch(typeof(InGame), nameof(InGame.OnVictoryDoubleRewardsButton))]
        public static class OnVictoryDoubleRewardsButtonPatch
        {
            public static void Prefix(InGame __instance)
            {
                MelonLogger.Msg($"[DoubleReward] button pressed, gem cost = {__instance.gemDoubleRewardCost}");
            }
        }

        [HarmonyPatch(typeof(AdventureTimePlayer), nameof(AdventureTimePlayer.TryUseGems))]
        public static class TryUseGemsPatch
        {
            public static void Postfix(int gemCost, string reason, bool __result)
            {
                if (reason == "DoubleRewardForGems")
                    MelonLogger.Msg($"[DoubleReward] TryUseGems({gemCost}) -> {(__result ? "paid" : "not enough gems, opening store")}");
            }
        }
    }
}
