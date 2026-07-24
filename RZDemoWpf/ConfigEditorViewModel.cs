using PVZ104;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.Serialization.Json;
using System.Text;
using System.Windows.Media;

namespace RZDemoWpf
{
    public sealed class ConfigEditorViewModel : INotifyPropertyChanged
    {
        private readonly DataContractJsonSerializer serializer = new DataContractJsonSerializer(typeof(MotionModuleConfigRoot));
        private MotionModuleConfigRoot root;
        private string selectedProjectItemNumber;
        private string selectedAxis;
        private string newProjectItemNumber = "";
        private string statusText = "等待编辑";
        private Brush statusBrush = Brushes.DimGray;

        private string baseScaleText;
        private string baseSmoothText;
        private string encoderText;
        private bool posLmtDown;
        private bool negLmtDown;

        private string homeModeText;
        private string homeDirText;
        private string homeAccText;
        private string homeScan1stVelText;
        private string homeScan2ndVelText;
        private string homeReScanEnText;
        private string homeEdgeText;
        private string homeLmtEdgeText;
        private string homeZEdgeText;
        private string homeIniRetPosText;
        private string homeRetSwOffsetText;
        private string homeSafeLenText;
        private string homeUsePreSetPtpParaText;

        private string jogAccText;
        private string jogDecText;
        private string jogSmoothText;

        private string moveAccText;
        private string moveDecText;
        private string moveSmoothText;
        private string moveStartVelText;
        private string moveEndVelText;

        private string triggerOutputChnText;
        private string triggerOutputTypeText;
        private string triggerChnTypeText;
        private string triggerDir1NoText;
        private string triggerDir2NoText;
        private string triggerPosSrcText;
        private string triggerStLevelText;
        private string triggerErrZoneText;
        private string triggerDirectOutZoneText;
        private string triggerVibrateRangeText;
        private string triggerGateTimeText;
        private string triggerMinIntervalTimeText;

        private bool linearEnabled;
        private string linearScaleFactorText;
        private string linearScaleOffsetText;

        private string discreteNumText;
        private string discreteStartPosText;
        private string discreteCmpLenText;
        private string discretePCmpPosText;
        private string discreteNCmpPosText;

        public ConfigEditorViewModel(string preferredProjectItemNumber, string preferredAxis)
        {
            ProjectItems = new ObservableCollection<string>();
            AxisItems = new ObservableCollection<string>();
            ConfigPathText = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "MotionModule.json");
            ReloadCommand = new RelayCommand(_ => Reload(SelectedProjectItemNumber, SelectedAxis));
            SaveCommand = new RelayCommand(_ => Save());
            AddProjectCommand = new RelayCommand(_ => AddProjectFromCurrent());
            Reload(preferredProjectItemNumber, preferredAxis);
        }

        public event PropertyChangedEventHandler PropertyChanged;

        public ObservableCollection<string> ProjectItems { get; }
        public ObservableCollection<string> AxisItems { get; }
        public RelayCommand ReloadCommand { get; }
        public RelayCommand SaveCommand { get; }
        public RelayCommand AddProjectCommand { get; }
        public string ConfigPathText { get; }
        public bool HasSaved { get; private set; }

        public string SelectedProjectItemNumber
        {
            get => selectedProjectItemNumber;
            set
            {
                if (SetProperty(ref selectedProjectItemNumber, value))
                {
                    RefreshAxisItems(null);
                    LoadSelectedAxisValues();
                }
            }
        }

        public string SelectedAxis
        {
            get => selectedAxis;
            set
            {
                if (SetProperty(ref selectedAxis, value))
                {
                    LoadSelectedAxisValues();
                }
            }
        }

        public string NewProjectItemNumber
        {
            get => newProjectItemNumber;
            set => SetProperty(ref newProjectItemNumber, value);
        }

        public string StatusText
        {
            get => statusText;
            private set => SetProperty(ref statusText, value);
        }

        public Brush StatusBrush
        {
            get => statusBrush;
            private set => SetProperty(ref statusBrush, value);
        }

