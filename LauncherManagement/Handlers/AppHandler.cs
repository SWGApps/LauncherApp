﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace LauncherManagement
{
    public class AppHandler
    {
        static readonly bool _cuClient = false;

        public static async Task StartGameAsync(string serverPath, string password = "", 
            string username = "", string charactername = "", bool autoEnterZone = false)
        {
            // Run Async to prevent launcher from locking up when starting game and writing bytes
            await Task.Run(async () =>
            {
                try
                {
                    bool configWritten = await WriteLoginConfigAsync();
                    await WriteLauncherConfigAsync();
                    await WriteLiveConfigAsync();

                    if (configWritten)
                    {
                        if (!_cuClient)
                        {
                            // Read SWGEmu.exe and seek to 0x1153
                            string exePath = Path.Join(serverPath, "SWGEmu.exe");
                            FileStream rs = File.OpenRead(exePath);
                            rs.Seek(0x1153, SeekOrigin.Begin);

                            // Convert FPS integer (casted to float) to hex
                            string hexSelection = BitConverter.ToString(
                                BitConverter.GetBytes((float)GameOptionsProperties.Fps)).Replace("-", "");

                            // Read the next 3 bytes to ensure we're at the right position and 
                            // the binary hasn't been altered
                            if (rs.ReadByte() == 199 && rs.ReadByte() == 69 && rs.ReadByte() == 148)
                            {
                                string hex = "";

                                // Get hex of next 4 bytes at 0x1153 (float)
                                for (int i = 0; i <= 3; i++)
                                {
                                    byte[] b = { (byte)rs.ReadByte() };
                                    hex += BitConverter.ToString(b).Replace("-", "");
                                }

                                // Close Read to prepare for write
                                rs.Dispose();

                                // If the selected FPS already matches hex value in the 
                                // binary, don't write to it again (faster loading)
                                if (hex != hexSelection)
                                {
                                    using FileStream ws = File.OpenWrite(exePath);
                                    ws.Seek(0x1156, SeekOrigin.Begin);

                                    // Create byte array of FPS value
                                    byte[] bytes = BitConverter.GetBytes((float)GameOptionsProperties.Fps);

                                    // Write FPS float at 0x1156
                                    foreach (byte b in bytes)
                                    {
                                        ws.WriteByte(b);
                                    }
                                }
                            }

                            var startInfo = new ProcessStartInfo();

                            if (autoEnterZone)
                            {
                                startInfo.Arguments = $"-- -s ClientGame loginClientPassword={password} autoConnectToLoginServer=1 loginClientID={username} avatarName={charactername} autoConnectToGameServer=1 -s Station -s SwgClient allowMultipleInstances=true";
                            }
                            else
                            {
                                startInfo.Arguments = $"-- -s ClientGame loginClientPassword={password} autoConnectToLoginServer=1 loginClientID={username} autoConnectToGameServer=0 -s Station -s SwgClient allowMultipleInstances=true";
                            }

                            startInfo.EnvironmentVariables["SWGCLIENT_MEMORY_SIZE_MB"] = GameOptionsProperties.Ram.ToString();
                            startInfo.UseShellExecute = false;
                            startInfo.WorkingDirectory = serverPath;
                            startInfo.FileName = Path.Join(serverPath, "SWGEmu.exe");

                            Process.Start(startInfo);
                        }
                        else
                        {
                            string exePath = Path.Join(serverPath, "SwgClient_r.exe");

                            ProcessStartInfo startInfo = new();
                            startInfo.EnvironmentVariables["SWGCLIENT_MEMORY_SIZE_MB"] = GameOptionsProperties.Ram.ToString();
                            startInfo.UseShellExecute = false;
                            startInfo.WorkingDirectory = serverPath;
                            startInfo.FileName = Path.Join(serverPath, "SwgClient_r.exe");

                            Process.Start(startInfo);
                        }
                    }
                    else
                    {
                        Trace.WriteLine("Error writing to login.cfg!");
                    }
                }
                catch (Exception e)
                {
                    Trace.WriteLine(e.Message.ToString());
                }
            });
        }

        public static void WriteMissingConfigs(string gameLocation)
        {
            string[] configs =
            {
                "user.cfg",
                "launcher.cfg"
            };

            foreach (string config in configs)
            {
                string filePath = Path.Join(gameLocation, config);
                if (!File.Exists(filePath))
                {
                    StreamWriter sw = new(filePath);
                    sw.Write("");
                }
            }
        }

        public static async Task<bool> WriteConfigAsync(string file, string text)
        {
            SettingsHandler _settingsHandler = new();
            string gameLocation = await _settingsHandler.GetGameLocationAsync();

            if (!string.IsNullOrEmpty(gameLocation))
            {
                string cfg = "";

                switch (file)
                {
                    case "login": cfg = _cuClient ? "login.cfg" : "swgemu_login.cfg"; break;
                    case "live": cfg = _cuClient ? "live.cfg" : "swgemu_live.cfg"; break;
                    case "launcher": cfg = "launcher.cfg"; break;
                    default:
                        break;
                }

                string filePath = Path.Join(gameLocation, cfg);

                new FileInfo(filePath).Directory.Create();

                try
                {
                    using StreamWriter sw = new(filePath);
                    await sw.WriteAsync(text);

                    return true;
                }
                catch (Exception e)
                {
                    Trace.WriteLine(e.Message);
                }
            }

            return false;
        }

        static async Task WriteLauncherConfigAsync()
        {
            SettingsHandler _settingsHandler = new();

            bool admin = await _settingsHandler.GetAdminAsync();
            bool debugExamine = await _settingsHandler.GetDebugExamineAsync();

            string cfgText = $"[SwgClient]\n" +
                "\tallowMultipleInstances=true\n\n" +
                "[ClientGame]\n" +
                $"\t0fd345d9={admin.ToString().ToLower()}\n\n" +
                "[ClientUserInterface]\n" +
                $"\tdebugExamine={debugExamine.ToString().ToLower()}";

            await WriteConfigAsync("launcher", cfgText);
        }

        static async Task WriteLiveConfigAsync()
        {
            List<string> treList = await DownloadHandler.DownloadTreList();

            StringBuilder sb = new();

            string header = "[SharedFile]\n" +
                "\tmaxSearchPriority=99\n";

            sb.Append(header);

            TreModHandler treModHandler = new();
            Dictionary<string, List<string>> modList = await treModHandler.GetMods();

            int count = treList.Count;
            int modFileCount = 0;

            foreach (KeyValuePair<string, List<string>> mod in modList)
            {
                modFileCount += mod.Value.Count;
            }

            modFileCount += count;

            foreach (KeyValuePair<string, List<string>> mod in modList)
            {
                mod.Value.Reverse();

                foreach (string treFile in mod.Value)
                {
                    sb.Append($"\tsearchTree_00_{modFileCount}={treFile}\n");
                    modFileCount -= 1;
                }
            }

            foreach (string treFile in treList)
            {
                sb.Append($"\tsearchTree_00_{count}={treFile}\n");
                count -= 1;
            }

            string footer = "\n[SharedNetwork]\n" +
                "\tnetworkHandlerDispatchThrottle=true\n\n" +
                "[ClientUserInterface]\n" +
                "\tmessageOfTheDayTable=live_motd\n\n" +
                "[SwgClientUserInterface/SwgCuiService]\n" +
                "\tknownIssuesArticle=10424\n\n" +
                "[Station]\n" +
                "\tsubscriptionFeatures=1\n" +
                "\tgameFeatures=65535\n";

            sb.Append(footer);

            await WriteConfigAsync("live", sb.ToString());
        }

        static async Task<bool> WriteLoginConfigAsync()
        {
            SettingsHandler settingsHandler = new();
            LauncherConfigHandler configHandler = new();
            Dictionary<string, string> gameSettings = await settingsHandler.GetGameOptionsControls();
            Dictionary<string, string> settings = await configHandler.GetLauncherSettings();

            settings.TryGetValue("SWGLoginHost", out string host);
            settings.TryGetValue("SWGLoginPort", out string port);
            gameSettings.TryGetValue("MaxZoom", out string maxZoom);

            string cfgText = $"[ClientGame]\n" +
                $"loginServerAddress0={host}\n" +
                $"loginServerPort0={port}\n" +
                $"freeChaseCameraMaximumZoom={maxZoom}";

            return await WriteConfigAsync("login", cfgText);
        }

        public static void StartGameConfig(string serverPath)
        {
            try
            {
                ProcessStartInfo startInfo = new();

                if (!_cuClient)
                {
                    startInfo.UseShellExecute = true;
                    startInfo.WorkingDirectory = serverPath;
                    startInfo.FileName = Path.Join(serverPath, "SWGEmu_Setup.exe");
                }
                else
                {
                    startInfo.UseShellExecute = true;
                    startInfo.WorkingDirectory = serverPath;
                    startInfo.FileName = Path.Join(serverPath, "SwgClientSetup_r.exe");
                }

                Process.Start(startInfo);
            }
            catch
            { }
        }

        public static void OpenDefaultBrowser(string url)
        {
            Process myProcess = new();

            try
            {
                myProcess.StartInfo.UseShellExecute = true;
                myProcess.StartInfo.FileName = url;
                myProcess.Start();
            }
            catch (Exception e)
            {
                Trace.WriteLine(e.Message);
            }
        }
    }
}
