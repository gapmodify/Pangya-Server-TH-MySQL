using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
namespace PangyaAPI.Tools
{
    public class IniFile : IDisposable
    {
        [DllImport("kernel32", CharSet = CharSet.Auto)]
        static extern long WritePrivateProfileString(string section, string key, string val, string filePath);
        
        [DllImport("kernel32", CharSet = CharSet.Auto)]
        static extern int GetPrivateProfileString(string section, string key, string def, StringBuilder retVal, int size, string filePath);

        /// <summary>
        /// Local do arquivo
        /// </summary>
        public string FilePath { get; set; }

        /// <summary>
        /// Construtor
        /// </summary>
        /// <param name="_filename">nome do arquivo</param>
        public IniFile(string _filename)
        {
            try
            {
                // เช็คว่าไฟล์มีอยู่ในตำแหน่งที่ระบุหรือไม่
                if (File.Exists(_filename))
                {
                    // ถ้ามี ใช้ path เต็ม
                    FilePath = Path.GetFullPath(_filename);
                }
                else
                {
                    // ถ้าไม่มี ลองหาใน BaseDirectory
                    string fullPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, _filename);
                    if (File.Exists(fullPath))
                    {
                        FilePath = fullPath;
                    }
                    else
                    {
                        throw new Exception($"File not exist: {_filename}");
                    }
                }
                
                WriteConsole.WriteLine($"[INI_FILE]: Loading from {FilePath}", ConsoleColor.Cyan);
            }
            catch (Exception ex)
            {
                WriteConsole.WriteLine($"[INI_FILE_ERROR]: {ex.Message}", ConsoleColor.Red);
                throw;
            }
        }
        /// <summary>
        /// Destrutor
        /// </summary>
        ~IniFile()
        {
            Dispose(false);
        }
        /// <summary>
        /// Cria o arquivo .ini
        /// </summary>
        /// <param name="section">Seção = cabeçario [Config]</param>
        /// <param name="key">Local = nomedealgo</param>
        /// <param name="value">valor</param>
        /// <returns>string</returns>
        public void Write(string section, string key, string value)
        {
            WritePrivateProfileString(section, key, value.ToLower(), FilePath);
        }
        /// <summary>
        /// Ler o arquivo .ini e retorna string
        /// </summary>
        /// <param name="section">Seção = cabeçario [Config]</param>
        /// <param name="key">Local = nomedealgo = 0 </param>
        /// <param name="def">padrao = caso não encontre o valor no key, retorna o def</param>
        /// <returns>string</returns>
        public string Read(string section, string key)
        {
            StringBuilder BuildStr = new StringBuilder(255);
            GetPrivateProfileString(section, key, "", BuildStr, 255, FilePath);
            string result = BuildStr.ToString();
            
            if (string.IsNullOrEmpty(result))
            {
                throw new Exception($"Cannot read [{section}] {key} from {FilePath}");
            }
            
            return result;
        }
        
        // แบบมี default - ถ้าอ่านไม่ได้คืนค่า default
        public string Read(string section, string key, object def)
        {
            StringBuilder BuildStr = new StringBuilder(255);
            GetPrivateProfileString(section, key, def.ToString(), BuildStr, 255, FilePath);
            return BuildStr.ToString();
        }
        
        /// <summary>
        /// Ler o arquivo .ini e retorna string
        /// </summary>
        /// <param name="section">Seção = cabeçario [Config]</param>
        /// <param name="key">Local = nomedealgo = 0 </param>
        /// <param name="def">padrao = caso não encontre o valor no key, retorna o def</param>
        /// <returns>string</returns>
        public string ReadString(string section, string key)
        {
            StringBuilder SB = new StringBuilder(255);
            GetPrivateProfileString(section, key, "", SB, 255, this.FilePath);
            string result = SB.ToString();
            
            if (string.IsNullOrEmpty(result))
            {
                throw new Exception($"Cannot read [{section}] {key} from {FilePath}");
            }
            
            WriteConsole.WriteLine($"[INI_READ]: [{section}] {key} = '{result}'", ConsoleColor.DarkGray);
            return result;
        }
        
        // แบบมี default
        public string ReadString(string section, string key, string def)
        {
            StringBuilder SB = new StringBuilder(255);
            GetPrivateProfileString(section, key, def, SB, 255, this.FilePath);
            string result = SB.ToString();
            
            WriteConsole.WriteLine($"[INI_READ]: [{section}] {key} = '{result}'", ConsoleColor.DarkGray);
            return result;
        }
        
        /// <summary>
        /// Ler o arquivo .ini e retorna Char
        /// </summary>
        /// <param name="section">Seção = cabeçario [Config]</param>
        /// <param name="key">Local = nomedealgo = 0 </param>
        /// <param name="def">padrao = caso não encontre o valor no key, retorna o def</param>
        /// <returns>string</returns>
        public Char ReadChar(string section, string key, char def)
        {
            StringBuilder SB = new StringBuilder(255);
            GetPrivateProfileString(section, key, def.ToString(), SB, 255, this.FilePath);
            return Convert.ToChar(SB.ToString());
        }
        
        /// <summary>
        /// Ler o arquivo .ini e retorna Int32
        /// </summary>
        /// <param name="section">Seção = cabeçario [Config]</param>
        /// <param name="key">Local = nomedealgo = 0 </param>
        /// <param name="def">padrao = caso não encontre o valor no key, retorna o def</param>
        /// <returns>string</returns>
        public Int32 ReadInt32(string section, string key, int def)
        {
            StringBuilder SB = new StringBuilder(255);
            GetPrivateProfileString(section, key, def.ToString(), SB, 255, this.FilePath);
            return Convert.ToInt32(SB.ToString());
        }
        