        public string BaseScaleText { get => baseScaleText; set => SetProperty(ref baseScaleText, value); }
        public string BaseSmoothText { get => baseSmoothText; set => SetProperty(ref baseSmoothText, value); }
        public string EncoderText { get => encoderText; set => SetProperty(ref encoderText, value); }
        public bool PosLmtDown { get => posLmtDown; set => SetProperty(ref posLmtDown, value); }
        public bool NegLmtDown { get => negLmtDown; set => SetProperty(ref negLmtDown, value); }

        public string HomeModeText { get => homeModeText; set => SetProperty(ref homeModeText, value); }
        public string HomeDirText { get => homeDirText; set => SetProperty(ref homeDirText, value); }
        public string HomeAccText { get => homeAccText; set => SetProperty(ref homeAccText, value); }
        public string HomeScan1stVelText { get => homeScan1stVelText; set => SetProperty(ref homeScan1stVelText, value); }
        public string HomeScan2ndVelText { get => homeScan2ndVelText; set => SetProperty(ref homeScan2ndVelText, value); }
        public string HomeReScanEnText { get => homeReScanEnText; set => SetProperty(ref homeReScanEnText, value); }
        public string HomeEdgeText { get => homeEdgeText; set => SetProperty(ref homeEdgeText, value); }
        public string HomeLmtEdgeText { get => homeLmtEdgeText; set => SetProperty(ref homeLmtEdgeText, value); }
        public string HomeZEdgeText { get => homeZEdgeText; set => SetProperty(ref homeZEdgeText, value); }
        public string HomeIniRetPosText { get => homeIniRetPosText; set => SetProperty(ref homeIniRetPosText, value); }
        public string HomeRetSwOffsetText { get => homeRetSwOffsetText; set => SetProperty(ref homeRetSwOffsetText, value); }
        public string HomeSafeLenText { get => homeSafeLenText; set => SetProperty(ref homeSafeLenText, value); }
        public string HomeUsePreSetPtpParaText { get => homeUsePreSetPtpParaText; set => SetProperty(ref homeUsePreSetPtpParaText, value); }

        public string JogAccText { get => jogAccText; set => SetProperty(ref jogAccText, value); }
        public string JogDecText { get => jogDecText; set => SetProperty(ref jogDecText, value); }
        public string JogSmoothText { get => jogSmoothText; set => SetProperty(ref jogSmoothText, value); }

        public string MoveAccText { get => moveAccText; set => SetProperty(ref moveAccText, value); }
        public string MoveDecText { get => moveDecText; set => SetProperty(ref moveDecText, value); }
        public string MoveSmoothText { get => moveSmoothText; set => SetProperty(ref moveSmoothText, value); }
        public string MoveStartVelText { get => moveStartVelText; set => SetProperty(ref moveStartVelText, value); }
        public string MoveEndVelText { get => moveEndVelText; set => SetProperty(ref moveEndVelText, value); }

        public string TriggerOutputChnText { get => triggerOutputChnText; set => SetProperty(ref triggerOutputChnText, value); }
        public string TriggerOutputTypeText { get => triggerOutputTypeText; set => SetProperty(ref triggerOutputTypeText, value); }
        public string TriggerChnTypeText { get => triggerChnTypeText; set => SetProperty(ref triggerChnTypeText, value); }
        public string TriggerDir1NoText { get => triggerDir1NoText; set => SetProperty(ref triggerDir1NoText, value); }
        public string TriggerDir2NoText { get => triggerDir2NoText; set => SetProperty(ref triggerDir2NoText, value); }
        public string TriggerPosSrcText { get => triggerPosSrcText; set => SetProperty(ref triggerPosSrcText, value); }
        public string TriggerStLevelText { get => triggerStLevelText; set => SetProperty(ref triggerStLevelText, value); }
        public string TriggerErrZoneText { get => triggerErrZoneText; set => SetProperty(ref triggerErrZoneText, value); }
        public string TriggerDirectOutZoneText { get => triggerDirectOutZoneText; set => SetProperty(ref triggerDirectOutZoneText, value); }
        public string TriggerVibrateRangeText { get => triggerVibrateRangeText; set => SetProperty(ref triggerVibrateRangeText, value); }
        public string TriggerGateTimeText { get => triggerGateTimeText; set => SetProperty(ref triggerGateTimeText, value); }
        public string TriggerMinIntervalTimeText { get => triggerMinIntervalTimeText; set => SetProperty(ref triggerMinIntervalTimeText, value); }

