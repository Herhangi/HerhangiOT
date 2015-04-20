using System;
using HerhangiOT.ServerLibrary.Utility;
using LuaInterface;
using System.Collections.Generic;

namespace HerhangiOT.ServerLibrary
{
    public enum ConfigInt
    {
        GameServerPort = 0,
        LoginServerPort, //+
        StatusServerPort, //+
        LoginServerSecretPort, //+

        MOTDNum, //+

        GameServerId, //+
        MaxPlayers, //-

        DatabaseMysqlPort, //-
        PzLocked, //-
        DefaultDespawnrange, //-
        DefaultDespawnradius, //-
        RateExperience, //-
        RateSkill, //-
        RateLoot, //-
        RateMagic, //-
        RateSpawn, //-
        HousePrice, //-
        KillsToRed, //-
        KillsToBlack, //-
        MaxMessageBuffer, //-
        ActionsDelayInterval, //-
        ExActionsDelayInterval, //-
        KickAfterMinutes, //-
        ProtectionLevel, //-
        DeathLosePercent, //-
        StatusQueryTimeout, //-
        FragTime, //-
        WhiteSkullTime, //-
        StairhopDelay, //-
        MarketOfferDuration, //-
        CheckExpiredMarketOffersEachMinutes, //-
        MaxMarketOffersAtATimePerPlayer, //-
        ExpFromPlayersLevelRange, //-
        MaxPacketsPerSecond, //-

        LastIntegerConfig
    }

    public enum ConfigStr
    {
        GameServerIP = 0,
        LoginServerIP,

        MOTD,
        DatabaseType,
        DatabaseMssqlConnectionString,

        DatabaseJsonAccountPath,
        DatabaseJsonCharacterPath,

        DatabaseMysqlHost,
        DatabaseMysqlUser,
        DatabaseMysqlPass,
        DatabaseMysqlDb,
        DatabaseMysqlSock,

        PasswordHashAlgorithm,
        GameServerSecret,

        MinConsoleLogLevel,

        MapName,
        MapAuthor,//-
        WorldType,
        DefaultPriority,
        HouseRentPeriod, //-

        StatusServerName, //-
        StatusOwnerName, //-
        StatusOwnerEmail, //-
        StatusUrl, //-
        StatusLocation,//-

        LastStringConfig
    }

    public enum ConfigBool
    {
        UseExternalLoginServer = 0,

        AllowChangeOutfit, //-
        AimbotHotkeyEnabled, //-
        RemoveAmmo, //-
        RemoveRuneCharges,//-
        ExperienceFromPlayers, //-
        FreePremium, //-
        ReplaceKickOnLogin, //-
        BindOnlyGlobalAddress, //+
        MarketPremium, //-
        EmoteSpells, //-
        StaminaSystem, //-

        LastBooleanConfig

        // DISABLED FEATURES - Might be implemented in the future
        //ALLOW_CLONES,
        //ONE_PLAYER_ON_ACCOUNT,
        //OPTIMIZE_DATABASE
        //WARN_UNSAFE_SCRIPTS,
        //CONVERT_UNSAFE_SCRIPTS,
    }

    public class ConfigManager
    {
        private static bool _isLoaded;
        private static string _lastLoadedFile;

        public int this[ConfigInt configName]
        {
            get { return IntConfigs[configName]; }
        }

        public bool this[ConfigBool configName]
        {
            get { return BoolConfigs[configName]; }
        }

        public string this[ConfigStr configName]
        {
            get { return StrConfigs[configName]; }
        }

        private static readonly Dictionary<ConfigInt, int> IntConfigs = new Dictionary<ConfigInt, int>();
        private static readonly Dictionary<ConfigBool, bool> BoolConfigs = new Dictionary<ConfigBool, bool>();
        private static readonly Dictionary<ConfigStr, string> StrConfigs = new Dictionary<ConfigStr, string>();

        public static event Action ConfigsLoaded;

        public static ConfigManager Instance { get; private set; }

