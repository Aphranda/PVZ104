using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Runtime.InteropServices;
using System.Runtime.Serialization.Json;
using System.Text;
using System.IO;

namespace PVZ104
{
    public class FileConfiguration
    {
        private readonly string relinipath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "runparam.ini");
        private readonly string axisConfigPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "axisconfig.ini");
        private readonly string motionModuleConfigPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "MotionModule.json");
        IniHelper iniHelper = new IniHelper();
        private MotionModuleConfigRoot motionConfigRoot;
        private MotionProjectConfig motionProjectConfig;
        private string selectedItemNumber;

        public FileConfiguration()
        {
            iniHelper.inipath = relinipath;
        }

        public string SelectedItemNumber
        {
            get { return selectedItemNumber; }
            set
            {
                if (!string.Equals(selectedItemNumber, value, StringComparison.OrdinalIgnoreCase))
                {
                    selectedItemNumber = value;
                    motionProjectConfig = null;
                }
            }
        }

        public string ActiveItemNumber
        {
            get { return GetMotionProjectConfig().ItemNumber; }
        }

        public string[] GetAvailableProjectItemNumbers()
        {
            MotionModuleConfigRoot root = GetMotionConfigRoot();
            return root.MotionConfigure
                .Where(item => item != null && !string.IsNullOrWhiteSpace(item.ItemNumber))
                .Select(item => item.ItemNumber)
                .ToArray();
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
            int axisIndex = axishandle >= 257 ? axishandle - 257 : axishandle;
            return GetMotionPara(axisIndex, axishandle);
        }

        public MotionPara GetMotionPara(int axisIndex, ushort axishandle)
        {
            MotionPara motionPara = new MotionPara();
            motionPara.AxisNumber = axishandle;
            MotionAxisConfig axisConfig = GetAxisConfig(axisIndex);

            motionPara.Scale = axisConfig.GetEffectiveScale(axisIndex);
            motionPara.Smoth = axisConfig.MoveParameters.Smooth.Value;
            motionPara.Acc = axisConfig.MoveParameters.Acc.Value;
            motionPara.Dec = axisConfig.MoveParameters.Dec.Value;
            motionPara.JumpVel = axisConfig.MoveParameters.StartVel.Value;
            motionPara.EndVel = axisConfig.MoveParameters.EndVel.Value;
            return motionPara;
        }

        public short[] GetCompensationPara(ushort axishandle)
        {
            int axisIndex = axishandle >= 257 ? axishandle - 257 : axishandle;
            CompensationParametersConfigure compensation = GetCompensationParameters(axisIndex);
            if (compensation == null || compensation.PCmpPos == null || compensation.NCmpPos == null)
            {
                throw new InvalidOperationException("MotionModule.json 缺少 axis" + (axisIndex + 1) + " discrete_compensation_parameters。");
            }

            int comPosLen = compensation.PCmpPos.Length;
            int comNegLen = compensation.NCmpPos.Length;

            short[] comData = new short[comPosLen + comNegLen];

            for (int i = 0; i < compensation.PCmpPos.Length; i++)
            {
                comData[i] = compensation.PCmpPos[i];
            }
            for (int i = comPosLen; i < comPosLen + comNegLen; i++)
            {
                comData[i] = compensation.NCmpPos[i - comPosLen];
            }
            return comData;
        }

        public AxisMechanicalConfig[] GetDefaultAxisMechanicalConfigs()
        {
            return new AxisMechanicalConfig[]
            {
                new AxisMechanicalConfig { AxisIndex = 0, Scale = 18000, IsPosLmtDown = false, IsNegLmtDown = false, Encoder = 256 },
            };
        }

        public AxisMechanicalConfig[] GetAxisMechanicalConfigs(int count = 4)
        {
            if (count < 0)
            {
                throw new ArgumentOutOfRangeException("count", "轴数量不能为负数。");
            }

            int configuredAxisCount = GetConfiguredAxisCount();
            int actualCount = Math.Min(count, configuredAxisCount);
            AxisMechanicalConfig[] configs = new AxisMechanicalConfig[actualCount];
            for (int i = 0; i < actualCount; i++)
            {
                MotionAxisConfig axisConfig = GetAxisConfig(i);
                configs[i] = new AxisMechanicalConfig
                {
                    AxisIndex = i,
                    Scale = axisConfig.MotorBaseConfigure.Scale.Value,
                    IsPosLmtDown = axisConfig.MotorBaseConfigure.PosLmtDown.Value,
                    IsNegLmtDown = axisConfig.MotorBaseConfigure.NegLmtDown.Value,
                    Encoder = axisConfig.MotorBaseConfigure.Encoder.Value,
                };

                configs[i].Validate();
            }

            return configs;
        }

        public HomePara GetHomePara(int axisIndex)
        {
            MotionAxisConfig axisConfig = GetAxisConfig(axisIndex);
            HomeParametersConfigure home = axisConfig.HomeParameters;
            return new HomePara
            {
                HomeMode = home.HomeMode.Value,
                IsNegHome = home.Dir.Value == 0,
                HomeAcc = home.Acc.Value,
                SerchHomeVel = home.Scan1stVel.Value,
                HomeBackVel = home.Scan2ndVel.Value,
                IsHomeTwice = home.ReScanEn.Value != 0,
                IsHomeUp = home.HomeEdge.Value != 0,
                IsLmtUp = home.LmtEdge.Value != 0,
                IsZUp = home.ZEdge.Value != 0,
                HomeOffsetBegin = home.IniRetPos.Value,
                HomeOffsetLmt = home.RetSwOffset.Value,
                HomeMaxPos = home.SafeLen.Value,
            };
        }

        public JogParametersConfigure GetJogParameters(int axisIndex)
        {
            return GetAxisConfig(axisIndex).JogParameters;
        }

        public TriggerParametersConfigure GetTriggerParameters(int axisIndex)
        {
            return GetAxisConfig(axisIndex).TriggerParameters;
        }

        public CompensationParametersConfigure GetCompensationParameters(int axisIndex)
        {
            return GetAxisConfig(axisIndex).GetFullStrokeDiscreteCompensationParameters();
        }

        public int GetConfiguredAxisCount()
        {
            return GetMotionProjectConfig().GetConfiguredAxisCount();
        }

        private MotionAxisConfig GetAxisConfig(int axisIndex)
        {
            MotionProjectConfig projectConfig = GetMotionProjectConfig();
            MotionAxisConfig axisConfig = projectConfig.GetAxis(axisIndex);
            if (axisConfig == null)
            {
                throw new InvalidOperationException("MotionModule.json 缺少 axis" + (axisIndex + 1) + " 配置。");
            }

            axisConfig.Validate(axisIndex);
            return axisConfig;
        }

        private MotionProjectConfig GetMotionProjectConfig()
        {
            if (motionProjectConfig != null)
            {
                return motionProjectConfig;
            }

            MotionModuleConfigRoot root = GetMotionConfigRoot();
            if (string.IsNullOrWhiteSpace(selectedItemNumber))
            {
                motionProjectConfig = root.MotionConfigure[0];
            }
            else
            {
                motionProjectConfig = root.MotionConfigure.FirstOrDefault(
                    item => item != null &&
                        string.Equals(item.ItemNumber, selectedItemNumber, StringComparison.OrdinalIgnoreCase));
                if (motionProjectConfig == null)
                {
                    throw new InvalidOperationException("MotionModule.json 未找到项目配置：" + selectedItemNumber + "。");
                }
            }

            if (motionProjectConfig == null)
            {
                throw new InvalidOperationException("MotionModule.json 中项目配置无效。");
            }

            return motionProjectConfig;
        }

        private MotionModuleConfigRoot GetMotionConfigRoot()
        {
            if (motionConfigRoot != null)
            {
                return motionConfigRoot;
            }

            if (!File.Exists(motionModuleConfigPath))
            {
                throw new FileNotFoundException("缺少运动模组配置文件。", motionModuleConfigPath);
            }

            using (FileStream stream = File.OpenRead(motionModuleConfigPath))
            {
                DataContractJsonSerializer serializer = new DataContractJsonSerializer(typeof(MotionModuleConfigRoot));
                motionConfigRoot = serializer.ReadObject(stream) as MotionModuleConfigRoot;
                if (motionConfigRoot == null ||
                    motionConfigRoot.MotionConfigure == null ||
                    motionConfigRoot.MotionConfigure.Length == 0)
                {
                    throw new InvalidOperationException("MotionModule.json 中 motion_configure 为空。");
                }
            }

            return motionConfigRoot;
        }

        public void SaveAxisMechanicalConfigs(IEnumerable<AxisMechanicalConfig> configs)
        {
            if (configs == null)
            {
                throw new ArgumentNullException("configs");
            }

            MotionProjectConfig projectConfig = GetMotionProjectConfig();
            foreach (AxisMechanicalConfig config in configs)
            {
                config.Validate();
                MotionAxisConfig axisConfig = projectConfig.GetAxis(config.AxisIndex);
                if (axisConfig == null || axisConfig.MotorBaseConfigure == null)
                {
                    throw new InvalidOperationException("MotionModule.json 缺少 " + config.AxisName + " motor_base_configure。");
                }

                axisConfig.MotorBaseConfigure.Scale = config.Scale;
                axisConfig.MotorBaseConfigure.PosLmtDown = config.IsPosLmtDown;
                axisConfig.MotorBaseConfigure.NegLmtDown = config.IsNegLmtDown;
                axisConfig.MotorBaseConfigure.Encoder = config.Encoder;
            }

            SaveMotionModuleConfig();
        }

        private void SaveMotionModuleConfig()
        {
            using (FileStream stream = File.Create(motionModuleConfigPath))
            {
                DataContractJsonSerializer serializer = new DataContractJsonSerializer(typeof(MotionModuleConfigRoot));
                serializer.WriteObject(stream, motionConfigRoot);
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

        private int ReadInt(IniHelper ini, string section, string key, int fallback)
        {
            string value = ini.IniReadValue(section, key);
            if (int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out int result))
            {
                return result;
            }
            if (int.TryParse(value, NumberStyles.Integer, CultureInfo.CurrentCulture, out result))
            {
                return result;
            }
            return fallback;
        }

        private short ReadInt16(string value, short fallback)
        {
            if (short.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out short result))
            {
                return result;
            }
            if (short.TryParse(value, NumberStyles.Integer, CultureInfo.CurrentCulture, out result))
            {
                return result;
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