        public bool LinearEnabled { get => linearEnabled; set => SetProperty(ref linearEnabled, value); }
        public string LinearScaleFactorText { get => linearScaleFactorText; set => SetProperty(ref linearScaleFactorText, value); }
        public string LinearScaleOffsetText { get => linearScaleOffsetText; set => SetProperty(ref linearScaleOffsetText, value); }

        public string DiscreteNumText { get => discreteNumText; set => SetProperty(ref discreteNumText, value); }
        public string DiscreteStartPosText { get => discreteStartPosText; set => SetProperty(ref discreteStartPosText, value); }
        public string DiscreteCmpLenText { get => discreteCmpLenText; set => SetProperty(ref discreteCmpLenText, value); }
        public string DiscretePCmpPosText { get => discretePCmpPosText; set => SetProperty(ref discretePCmpPosText, value); }
        public string DiscreteNCmpPosText { get => discreteNCmpPosText; set => SetProperty(ref discreteNCmpPosText, value); }

        private void Reload(string preferredProjectItemNumber, string preferredAxis)
        {
            try
            {
                if (!File.Exists(ConfigPathText))
                {
                    throw new FileNotFoundException("未找到运行目录 MotionModule.json。", ConfigPathText);
                }

                using (FileStream stream = File.OpenRead(ConfigPathText))
                {
                    root = (MotionModuleConfigRoot)serializer.ReadObject(stream);
                }

                if (root == null || root.MotionConfigure == null || root.MotionConfigure.Length == 0)
                {
                    throw new InvalidDataException("MotionModule.json 中项目配置无效。");
                }

                ProjectItems.Clear();
                foreach (string itemNumber in root.MotionConfigure.Where(item => item != null).Select(item => item.ItemNumber))
                {
                    ProjectItems.Add(itemNumber);
                }

                string selectedProject = ProjectItems.FirstOrDefault(
                    item => string.Equals(item, preferredProjectItemNumber, StringComparison.OrdinalIgnoreCase)) ?? ProjectItems.FirstOrDefault();
                selectedProjectItemNumber = selectedProject;
                OnPropertyChanged(nameof(SelectedProjectItemNumber));
                RefreshAxisItems(preferredAxis);
                LoadSelectedAxisValues();
                SetSuccess("配置已加载");
            }
            catch (Exception ex)
            {
                SetFailure("配置加载异常：" + ex.Message);
            }
        }

        private void RefreshAxisItems(string preferredAxis)
        {
            AxisItems.Clear();
            MotionProjectConfig project = GetSelectedProject();
            int count = project == null ? 0 : project.GetConfiguredAxisCount();
            for (int i = 0; i < count; i++)
            {
                AxisItems.Add(string.Format("Axis{0:00}", i + 1));
            }

            string axis = AxisItems.FirstOrDefault(item => string.Equals(item, preferredAxis, StringComparison.OrdinalIgnoreCase)) ?? AxisItems.FirstOrDefault();
            selectedAxis = axis;
            OnPropertyChanged(nameof(SelectedAxis));
        }

