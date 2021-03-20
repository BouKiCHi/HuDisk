using System;
using System.Collections.Generic;

namespace Disk {

    public enum LogType {
        Error,
        Warning,
        Info,
        Debug,
        Verbose,
    }

    public class LogData {
        public LogType Type;
        public string Message;

        public LogData(LogType type, string message) {
            Type = type;
            Message = message;
        }

        public override string ToString() => $"{Message}";
    }
    public class Log { 
        List<LogData> Data = new List<LogData>();

        bool ShowInfo;
        bool ShowDebug;
        bool ShowError;
        bool ShowWarning;
        bool ShowVerbose;

        public Log() {
            ShowError = true;
            ShowWarning = true;
            ShowInfo = true;
        }

        public void SetQuite() {
            ShowError = false;
            ShowWarning = false;
            ShowInfo = false;
            ShowDebug = false;
        }

        public void SetVerbose() {
            ShowVerbose = true;
        }

        public void Info(string v) {
            AddLog(LogType.Info, v);
        }
        public void Error(string v) {
            AddLog(LogType.Error, v);
        }

        public void Warning(string v) {
            AddLog(LogType.Warning, v);
        }

        public void Verbose(string v) {
            AddLog(LogType.Verbose, v);
        }



        public bool IsShow(LogType Type) {
            switch (Type) {
                case LogType.Error:
                    return ShowError;
                case LogType.Warning:
                    return ShowWarning;

                case LogType.Info:
                    return ShowInfo;
                case LogType.Debug:
                    return ShowDebug;

                case LogType.Verbose:
                    return ShowVerbose;
            }
            return false;
        }


        public void AddLog(LogType Type, string Message) {
            var m = new LogData(Type, Message);
            Data.Add(m);
            ShowLog(m);
        }

        private void ShowLog(LogData m) {
            if (IsShow(m.Type)) Console.WriteLine(m);
        }

    }
}
