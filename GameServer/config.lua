-- START SERVER CONNECTIONS
useExternalLoginServer = "no"
loginServerIp = "127.0.0.1"
loginServerPort = 7171
statusServerPort = 7171
loginServerSecretPort = 7180

gameServerId = 1
gameServerIp = "127.0.0.1"
gameServerPort = 7172
gameServerSecret = "c#8SJ6zmZM8@PDBHE@f^4mvf"
-- END SERVER CONNECTIONS

-- START LOGIN SERVER SPECIFICS
motdNum = 0
motd = "Welcome to Herhangi Server!"
-- END LOGIN SERVER SPECIFICS

-- START GAME SERVER SPECIFICS
-- Monsters
deSpawnRange = 2
deSpawnRadius = 50

-- Combat settings
-- World Types: pvp, no-pvp, pvp-enforced
worldType = "pvp"
-- END GAME SERVER SPECIFICS

-- START DATABASE
-- Database Types: mssql, sqlite, mysql, xml, json
databaseType = "json"
-- Password Hash Algorithms: sha1, md5
passwordHashAlgorithm = "sha1"

-- MSSQL
mssqlConnectionString = "Data Source=(LocalDB)\v11.0;Initial Catalog=LoginServerDatabase;Integrated Security=True"

-- JSON
jsonAccountPath = "Data/Account/"
jsonCharacterPath = "Data/Character/"

mysqlHost = "127.0.0.1"
mysqlUser = "root"
mysqlPass = ""
mysqlDatabase = "tibia"
mysqlPort = 3306
mysqlSock = ""
-- END DATABASE

-- START LOGGING
-- LogLevels: Error, Operation, Warning, Information, Debug
minConsoleLogLevel = "Information"
-- !!!FILE LOGGING IS CURRENTLY IN PROGRESS!!!
logToFile = "no"
logFilePath = "Logs/GameServer.txt"
minFileLogLevel = "Information"
-- END LOGGING


bindOnlyGlobalAddress = "no"
statusProtocolPort = 7171
maxPlayers = 10
motd = "Welcome to Herhangi Server!"
onePlayerOnlinePerAccount = "yes"
allowClones = "no"
serverName = "Herhangi"
statusTimeout = 5000
replaceKickOnLogin = "yes"
maxPacketsPerSecond = 25

-- Combat settings
hotkeyAimbotEnabled = "yes"
protectionLevel = 1
killsToRedSkull = 3
killsToBlackSkull = 6
pzLocked = 60000
removeAmmoWhenUsingDistanceWeapon = "yes"
removeChargesFromRunes = "yes"
timeToDecreaseFrags = 24 * 60 * 60 * 1000
whiteSkullTime = 15 * 60 * 1000
stairJumpExhaustion = 2000
experienceByKillingPlayers = "no"
expFromPlayersLevelRange = 75
noDamageToSameLookfeet = "no"


-- Deaths
-- NOTE: Leave deathLosePercent as -1 if you want to use the default
-- death penalty formula. For the old formula, set it to 10. For
-- no skill/experience loss, set it to 0.
deathLosePercent = -1

-- Houses
-- NOTE: set housePriceEachSQM to -1 to disable the ingame buy house functionality
housePriceEachSQM = 1000
houseRentPeriod = "never"

-- Item Usage
timeBetweenActions = 200
timeBetweenExActions = 1000

-- Map
-- NOTE: set mapName WITHOUT .otbm at the end
mapName = "forgotten"
mapAuthor = "Komic"

-- Market
marketOfferDuration = 30 * 24 * 60 * 60
premiumToCreateMarketOffer = "yes"
checkExpiredMarketOffersEachMinutes = 60
maxMarketOffersAtATimePerPlayer = 100


-- Misc.
allowChangeOutfit = "yes"
freePremium = "no"
kickIdlePlayerAfterMinutes = 15
maxMessageBuffer = 4
emoteSpells = "no"

-- Rates
-- NOTE: rateExp is not used if you have enabled stages in data/XML/stages.xml
rateExp = 5
rateSkill = 3
rateLoot = 2
rateMagic = 3
rateSpawn = 1


-- Stamina
staminaSystem = "yes"

-- Scripts
warnUnsafeScripts = "no"
convertUnsafeScripts = "no"

-- Startup
-- NOTE: defaultPriority only works on Windows and sets process priority.
defaultPriority = "high"
startupDatabaseOptimization = "no"

-- Status server information
ownerName = ""
ownerEmail = ""
url = "http://otland.net/"
location = "Sweden"
