using System;
using HerhangiOT.ServerLibrary.Utility;
using LuaInterface;
using System.Collections.Generic;

namespace HerhangiOT.ServerLibrary
{
    public enum ConfigInt
    {
        GAME_SERVER_PORT = 0,
        LOGIN_SERVER_PORT,
        STATUS_SERVER_PORT,
        LOGIN_SERVER_SECRET_PORT,

        MOTD_NUM,

        GAME_SERVER_ID,
        MAX_PLAYERS,

        SQL_PORT,
        PZ_LOCKED,
        DEFAULT_DESPAWNRANGE,
        DEFAULT_DESPAWNRADIUS,
        RATE_EXPERIENCE,
        RATE_SKILL,
        RATE_LOOT,
        RATE_MAGIC,
        RATE_SPAWN,
        HOUSE_PRICE,
        KILLS_TO_RED,
        KILLS_TO_BLACK,
        MAX_MESSAGEBUFFER,
        ACTIONS_DELAY_INTERVAL,
        EX_ACTIONS_DELAY_INTERVAL,
        KICK_AFTER_MINUTES,
        PROTECTION_LEVEL,
        DEATH_LOSE_PERCENT,
        STATUSQUERY_TIMEOUT,
        FRAG_TIME,
        WHITE_SKULL_TIME,
        STAIRHOP_DELAY,
        MARKET_OFFER_DURATION,
        CHECK_EXPIRED_MARKET_OFFERS_EACH_MINUTES,
        MAX_MARKET_OFFERS_AT_A_TIME_PER_PLAYER,
        EXP_FROM_PLAYERS_LEVEL_RANGE,
        MAX_PACKETS_PER_SECOND,
        LAST_INTEGER_CONFIG
    }

    public enum ConfigStr
    {
        GAME_SERVER_IP = 0,
        LOGIN_SERVER_IP,

        MOTD,
        DATABASE_TYPE,
        DATABASE_MSSQL_CONNECTION_STRING,

        DATABASE_JSON_ACCOUNT_PATH,
        DATABASE_JSON_CHARACTER_PATH,

        PASSWORD_HASH_ALGORITHM,
        GAME_SERVER_SECRET,

        MIN_CONSOLE_LOG_LEVEL,

        DUMMY_STR,
        MAP_NAME,
        HOUSE_RENT_PERIOD,
        SERVER_NAME,
        OWNER_NAME,
        OWNER_EMAIL,
        URL,
        LOCATION,
        WORLD_TYPE,
        MYSQL_HOST,
        MYSQL_USER,
        MYSQL_PASS,
        MYSQL_DB,
        MYSQL_SOCK,
        DEFAULT_PRIORITY,
        MAP_AUTHOR,
        LAST_STRING_CONFIG
    }

    public enum ConfigBool
    {
        USE_EXTERNAL_LOGIN_SERVER,

        ALLOW_CHANGEOUTFIT,
        CANNOT_ATTACK_SAME_LOOKFEET,
        ONE_PLAYER_ON_ACCOUNT,
        AIMBOT_HOTKEY_ENABLED,
        REMOVE_AMMO,
        REMOVE_RUNE_CHARGES,
        EXPERIENCE_FROM_PLAYERS,
        FREE_PREMIUM,
        REPLACE_KICK_ON_LOGIN,
        ALLOW_CLONES,
        BIND_ONLY_GLOBAL_ADDRESS,
        OPTIMIZE_DATABASE,
        MARKET_PREMIUM,
        EMOTE_SPELLS,
        STAMINA_SYSTEM,
        WARN_UNSAFE_SCRIPTS,
        CONVERT_UNSAFE_SCRIPTS,
        LAST_BOOLEAN_CONFIG
    }

    public class ConfigManager
    {
        private static bool _isLoaded;
        private static string _lastLoadedFile;
        
        public int this[ConfigInt configName]
        {
            get { return _intConfigs[configName]; }
        }
        public bool this[ConfigBool configName]
        {
            get { return _boolConfigs[configName]; }
        }
        public string this[ConfigStr configName]
        {
            get { return _strConfigs[configName]; }
        }

