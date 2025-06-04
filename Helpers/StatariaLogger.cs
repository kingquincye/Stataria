using System;
using System.IO;
using Terraria;
using Terraria.ModLoader;
using Terraria.ID;
using Steamworks;

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

            string header = $"=== {modName} Log Started at {DateTime.Now} ===\n";
            File.WriteAllText(logFilePath, header);

            initialized = true;

            if (isAdmin)
            {
                Info("Logging enabled for admin user");
            }
            else if (GlobalDebugMode)
            {
                Info("Logging enabled via debug mode");
            }
            else if (isServer)
            {
                Info("Logging enabled for server");
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
                    System.Diagnostics.Debug.WriteLine($"Failed to check/clear log file: {ex.Message}");
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
                WriteLog($"[INFO] {message}");
            }
        }

        public static void Warning(string message)
        {
            if (isAdmin || GlobalDebugMode || isServer)
            {
                WriteLog($"[WARNING] {message}");
            }
        }

        public static void Error(string message)
        {
            if (isAdmin || GlobalDebugMode || isServer)
            {
                WriteLog($"[ERROR] {message}");
            }
        }

        public static void Debug(string message)
        {
            if (isAdmin || GlobalDebugMode || isServer)
            {
                WriteLog($"[DEBUG] {message}");
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
                ModContent.GetInstance<Stataria>().Logger.Error($"Failed to write to log file: {ex.Message}");
            }
        }

        public static bool IsLoggingActive()
        {
            return initialized && (isAdmin || GlobalDebugMode || isServer);
        }
    }
}