        private void LoadSelectedAxisValues()
        {
            MotionAxisConfig axis = GetSelectedAxis();
            if (axis == null)
            {
                return;
            }

            MotorBaseConfigure motor = axis.MotorBaseConfigure;
            BaseScaleText = Format(motor?.Scale);
            BaseSmoothText = Format(motor?.Smooth);
            EncoderText = Format(motor?.Encoder);
            PosLmtDown = motor?.PosLmtDown ?? false;
            NegLmtDown = motor?.NegLmtDown ?? false;

            HomeParametersConfigure home = axis.HomeParameters;
            HomeModeText = Format(home?.HomeMode);
            HomeDirText = Format(home?.Dir);
            HomeAccText = Format(home?.Acc);
            HomeScan1stVelText = Format(home?.Scan1stVel);
            HomeScan2ndVelText = Format(home?.Scan2ndVel);
            HomeReScanEnText = Format(home?.ReScanEn);
            HomeEdgeText = Format(home?.HomeEdge);
            HomeLmtEdgeText = Format(home?.LmtEdge);
            HomeZEdgeText = Format(home?.ZEdge);
            HomeIniRetPosText = Format(home?.IniRetPos);
            HomeRetSwOffsetText = Format(home?.RetSwOffset);
            HomeSafeLenText = Format(home?.SafeLen);
            HomeUsePreSetPtpParaText = Format(home?.UsePreSetPtpPara);

            JogParametersConfigure jog = axis.JogParameters;
            JogAccText = Format(jog?.Acc);
            JogDecText = Format(jog?.Dec);
            JogSmoothText = Format(jog?.Smooth);

            MoveParametersConfigure move = axis.MoveParameters;
            MoveAccText = Format(move?.Acc);
            MoveDecText = Format(move?.Dec);
            MoveSmoothText = Format(move?.Smooth);
            MoveStartVelText = Format(move?.StartVel);
            MoveEndVelText = Format(move?.EndVel);

            TriggerParametersConfigure trigger = axis.TriggerParameters;
            TriggerOutputChnText = Format(trigger?.OutputChn);
            TriggerOutputTypeText = Format(trigger?.OutputType);
            TriggerChnTypeText = Format(trigger?.ChnType);
            TriggerDir1NoText = Format(trigger?.Dir1No);
            TriggerDir2NoText = Format(trigger?.Dir2No);
            TriggerPosSrcText = Format(trigger?.PosSrc);
            TriggerStLevelText = Format(trigger?.StLevel);
            TriggerErrZoneText = Format(trigger?.ErrZone);
            TriggerDirectOutZoneText = Format(trigger?.DirectOutZone);
            TriggerVibrateRangeText = Format(trigger?.VibrateRange);
            TriggerGateTimeText = Format(trigger?.GateTime);
            TriggerMinIntervalTimeText = Format(trigger?.MinIntervalTime);

            LinearCompensationParametersConfigure linear = axis.LinearCompensationParameters;
            LinearEnabled = linear?.Enabled ?? false;
            LinearScaleFactorText = Format(linear?.ScaleFactor ?? 1.0);
            LinearScaleOffsetText = Format(linear?.ScaleOffset ?? 0.0);

            CompensationParametersConfigure discrete = axis.GetFullStrokeDiscreteCompensationParameters();
            DiscreteNumText = Format(discrete?.Num);
            DiscreteStartPosText = Format(discrete?.StartPos);
            DiscreteCmpLenText = Format(discrete?.CmpLen);
            DiscretePCmpPosText = FormatArray(discrete?.PCmpPos);
            DiscreteNCmpPosText = FormatArray(discrete?.NCmpPos);
        }

        private void Save()
        {
            try
            {
                ApplyCurrentAxisValues();
                SaveRoot();
                HasSaved = true;
                SetSuccess("配置已保存到 JSON");
            }
            catch (Exception ex)
            {
                SetFailure("保存异常：" + ex.Message);
            }
        }

        private void AddProjectFromCurrent()
        {
            try
            {
                string itemNumber = (NewProjectItemNumber ?? string.Empty).Trim();
                if (string.IsNullOrWhiteSpace(itemNumber))
                {
                    throw new InvalidDataException("请输入新项目型号。");
                }

                if (root == null || root.MotionConfigure == null || root.MotionConfigure.Length == 0)
                {
                    throw new InvalidDataException("MotionModule.json 中项目配置无效。");
                }

                bool exists = root.MotionConfigure.Any(
                    item => item != null &&
                        string.Equals(item.ItemNumber, itemNumber, StringComparison.OrdinalIgnoreCase));
                if (exists)
                {
                    throw new InvalidDataException("项目型号已存在：" + itemNumber);
                }

                MotionProjectConfig source = GetSelectedProject();
                if (source == null)
                {
                    throw new InvalidDataException("未选择可复制的项目配置。");
                }

                ApplyCurrentAxisValues();
                MotionProjectConfig copy = CloneProject(source);
                copy.ItemNumber = itemNumber;

                List<MotionProjectConfig> projects = root.MotionConfigure.Where(item => item != null).ToList();
                projects.Add(copy);
                root.MotionConfigure = projects.ToArray();
                SaveRoot();

                ProjectItems.Add(itemNumber);
                SelectedProjectItemNumber = itemNumber;
                NewProjectItemNumber = "";
                HasSaved = true;
                SetSuccess("已新增项目配置：" + itemNumber);
            }
            catch (Exception ex)
            {
                SetFailure("新增项目异常：" + ex.Message);
            }
        }

