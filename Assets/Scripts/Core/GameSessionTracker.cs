using UnityEngine;

namespace Musashi.Core
{
    public static class GameSessionTracker
    {
        public static int CoinsObtained { get; set; } = 0;
        public static int CoinsSpent { get; set; } = 0;

        public static int ApplesObtained { get; set; } = 0;
        public static int ApplesUsed { get; set; } = 0;

        public static int ShurikensObtained { get; set; } = 0;
        public static int ShurikensUsed { get; set; } = 0;

        public static int SakeObtained { get; set; } = 0;
        public static int SakeUsed { get; set; } = 0;

        public static float BattleStartTime { get; private set; } = 0f;
        public static bool IsTracking { get; private set; } = false;

        public static void StartTracking()
        {
            if (!IsTracking)
            {
                BattleStartTime = Time.time;
                IsTracking = true;
            }
        }

        public static float GetElapsedTime()
        {
            if (!IsTracking) return 0f;
            return Mathf.Max(0f, Time.time - BattleStartTime);
        }

        public static string GetFormattedTime()
        {
            float elapsed = GetElapsedTime();
            int minutes = Mathf.FloorToInt(elapsed / 60f);
            int seconds = Mathf.FloorToInt(elapsed % 60f);
            return string.Format("{0:00}:{1:00}", minutes, seconds);
        }

        public static void RegisterInitialInventory(int coins, int apples, int shurikens, int sake)
        {
            CoinsObtained = Mathf.Max(CoinsObtained, coins);
            ApplesObtained = Mathf.Max(ApplesObtained, apples);
            ShurikensObtained = Mathf.Max(ShurikensObtained, shurikens);
            SakeObtained = Mathf.Max(SakeObtained, sake);
            StartTracking();
        }

        public static void ResetSession()
        {
            CoinsObtained = 0;
            CoinsSpent = 0;
            ApplesObtained = 0;
            ApplesUsed = 0;
            ShurikensObtained = 0;
            ShurikensUsed = 0;
            SakeObtained = 0;
            SakeUsed = 0;
            BattleStartTime = Time.time;
            IsTracking = true;
        }
    }
}
