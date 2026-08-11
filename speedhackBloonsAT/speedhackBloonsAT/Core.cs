using MelonLoader;
using UnityEngine;
using Il2CppAssets.Scripts;

[assembly: MelonInfo(typeof(speedhackBloonsAT.Core), "speedhackBloonsAT", "1.0.0", "xenia617", null)]
[assembly: MelonGame("Ninja Kiwi", "Bloons Adventure Time TD")]

namespace speedhackBloonsAT
{
    public class Core : MelonMod
    {
        public static float speed = 0.02f;
        public static float fasterSpeed = 0.033333336f;
        public static float evenFasterSpeed = 0.08333334f;
        public static float normalAmount = 0.016666668f;
        public override void OnInitializeMelon()
        {
            LoggerInstance.Msg("Initialized SpeedhackBATTD.");
        }

        public override void OnUpdate()
        {
            base.OnUpdate();

            //debug key (for testing and checking values)
            if (Input.GetKeyDown(KeyCode.F8))
            {
                MelonLogger.Msg("FFTimeScale: " + Constants.fastFowardTimeScale);
                MelonLogger.Msg("RegTimeScale: " + Constants.regularTimeScale);
                MelonLogger.Msg("fixedUpdateTime: " + Constants.fixedUpdateTime);
            }

            // 2x speed key
            if (Input.GetKeyDown(KeyCode.F2))
            {
                speed = fasterSpeed;
                Constants.fastFowardTimeScale = speed;
                MelonLogger.Msg("Speed set to 2x faster");
            }

            // 5x speed key
            if (Input.GetKeyDown(KeyCode.F3))
            {
                speed = evenFasterSpeed;
                Constants.fastFowardTimeScale = speed;
                MelonLogger.Msg("Speed set to 5x faster");
            }

            // Normal FF speed key
            if (Input.GetKeyDown(KeyCode.F1))
            {
                speed = normalAmount;
                Constants.fastFowardTimeScale = speed;
                MelonLogger.Msg("Speed set to normal speed.");
            }
        }
    }
}