        private void ApplyCurrentAxisValues()
        {
            MotionAxisConfig axis = GetSelectedAxis();
            if (axis == null)
            {
                throw new InvalidDataException("未选择有效轴配置。");
            }

            axis.MotorBaseConfigure = new MotorBaseConfigure
            {
                Scale = ParseDouble(BaseScaleText, "scale"),
                Smooth = ParseDouble(BaseSmoothText, "smooth"),
                Encoder = ParseShort(EncoderText, "encoder"),
                PosLmtDown = PosLmtDown,
                NegLmtDown = NegLmtDown
            };

            axis.HomeParameters = new HomeParametersConfigure
            {
                HomeMode = ParseShort(HomeModeText, "homeMode"),
                Dir = ParseShort(HomeDirText, "dir"),
                Acc = ParseDouble(HomeAccText, "home acc"),
                Scan1stVel = ParseDouble(HomeScan1stVelText, "scan1stVel"),
                Scan2ndVel = ParseDouble(HomeScan2ndVelText, "scan2ndVel"),
                ReScanEn = ParseShort(HomeReScanEnText, "reScanEn"),
                HomeEdge = ParseShort(HomeEdgeText, "homeEdge"),
                LmtEdge = ParseShort(HomeLmtEdgeText, "lmtEdge"),
                ZEdge = ParseShort(HomeZEdgeText, "zEdge"),
                IniRetPos = ParseDouble(HomeIniRetPosText, "iniRetPos"),
                RetSwOffset = ParseDouble(HomeRetSwOffsetText, "retSwOffset"),
                SafeLen = ParseDouble(HomeSafeLenText, "safeLen"),
                UsePreSetPtpPara = ParseShort(HomeUsePreSetPtpParaText, "usePreSetPtpPara")
            };

            axis.JogParameters = new JogParametersConfigure
            {
                Acc = ParseDouble(JogAccText, "jog acc"),
                Dec = ParseDouble(JogDecText, "jog dec"),
                Smooth = ParseDouble(JogSmoothText, "jog smooth")
            };

            axis.MoveParameters = new MoveParametersConfigure
            {
                Acc = ParseDouble(MoveAccText, "move acc"),
                Dec = ParseDouble(MoveDecText, "move dec"),
                Smooth = ParseDouble(MoveSmoothText, "move smooth"),
                StartVel = ParseDouble(MoveStartVelText, "startVel"),
                EndVel = ParseDouble(MoveEndVelText, "endVel")
            };

            axis.TriggerParameters = new TriggerParametersConfigure
            {
                OutputChn = ParseShort(TriggerOutputChnText, "outputChn"),
                OutputType = ParseShort(TriggerOutputTypeText, "outputType"),
                ChnType = ParseShort(TriggerChnTypeText, "chnType"),
                Dir1No = ParseShort(TriggerDir1NoText, "dir1No"),
                Dir2No = ParseShort(TriggerDir2NoText, "dir2No"),
                PosSrc = ParseShort(TriggerPosSrcText, "posSrc"),
                StLevel = ParseShort(TriggerStLevelText, "stLevel"),
                ErrZone = ParseShort(TriggerErrZoneText, "errZone"),
                DirectOutZone = ParseShort(TriggerDirectOutZoneText, "directOutZone"),
                VibrateRange = ParseShort(TriggerVibrateRangeText, "vibrateRange"),
                GateTime = ParseInt(TriggerGateTimeText, "gateTime"),
                MinIntervalTime = ParseInt(TriggerMinIntervalTimeText, "minIntervalTime")
            };

            axis.LinearCompensationParameters = new LinearCompensationParametersConfigure
            {
                Enabled = LinearEnabled,
                ScaleFactor = ParseDouble(LinearScaleFactorText, "scale_factor"),
                ScaleOffset = ParseDouble(LinearScaleOffsetText, "scale_offset")
            };

            axis.DiscreteCompensationParameters = new CompensationParametersConfigure
            {
                Num = ParseInt(DiscreteNumText, "discrete num"),
                StartPos = ParseInt(DiscreteStartPosText, "discrete startPos"),
                CmpLen = ParseInt(DiscreteCmpLenText, "discrete cmpLen"),
                PCmpPos = ParseShortArray(DiscretePCmpPosText, "pCmpPos"),
                NCmpPos = ParseShortArray(DiscreteNCmpPosText, "nCmpPos")
            };
            axis.FullStrokeDiscreteCompensationParameters = null;
            axis.CompensationParameters = null;

            axis.Validate(GetSelectedAxisIndex());
        }