        private static readonly Dictionary<ConfigInt, int> _intConfigs = new Dictionary<ConfigInt, int>();
        private static readonly Dictionary<ConfigBool, bool> _boolConfigs = new Dictionary<ConfigBool, bool>();        
        private static readonly Dictionary<ConfigStr, string> _strConfigs = new Dictionary<ConfigStr, string>();

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
                _boolConfigs.Add(ConfigBool.BIND_ONLY_GLOBAL_ADDRESS, ReadBoolFromConfig(config, "bindOnlyGlobalAddress", false));
                _boolConfigs.Add(ConfigBool.OPTIMIZE_DATABASE, ReadBoolFromConfig(config, "startupDatabaseOptimization", true));
                _boolConfigs.Add(ConfigBool.USE_EXTERNAL_LOGIN_SERVER, ReadBoolFromConfig(config, "useExternalLoginServer", false));

                _strConfigs.Add(ConfigStr.DATABASE_TYPE, ReadStrFromConfig(config, "databaseType", "json"));

                switch (_strConfigs[ConfigStr.DATABASE_TYPE])
                {
                    case "mssql":
                        _strConfigs.Add(ConfigStr.DATABASE_MSSQL_CONNECTION_STRING, ReadStrFromConfig(config, "mssqlConnectionString"));
                        break;
                    case "json":
                        _strConfigs.Add(ConfigStr.DATABASE_JSON_ACCOUNT_PATH, ReadStrFromConfig(config, "jsonAccountPath", "Data/Account"));
                        _strConfigs.Add(ConfigStr.DATABASE_JSON_CHARACTER_PATH, ReadStrFromConfig(config, "jsonCharacterPath", "Data/Character"));
                        break;
                    case "mysql":
                        _strConfigs.Add(ConfigStr.MYSQL_HOST, ReadStrFromConfig(config, "mysqlHost", "127.0.0.1"));
                        _strConfigs.Add(ConfigStr.MYSQL_USER, ReadStrFromConfig(config, "mysqlUser", "herhangi"));
                        _strConfigs.Add(ConfigStr.MYSQL_PASS, ReadStrFromConfig(config, "mysqlPass"));
                        _strConfigs.Add(ConfigStr.MYSQL_DB, ReadStrFromConfig(config, "mysqlDatabase", "herhangi"));
                        _strConfigs.Add(ConfigStr.MYSQL_SOCK, ReadStrFromConfig(config, "mysqlSock"));
                        _intConfigs.Add(ConfigInt.SQL_PORT, ReadIntFromConfig(config, "mysqlPort", 3306));                        
                        break;
                    default:
                        Logger.LogOperationFailed("config.lua contains invalid database type!");
                        return false;

                }

                _strConfigs.Add(ConfigStr.PASSWORD_HASH_ALGORITHM, ReadStrFromConfig(config, "passwordHashAlgorithm", "sha1"));
                _strConfigs.Add(ConfigStr.GAME_SERVER_IP, ReadStrFromConfig(config, "gameServerIp", "127.0.0.1"));
                _strConfigs.Add(ConfigStr.LOGIN_SERVER_IP, ReadStrFromConfig(config, "loginServerIp", "127.0.0.1"));
                _strConfigs.Add(ConfigStr.MAP_NAME, ReadStrFromConfig(config, "mapName", "forgotten"));
                _strConfigs.Add(ConfigStr.MAP_AUTHOR, ReadStrFromConfig(config, "mapAuthor", "Unknown"));
                _strConfigs.Add(ConfigStr.HOUSE_RENT_PERIOD, ReadStrFromConfig(config, "houseRentPeriod", "never"));
                _strConfigs.Add(ConfigStr.GAME_SERVER_SECRET, ReadStrFromConfig(config, "gameServerSecret", ""));

                _intConfigs.Add(ConfigInt.GAME_SERVER_ID, ReadIntFromConfig(config, "gameServerId", 1));
                _intConfigs.Add(ConfigInt.GAME_SERVER_PORT, ReadIntFromConfig(config, "gameServerPort", 7172));
                _intConfigs.Add(ConfigInt.LOGIN_SERVER_PORT, ReadIntFromConfig(config, "loginServerPort", 7171));
                _intConfigs.Add(ConfigInt.STATUS_SERVER_PORT, ReadIntFromConfig(config, "statusServerPort", 7171));
                _intConfigs.Add(ConfigInt.LOGIN_SERVER_SECRET_PORT, ReadIntFromConfig(config, "loginServerSecretPort", 7180));
                _intConfigs.Add(ConfigInt.MARKET_OFFER_DURATION, ReadIntFromConfig(config, "marketOfferDuration", 30 * 24 * 60 * 60));
            }