        /// <summary>
        /// Ler o arquivo .ini e retorna UInt32
        /// </summary>
        /// <param name="section">Seção = cabeçario [Config]</param>
        /// <param name="key">Local = nomedealgo = 0 </param>
        /// <param name="def">padrao = caso não encontre o valor no key, retorna o def</param>
        /// <returns>string</returns>
        public UInt32 ReadUInt32(string section, string key)
        {
            string value = ReadString(section, key);
            uint result = Convert.ToUInt32(value);
            WriteConsole.WriteLine($"[INI_READ]: [{section}] {key} = {result}", ConsoleColor.DarkGray);
            return result;
        }
        
        public UInt32 ReadUInt32(string section, string key, uint def)
        {
            StringBuilder SB = new StringBuilder(255);
            GetPrivateProfileString(section, key, def.ToString(), SB, 255, this.FilePath);
            string rawValue = SB.ToString();
            uint result = Convert.ToUInt32(rawValue);
            WriteConsole.WriteLine($"[INI_READ]: [{section}] {key} = {result}", ConsoleColor.DarkGray);
            return result;
        }
        
        /// <summary>
        /// Ler o arquivo .ini e retorna Int64
        /// </summary>
        /// <param name="section">Seção = cabeçario [Config]</param>
        /// <param name="key">Local = nomedealgo = 0 </param>
        /// <param name="def">padrao = caso não encontre o valor no key, retorna o def</param>
        /// <returns>string</returns>
        public Int64 ReadInt64(string section, string key, long def)
        {
            StringBuilder SB = new StringBuilder(255);
            GetPrivateProfileString(section, key, def.ToString(), SB, 255, this.FilePath);
            return Convert.ToInt64(SB.ToString());
        }
        
        /// <summary>
        /// Ler o arquivo .ini e retorna UInt64
        /// </summary>
        /// <param name="section">Seção = cabeçario [Config]</param>
        /// <param name="key">Local = nomedealgo = 0 </param>
        /// <param name="def">padrao = caso não encontre o valor no key, retorna o def</param>
        /// <returns>string</returns>
        public UInt64 ReadUInt64(string section, string key, ulong def)
        {
            StringBuilder SB = new StringBuilder(255);
            GetPrivateProfileString(section, key, def.ToString(), SB, 255, this.FilePath);
            return Convert.ToUInt64(SB.ToString());
        }
        
        /// <summary>
        /// Ler o arquivo .ini e retorna Int16
        /// </summary>
        /// <param name="section">Seção = cabeçario [Config]</param>
        /// <param name="key">Local = nomedealgo = 0 </param>
        /// <param name="def">padrao = caso não encontre o valor no key, retorna o def</param>
        /// <returns>string</returns>
        public Int16 ReadInt16(string section, string key, short def)
        {
            StringBuilder SB = new StringBuilder(255);
            GetPrivateProfileString(section, key, def.ToString(), SB, 255, this.FilePath);
            return Convert.ToInt16(SB.ToString());
        }
        
        /// <summary>
        /// Ler o arquivo .ini e retorna UInt16
        /// </summary>
        /// <param name="section">Seção = cabeçario [Config]</param>
        /// <param name="key">Local = nomedealgo = 0 </param>
        /// <param name="def">padrao = caso não encontre o valor no key, retorna o def</param>
        /// <returns>string</returns>
        public UInt16 ReadUInt16(string section, string key, ushort def)
        {
            StringBuilder SB = new StringBuilder(255);
            GetPrivateProfileString(section, key, def.ToString(), SB, 255, this.FilePath);
            return Convert.ToUInt16(SB.ToString());
        }
        
        /// <summary>
        /// Ler o arquivo .ini e retorna Byte
        /// </summary>
        /// <param name="section">Seção = cabeçario [Config]</param>
        /// <param name="key">Local = nomedealgo = 0 </param>
        /// <param name="def">padrao = caso não encontre o valor no key, retorna o def</param>
        /// <returns>string</returns>
        public Byte ReadByte(string section, string key, Byte def)
        {
            StringBuilder SB = new StringBuilder(255);
            GetPrivateProfileString(section, key, def.ToString(), SB, 255, this.FilePath);
            return Convert.ToByte(SB.ToString());
        }
        
        /// <summary>
        /// Ler o arquivo .ini e retorna SByte
        /// </summary>
        /// <param name="section">Seção = cabeçario [Config]</param>
        /// <param name="key">Local = nomedealgo = 0 </param>
        /// <param name="def">padrao = caso não encontre o valor no key, retorna o def</param>
        /// <returns>string</returns>
        public SByte ReadSByte(string section, string key, sbyte def)
        {
            StringBuilder SB = new StringBuilder(255);
            GetPrivateProfileString(section, key, def.ToString(), SB, 255, this.FilePath);
            return Convert.ToSByte(SB.ToString());
        }

        /// <summary>
        /// Ler o arquivo .ini e retorna bool
        /// </summary>
        /// <param name="section">Seção = cabeçario [Config]</param>
        /// <param name="key">Local = nomedealgo = 0 </param>
        /// <param name="def">padrao = caso não encontre o valor no key, retorna o def</param>
        /// <returns>string</returns>
        public bool ReadBool(string section, string key, bool def)
        {
            StringBuilder SB = new StringBuilder(255);
            GetPrivateProfileString(section, key, def.ToString(), SB, 255, this.FilePath);
            return Convert.ToBoolean(SB.ToString());
        }

        #region IDisposable Support
        private bool disposedValue = false; // Para detectar chamadas redundantes

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                }

                disposedValue = true;
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
        #endregion
    }
}