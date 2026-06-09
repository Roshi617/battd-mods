using Il2CppAssets.Scripts.Simulation;
using MelonLoader;
using HarmonyLib;

[assembly: MelonInfo(typeof(BonusEndOfRoundCashBATTD.Core), "BonusEndOfRoundCashBATTD", "1.0.0", "xenia617", null)]
[assembly: MelonGame("Ninja Kiwi", "Bloons Adventure Time TD")]

namespace BonusEndOfRoundCashBATTD
{
    public class Core : MelonMod
    {
        public static double cashIncrease(Simulation __instance)
        {
            Simulation.CashIncreaseReason test = Simulation.CashIncreaseReason.RoundBonus;
            double increaseCashAmount = 1000.0; //change this to your liking
            return __instance.IncreaseCash(increaseCashAmount, test);
        }

        public override void OnInitializeMelon()
        {
            LoggerInstance.Msg("BonusEndOfRoundCash mod loaded!");
        }

        [HarmonyPatch(typeof(Simulation), nameof(Simulation.GetEndRound))]
        public class SimulationOnRoundEndCompleted_Patch
        {
            [HarmonyPostfix]
            public static void Postfix(Simulation __instance)
            {
                cashIncrease(__instance);
            }
        }
    }
}