            _boolConfigs.Add(ConfigBool.ALLOW_CHANGEOUTFIT, ReadBoolFromConfig(config, "allowChangeOutfit", true));
            _boolConfigs.Add(ConfigBool.ONE_PLAYER_ON_ACCOUNT, ReadBoolFromConfig(config, "onePlayerOnlinePerAccount", true));
            _boolConfigs.Add(ConfigBool.CANNOT_ATTACK_SAME_LOOKFEET, ReadBoolFromConfig(config, "noDamageToSameLookfeet", false));
            _boolConfigs.Add(ConfigBool.AIMBOT_HOTKEY_ENABLED, ReadBoolFromConfig(config, "hotkeyAimbotEnabled", true));
            _boolConfigs.Add(ConfigBool.REMOVE_AMMO, ReadBoolFromConfig(config, "removeAmmoWhenUsingDistanceWeapon", true));
            _boolConfigs.Add(ConfigBool.REMOVE_RUNE_CHARGES, ReadBoolFromConfig(config, "removeChargesFromRunes", true));
            _boolConfigs.Add(ConfigBool.EXPERIENCE_FROM_PLAYERS, ReadBoolFromConfig(config, "experienceByKillingPlayers", false));
            _boolConfigs.Add(ConfigBool.FREE_PREMIUM, ReadBoolFromConfig(config, "freePremium", false));
            _boolConfigs.Add(ConfigBool.REPLACE_KICK_ON_LOGIN, ReadBoolFromConfig(config, "replaceKickOnLogin", true));
            _boolConfigs.Add(ConfigBool.ALLOW_CLONES, ReadBoolFromConfig(config, "allowClones", false));
            _boolConfigs.Add(ConfigBool.MARKET_PREMIUM, ReadBoolFromConfig(config, "premiumToCreateMarketOffer", true));
            _boolConfigs.Add(ConfigBool.EMOTE_SPELLS, ReadBoolFromConfig(config, "emoteSpells", false));
            _boolConfigs.Add(ConfigBool.STAMINA_SYSTEM, ReadBoolFromConfig(config, "staminaSystem", true));
            _boolConfigs.Add(ConfigBool.WARN_UNSAFE_SCRIPTS, ReadBoolFromConfig(config, "warnUnsafeScripts", false));
            _boolConfigs.Add(ConfigBool.CONVERT_UNSAFE_SCRIPTS, ReadBoolFromConfig(config, "convertUnsafeScripts", false));

            _strConfigs.Add(ConfigStr.MOTD, ReadStrFromConfig(config, "motd"));
            _strConfigs.Add(ConfigStr.DEFAULT_PRIORITY, ReadStrFromConfig(config, "defaultPriority", "high"));
            _strConfigs.Add(ConfigStr.SERVER_NAME, ReadStrFromConfig(config, "serverName"));
            _strConfigs.Add(ConfigStr.OWNER_NAME, ReadStrFromConfig(config, "ownerName"));
            _strConfigs.Add(ConfigStr.OWNER_EMAIL, ReadStrFromConfig(config, "ownerEmail"));
            _strConfigs.Add(ConfigStr.URL, ReadStrFromConfig(config, "url"));
            _strConfigs.Add(ConfigStr.LOCATION, ReadStrFromConfig(config, "location"));
            _strConfigs.Add(ConfigStr.WORLD_TYPE, ReadStrFromConfig(config, "worldType", "pvp"));
            _strConfigs.Add(ConfigStr.MIN_CONSOLE_LOG_LEVEL, ReadStrFromConfig(config, "minConsoleLogLevel", "information"));

