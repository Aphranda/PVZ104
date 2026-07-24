using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.IO;
using System.Threading.Tasks;

namespace PVZ104
{
    public class FileConfiguration
    {
        private string relinipath = System.IO.Directory.GetCurrentDirectory() + "\\runparam.ini";//程序运行目录
        private string axisConfigPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "axisconfig.ini");
        IniHelper iniHelper = new IniHelper();

        public FileConfiguration()
        {
            iniHelper.inipath = relinipath;
        }

        /// <summary>
        /// 创建空配置
        /// </summary>
        /// <param name="ini"></param>
        /// <param name="count"></param>
        public void NewInifile(int count)
        {
            IniHelper ini = iniHelper;
            int n = 257;
            for (int i = n; i < count + n; i++)
            {
                ini.IniWriteValue(i.ToString(), "POS", "36");
                ini.IniWriteValue(i.ToString(), "VEL", "50");
                ini.IniWriteValue(i.ToString(), "ACC", "25");
                ini.IniWriteValue(i.ToString(), "DEC", "25");
                ini.IniWriteValue(i.ToString(), "JUMPVEL", "0.0");
                ini.IniWriteValue(i.ToString(), "ENDVEL", "0.0");
                ini.IniWriteValue(i.ToString(), "MODE", "0");
                ini.IniWriteValue(i.ToString(), "SCALE", "18000");
                ini.IniWriteValue(i.ToString(), "ZEROPOS", "1");
                ini.IniWriteValue(i.ToString(), "SMOTH", "100");
                ini.IniWriteValue(i.ToString(), "DIRECTION", "true");
                ini.IniWriteValue(i.ToString(), "COMPOS", "0");
                ini.IniWriteValue(i.ToString(), "COMNEG", "0");
            }
        }

        /// <summary>
        /// 获取配置文件
        /// </summary>
        /// <param name="axishandle"></param>
        /// <param name="iniHelper"></param>
        /// <returns></returns>
        public MotionPara GetMotionPara(ushort axishandle)
        {
            MotionPara motionPara = new MotionPara();
            motionPara.AxisNumber = axishandle;
            motionPara.Pos = Convert.ToDouble(iniHelper.IniReadValue(axishandle.ToString(), "POS"));
            motionPara.Vel = Convert.ToDouble(iniHelper.IniReadValue(axishandle.ToString(), "VEL"));
            motionPara.Acc = Convert.ToDouble(iniHelper.IniReadValue(axishandle.ToString(), "ACC"));
            motionPara.Dec = Convert.ToDouble(iniHelper.IniReadValue(axishandle.ToString(), "DEC"));
            motionPara.Mode = Convert.ToInt32(iniHelper.IniReadValue(axishandle.ToString(), "MODE"));
            motionPara.Scale = Convert.ToDouble(iniHelper.IniReadValue(axishandle.ToString(), "SCALE"));
            motionPara.JumpVel = Convert.ToDouble(iniHelper.IniReadValue(axishandle.ToString(), "JUMPVEL"));
            motionPara.EndVel = Convert.ToDouble(iniHelper.IniReadValue(axishandle.ToString(), "ENDVEL"));
            motionPara.ZeroPos = Convert.ToInt32(iniHelper.IniReadValue(axishandle.ToString(), "ZEROPOS"));
            motionPara.Smoth = Convert.ToInt32(iniHelper.IniReadValue(axishandle.ToString(), "SMOTH"));
            motionPara.Direction = Convert.ToBoolean(iniHelper.IniReadValue(axishandle.ToString(), "DIRECTION"));
            return motionPara;
        }

        public short[] GetCompensationPara(ushort axishandle)
        {

            string[] comPos = iniHelper.IniReadValue(axishandle.ToString(), "COMPOS").Split(',');
            string[] comNeg = iniHelper.IniReadValue(axishandle.ToString(), "COMNEG").Split(',');

            int comPosLen = comPos.Length;
            int comNegLen = comNeg.Length;

            short[] comData = new short[comPosLen + comNegLen];

            for (int i = 0; i < comPos.Length; i++)
            {
                comData[i] = Convert.ToInt16(comPos[i]);
            }
            for (int i = comPosLen; i < comPosLen + comNegLen; i++)
            {
                comData[i] = Convert.ToInt16(comNeg[i - comPosLen]);
            }
            return comData;
        }

        public AxisMechanicalConfig[] GetDefaultAxisMechanicalConfigs()
        {
            return new AxisMechanicalConfig[]
            {
                new AxisMechanicalConfig { AxisIndex = 0, Scale = 18000, IsPosLmtDown = false, IsNegLmtDown = false },
                new AxisMechanicalConfig { AxisIndex = 1, Scale = 252505, IsPosLmtDown = false, IsNegLmtDown = false },
                new AxisMechanicalConfig { AxisIndex = 2, Scale = 30010.5, IsPosLmtDown = true, IsNegLmtDown = true },
                new AxisMechanicalConfig { AxisIndex = 3, Scale = 20000, IsPosLmtDown = true, IsNegLmtDown = false },
            };
        }

