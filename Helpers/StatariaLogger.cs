using System;
using System.IO;
using Terraria;
using Terraria.ModLoader;
using Terraria.ID;
using Steamworks;
using Terraria.Localization;

namespace Stataria
{
    public class StatariaLogger
    {
        private static string logFilePath;
        private static bool initialized = false;
        public static bool GlobalDebugMode = false;
        private static bool isServer = false;
        private const string AdminSteamID = ""; // your steamID
        private static bool isAdmin = false;

        public static void Initialize(Mod mod)
        {
            CheckAdminStatus();
            isServer = Main.netMode == NetmodeID.Server;

            if (isAdmin || GlobalDebugMode || isServer)
            {
                InitializeLogger(mod);
            }
        }

        private static void CheckAdminStatus()
        {
            try
            {
                if (!Main.dedServ)
                {
                    if (SteamUser.BLoggedOn())
                    {
                        var steamId = SteamUser.GetSteamID();
                        isAdmin = (steamId.m_SteamID.ToString() == AdminSteamID);
                    }
                }
                isServer = Main.netMode == NetmodeID.Server;
            }
            catch (Exception)
            {
                isAdmin = false;
            }
        }

        private static void InitializeLogger(Mod mod)
        {
            if (initialized)
                return;

            string modName = mod.Name;
            string saveDir = Path.Combine(Main.SavePath, "Mods", modName);

            if (!Directory.Exists(saveDir))
                Directory.CreateDirectory(saveDir);

            logFilePath = Path.Combine(saveDir, $"{modName}_log.txt");

            ClearLogIfNeeded();

            string header = Language.GetTextValue("Mods.Stataria.Logger.LogStarted", modName, DateTime.Now) + "\n";
            File.WriteAllText(logFilePath, header);

            initialized = true;

            if (isAdmin)
            {
                Info(Language.GetTextValue("Mods.Stataria.Logger.EnabledAdmin"));
            }
            else if (GlobalDebugMode)
            {
                Info(Language.GetTextValue("Mods.Stataria.Logger.EnabledDebug"));
            }
            else if (isServer)
            {
                Info(Language.GetTextValue("Mods.Stataria.Logger.EnabledServer"));
            }
        }

        private static void ClearLogIfNeeded()
        {
            const long MAX_LOG_SIZE = 1024 * 1024;

            if (File.Exists(logFilePath))
            {
                try
                {
                    FileInfo fileInfo = new FileInfo(logFilePath);
                    if (fileInfo.Length > MAX_LOG_SIZE)
                    {
                        string backupPath = logFilePath + ".old";
                        if (File.Exists(backupPath))
                            File.Delete(backupPath);

                        File.Move(logFilePath, backupPath);

                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine(Language.GetTextValue("Mods.Stataria.Logger.ClearLogFailed", ex.Message));
                }
            }
        }

        public static void UpdateDebugMode(Mod mod, bool debugModeEnabled)
        {
            bool wasDebugMode = GlobalDebugMode;
            GlobalDebugMode = debugModeEnabled;

            CheckAdminStatus();

            isServer = Main.netMode == NetmodeID.Server;

            if ((GlobalDebugMode || isAdmin || isServer) && !initialized)
            {
                InitializeLogger(mod);
            }
            else if (!GlobalDebugMode && !isAdmin && !isServer && initialized)
            {
                initialized = false;
            }
        }

        public static void Info(string message)
        {
            if (isAdmin || GlobalDebugMode || isServer)
            {
                WriteLog(Language.GetTextValue("Mods.Stataria.Logger.Info", message));
            }
        }

        public static void Warning(string message)
        {
            if (isAdmin || GlobalDebugMode || isServer)
            {
                WriteLog(Language.GetTextValue("Mods.Stataria.Logger.Warning", message));
            }
        }

        public static void Error(string message)
        {
            if (isAdmin || GlobalDebugMode || isServer)
            {
                WriteLog(Language.GetTextValue("Mods.Stataria.Logger.Error", message));
            }
        }

        public static void Debug(string message)
        {
            if (isAdmin || GlobalDebugMode || isServer)
            {
                WriteLog(Language.GetTextValue("Mods.Stataria.Logger.Debug", message));
            }
        }

        private static void WriteLog(string logMessage)
        {
            if (!initialized || (!isAdmin && !GlobalDebugMode && !isServer))
                return;

            try
            {
                string formattedMessage = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] {logMessage}";

                using (StreamWriter writer = File.AppendText(logFilePath))
                {
                    writer.WriteLine(formattedMessage);
                }
            }
            catch (Exception ex)
            {
                ModContent.GetInstance<Stataria>().Logger.Error(Language.GetTextValue("Mods.Stataria.Logger.WriteLogFailed", ex.Message));
            }
        }

        public static bool IsLoggingActive()
        {
            return initialized && (isAdmin || GlobalDebugMode || isServer);
        }
    }
}