        private void SaveRoot()
        {
            using (FileStream stream = File.Create(ConfigPathText))
            {
                serializer.WriteObject(stream, root);
            }
        }

        private MotionProjectConfig CloneProject(MotionProjectConfig source)
        {
            using (MemoryStream stream = new MemoryStream())
            {
                serializer.WriteObject(stream, new MotionModuleConfigRoot
                {
                    Version = root.Version,
                    MotionConfigure = new[] { source }
                });
                stream.Position = 0;
                MotionModuleConfigRoot clonedRoot = (MotionModuleConfigRoot)serializer.ReadObject(stream);
                return clonedRoot.MotionConfigure[0];
            }
        }

        private MotionProjectConfig GetSelectedProject()
        {
            if (root == null || root.MotionConfigure == null)
            {
                return null;
            }

            return root.MotionConfigure.FirstOrDefault(
                item => item != null &&
                    string.Equals(item.ItemNumber, SelectedProjectItemNumber, StringComparison.OrdinalIgnoreCase));
        }

        private MotionAxisConfig GetSelectedAxis()
        {
            MotionProjectConfig project = GetSelectedProject();
            return project == null ? null : project.GetAxis(GetSelectedAxisIndex());
        }

        private int GetSelectedAxisIndex()
        {
            if (string.IsNullOrWhiteSpace(SelectedAxis) || SelectedAxis.Length < 6)
            {
                return 0;
            }

            if (int.TryParse(SelectedAxis.Substring(4), NumberStyles.Integer, CultureInfo.InvariantCulture, out int axisNumber))
            {
                return Math.Max(0, axisNumber - 1);
            }

            return 0;
        }

        private static string Format(double? value)
        {
            return value.HasValue ? value.Value.ToString("0.##########", CultureInfo.InvariantCulture) : "";
        }

        private static string Format(int? value)
        {
            return value.HasValue ? value.Value.ToString(CultureInfo.InvariantCulture) : "";
        }

        private static string Format(short? value)
        {
            return value.HasValue ? value.Value.ToString(CultureInfo.InvariantCulture) : "";
        }

        private static string FormatArray(short[] values)
        {
            return values == null ? "" : string.Join(", ", values.Select(item => item.ToString(CultureInfo.InvariantCulture)));
        }

        private static double ParseDouble(string value, string fieldName)
        {
            if (double.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out double result) ||
                double.TryParse(value, NumberStyles.Float, CultureInfo.CurrentCulture, out result))
            {
                return result;
            }

            throw new FormatException(fieldName + " 格式不正确。");
        }

        private static int ParseInt(string value, string fieldName)
        {
            if (int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out int result))
            {
                return result;
            }

            throw new FormatException(fieldName + " 格式不正确。");
        }

        private static short ParseShort(string value, string fieldName)
        {
            int valueAsInt = ParseInt(value, fieldName);
            if (valueAsInt < short.MinValue || valueAsInt > short.MaxValue)
            {
                throw new FormatException(fieldName + " 超出 Int16 范围。");
            }

            return (short)valueAsInt;
        }

        private static short[] ParseShortArray(string value, string fieldName)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return Array.Empty<short>();
            }

            string[] parts = value.Split(new[] { ',', ';', '\t', '\r', '\n', ' ' }, StringSplitOptions.RemoveEmptyEntries);
            List<short> result = new List<short>();
            foreach (string part in parts)
            {
                result.Add(ParseShort(part, fieldName));
            }

            return result.ToArray();
        }

        private void SetSuccess(string text)
        {
            StatusText = text;
            StatusBrush = Brushes.SeaGreen;
        }

        private void SetFailure(string text)
        {
            StatusText = text;
            StatusBrush = Brushes.Firebrick;
        }

        private bool SetProperty<T>(ref T storage, T value, [CallerMemberName] string propertyName = null)
        {
            if (Equals(storage, value))
            {
                return false;
            }

            storage = value;
            OnPropertyChanged(propertyName);
            return true;
        }

        private void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
