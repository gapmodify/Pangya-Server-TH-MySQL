using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;

namespace Connector.DataBase
{
    public static class DatabaseConfig
    {
        private static string _connectionString;
        private static bool _initialized = false;
        private static string _dbEngine;
        private static string _lastIniFile;
        private static readonly object _lock = new object(); // Thread safety

        /// <summary>
        /// ตรวจสอบว่าไฟล์ .ini มีอยู่หรือไม่ ถ้าไม่มีให้สร้างขึ้นมาพร้อมค่าเริ่มต้น
        /// </summary>
        public static void EnsureConfigFileExists(string configFileName)
        {
            if (string.IsNullOrEmpty(configFileName))
                return;

            string baseDir = AppDomain.CurrentDomain.BaseDirectory;
            string configPath = Path.Combine(baseDir, configFileName);

            if (File.Exists(configPath))
            {
                return;
            }

            Console.WriteLine($"[CONFIG_CHECK]: {configFileName} not found! Creating default configuration...");

            try
            {
                string defaultContent = GetDefaultConfigContent(configFileName);
                File.WriteAllText(configPath, defaultContent, Encoding.UTF8);
                Console.WriteLine($"[CONFIG_CREATED]: {configFileName} has been created at: {configPath}");
                Console.WriteLine($"[CONFIG_WARNING]: Please review and update the configuration file as needed!");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[CONFIG_ERROR]: Failed to create {configFileName}: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// สร้างเนื้อหาเริ่มต้นสำหรับไฟล์ .ini ตามชื่อไฟล์
        /// </summary>
        private static string GetDefaultConfigContent(string fileName)
        {
            string lowerFileName = fileName.ToLower();

            if (lowerFileName == "game.ini")
            {
                return @"[Config]
# Name
Name = S8 TH 

# Versão
Version = SV_GS_Release_2.0

# ID
UID = 20201

#Porta
Port = 20201

IP = 127.0.0.1

MaxPlayers = 3000

#Shows the packet information
PacketLog = true

# Property 
# 2048 = Grand Prix
# 64 = only rookie
# 128 = Natural Mode(TH, JP)
# 16 = Invisible(Only GM AND ADM)
# 256 = Unknown
# 1024 = BLUE 
# 512 = GREEN
Property = 2048

#Bloqueia alguma função no servidor 
BlockFuncSystem = 0

# Event Flag
# Essa é Event Flag pode fazer manual aqui mas o  já
# -> ver o pang, xp, angel event e mastery rate e coloca na flag
# 16 x3 xp
# 2 Pang	# 4 Exp x2		# 8 Angel event		# 16 Exp x3
# 32 Nada	# 64 Nada E da Erro	# 128 Club Mastery	# 256 Nada
# 512 Nada	# 1024 Nada		# 2048 Nada		# 4096 Nada
# 8192 Nada	# 16384 Nada		# 32768	Nada		# 0 Nada
EventFlag = 4096

###############################
#[Icon Flag]
#0 = BlackPapel
#1 = Pippin
#2 = TitanBoo
#3 = Dolfini
#4 = Lolo
#5 = Quma
################################
Icon = 1

#Conexao AuthServer
AuthServer_IP = 127.0.0.1
AuthServer_Port = 7997


# Messenger Server (false = ปิด, true = เปิด)
Messenger_Server = false

[NORMAL_DB]

DBENGINE	=  mysql
DBIP		=  127.0.0.1
DBNAME		=  pangya
DBUSER		=  root
DBPASS		=  root
DBPORT		=  3306

[Channel]
#Canais 
ChannelCount = 1

###################################
#########[Channel Flag]############
# 1 = All(todos os players)
#16 = GMOnly (somente GM's)
#32 = Junior (somente juniors)
#128 = UN
#164 = NaturalAndBeginner
#2048 = Rookie(somente iniciantes)    
#1024 = Junior_2
#4096 = BeginnerAndJuniorOnly
#8192 JuniorAndSeniorsOnly
#512 = nao sei
#################################

#Lobby 1
ChannelName_1 = #Lobby 1
ChannelMaxUser_1 = 200
ChannelID_1 = 0
ChannelFlag_1 = 1

##Lobby 2
#ChannelName_2 = #Lobby 2
#ChannelMaxUser_2 = 200
#ChannelID_2 = 1
#ChannelFlag_2 = 512

##Lobby 3
#ChannelName_3 = #Lobby 3
#ChannelMaxUser_3 = 200
#ChannelID_3 = 2
#ChannelFlag_3 = 128
";
            }
            else if (lowerFileName == "login.ini")
            {
                return @"[Config]
Name = LoginServer
Version = SV_LS_Release_2.0
UID = 10201
Port = 10201
IP = 127.0.0.1
MaxPlayers = 3000
PacketLog = false
AuthServer_IP = 127.0.0.1
AuthServer_Port = 7997

[NORMAL_DB]
DBENGINE = mysql
DBIP = 127.0.0.1
DBNAME = pangya
DBUSER = root
DBPASS = root
DBPORT = 3306
";
            }
            else if (lowerFileName == "msg.ini")
            {
                return @"[Config]
Name = Message Server
Version = SV_MS_Release_1.0
UID = 30303
Port = 30303
IP = 127.0.0.1
MaxPlayers = 3000
Property = 4096
AuthServer_IP = 127.0.0.1
AuthServer_Port = 7997

[NORMAL_DB]
DBENGINE = mysql
DBIP = 127.0.0.1
DBNAME = pangya
DBUSER = root
DBPASS = root
DBPORT = 3306
";
            }
            else if (lowerFileName == "auth.ini")
            {
                return @"[Config]
Name = AuthServer
Version = SV_AS_Release_1.0
UID = 7997
Port = 7997
IP = 127.0.0.1
MaxPlayers = 100

[NORMAL_DB]
DBENGINE = mysql
DBIP = 127.0.0.1
DBNAME = pangya
DBUSER = root
DBPASS = root
DBPORT = 3306
";
            }
            else
            {
                // Default generic config
                return @"[Config]
                        Name = Server
                        Version = 1.0
                        UID = 10000
                        Port = 10000
                        IP = 127.0.0.1
                        MaxPlayers = 1000

                        [NORMAL_DB]
                        DBENGINE = mysql
                        DBIP = 127.0.0.1
                        DBNAME = pangya
                        DBUSER = root
                        DBPASS = root
                        DBPORT = 3306
                        ";
            }
        }

        public static void Initialize(string iniFilePath = null)
        {
            // Thread-safe initialization
            lock (_lock)
            {
                try
                {
                    string configFile = iniFilePath;

                    // ตรวจสอบว่ามีไฟล์ .ini ที่ระบุหรือไม่ ถ้าไม่มีให้สร้าง
                    if (!string.IsNullOrEmpty(configFile))
                    {
                        EnsureConfigFileExists(configFile);
                    }

                    // ค้นหา INI file ที่มีอยู่ในระบบ
                    if (string.IsNullOrEmpty(configFile))
                    {
                        string baseDir = AppDomain.CurrentDomain.BaseDirectory;
                        string[] possibleFiles = { "Auth.ini", "Game.ini", "Login.ini", "Msg.ini", "Server.ini" };
                        
                        foreach (string fileName in possibleFiles)
                        {
                            string fullPath = Path.Combine(baseDir, fileName);
                            if (File.Exists(fullPath))
                            {
                                configFile = fileName;
                                break;
                            }
                        }
                    }

                    // ถ้า initialized แล้วและ INI file เป็นตัวเดียวกัน ให้ skip
                    if (_initialized && _lastIniFile == configFile && _connectionString != null)
                    {
                        return;
                    }

                    if (string.IsNullOrEmpty(configFile))
                    {
                        throw new Exception("No configuration file found. Please create Auth.ini, Game.ini, Login.ini, or Msg.ini in the application directory.");
                    }

                    var ini = new SimpleIniReader(configFile);

                    _dbEngine = ini.ReadString("NORMAL_DB", "DBENGINE", "mysql").Trim().ToLower();
                    string dbIP = ini.ReadString("NORMAL_DB", "DBIP", "localhost").Trim();
                    string dbName = ini.ReadString("NORMAL_DB", "DBNAME", "pangya").Trim();
                    string dbUser = ini.ReadString("NORMAL_DB", "DBUSER", "root").Trim();
                    string dbPass = ini.ReadString("NORMAL_DB", "DBPASS", "root").Trim();
                    int dbPort = ini.ReadInt32("NORMAL_DB", "DBPORT", 3306);

                    if (_dbEngine == "mysql")
                    {
                        // MySQL Connection String with UTF-8 support
                        string serverAddress = dbIP;
                        if (dbPort > 0 && dbPort != 3306)
                        {
                            serverAddress = $"{dbIP}:{dbPort}";
                        }
                        
                        _connectionString = $"server={serverAddress};database={dbName};uid={dbUser};pwd={dbPass};charset=utf8mb4;";
                    }
                    else if (_dbEngine == "mssql" || _dbEngine == "sqlserver")
                    {
                        bool useWindowsAuth = string.IsNullOrEmpty(dbUser) || string.IsNullOrEmpty(dbPass);
                        
                        if (useWindowsAuth)
                        {
                            _connectionString = $"Data Source={dbIP};Initial Catalog={dbName};Integrated Security=True;MultipleActiveResultSets=True";
                        }
                        else
                        {
                            if (dbPort > 0 && dbPort != 1433)
                            {
                                _connectionString = $"Data Source={dbIP},{dbPort};Initial Catalog={dbName};User ID={dbUser};Password={dbPass};MultipleActiveResultSets=True";
                            }
                            else
                            {
                                _connectionString = $"Data Source={dbIP};Initial Catalog={dbName};User ID={dbUser};Password={dbPass};MultipleActiveResultSets=True";
                            }
                        }
                    }
                    else
                    {
                        throw new Exception($"Database engine '{_dbEngine}' is not supported. Use 'mysql' or 'mssql'.");
                    }

                    _initialized = true;
                    _lastIniFile = configFile;
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[DB_CONFIG_ERROR]: {ex.Message}");
                    throw new Exception($"Failed to initialize database configuration: {ex.Message}", ex);
                }
            }
        }

        public static string GetConnectionString()
        {
            if (!_initialized || _connectionString == null)
            {
                Initialize();
            }
            return _connectionString;
        }

        public static string GetDbEngine()
        {
            if (!_initialized)
            {
                Initialize();
            }
            return _dbEngine;
        }

        public static void Reset()
        {
            lock (_lock)
            {
                _initialized = false;
                _connectionString = null;
                _dbEngine = null;
                _lastIniFile = null;
            }
        }
    }

    internal class SimpleIniReader
    {
        [DllImport("kernel32", CharSet = CharSet.Unicode)]
        private static extern int GetPrivateProfileString(string section, string key, string def, StringBuilder retVal, int size, string filePath);

        private string _filePath;

        public SimpleIniReader(string filename)
        {
            if (!File.Exists(filename))
            {
                string fullPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, filename);
                if (!File.Exists(fullPath))
                {
                    throw new Exception($"File not exist: {filename}");
                }
                _filePath = fullPath;
            }
            else
            {
                _filePath = Path.GetFullPath(filename);
            }
        }

        public string ReadString(string section, string key, string defaultValue)
        {
            StringBuilder sb = new StringBuilder(255);
            GetPrivateProfileString(section, key, defaultValue, sb, 255, _filePath);
            return sb.ToString();
        }

        public int ReadInt32(string section, string key, int defaultValue)
        {
            string value = ReadString(section, key, defaultValue.ToString());
            if (int.TryParse(value, out int result))
            {
                return result;
            }
            return defaultValue;
        }
    }
}
