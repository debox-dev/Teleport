using System;
using DeBox.Teleport.Logging;

namespace DeBox.Teleport.Unity
{
    public class UnityTeleportLogger : BaseTeleportLogger
    {
        public override void LogInternal(LoggingLevelType levelType, string text, params object[] param)
        {
            switch (levelType)
            {
                case LoggingLevelType.Debug:
                case LoggingLevelType.Info:
                    UnityEngine.Debug.Log(string.Format(text, param));
                    return;
                case LoggingLevelType.Warn:
                    UnityEngine.Debug.LogWarning(string.Format(text, param));
                    return;
                case LoggingLevelType.Error:
                    UnityEngine.Debug.LogError(string.Format(text, param));
                    return;
                default:
                    Error("UnityTeleportLogger: Unknown logging level type: %s", levelType);
                    return;
            }
        }

        public override void Log(Exception e)
        {
            UnityEngine.Debug.LogException(e);
        }
    }
}


