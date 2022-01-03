﻿namespace LauncherManagement
{
    public class SettingsHandler : DatabaseHandler
    {
        readonly string _defaultGameLocation = "";
        readonly string _defaultServername = "SWGLegacy";
        readonly int _defaultAutoLogin = 0;
        readonly int _defaultVerified = 0;
        readonly int _defaultFps = 60;
        readonly int _defaultRam = 2048;
        readonly int _defaultMaxZoom = 5;
        readonly int _defaultAdmin = 0;
        readonly int _defaultDebugExamine = 0;
        readonly int _defaultReshade = 0;
        readonly int _defaultHDTextures = 0;

        public async Task<string> GetServerNameAsync()
        {
            List<DatabaseProperties.Settings>? config = await ExecuteSettingsAsync
                (
                    "SELECT ServerName " +
                    "FROM Settings " +
                    "where Id = 1;"
                );

            return (config is not null && config.Count > 0) ? config[0].ServerName ?? "" : "";
        }

        public async Task SetGameLocationAsync(string gamePath)
        {
            await ExecuteSettingsAsync
                (
                    "UPDATE Settings SET " +
                    $"GameLocation = '{gamePath}' " +
                    "where Id = 1;"
                );
        }

        public async Task<string> GetGameLocationAsync()
        {
            List<DatabaseProperties.Settings>? config = await ExecuteSettingsAsync
                (
                    "SELECT GameLocation " +
                    "FROM Settings " +
                    "where Id = 1;"
                );

            return (config is not null && config.Count > 0) ? config[0].GameLocation ?? "" : "";
        }

        public async Task GetGameOptions()
        {

            List<DatabaseProperties.Settings>? config = await ExecuteSettingsAsync
                (
                    "SELECT * " +
                    "FROM Settings;"
                );

            GameOptionsProperties.Fps = config![0].Fps;
            GameOptionsProperties.Ram = config[0].Ram;
            GameOptionsProperties.MaxZoom = config[0].MaxZoom;
        }

        public async Task SetGameOptions(DatabaseProperties.Settings settings)
        {
            await ExecuteSettingsAsync
                (
                    "UPDATE Settings SET " +
                    $"Fps = {settings.Fps}, Ram = {settings.Ram}, MaxZoom = {settings.MaxZoom};"
                );
        }

        public async Task<Dictionary<string, string>> GetGameOptionsControls()
        {
            List<DatabaseProperties.Settings>? config = await ExecuteSettingsAsync
                (
                    "SELECT * " +
                    "FROM Settings;"
                );

            return new Dictionary<string, string>
            {
                { "GameLocation", config![0].GameLocation ?? "" },
                { "ServerName", config[0].ServerName ?? "" },
                { "AutoLogin", config[0].AutoLogin.ToString() },
                { "Verified", config[0].Verified.ToString() },
                { "Fps", config[0].Fps.ToString() },
                { "Ram", config[0].Ram.ToString() },
                { "MaxZoom", config[0].MaxZoom.ToString() },
            };
        }

        public async Task<bool> CheckAutoLoginEnabledAsync()
        {
            List<DatabaseProperties.Settings>? config = await ExecuteSettingsAsync
                (
                    "SELECT AutoLogin " +
                    "FROM Settings " +
                    "where Id = 1;"
                );

            return config![0].AutoLogin;
        }

        public async Task ToggleAutoLoginAsync(bool flag)
        {
            int val;

            switch (flag)
            {
                case true: val = 1; break;
                case false: val = 0; break;
            }

            await ExecuteSettingsAsync
                (
                    "UPDATE Settings " +
                    $"SET AutoLogin = {val} " +
                    "where Id = 1;"
                );
        }

        public async Task SetVerifiedAsync()
        {
            await ExecuteSettingsAsync
                (
                    "UPDATE Settings " +
                    "SET Verified = 1 " +
                    "where Id = 1;"
                );
        }

        public async Task<bool> GetVerifiedAsync()
        {
            List<DatabaseProperties.Settings>? config = await ExecuteSettingsAsync
                (
                    "SELECT Verified " +
                    "FROM Settings " +
                    "where Id = 1;"
                );

            return config![0].Verified;
        }

        public async Task ToggleSettingsAsync(string type, bool flag)
        {
            int val;

            switch (flag)
            {
                case true: val = 1; break;
                case false: val = 0; break;
            }

            await ExecuteSettingsAsync
                (
                    "UPDATE Settings " +
                    $"SET {type} = {val} " +
                    "where Id = 1;"
                );
        }

        public async Task<bool> GetAdminAsync()
        {
            List<DatabaseProperties.Settings>? config = await ExecuteSettingsAsync
                (
                    "SELECT Admin " +
                    "FROM Settings " +
                    "where Id = 1;"
                );

            return config![0].Admin == 1;
        }

        public async Task<bool> GetDebugExamineAsync()
        {
            List<DatabaseProperties.Settings>? config = await ExecuteSettingsAsync
                (
                    "SELECT DebugExamine " +
                    "FROM Settings " +
                    "where Id = 1;"
                );

            return config![0].DebugExamine == 1;
        }

        public async Task<bool> GetReshadeAsync()
        {
            List<DatabaseProperties.Settings>? config = await ExecuteSettingsAsync
                (
                    "SELECT Reshade " +
                    "FROM Settings " +
                    "where Id = 1;"
                );

            return config![0].Reshade == 1;
        }

        public async Task<bool> GetHDTexturesAsync()
        {
            List<DatabaseProperties.Settings>? config = await ExecuteSettingsAsync
                (
                    "SELECT HDTextures " +
                    "FROM Settings " +
                    "where Id = 1;"
                );

            return config![0].HDTextures == 1;
        }

        public async Task InsertDefaultRow()
        {
            List<DatabaseProperties.Settings>? config = await ExecuteSettingsAsync("SELECT * FROM Settings;");

            if (config is not null && config.Count < 1)
            {
                await ExecuteSettingsAsync
                    (
                        "INSERT INTO Settings " +
                        "(GameLocation, ServerName, AutoLogin, Verified, Fps, Ram, MaxZoom, Admin, DebugExamine, Reshade, HDTextures) " +
                        "VALUES " +
                        $"('{_defaultGameLocation}', '{_defaultServername}', {_defaultAutoLogin}, {_defaultVerified}, {_defaultFps}, {_defaultRam}, {_defaultMaxZoom}, {_defaultAdmin}, {_defaultDebugExamine}, {_defaultReshade}, {_defaultHDTextures});"
                    );
            }
        }
    }
}