            _intConfigs.Add(ConfigInt.MOTD_NUM, ReadIntFromConfig(config, "motdNum"));
            _intConfigs.Add(ConfigInt.MAX_PLAYERS, ReadIntFromConfig(config, "maxPlayers"));
            _intConfigs.Add(ConfigInt.PZ_LOCKED, ReadIntFromConfig(config, "pzLocked", 60000));
            _intConfigs.Add(ConfigInt.DEFAULT_DESPAWNRANGE, ReadIntFromConfig(config, "deSpawnRange", 2));
            _intConfigs.Add(ConfigInt.DEFAULT_DESPAWNRADIUS, ReadIntFromConfig(config, "deSpawnRadius", 50));
            _intConfigs.Add(ConfigInt.RATE_EXPERIENCE, ReadIntFromConfig(config, "rateExp", 5));
            _intConfigs.Add(ConfigInt.RATE_SKILL, ReadIntFromConfig(config, "rateSkill", 3));
            _intConfigs.Add(ConfigInt.RATE_LOOT, ReadIntFromConfig(config, "rateLoot", 2));
            _intConfigs.Add(ConfigInt.RATE_MAGIC, ReadIntFromConfig(config, "rateMagic", 3));
            _intConfigs.Add(ConfigInt.RATE_SPAWN, ReadIntFromConfig(config, "rateSpawn", 1));
            _intConfigs.Add(ConfigInt.HOUSE_PRICE, ReadIntFromConfig(config, "housePriceEachSQM", 1000));
            _intConfigs.Add(ConfigInt.KILLS_TO_RED, ReadIntFromConfig(config, "killsToRedSkull", 3));
            _intConfigs.Add(ConfigInt.KILLS_TO_BLACK, ReadIntFromConfig(config, "killsToBlackSkull", 6));
            _intConfigs.Add(ConfigInt.ACTIONS_DELAY_INTERVAL, ReadIntFromConfig(config, "timeBetweenActions", 200));
            _intConfigs.Add(ConfigInt.EX_ACTIONS_DELAY_INTERVAL, ReadIntFromConfig(config, "timeBetweenExActions", 1000));
            _intConfigs.Add(ConfigInt.MAX_MESSAGEBUFFER, ReadIntFromConfig(config, "maxMessageBuffer", 4));
            _intConfigs.Add(ConfigInt.KICK_AFTER_MINUTES, ReadIntFromConfig(config, "kickIdlePlayerAfterMinutes", 15));
            _intConfigs.Add(ConfigInt.PROTECTION_LEVEL, ReadIntFromConfig(config, "protectionLevel", 1));
            _intConfigs.Add(ConfigInt.DEATH_LOSE_PERCENT, ReadIntFromConfig(config, "deathLosePercent", -1));
            _intConfigs.Add(ConfigInt.STATUSQUERY_TIMEOUT, ReadIntFromConfig(config, "statusTimeout", 5000));
            _intConfigs.Add(ConfigInt.FRAG_TIME, ReadIntFromConfig(config, "timeToDecreaseFrags", 24 * 60 * 60 * 1000));
            _intConfigs.Add(ConfigInt.WHITE_SKULL_TIME, ReadIntFromConfig(config, "whiteSkullTime", 15 * 60 * 1000));
            _intConfigs.Add(ConfigInt.STAIRHOP_DELAY, ReadIntFromConfig(config, "stairJumpExhaustion", 2000));
            _intConfigs.Add(ConfigInt.EXP_FROM_PLAYERS_LEVEL_RANGE, ReadIntFromConfig(config, "expFromPlayersLevelRange", 75));
            _intConfigs.Add(ConfigInt.CHECK_EXPIRED_MARKET_OFFERS_EACH_MINUTES, ReadIntFromConfig(config, "checkExpiredMarketOffersEachMinutes", 60));
            _intConfigs.Add(ConfigInt.MAX_MARKET_OFFERS_AT_A_TIME_PER_PLAYER, ReadIntFromConfig(config, "maxMarketOffersAtATimePerPlayer", 100));
            _intConfigs.Add(ConfigInt.MAX_PACKETS_PER_SECOND, ReadIntFromConfig(config, "maxPacketsPerSecond", 25));

            Logger.LogOperationDone();
            _isLoaded = true;
            config.Dispose();

            if (ConfigsLoaded != null)
                ConfigsLoaded();

            return true;
        }
        public static bool Reload()
        {           
	        if (!_isLoaded) {
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
