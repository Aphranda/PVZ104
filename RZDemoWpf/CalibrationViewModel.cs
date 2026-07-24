using Microsoft.Win32;
using PVZ104;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.Serialization.Json;
using System.Text;
using System.Windows;
using System.Windows.Media;

namespace RZDemoWpf
{
    public sealed class CalibrationViewModel : INotifyPropertyChanged
    {
        private readonly string selectedProjectItemNumber;
        private readonly int currentAxisIndex;
        private CalibrationFitResult linearResult;
        private DiscreteCompensationResult discreteResult;
        private string linearCsvPath = "";
        private string linearPointCountText = "0";
        private string linearSlopeText = "--";
        private string linearInterceptText = "--";
        private string linearScaleFactorText = "--";
        private string linearRmsErrorText = "--";
        private string discreteCsvPath = "";
        private string discretePointCountText = "0";
        private string discreteStartPosText = "--";
        private string discreteCmpLenText = "--";
        private string discreteMinPulseText = "--";
        private string discreteMaxPulseText = "--";
        private string discreteMaxResidualText = "--";
        private string effectiveScaleText = "--";
        private string statusText = "等待导入";
        private Brush statusBrush = Brushes.DimGray;
        private IEnumerable<CalibrationPlotSeries> linearFitPlotSeries = Array.Empty<CalibrationPlotSeries>();
        private IEnumerable<CalibrationPlotSeries> linearResidualPlotSeries = Array.Empty<CalibrationPlotSeries>();
        private IEnumerable<CalibrationPlotSeries> discreteCompensationPlotSeries = Array.Empty<CalibrationPlotSeries>();
        private IEnumerable<CalibrationPlotSeries> discreteResidualPlotSeries = Array.Empty<CalibrationPlotSeries>();

        public CalibrationViewModel(string selectedProjectItemNumber, string selectedAxis, int currentAxisIndex)
        {
            this.selectedProjectItemNumber = selectedProjectItemNumber;
            SelectedAxis = selectedAxis;
            this.currentAxisIndex = currentAxisIndex;
            ConfigPathText = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "MotionModule.json");

            ImportLinearCsvCommand = new RelayCommand(_ => ImportLinearCsv());
            WriteLinearJsonCommand = new RelayCommand(_ => WriteLinearJson(), _ => linearResult != null);
            ImportDiscreteCsvCommand = new RelayCommand(_ => ImportDiscreteCsv());
            WriteDiscreteJsonCommand = new RelayCommand(_ => WriteDiscreteJson(), _ => discreteResult != null);
            RefreshEffectiveScale();
        }

        public event PropertyChangedEventHandler PropertyChanged;

        public RelayCommand ImportLinearCsvCommand { get; }
        public RelayCommand WriteLinearJsonCommand { get; }
        public RelayCommand ImportDiscreteCsvCommand { get; }
        public RelayCommand WriteDiscreteJsonCommand { get; }

        public string ProjectText => string.IsNullOrWhiteSpace(selectedProjectItemNumber) ? "默认项目" : selectedProjectItemNumber;
        public string SelectedAxis { get; }
        public string HeaderTargetText => ProjectText + " / " + SelectedAxis;
        public string ConfigPathText { get; }

        public string LinearCsvPath
        {
            get => linearCsvPath;
            private set => SetProperty(ref linearCsvPath, value);
        }

        public string LinearPointCountText
        {
            get => linearPointCountText;
            private set => SetProperty(ref linearPointCountText, value);
        }

        public string LinearSlopeText
        {
            get => linearSlopeText;
            private set => SetProperty(ref linearSlopeText, value);
        }

        public string LinearInterceptText
        {
            get => linearInterceptText;
            private set => SetProperty(ref linearInterceptText, value);
        }

        public string LinearScaleFactorText
        {
            get => linearScaleFactorText;
            private set => SetProperty(ref linearScaleFactorText, value);
        }

        public string LinearRmsErrorText
        {
            get => linearRmsErrorText;
            private set => SetProperty(ref linearRmsErrorText, value);
        }

        public string DiscreteCsvPath
        {
            get => discreteCsvPath;
            private set => SetProperty(ref discreteCsvPath, value);
        }

        public string DiscretePointCountText
        {
            get => discretePointCountText;
            private set => SetProperty(ref discretePointCountText, value);
        }

