using MelonLoader;
using HarmonyLib;
using Il2CppNinjakiwi.Players.Files;

[assembly: MelonInfo(typeof(AppIDPasswordTest.Core), "AppIDPasswordTest", "1.0.0", "xenia617", null)]
[assembly: MelonGame("Ninja Kiwi", "Bloons Adventure Time TD")]

namespace AppIDPasswordTest
{
    public class Core : MelonMod
    {
        public override void OnUpdate()
        {

        }

        [HarmonyPatch(typeof(PasswordGenerator), nameof(PasswordGenerator.GetPassword))]
        public class PasswordPatch
        {
            [HarmonyPostfix]
            public static void Postfix(PasswordGenerator __instance)
            {
                //MelonLogger.Msg("idk patched maybe");

                StringAndVersion strandver = __instance.GetLatestPassword();
                string value = strandver.Value;
                string version = strandver.Version.ToString();
                MelonLogger.Msg("Value: " + value);
                MelonLogger.Msg("Version: " + version);

                string password = __instance.GetPassword(strandver.Version);
                MelonLogger.Msg(password);
                
            }
        }
    }
}