        public static bool Load(string filename)
        {
            Logger.LogOperationStart("Loading configurations");
            _lastLoadedFile = filename;
            // Creating instance here instead of getter for performance gain
            if (Instance == null) Instance = new ConfigManager();

            Lua config = new Lua();
            try
            {
                config.DoFile(filename);
            }
            catch (Exception)
            {
                Logger.LogOperationFailed("ConfigManager could not load config.lua!");
                return false;
            }

            //info that must be loaded one time (unless we reset the modules involved)
            if (!_isLoaded)
            {
                BoolConfigs.Add(ConfigBool.BindOnlyGlobalAddress, ReadBoolFromConfig(config, "bindOnlyGlobalAddress", false));
                BoolConfigs.Add(ConfigBool.UseExternalLoginServer, ReadBoolFromConfig(config, "useExternalLoginServer", false));

                StrConfigs.Add(ConfigStr.DatabaseType, ReadStrFromConfig(config, "databaseType", "json"));

                switch (StrConfigs[ConfigStr.DatabaseType])
                {
                    case "mssql":
                        StrConfigs.Add(ConfigStr.DatabaseMssqlConnectionString, ReadStrFromConfig(config, "mssqlConnectionString"));
                        break;
                    case "json":
                        StrConfigs.Add(ConfigStr.DatabaseJsonAccountPath, ReadStrFromConfig(config, "jsonAccountPath", "Data/Account"));
                        StrConfigs.Add(ConfigStr.DatabaseJsonCharacterPath, ReadStrFromConfig(config, "jsonCharacterPath", "Data/Character"));
                        break;
                    case "mysql":
                        StrConfigs.Add(ConfigStr.DatabaseMysqlHost, ReadStrFromConfig(config, "mysqlHost", "127.0.0.1"));
                        StrConfigs.Add(ConfigStr.DatabaseMysqlUser, ReadStrFromConfig(config, "mysqlUser", "herhangi"));
                        StrConfigs.Add(ConfigStr.DatabaseMysqlPass, ReadStrFromConfig(config, "mysqlPass"));
                        StrConfigs.Add(ConfigStr.DatabaseMysqlDb, ReadStrFromConfig(config, "mysqlDatabase", "herhangi"));
                        StrConfigs.Add(ConfigStr.DatabaseMysqlSock, ReadStrFromConfig(config, "mysqlSock"));
                        IntConfigs.Add(ConfigInt.DatabaseMysqlPort, ReadIntFromConfig(config, "mysqlPort", 3306));
                        break;
                    default:
                        Logger.LogOperationFailed("config.lua contains invalid database type!");
                        return false;

                }

                StrConfigs.Add(ConfigStr.PasswordHashAlgorithm, ReadStrFromConfig(config, "passwordHashAlgorithm", "sha1"));
                StrConfigs.Add(ConfigStr.GameServerIP, ReadStrFromConfig(config, "gameServerIp", "127.0.0.1"));
                StrConfigs.Add(ConfigStr.LoginServerIP, ReadStrFromConfig(config, "loginServerIp", "127.0.0.1"));
                StrConfigs.Add(ConfigStr.MapName, ReadStrFromConfig(config, "mapName", "forgotten"));
                StrConfigs.Add(ConfigStr.MapAuthor, ReadStrFromConfig(config, "mapAuthor", "Unknown"));
                StrConfigs.Add(ConfigStr.HouseRentPeriod, ReadStrFromConfig(config, "houseRentPeriod", "never"));
                StrConfigs.Add(ConfigStr.GameServerSecret, ReadStrFromConfig(config, "gameServerSecret"));

                IntConfigs.Add(ConfigInt.GameServerId, ReadIntFromConfig(config, "gameServerId", 1));
                IntConfigs.Add(ConfigInt.GameServerPort, ReadIntFromConfig(config, "gameServerPort", 7172));
                IntConfigs.Add(ConfigInt.LoginServerPort, ReadIntFromConfig(config, "loginServerPort", 7171));
                IntConfigs.Add(ConfigInt.StatusServerPort, ReadIntFromConfig(config, "statusServerPort", 7171));
                IntConfigs.Add(ConfigInt.LoginServerSecretPort, ReadIntFromConfig(config, "loginServerSecretPort", 7180));
                IntConfigs.Add(ConfigInt.MarketOfferDuration, ReadIntFromConfig(config, "marketOfferDuration", 30 * 24 * 60 * 60));
            }

            BoolConfigs.Add(ConfigBool.AllowChangeOutfit, ReadBoolFromConfig(config, "allowChangeOutfit", true));
            BoolConfigs.Add(ConfigBool.AimbotHotkeyEnabled, ReadBoolFromConfig(config, "hotkeyAimbotEnabled", true));
            BoolConfigs.Add(ConfigBool.RemoveAmmo, ReadBoolFromConfig(config, "removeAmmoWhenUsingDistanceWeapon", true));
            BoolConfigs.Add(ConfigBool.RemoveRuneCharges, ReadBoolFromConfig(config, "removeChargesFromRunes", true));
            BoolConfigs.Add(ConfigBool.ExperienceFromPlayers, ReadBoolFromConfig(config, "experienceByKillingPlayers", false));
            BoolConfigs.Add(ConfigBool.FreePremium, ReadBoolFromConfig(config, "freePremium", false));
            BoolConfigs.Add(ConfigBool.ReplaceKickOnLogin, ReadBoolFromConfig(config, "replaceKickOnLogin", true));
            BoolConfigs.Add(ConfigBool.MarketPremium, ReadBoolFromConfig(config, "premiumToCreateMarketOffer", true));
            BoolConfigs.Add(ConfigBool.EmoteSpells, ReadBoolFromConfig(config, "emoteSpells", false));
            BoolConfigs.Add(ConfigBool.StaminaSystem, ReadBoolFromConfig(config, "staminaSystem", true));

            StrConfigs.Add(ConfigStr.MOTD, ReadStrFromConfig(config, "motd"));
            StrConfigs.Add(ConfigStr.DefaultPriority, ReadStrFromConfig(config, "defaultPriority", "high"));
            StrConfigs.Add(ConfigStr.StatusServerName, ReadStrFromConfig(config, "serverName"));
            StrConfigs.Add(ConfigStr.StatusOwnerName, ReadStrFromConfig(config, "ownerName"));
            StrConfigs.Add(ConfigStr.StatusOwnerEmail, ReadStrFromConfig(config, "ownerEmail"));
            StrConfigs.Add(ConfigStr.StatusUrl, ReadStrFromConfig(config, "url"));
            StrConfigs.Add(ConfigStr.StatusLocation, ReadStrFromConfig(config, "location"));
            StrConfigs.Add(ConfigStr.WorldType, ReadStrFromConfig(config, "worldType", "pvp"));
            StrConfigs.Add(ConfigStr.MinConsoleLogLevel, ReadStrFromConfig(config, "minConsoleLogLevel", "information"));

            IntConfigs.Add(ConfigInt.MOTDNum, ReadIntFromConfig(config, "motdNum"));
            IntConfigs.Add(ConfigInt.MaxPlayers, ReadIntFromConfig(config, "maxPlayers"));
            IntConfigs.Add(ConfigInt.PzLocked, ReadIntFromConfig(config, "pzLocked", 60000));
            IntConfigs.Add(ConfigInt.DefaultDespawnrange, ReadIntFromConfig(config, "deSpawnRange", 2));
            IntConfigs.Add(ConfigInt.DefaultDespawnradius, ReadIntFromConfig(config, "deSpawnRadius", 50));
            IntConfigs.Add(ConfigInt.RateExperience, ReadIntFromConfig(config, "rateExp", 5));
            IntConfigs.Add(ConfigInt.RateSkill, ReadIntFromConfig(config, "rateSkill", 3));
            IntConfigs.Add(ConfigInt.RateLoot, ReadIntFromConfig(config, "rateLoot", 2));
            IntConfigs.Add(ConfigInt.RateMagic, ReadIntFromConfig(config, "rateMagic", 3));
            IntConfigs.Add(ConfigInt.RateSpawn, ReadIntFromConfig(config, "rateSpawn", 1));
            IntConfigs.Add(ConfigInt.HousePrice, ReadIntFromConfig(config, "housePriceEachSQM", 1000));
            IntConfigs.Add(ConfigInt.KillsToRed, ReadIntFromConfig(config, "killsToRedSkull", 3));
            IntConfigs.Add(ConfigInt.KillsToBlack, ReadIntFromConfig(config, "killsToBlackSkull", 6));
            IntConfigs.Add(ConfigInt.ActionsDelayInterval, ReadIntFromConfig(config, "timeBetweenActions", 200));
            IntConfigs.Add(ConfigInt.ExActionsDelayInterval, ReadIntFromConfig(config, "timeBetweenExActions", 1000));
            IntConfigs.Add(ConfigInt.MaxMessageBuffer, ReadIntFromConfig(config, "maxMessageBuffer", 4));
            IntConfigs.Add(ConfigInt.KickAfterMinutes, ReadIntFromConfig(config, "kickIdlePlayerAfterMinutes", 15));
            IntConfigs.Add(ConfigInt.ProtectionLevel, ReadIntFromConfig(config, "protectionLevel", 1));
            IntConfigs.Add(ConfigInt.DeathLosePercent, ReadIntFromConfig(config, "deathLosePercent", -1));
            IntConfigs.Add(ConfigInt.StatusQueryTimeout, ReadIntFromConfig(config, "statusTimeout", 5000));
            IntConfigs.Add(ConfigInt.FragTime, ReadIntFromConfig(config, "timeToDecreaseFrags", 24 * 60 * 60 * 1000));
            IntConfigs.Add(ConfigInt.WhiteSkullTime, ReadIntFromConfig(config, "whiteSkullTime", 15 * 60 * 1000));
            IntConfigs.Add(ConfigInt.StairhopDelay, ReadIntFromConfig(config, "stairJumpExhaustion", 2000));
            IntConfigs.Add(ConfigInt.ExpFromPlayersLevelRange, ReadIntFromConfig(config, "expFromPlayersLevelRange", 75));
            IntConfigs.Add(ConfigInt.CheckExpiredMarketOffersEachMinutes, ReadIntFromConfig(config, "checkExpiredMarketOffersEachMinutes", 60));
            IntConfigs.Add(ConfigInt.MaxMarketOffersAtATimePerPlayer, ReadIntFromConfig(config, "maxMarketOffersAtATimePerPlayer", 100));
            IntConfigs.Add(ConfigInt.MaxPacketsPerSecond, ReadIntFromConfig(config, "maxPacketsPerSecond", 25));

            Logger.LogOperationDone();
            _isLoaded = true;
            config.Dispose();

            if (ConfigsLoaded != null)
                ConfigsLoaded();

            return true;
        }
        public static bool Reload()
        {
            if (!_isLoaded)
            {
                return false;
            }

            bool result = Load(_lastLoadedFile);
            //if (transformToSHA1(getString(ConfigManager::MOTD)) != g_game.getMotdHash()) {
            //    g_game.incrementMotdNum();
            //}
            return result;
        }

        private static int ReadIntFromConfig(Lua config, string identifier, int @default = 0)
        {
            try
            {
                return (int)config.GetNumber(identifier);
            }
            catch (Exception)
            {
                return @default;
            }
        }
        private static string ReadStrFromConfig(Lua config, string identifier, string @default = "")
        {
            try
            {
                return config.GetString(identifier);
            }
            catch (Exception)
            {
                return @default;
            }
        }
        private static bool ReadBoolFromConfig(Lua config, string identifier, bool @default)
        {
            try
            {
                return Tools.ConvertLuaBoolean(config.GetString(identifier));
            }
            catch (Exception)
            {
                return @default;
            }
        }
    }
}
