using System;
namespace DeBox.Teleport.Logging
{
    public enum LoggingLevelType
    {
        Debug = 0,
        Info = 1,
        Warn = 2,
        Error = 3,
        None = 4,
    }

    public interface ITeleportLogger
    {
        void Log(LoggingLevelType levelType, string text, params object[] param);
        void Log(Exception e);
    }

    public abstract class BaseTeleportLogger : ITeleportLogger
    {
        public LoggingLevelType Level { get; private set; }

        public void SetLevel(LoggingLevelType level) => Level = level;

        public void Debug(string text, params object[] param) => Log(LoggingLevelType.Debug, text, param);
        public void Info(string text, params object[] param) => Log(LoggingLevelType.Info, text, param);
        public void Error(string text, params object[] param) => Log(LoggingLevelType.Error, text, param);
        public void Exception(Exception e) => Log(e);

        public abstract void LogInternal(LoggingLevelType levelType, string text, params object[] param);

        public void Log(LoggingLevelType level, string text, params object[] param)
        {
            if ((int)level >= (int)this.Level)
            {
                LogInternal(level, text, param);
            }
        }
        public abstract void Log(Exception e);
    }
}