        public string DiscreteStartPosText
        {
            get => discreteStartPosText;
            private set => SetProperty(ref discreteStartPosText, value);
        }

        public string DiscreteCmpLenText
        {
            get => discreteCmpLenText;
            private set => SetProperty(ref discreteCmpLenText, value);
        }

        public string DiscreteMinPulseText
        {
            get => discreteMinPulseText;
            private set => SetProperty(ref discreteMinPulseText, value);
        }

        public string DiscreteMaxPulseText
        {
            get => discreteMaxPulseText;
            private set => SetProperty(ref discreteMaxPulseText, value);
        }

        public string DiscreteMaxResidualText
        {
            get => discreteMaxResidualText;
            private set => SetProperty(ref discreteMaxResidualText, value);
        }

        public string EffectiveScaleText
        {
            get => effectiveScaleText;
            private set => SetProperty(ref effectiveScaleText, value);
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

        public IEnumerable<CalibrationPlotSeries> LinearFitPlotSeries
        {
            get => linearFitPlotSeries;
            private set => SetProperty(ref linearFitPlotSeries, value);
        }

        public IEnumerable<CalibrationPlotSeries> LinearResidualPlotSeries
        {
            get => linearResidualPlotSeries;
            private set => SetProperty(ref linearResidualPlotSeries, value);
        }

        public IEnumerable<CalibrationPlotSeries> DiscreteCompensationPlotSeries
        {
            get => discreteCompensationPlotSeries;
            private set => SetProperty(ref discreteCompensationPlotSeries, value);
        }

        public IEnumerable<CalibrationPlotSeries> DiscreteResidualPlotSeries
        {
            get => discreteResidualPlotSeries;
            private set => SetProperty(ref discreteResidualPlotSeries, value);
        }

        private void ImportLinearCsv()
        {
            string path = SelectCsvFile();
            if (path == null)
            {
                return;
            }

            try
            {
                linearResult = CalculateCalibrationFit(path);
                LinearCsvPath = path;
                LinearPointCountText = linearResult.Count.ToString(CultureInfo.InvariantCulture);
                LinearSlopeText = linearResult.Slope.ToString("0.##########", CultureInfo.InvariantCulture);
                LinearInterceptText = linearResult.Intercept.ToString("0.##########", CultureInfo.InvariantCulture);
                LinearScaleFactorText = linearResult.ScaleFactor.ToString("0.##########", CultureInfo.InvariantCulture);
                LinearRmsErrorText = linearResult.RmsError.ToString("0.##########", CultureInfo.InvariantCulture);
                LinearFitPlotSeries = BuildLinearFitSeries(linearResult);
                LinearResidualPlotSeries = BuildLinearResidualSeries(linearResult);
                SetSuccess("线性补偿计算完成");
            }
            catch (Exception ex)
            {
                linearResult = null;
                SetFailure("线性补偿异常：" + ex.Message);
            }
            finally
            {
                WriteLinearJsonCommand.RaiseCanExecuteChanged();
            }
        }

        private void WriteLinearJson()
        {
            if (linearResult == null)
            {
                return;
            }

            try
            {
                MotionConfigSelection selection = LoadSelectedMotionConfig();
                if (selection.Axis.LinearCompensationParameters == null)
                {
                    selection.Axis.LinearCompensationParameters = new LinearCompensationParametersConfigure();
                }

                selection.Axis.LinearCompensationParameters.Enabled = true;
                selection.Axis.LinearCompensationParameters.ScaleFactor = linearResult.ScaleFactor;
                selection.Axis.LinearCompensationParameters.ScaleOffset = 0.0;
                SaveMotionConfig(selection);
                RefreshEffectiveScale();
                SetSuccess("线性补偿已写入 JSON");
            }
            catch (Exception ex)
            {
                SetFailure("写入线性补偿异常：" + ex.Message);
            }
        }

        private void ImportDiscreteCsv()
        {
            string path = SelectCsvFile();
            if (path == null)
            {
                return;
            }

            try
            {
                MotionConfigSelection selection = LoadSelectedMotionConfig();
                double effectiveScale = selection.Axis.GetEffectiveScale(currentAxisIndex);
                discreteResult = CalculateDiscreteCompensation(path, effectiveScale);
                DiscreteCsvPath = path;
                DiscretePointCountText = discreteResult.Count.ToString(CultureInfo.InvariantCulture);
                DiscreteStartPosText = discreteResult.StartPos.ToString(CultureInfo.InvariantCulture);
                DiscreteCmpLenText = discreteResult.CmpLen.ToString(CultureInfo.InvariantCulture);
                DiscreteMinPulseText = discreteResult.MinPulse.ToString(CultureInfo.InvariantCulture);
                DiscreteMaxPulseText = discreteResult.MaxPulse.ToString(CultureInfo.InvariantCulture);
                DiscreteMaxResidualText = discreteResult.MaxAbsResidual.ToString("0.##########", CultureInfo.InvariantCulture);
                DiscreteCompensationPlotSeries = BuildDiscreteCompensationSeries(discreteResult);
                DiscreteResidualPlotSeries = BuildDiscreteResidualSeries(discreteResult);
                EffectiveScaleText = effectiveScale.ToString("0.##########", CultureInfo.InvariantCulture);
                SetSuccess("离散补偿计算完成");
            }
            catch (Exception ex)
            {
                discreteResult = null;
                SetFailure("离散补偿异常：" + ex.Message);
            }
            finally
            {
                WriteDiscreteJsonCommand.RaiseCanExecuteChanged();
            }
        }

        private void WriteDiscreteJson()
        {
            if (discreteResult == null)
            {
                return;
            }

            try
            {
                MotionConfigSelection selection = LoadSelectedMotionConfig();
                selection.Axis.DiscreteCompensationParameters = new CompensationParametersConfigure
                {
                    Num = discreteResult.Count,
                    StartPos = discreteResult.StartPos,
                    CmpLen = discreteResult.CmpLen,
                    PCmpPos = discreteResult.PositiveCompensation,
                    NCmpPos = discreteResult.NegativeCompensation
                };

                SaveMotionConfig(selection);
                SetSuccess("离散补偿已写入 JSON");
            }
            catch (Exception ex)
            {
                SetFailure("写入离散补偿异常：" + ex.Message);
            }
        }

        private void RefreshEffectiveScale()
        {
            try
            {
                MotionConfigSelection selection = LoadSelectedMotionConfig();
                EffectiveScaleText = selection.Axis.GetEffectiveScale(currentAxisIndex).ToString("0.##########", CultureInfo.InvariantCulture);
            }
            catch
            {
                EffectiveScaleText = "--";
            }
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

        private static string SelectCsvFile()
        {
            OpenFileDialog dialog = new OpenFileDialog
            {
                Filter = "CSV 文件 (*.csv)|*.csv|文本文件 (*.txt)|*.txt|所有文件 (*.*)|*.*",
                CheckFileExists = true,
                Multiselect = false
            };

            return dialog.ShowDialog() == true ? dialog.FileName : null;
        }

        private MotionConfigSelection LoadSelectedMotionConfig()
        {
            string configPath = ConfigPathText;
            if (!File.Exists(configPath))
            {
                throw new FileNotFoundException("未找到运行目录 MotionModule.json。", configPath);
            }

            DataContractJsonSerializer serializer = new DataContractJsonSerializer(typeof(MotionModuleConfigRoot));
            MotionModuleConfigRoot root;
            using (FileStream stream = File.OpenRead(configPath))
            {
                root = (MotionModuleConfigRoot)serializer.ReadObject(stream);
            }

            if (root == null || root.MotionConfigure == null || root.MotionConfigure.Length == 0)
            {
                throw new InvalidDataException("MotionModule.json 中项目配置无效。");
            }

            MotionProjectConfig project = root.MotionConfigure.FirstOrDefault(
                item => item != null &&
                    string.Equals(item.ItemNumber, selectedProjectItemNumber, StringComparison.OrdinalIgnoreCase));
            if (project == null)
            {
                project = root.MotionConfigure.FirstOrDefault(item => item != null);
            }

            if (project == null)
            {
                throw new InvalidDataException("MotionModule.json 未找到可写入的项目配置。");
            }

            MotionAxisConfig axis = project.GetAxis(currentAxisIndex);
            if (axis == null)
            {
                throw new InvalidDataException("MotionModule.json 未找到 " + SelectedAxis + " 配置。");
            }

            axis.Validate(currentAxisIndex);

            return new MotionConfigSelection
            {
                ConfigPath = configPath,
                Serializer = serializer,
                Root = root,
                Axis = axis
            };
        }

        private static void SaveMotionConfig(MotionConfigSelection selection)
        {
            using (FileStream stream = File.Create(selection.ConfigPath))
            {
                selection.Serializer.WriteObject(stream, selection.Root);
            }
        }

        private static CalibrationFitResult CalculateCalibrationFit(string path)
        {
            List<CalibrationSample> samples = new List<CalibrationSample>();
            foreach (string line in File.ReadLines(path, Encoding.UTF8))
            {
                if (TryParseCalibrationLine(line, out double command, out double measured))
                {
                    samples.Add(new CalibrationSample { Command = command, Measured = measured });
                }
            }

            if (samples.Count < 2)
            {
                throw new InvalidDataException("CSV 至少需要两行有效数据。");
            }

            double meanX = samples.Average(item => item.Command);
            double meanY = samples.Average(item => item.Measured);
            double numerator = 0;
            double denominator = 0;

            foreach (CalibrationSample sample in samples)
            {
                double dx = sample.Command - meanX;
                double dy = sample.Measured - meanY;
                numerator += dx * dy;
                denominator += dx * dx;
            }

            if (Math.Abs(denominator) < double.Epsilon)
            {
                throw new InvalidDataException("命令角度数据不能全部相同。");
            }

            double slope = numerator / denominator;
            if (slope <= 0)
            {
                throw new InvalidDataException("拟合斜率必须大于 0。");
            }

            double intercept = meanY - slope * meanX;
            double squaredError = 0;
            foreach (CalibrationSample sample in samples)
            {
                sample.Fitted = slope * sample.Command + intercept;
                sample.Residual = sample.Measured - sample.Fitted;
                squaredError += sample.Residual * sample.Residual;
            }

            return new CalibrationFitResult
            {
                Count = samples.Count,
                Slope = slope,
                Intercept = intercept,
                ScaleFactor = 1.0 / slope,
                RmsError = Math.Sqrt(squaredError / samples.Count),
                Samples = samples.OrderBy(item => item.Command).ToArray()
            };
        }

        private static DiscreteCompensationResult CalculateDiscreteCompensation(string path, double effectiveScale)
        {
            if (effectiveScale <= 0)
            {
                throw new InvalidDataException("当前轴有效 scale 必须大于 0。");
            }

            List<DiscreteCompensationSample> samples = new List<DiscreteCompensationSample>();
            foreach (string line in File.ReadLines(path, Encoding.UTF8))
            {
                if (TryParseDiscreteCompensationLine(line, out DiscreteCompensationSample sample))
                {
                    samples.Add(sample);
                }
            }

            if (samples.Count < 2)
            {
                throw new InvalidDataException("CSV 至少需要两行有效离散补偿数据。");
            }

            samples = samples.OrderBy(item => item.Command).ToList();
            if (samples.Count == 361 && Math.Abs((samples[samples.Count - 1].Command - samples[0].Command) - 360.0) < 0.000001)
            {
                samples.RemoveAt(samples.Count - 1);
            }

            if (samples.Count < 2 || samples.Count > 360)
            {
                throw new InvalidDataException("离散补偿点数必须在 2 到 360 之间；0-360 闭合数据会自动去掉最后一个 360° 重复点。");
            }

            for (int i = 1; i < samples.Count; i++)
            {
                if (Math.Abs(samples[i].Command - samples[i - 1].Command) < 0.000001)
                {
                    throw new InvalidDataException("CSV 存在重复命令角度：" + samples[i].Command.ToString("0.##########", CultureInfo.InvariantCulture));
                }
            }

            double stepDegrees = CalculateNominalStepDegrees(samples);
            int startPos = ToInt32Pulse(samples[0].Command * effectiveScale);
            int cmpLen = ToInt32Pulse((samples[samples.Count - 1].Command - samples[0].Command + stepDegrees) * effectiveScale);
            if (cmpLen <= 0)
            {
                throw new InvalidDataException("离散补偿覆盖长度必须大于 0。");
            }

            short[] positive = new short[samples.Count];
            short[] negative = new short[samples.Count];
            short minPulse = short.MaxValue;
            short maxPulse = short.MinValue;
            double maxAbsResidual = 0;

            for (int i = 0; i < samples.Count; i++)
            {
                samples[i].PositiveResidual = samples[i].PositiveMeasured - samples[i].Command;
                samples[i].NegativeResidual = samples[i].NegativeMeasured - samples[i].Command;
                positive[i] = ToCompensationPulse(-samples[i].PositiveResidual * effectiveScale);
                negative[i] = ToCompensationPulse(-samples[i].NegativeResidual * effectiveScale);
                samples[i].PositiveCompensationPulse = positive[i];
                samples[i].NegativeCompensationPulse = negative[i];

                minPulse = Min(minPulse, positive[i], negative[i]);
                maxPulse = Max(maxPulse, positive[i], negative[i]);
                maxAbsResidual = Math.Max(maxAbsResidual, Math.Abs(samples[i].PositiveResidual));
                maxAbsResidual = Math.Max(maxAbsResidual, Math.Abs(samples[i].NegativeResidual));
            }

            return new DiscreteCompensationResult
            {
                Count = samples.Count,
                StartPos = startPos,
                CmpLen = cmpLen,
                PositiveCompensation = positive,
                NegativeCompensation = negative,
                MinPulse = minPulse,
                MaxPulse = maxPulse,
                MaxAbsResidual = maxAbsResidual,
                StepDegrees = stepDegrees,
                EffectiveScale = effectiveScale,
                Samples = samples.ToArray()
            };
        }

        private static IEnumerable<CalibrationPlotSeries> BuildLinearFitSeries(CalibrationFitResult result)
        {
            return new[]
            {
                new CalibrationPlotSeries
                {
                    Name = "实测",
                    Stroke = Brushes.SteelBlue,
                    StrokeThickness = 1.5,
                    Points = result.Samples.Select(item => new Point(item.Command, item.Measured)).ToArray()
                },
                new CalibrationPlotSeries
                {
                    Name = "拟合",
                    Stroke = Brushes.SeaGreen,
                    StrokeThickness = 1.8,
                    Points = result.Samples.Select(item => new Point(item.Command, item.Fitted)).ToArray()
                }
            };
        }

        private static IEnumerable<CalibrationPlotSeries> BuildLinearResidualSeries(CalibrationFitResult result)
        {
            return new[]
            {
                new CalibrationPlotSeries
                {
                    Name = "残差",
                    Stroke = Brushes.DarkOrange,
                    StrokeThickness = 1.6,
                    Points = result.Samples.Select(item => new Point(item.Command, item.Residual)).ToArray()
                }
            };
        }

        private static IEnumerable<CalibrationPlotSeries> BuildDiscreteCompensationSeries(DiscreteCompensationResult result)
        {
            return new[]
            {
                new CalibrationPlotSeries
                {
                    Name = "正向",
                    Stroke = Brushes.SteelBlue,
                    StrokeThickness = 1.5,
                    Points = result.Samples.Select(item => new Point(item.Command, item.PositiveCompensationPulse)).ToArray()
                },
                new CalibrationPlotSeries
                {
                    Name = "反向",
                    Stroke = Brushes.Firebrick,
                    StrokeThickness = 1.5,
                    Points = result.Samples.Select(item => new Point(item.Command, item.NegativeCompensationPulse)).ToArray()
                }
            };
        }

        private static IEnumerable<CalibrationPlotSeries> BuildDiscreteResidualSeries(DiscreteCompensationResult result)
        {
            return new[]
            {
                new CalibrationPlotSeries
                {
                    Name = "正向残差",
                    Stroke = Brushes.SteelBlue,
                    StrokeThickness = 1.5,
                    Points = result.Samples.Select(item => new Point(item.Command, item.PositiveResidual)).ToArray()
                },
                new CalibrationPlotSeries
                {
                    Name = "反向残差",
                    Stroke = Brushes.Firebrick,
                    StrokeThickness = 1.5,
                    Points = result.Samples.Select(item => new Point(item.Command, item.NegativeResidual)).ToArray()
                }
            };
        }

        private static bool TryParseCalibrationLine(string line, out double command, out double actual)
        {
            command = 0;
            actual = 0;

            if (string.IsNullOrWhiteSpace(line))
            {
                return false;
            }

            string trimmed = line.Trim();
            if (trimmed.StartsWith("#", StringComparison.Ordinal))
            {
                return false;
            }

            string[] parts = trimmed.Split(new[] { ',', ';', '\t', ' ' }, StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length < 2)
            {
                return false;
            }

            return TryParseFlexibleDouble(parts[0], out command) &&
                   TryParseFlexibleDouble(parts[1], out actual);
        }

        private static bool TryParseDiscreteCompensationLine(string line, out DiscreteCompensationSample sample)
        {
            sample = null;

            if (string.IsNullOrWhiteSpace(line))
            {
                return false;
            }

            string trimmed = line.Trim();
            if (trimmed.StartsWith("#", StringComparison.Ordinal))
            {
                return false;
            }

            string[] parts = trimmed.Split(new[] { ',', ';', '\t', ' ' }, StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length < 2)
            {
                return false;
            }

            if (!TryParseFlexibleDouble(parts[0], out double command) ||
                !TryParseFlexibleDouble(parts[1], out double positiveMeasured))
            {
                return false;
            }

            double negativeMeasured = positiveMeasured;
            if (parts.Length >= 3 && !TryParseFlexibleDouble(parts[2], out negativeMeasured))
            {
                return false;
            }

            sample = new DiscreteCompensationSample
            {
                Command = command,
                PositiveMeasured = positiveMeasured,
                NegativeMeasured = negativeMeasured
            };
            return true;
        }

        private static double CalculateNominalStepDegrees(List<DiscreteCompensationSample> samples)
        {
            List<double> steps = new List<double>();
            for (int i = 1; i < samples.Count; i++)
            {
                double step = samples[i].Command - samples[i - 1].Command;
                if (step <= 0)
                {
                    throw new InvalidDataException("命令角度必须递增或可排序为递增。");
                }

                steps.Add(step);
            }

            steps.Sort();
            double median = steps[steps.Count / 2];
            for (int i = 0; i < steps.Count; i++)
            {
                if (Math.Abs(steps[i] - median) > Math.Max(0.001, Math.Abs(median) * 0.01))
                {
                    throw new InvalidDataException("离散补偿角度间隔不一致。");
                }
            }

            return median;
        }

        private static bool TryParseFlexibleDouble(string value, out double result)
        {
            return double.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out result) ||
                   double.TryParse(value, NumberStyles.Float, CultureInfo.CurrentCulture, out result);
        }

        private static int ToInt32Pulse(double pulse)
        {
            if (pulse >= int.MaxValue)
            {
                return int.MaxValue;
            }

            if (pulse <= int.MinValue)
            {
                return int.MinValue;
            }

            return (int)Math.Round(pulse);
        }

        private static short ToCompensationPulse(double pulse)
        {
            double rounded = Math.Round(pulse);
            if (rounded > short.MaxValue || rounded < short.MinValue)
            {
                throw new InvalidDataException(
                    "离散补偿值超出 Int16 范围：" + rounded.ToString("0", CultureInfo.InvariantCulture) + " pulse。");
            }

            return (short)rounded;
        }

        private static short Min(short current, short first, short second)
        {
            return (short)Math.Min(current, Math.Min(first, second));
        }

        private static short Max(short current, short first, short second)
        {
            return (short)Math.Max(current, Math.Max(first, second));
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

        private sealed class MotionConfigSelection
        {
            public string ConfigPath;
            public DataContractJsonSerializer Serializer;
            public MotionModuleConfigRoot Root;
            public MotionAxisConfig Axis;
        }

        private sealed class CalibrationSample
        {
            public double Command;
            public double Measured;
            public double Fitted;
            public double Residual;
        }

        private sealed class CalibrationFitResult
        {
            public int Count;
            public double Slope;
            public double Intercept;
            public double ScaleFactor;
            public double RmsError;
            public CalibrationSample[] Samples;
        }

        private sealed class DiscreteCompensationSample
        {
            public double Command;
            public double PositiveMeasured;
            public double NegativeMeasured;
            public double PositiveResidual;
            public double NegativeResidual;
            public short PositiveCompensationPulse;
            public short NegativeCompensationPulse;
        }

        private sealed class DiscreteCompensationResult
        {
            public int Count;
            public int StartPos;
            public int CmpLen;
            public short[] PositiveCompensation;
            public short[] NegativeCompensation;
            public short MinPulse;
            public short MaxPulse;
            public double MaxAbsResidual;
            public double StepDegrees;
            public double EffectiveScale;
            public DiscreteCompensationSample[] Samples;
        }
    }
}