        public AxisMechanicalConfig[] GetAxisMechanicalConfigs(int count = 4)
        {
            AxisMechanicalConfig[] defaults = GetDefaultAxisMechanicalConfigs();
            if (!File.Exists(axisConfigPath))
            {
                SaveAxisMechanicalConfigs(defaults);
                return defaults.Take(count).Select(config => config.Clone()).ToArray();
            }

            IniHelper ini = new IniHelper(axisConfigPath);
            AxisMechanicalConfig[] configs = new AxisMechanicalConfig[count];
            for (int i = 0; i < count; i++)
            {
                AxisMechanicalConfig fallback = i < defaults.Length
                    ? defaults[i]
                    : new AxisMechanicalConfig { AxisIndex = i, Scale = 1, IsPosLmtDown = true, IsNegLmtDown = false };

                string section = fallback.AxisName;
                configs[i] = new AxisMechanicalConfig
                {
                    AxisIndex = i,
                    Scale = ReadDouble(ini, section, "SCALE", fallback.Scale),
                    IsPosLmtDown = ReadBool(ini, section, "POS_LMT_DOWN", fallback.IsPosLmtDown),
                    IsNegLmtDown = ReadBool(ini, section, "NEG_LMT_DOWN", fallback.IsNegLmtDown),
                };

                try
                {
                    configs[i].Validate();
                }
                catch (ArgumentOutOfRangeException)
                {
                    configs[i] = fallback.Clone();
                }
            }

            return configs;
        }

        public void SaveAxisMechanicalConfigs(IEnumerable<AxisMechanicalConfig> configs)
        {
            IniHelper ini = new IniHelper(axisConfigPath);
            foreach (AxisMechanicalConfig config in configs)
            {
                config.Validate();
                ini.IniWriteValue(config.AxisName, "SCALE", config.Scale.ToString(CultureInfo.InvariantCulture));
                ini.IniWriteValue(config.AxisName, "POS_LMT_DOWN", config.IsPosLmtDown.ToString());
                ini.IniWriteValue(config.AxisName, "NEG_LMT_DOWN", config.IsNegLmtDown.ToString());
            }
        }

        private double ReadDouble(IniHelper ini, string section, string key, double fallback)
        {
            string value = ini.IniReadValue(section, key);
            if (double.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out double result))
            {
                return result;
            }
            if (double.TryParse(value, NumberStyles.Float, CultureInfo.CurrentCulture, out result))
            {
                return result;
            }
            return fallback;
        }

        private bool ReadBool(IniHelper ini, string section, string key, bool fallback)
        {
            string value = ini.IniReadValue(section, key);
            if (bool.TryParse(value, out bool result))
            {
                return result;
            }
            if (value == "1")
            {
                return true;
            }
            if (value == "0")
            {
                return false;
            }
            return fallback;
        }
    }

    public class IniHelper
    {
        public string inipath;

        //声明API函数

        [DllImport("kernel32")]
        private static extern long WritePrivateProfileString(string section, string key, string val, string filePath);
        [DllImport("kernel32")]
        private static extern int GetPrivateProfileString(string section, string key, string def, StringBuilder retVal, int size, string filePath);
        /// <summary> 
        /// 构造方法 
        /// </summary> 
        /// <param name="INIPath">文件路径</param> 
        public IniHelper(string INIPath)
        {
            inipath = INIPath;
        }

        public IniHelper() { }

        /// <summary> 
        /// 写入INI文件 
        /// </summary> 
        /// <param name="Section">项目名称(如 [TypeName] )</param> 
        /// <param name="Key">键</param> 
        /// <param name="Value">值</param> 
        public void IniWriteValue(string Section, string Key, string Value)
        {
            WritePrivateProfileString(Section, Key, Value, this.inipath);
        }
        /// <summary> 
        /// 读出INI文件 
        /// </summary> 
        /// <param name="Section">项目名称(如 [TypeName] )</param> 
        /// <param name="Key">键</param> 
        public string IniReadValue(string Section, string Key)
        {
            StringBuilder temp = new StringBuilder(500);
            int i = GetPrivateProfileString(Section, Key, "", temp, 500, this.inipath);
            return temp.ToString();
        }
        /// <summary> 
        /// 验证文件是否存在 
        /// </summary> 
        /// <returns>布尔值</returns> 
        public bool ExistINIFile()
        {
            return File.Exists(inipath);
        }
    }
}
