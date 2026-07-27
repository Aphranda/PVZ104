using PVZ104;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.Serialization.Json;
using System.Threading.Tasks;
using System.Windows.Media;
using System.Windows.Threading;

namespace RZDemoWpf
{
    internal sealed class MainViewModel : INotifyPropertyChanged, IDisposable
    {
        private const uint ServoStartupDelayMs = 3000;
        private readonly AutoGCApi controller = new AutoGCApi();
        private readonly DispatcherTimer pollTimer;
        private bool isConnected;
        private bool isInitialized;
        private bool isBusy;
        private bool isPolling;
        private string ipAddress = "192.168.1.10";
        private string selectedProjectItemNumber;
        private string selectedAxis = "Axis01";
        private string activeProjectText = "配置：未读取";
        private string connectionText = "未连接";
        private string motionStatusText = "未知";
        private string positionText = "--";
        private string speedText = "--";
        private string pollingText = "停止";
        private string busyText = "空闲";
        private string alarmId = "0";
        private string alarmDescription = "正常";
        private string homeStatusText = "未回零";
        private string lastResultText = "等待连接";
        private string diagnosticText = "无";
        private string footerText = "Ready";
        private bool isJogHolding;
        private bool isJogStopping;
        private Task<E_Result> currentJogStartTask = Task.FromResult(E_Result.E_FAILED);
        private string calibrationCsvPath = "";
        private string calibrationPointCountText = "0";
        private string calibrationSlopeText = "--";
        private string calibrationInterceptText = "--";
        private string calibrationScaleFactorText = "--";
        private string calibrationRmsErrorText = "--";
        private string discreteCsvPath = "";
        private string discretePointCountText = "0";
        private string discreteStartPosText = "--";
        private string discreteCmpLenText = "--";
        private string discreteMinPulseText = "--";
        private string discreteMaxPulseText = "--";
        private string discreteMaxResidualText = "--";
        private Brush connectionBrush = Brushes.Gray;
        private Brush statusBrush = Brushes.Gray;
        private Brush homeStatusBrush = Brushes.Gray;
        private Brush resultBrush = Brushes.DimGray;

        public MainViewModel()
        {
            AxisItems = new ObservableCollection<string> { "Axis01" };
            ConfigItems = new ObservableCollection<string>();
            LogEntries = new ObservableCollection<string>();

            TimeoutText = "60000";
            JogSpeedText = "5";
            HomeSpeedText = "10";
            HomeOffsetText = "0";
            MoveSpeedText = "10";
            RelativeMovePositionText = "10";
            AbsoluteMovePositionText = "90";
            TriggerStartText = "0";
            TriggerStopText = "360";
            TriggerStepText = "1";
            TriggerWidthText = "10";

            ConnectCommand = new RelayCommand(_ => RunOperationAsync("连接", ConnectCore), _ => !IsBusy && !IsConnected);
            DisconnectCommand = new RelayCommand(_ => RunOperationAsync("断开", DisconnectCore), _ => !IsBusy && IsConnected);
            InitCommand = new RelayCommand(_ => RunOperationAsync("初始化并上电", () => InitCore(false)), CanRunConnectedCommand);
            ServoResetCommand = new RelayCommand(_ => RunOperationAsync("重启伺服供电", () => InitCore(true)), CanRunConnectedCommand);
            StopCommand = new RelayCommand(_ => RunOperationAsync("停止", () => controller.Stop(CurrentAxis)), _ => IsConnected && IsInitialized);
            JogNegativeCommand = new RelayCommand(_ => RunOperationAsync("负向点动", () => controller.Jog(CurrentAxis, ParseDouble(JogSpeedText, "点动速度"), true)), CanRunMotionCommand);
            JogPositiveCommand = new RelayCommand(_ => RunOperationAsync("正向点动", () => controller.Jog(CurrentAxis, ParseDouble(JogSpeedText, "点动速度"), false)), CanRunMotionCommand);
            HomeCommand = new RelayCommand(_ => RunOperationAsync("回零", HomeCore), CanRunMotionCommand);
            ZeroCommand = new RelayCommand(_ => RunOperationAsync("清零", () => controller.Zero(CurrentAxis)), CanRunMotionCommand);
            MoveRelativeCommand = new RelayCommand(_ => RunOperationAsync("相对移动", MoveRelativeCore), CanRunMotionCommand);
            MoveAbsoluteCommand = new RelayCommand(_ => RunOperationAsync("绝对移动", MoveAbsoluteCore), CanRunMotionCommand);
            TriggerStartCommand = new RelayCommand(_ => RunOperationAsync("启动触发", TriggerStartCore), CanRunMotionCommand);
            TriggerStopCommand = new RelayCommand(_ => RunOperationAsync("停止触发", () => controller.TriggerStop(CurrentAxis)), CanRunMotionCommand);
            AutoHomeCommand = new RelayCommand(_ => RunOperationAsync("自动回零流程", AutoHomeCore), CanRunConnectedCommand);
            AutoIndexCommand = new RelayCommand(_ => RunOperationAsync("定位测试流程", AutoIndexCore), CanRunMotionCommand);
            OpenCalibrationCommand = new RelayCommand(_ => OpenCalibrationWindow(), _ => !IsBusy);
            OpenConfigEditorCommand = new RelayCommand(_ => OpenConfigEditorWindow(), _ => !IsBusy && !IsConnected);
            ClearLogCommand = new RelayCommand(_ => LogEntries.Clear());
            ImportCalibrationCsvCommand = new RelayCommand(_ => ImportCalibrationCsvAndUpdateJson());
            ImportDiscreteCompensationCsvCommand = new RelayCommand(_ => ImportDiscreteCompensationCsvAndUpdateJson());

            LoadProjectSummary();
            pollTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(250) };
            pollTimer.Tick += PollTimer_Tick;
        }

        public event PropertyChangedEventHandler PropertyChanged;

        public ObservableCollection<string> AxisItems { get; }
        public ObservableCollection<string> ConfigItems { get; }
        public ObservableCollection<string> LogEntries { get; }

        public RelayCommand ConnectCommand { get; }
        public RelayCommand DisconnectCommand { get; }
        public RelayCommand InitCommand { get; }
        public RelayCommand ServoResetCommand { get; }
        public RelayCommand StopCommand { get; }
        public RelayCommand JogNegativeCommand { get; }
        public RelayCommand JogPositiveCommand { get; }
        public RelayCommand HomeCommand { get; }
        public RelayCommand ZeroCommand { get; }
        public RelayCommand MoveRelativeCommand { get; }
        public RelayCommand MoveAbsoluteCommand { get; }
        public RelayCommand TriggerStartCommand { get; }
        public RelayCommand TriggerStopCommand { get; }
        public RelayCommand AutoHomeCommand { get; }
        public RelayCommand AutoIndexCommand { get; }
        public RelayCommand OpenCalibrationCommand { get; }
        public RelayCommand OpenConfigEditorCommand { get; }
        public RelayCommand ClearLogCommand { get; }
        public RelayCommand ImportCalibrationCsvCommand { get; }
        public RelayCommand ImportDiscreteCompensationCsvCommand { get; }

        public string IpAddress
        {
            get => ipAddress;
            set => SetProperty(ref ipAddress, value);
        }

        public string SelectedProjectItemNumber
        {
            get => selectedProjectItemNumber;
            set
            {
                if (SetProperty(ref selectedProjectItemNumber, value))
                {
                    ActiveProjectText = string.IsNullOrWhiteSpace(value) ? "配置：未选择" : "配置：" + value;
                }
            }
        }

        public string SelectedAxis
        {
            get => selectedAxis;
            set => SetProperty(ref selectedAxis, value);
        }

        public bool IsConfigurationSelectable => !IsConnected && !IsBusy;

        public string TimeoutText { get; set; }
        public string JogSpeedText { get; set; }
        public string HomeSpeedText { get; set; }
        public string HomeOffsetText { get; set; }
        public string MoveSpeedText { get; set; }
        public string RelativeMovePositionText { get; set; }
        public string AbsoluteMovePositionText { get; set; }
        public string TriggerStartText { get; set; }
        public string TriggerStopText { get; set; }
        public string TriggerStepText { get; set; }
        public string TriggerWidthText { get; set; }

        public string ActiveProjectText
        {
            get => activeProjectText;
            private set => SetProperty(ref activeProjectText, value);
        }

        public string ConnectionText
        {
            get => connectionText;
            private set => SetProperty(ref connectionText, value);
        }

        public string MotionStatusText
        {
            get => motionStatusText;
            private set => SetProperty(ref motionStatusText, value);
        }

        public string PositionText
        {
            get => positionText;
            private set => SetProperty(ref positionText, value);
        }

        public string SpeedText
        {
            get => speedText;
            private set => SetProperty(ref speedText, value);
        }

        public string PollingText
        {
            get => pollingText;
            private set => SetProperty(ref pollingText, value);
        }

        public string BusyText
        {
            get => busyText;
            private set => SetProperty(ref busyText, value);
        }

        public string AlarmId
        {
            get => alarmId;
            private set => SetProperty(ref alarmId, value);
        }

        public string AlarmDescription
        {
            get => alarmDescription;
            private set => SetProperty(ref alarmDescription, value);
        }

        public string HomeStatusText
        {
            get => homeStatusText;
            private set => SetProperty(ref homeStatusText, value);
        }

        public string LastResultText
        {
            get => lastResultText;
            private set => SetProperty(ref lastResultText, value);
        }

        public string DiagnosticText
        {
            get => diagnosticText;
            private set => SetProperty(ref diagnosticText, value);
        }

        public string FooterText
        {
            get => footerText;
            private set => SetProperty(ref footerText, value);
        }

        public string CalibrationCsvPath
        {
            get => calibrationCsvPath;
            private set => SetProperty(ref calibrationCsvPath, value);
        }

        public string CalibrationPointCountText
        {
            get => calibrationPointCountText;
            private set => SetProperty(ref calibrationPointCountText, value);
        }

        public string CalibrationSlopeText
        {
            get => calibrationSlopeText;
            private set => SetProperty(ref calibrationSlopeText, value);
        }

        public string CalibrationInterceptText
        {
            get => calibrationInterceptText;
            private set => SetProperty(ref calibrationInterceptText, value);
        }

        public string CalibrationScaleFactorText
        {
            get => calibrationScaleFactorText;
            private set => SetProperty(ref calibrationScaleFactorText, value);
        }

        public string CalibrationRmsErrorText
        {
            get => calibrationRmsErrorText;
            private set => SetProperty(ref calibrationRmsErrorText, value);
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

        public Brush ConnectionBrush
        {
            get => connectionBrush;
            private set => SetProperty(ref connectionBrush, value);
        }

        public Brush StatusBrush
        {
            get => statusBrush;
            private set => SetProperty(ref statusBrush, value);
        }

        public Brush HomeStatusBrush
        {
            get => homeStatusBrush;
            private set => SetProperty(ref homeStatusBrush, value);
        }

        public Brush ResultBrush
        {
            get => resultBrush;
            private set => SetProperty(ref resultBrush, value);
        }

        public bool IsConnected
        {
            get => isConnected;
            private set
            {
                if (SetProperty(ref isConnected, value))
                {
                    ConnectionText = value ? "已连接" : "未连接";
                    ConnectionBrush = value ? Brushes.SeaGreen : Brushes.Gray;
                    PollingText = value ? "运行" : "停止";
                    OnPropertyChanged(nameof(IsConfigurationSelectable));
                    RaiseCommandStates();
                }
            }
        }

        private bool IsInitialized
        {
            get => isInitialized;
            set
            {
                if (SetProperty(ref isInitialized, value))
                {
                    RaiseCommandStates();
                }
            }
        }

        public bool IsBusy
        {
            get => isBusy;
            private set
            {
                if (SetProperty(ref isBusy, value))
                {
                    BusyText = value ? "执行中" : "空闲";
                    OnPropertyChanged(nameof(IsConfigurationSelectable));
                    RaiseCommandStates();
                }
            }
        }

        private Dimension CurrentAxis => (Dimension)CurrentAxisIndex;

        private int CurrentAxisIndex
        {
            get
            {
                if (string.Equals(SelectedAxis, "Axis02", StringComparison.OrdinalIgnoreCase)) return 1;
                if (string.Equals(SelectedAxis, "Axis03", StringComparison.OrdinalIgnoreCase)) return 2;
                if (string.Equals(SelectedAxis, "Axis04", StringComparison.OrdinalIgnoreCase)) return 3;
                if (string.Equals(SelectedAxis, "Axis05", StringComparison.OrdinalIgnoreCase)) return 4;
                return 0;
            }
        }

        private bool CanRunMotionCommand(object parameter)
        {
            return IsConnected && IsInitialized && !IsBusy;
        }

        private bool CanRunConnectedCommand(object parameter)
        {
            return IsConnected && !IsBusy;
        }

        private E_Result ConnectCore()
        {
            byte[] ip = ParseIPv4(IpAddress);
            controller.ProjectItemNumber = SelectedProjectItemNumber;
            E_Result result = controller.Connect(ip);
            RunOnUi(() =>
            {
                IsConnected = result == E_Result.E_SUCCESS || result == E_Result.E_ALREADY_CONNECTED;
                IsInitialized = false;
                DiagnosticText = controller.LastConnectionMessage;
                if (IsConnected)
                {
                    pollTimer.Start();
                }
            });

            return result;
        }

        private E_Result DisconnectCore()
        {
            E_Result result = controller.Disconnect();
            RunOnUi(() =>
            {
                pollTimer.Stop();
                IsConnected = false;
                IsInitialized = false;
                MotionStatusText = "未知";
                StatusBrush = Brushes.Gray;
                PositionText = "--";
                SpeedText = "--";
                AlarmId = "--";
                AlarmDescription = "未连接";
                HomeStatusText = "未连接";
                HomeStatusBrush = Brushes.Gray;
                DiagnosticText = "无";
            });

            return result;
        }

        private E_Result InitCore(bool servoReset)
        {
            E_Result result = controller.Init(CurrentAxis, servoReset, ServoStartupDelayMs);
            RunOnUi(() =>
            {
                IsInitialized = result == E_Result.E_SUCCESS;
                DiagnosticText = controller.LastInitializationMessage;
                AppendLog(controller.LastInitializationMessage);
            });

            return result;
        }

        private E_Result HomeCore()
        {
            return controller.Home(
                CurrentAxis,
                ParseDouble(HomeSpeedText, "回零速度"),
                ParseDouble(HomeOffsetText, "回零偏置"),
                ParseTimeout());
        }

        private E_Result MoveRelativeCore()
        {
            return controller.MoveRelative(
                CurrentAxis,
                ParseDouble(MoveSpeedText, "移动速度"),
                ParseDouble(RelativeMovePositionText, "相对位置"),
                ParseTimeout());
        }

        private E_Result MoveAbsoluteCore()
        {
            return controller.MoveAbsolute(
                CurrentAxis,
                ParseDouble(MoveSpeedText, "移动速度"),
                ParseDouble(AbsoluteMovePositionText, "绝对位置"),
                ParseTimeout());
        }

        public void BeginJogHold(bool positive)
        {
            if (isJogHolding || isJogStopping || !CanRunMotionCommand(null))
            {
                return;
            }

            isJogHolding = true;
            string name = positive ? "正向点动" : "负向点动";
            _ = RunJogStartAsync(name, positive);
        }

        public async void EndJogHold()
        {
            if (!isJogHolding)
            {
                return;
            }

            isJogHolding = false;
            isJogStopping = true;
            try
            {
                try
                {
                    E_Result startResult = await currentJogStartTask;
                    if (startResult != E_Result.E_SUCCESS)
                    {
                        return;
                    }
                }
                catch
                {
                    // 启动异常会在 RunJogStartAsync 中发布，这里只保证释放动作继续尝试停轴。
                    return;
                }

                if (IsConnected && IsInitialized)
                {
                    await RunOperationTaskAsync("停止", () => controller.Stop(CurrentAxis));
                }
            }
            finally
            {
                isJogStopping = false;
            }
        }

        private E_Result TriggerStartCore()
        {
            return controller.Trigger(
                CurrentAxis,
                ParseDouble(TriggerStartText, "触发起点"),
                ParseDouble(TriggerStopText, "触发终点"),
                ParseDouble(TriggerStepText, "触发步长"),
                ParseInt(TriggerWidthText, "触发脉宽"),
                ParseTimeout());
        }

        private E_Result AutoHomeCore()
        {
            E_Result result = InitCore(false);
            if (result != E_Result.E_SUCCESS)
            {
                return result;
            }

            result = controller.Home(
                CurrentAxis,
                ParseDouble(HomeSpeedText, "回零速度"),
                ParseDouble(HomeOffsetText, "回零偏置"),
                ParseTimeout());
            if (result != E_Result.E_SUCCESS)
            {
                return result;
            }

            return controller.Zero(CurrentAxis);
        }

        private E_Result AutoIndexCore()
        {
            double speed = ParseDouble(MoveSpeedText, "移动速度");
            int timeout = ParseTimeout();
            double[] targets = { 0, 90, 180, 270 };
            foreach (double target in targets)
            {
                AppendLog("定位到 " + target.ToString("0.###", CultureInfo.InvariantCulture));
                E_Result result = controller.MoveAbsolute(CurrentAxis, speed, target, timeout);
                if (result != E_Result.E_SUCCESS)
                {
                    return result;
                }
            }

            return E_Result.E_SUCCESS;
        }

        private async void RunOperationAsync(string name, Func<E_Result> operation)
        {
            await RunOperationTaskAsync(name, operation);
        }

        private async Task RunOperationTaskAsync(string name, Func<E_Result> operation)
        {
            bool managesBusy = name != "停止";
            if (IsBusy && managesBusy)
            {
                return;
            }

            if (managesBusy)
            {
                IsBusy = true;
            }

            FooterText = name + " 中";
            AppendLog(name + " 开始");

            try
            {
                E_Result result = await Task.Run(operation);
                PublishResult(name, result);
                if (IsConnected)
                {
                    await RefreshStatusAsync();
                }
            }
            catch (Exception ex)
            {
                LastResultText = name + " 异常：" + ex.Message;
                ResultBrush = Brushes.Firebrick;
                AppendLog(name + " 异常：" + ex.GetType().Name + ": " + ex.Message);
            }
            finally
            {
                if (managesBusy)
                {
                    IsBusy = false;
                }

                FooterText = "Ready";
            }
        }

        private async Task RunJogStartAsync(string name, bool positive)
        {
            if (IsBusy)
            {
                return;
            }

            IsBusy = true;
            FooterText = name + " 中";
            AppendLog(name + " 开始");

            bool apiDirection = !positive;
            Task<E_Result> startTask = Task.Run(() => controller.Jog(CurrentAxis, ParseDouble(JogSpeedText, "点动速度"), apiDirection));
            currentJogStartTask = startTask;

            try
            {
                E_Result result = await startTask;
                PublishResult(name, result);
                if (result != E_Result.E_SUCCESS)
                {
                    isJogHolding = false;
                }

                if (IsConnected)
                {
                    await RefreshStatusAsync();
                }
            }
            catch (Exception ex)
            {
                isJogHolding = false;
                LastResultText = name + " 异常：" + ex.Message;
                ResultBrush = Brushes.Firebrick;
                AppendLog(name + " 异常：" + ex.GetType().Name + ": " + ex.Message);
            }
            finally
            {
                IsBusy = false;
                FooterText = "Ready";
            }
        }

        private void PublishResult(string name, E_Result result)
        {
            bool success = result == E_Result.E_SUCCESS || result == E_Result.E_ALREADY_CONNECTED || result == E_Result.E_ALREADY_DISCONNECTED;
            LastResultText = name + "：" + result;
            ResultBrush = success ? Brushes.SeaGreen : Brushes.Firebrick;
            AppendLog(name + " 结束：" + result);
        }

        private async void PollTimer_Tick(object sender, EventArgs e)
        {
            if (!IsConnected || isPolling)
            {
                return;
            }

            await RefreshStatusAsync();
        }

        private async Task RefreshStatusAsync()
        {
            if (isPolling)
            {
                return;
            }

            isPolling = true;
            try
            {
                StatusSnapshot snapshot = await Task.Run(ReadStatusSnapshot);
                ApplyStatusSnapshot(snapshot);
            }
            catch (Exception ex)
            {
                MotionStatusText = "读取异常";
                StatusBrush = Brushes.Firebrick;
                HomeStatusText = "读取异常";
                HomeStatusBrush = Brushes.Firebrick;
                AlarmDescription = "状态读取异常：" + ex.Message;
                AppendLog("状态读取异常：" + ex.Message);
            }
            finally
            {
                isPolling = false;
            }
        }

        private StatusSnapshot ReadStatusSnapshot()
        {
            StatusSnapshot snapshot = new StatusSnapshot();
            snapshot.StatusResult = controller.GetStatus(CurrentAxis, out snapshot.Status, out snapshot.Alarm);
            snapshot.PositionResult = controller.GetPosition(CurrentAxis, out snapshot.Position);
            snapshot.SpeedResult = controller.GetSpeed(CurrentAxis, out snapshot.Speed);
            snapshot.HomeStatusResult = controller.GetHomeStatus(CurrentAxis, out snapshot.HomeStatus);
            return snapshot;
        }

        private void ApplyStatusSnapshot(StatusSnapshot snapshot)
        {
            if (snapshot.StatusResult == E_Result.E_SUCCESS)
            {
                MotionStatusText = TranslateStatus(snapshot.Status);
                StatusBrush = GetStatusBrush(snapshot.Status);
                AlarmId = snapshot.Alarm.ToString(CultureInfo.InvariantCulture);
                AlarmDescription = TranslateAlarm(snapshot.Alarm);
                ApplyHomeStatus(snapshot);
            }
            else if (snapshot.StatusResult == E_Result.E_INVALID_ARGUMENT)
            {
                MotionStatusText = "未初始化";
                StatusBrush = Brushes.DarkOrange;
                AlarmId = "--";
                AlarmDescription = "轴未初始化或句柄无效";
                HomeStatusText = "未初始化";
                HomeStatusBrush = Brushes.DarkOrange;
                IsInitialized = false;
            }
            else
            {
                MotionStatusText = "读取失败";
                StatusBrush = Brushes.Firebrick;
                AlarmId = snapshot.Alarm.ToString(CultureInfo.InvariantCulture);
                AlarmDescription = TranslateAlarm(snapshot.Alarm);
                ApplyHomeStatus(snapshot);
            }

            PositionText = snapshot.PositionResult == E_Result.E_SUCCESS
                ? snapshot.Position.ToString("0.###", CultureInfo.InvariantCulture)
                : "--";
            SpeedText = snapshot.SpeedResult == E_Result.E_SUCCESS
                ? snapshot.Speed.ToString("0.###", CultureInfo.InvariantCulture)
                : "--";
        }

        private void ApplyHomeStatus(StatusSnapshot snapshot)
        {
            if (snapshot.HomeStatusResult != E_Result.E_SUCCESS)
            {
                HomeStatusText = "读取失败";
                HomeStatusBrush = Brushes.Firebrick;
                return;
            }

            HomeStatusText = TranslateHomeStatus(snapshot.HomeStatus);
            HomeStatusBrush = GetHomeStatusBrush(snapshot.HomeStatus);
        }

        private void LoadProjectSummary()
        {
            try
            {
                string[] itemNumbers = controller.GetAvailableProjectItemNumbers();
                ConfigItems.Clear();
                foreach (string itemNumber in itemNumbers)
                {
                    ConfigItems.Add(itemNumber);
                }

                if (ConfigItems.Count == 0)
                {
                    ActiveProjectText = "配置：未找到项目型号";
                    return;
                }

                SelectedProjectItemNumber = ConfigItems[0];
            }
            catch (Exception ex)
            {
                ActiveProjectText = "配置读取失败：" + ex.Message;
            }
        }

        private void ImportCalibrationCsvAndUpdateJson()
        {
            OpenFileDialog dialog = new OpenFileDialog
            {
                Filter = "CSV 文件 (*.csv)|*.csv|文本文件 (*.txt)|*.txt|所有文件 (*.*)|*.*",
                CheckFileExists = true,
                Multiselect = false
            };

            if (dialog.ShowDialog() != true)
            {
                return;
            }

            try
            {
                CalibrationFitResult result = CalculateCalibrationFit(dialog.FileName);
                string configPath = UpdateLinearCompensationJson(result);
                CalibrationCsvPath = dialog.FileName;
                CalibrationPointCountText = result.Count.ToString(CultureInfo.InvariantCulture);
                CalibrationSlopeText = result.Slope.ToString("0.##########", CultureInfo.InvariantCulture);
                CalibrationInterceptText = result.Intercept.ToString("0.##########", CultureInfo.InvariantCulture);
                CalibrationScaleFactorText = result.ScaleFactor.ToString("0.##########", CultureInfo.InvariantCulture);
                CalibrationRmsErrorText = result.RmsError.ToString("0.##########", CultureInfo.InvariantCulture);
                LastResultText = "补偿计算：已写入 JSON";
                ResultBrush = Brushes.SeaGreen;
                AppendLog("补偿计算完成：a=" + CalibrationSlopeText + " b=" + CalibrationInterceptText);
                AppendLog("写入配置：" + configPath);
            }
            catch (Exception ex)
            {
                LastResultText = "补偿计算异常：" + ex.Message;
                ResultBrush = Brushes.Firebrick;
                AppendLog("补偿计算异常：" + ex.GetType().Name + ": " + ex.Message);
            }
        }

        private void OpenCalibrationWindow()
        {
            CalibrationWindow window = new CalibrationWindow(
                new CalibrationViewModel(SelectedProjectItemNumber, SelectedAxis, CurrentAxisIndex));
            if (System.Windows.Application.Current.MainWindow != null)
            {
                window.Owner = System.Windows.Application.Current.MainWindow;
            }

            window.ShowDialog();
        }

        private void OpenConfigEditorWindow()
        {
            ConfigEditorViewModel editorViewModel = new ConfigEditorViewModel(SelectedProjectItemNumber, SelectedAxis);
            ConfigEditorWindow window = new ConfigEditorWindow(editorViewModel);
            if (System.Windows.Application.Current.MainWindow != null)
            {
                window.Owner = System.Windows.Application.Current.MainWindow;
            }

            window.ShowDialog();
            if (editorViewModel.HasSaved)
            {
                LoadProjectSummary();
            }
        }

        private void ImportDiscreteCompensationCsvAndUpdateJson()
        {
            OpenFileDialog dialog = new OpenFileDialog
            {
                Filter = "CSV 文件 (*.csv)|*.csv|文本文件 (*.txt)|*.txt|所有文件 (*.*)|*.*",
                CheckFileExists = true,
                Multiselect = false
            };

            if (dialog.ShowDialog() != true)
            {
                return;
            }

            try
            {
                MotionConfigSelection selection = LoadSelectedMotionConfig();
                double effectiveScale = selection.Axis.GetEffectiveScale(CurrentAxisIndex);
                DiscreteCompensationResult result = CalculateDiscreteCompensation(dialog.FileName, effectiveScale);

                selection.Axis.DiscreteCompensationParameters = new CompensationParametersConfigure
                {
                    Num = result.Count,
                    StartPos = result.StartPos,
                    CmpLen = result.CmpLen,
                    PCmpPos = result.PositiveCompensation,
                    NCmpPos = result.NegativeCompensation
                };

                SaveMotionConfig(selection);

                DiscreteCsvPath = dialog.FileName;
                DiscretePointCountText = result.Count.ToString(CultureInfo.InvariantCulture);
                DiscreteStartPosText = result.StartPos.ToString(CultureInfo.InvariantCulture);
                DiscreteCmpLenText = result.CmpLen.ToString(CultureInfo.InvariantCulture);
                DiscreteMinPulseText = result.MinPulse.ToString(CultureInfo.InvariantCulture);
                DiscreteMaxPulseText = result.MaxPulse.ToString(CultureInfo.InvariantCulture);
                DiscreteMaxResidualText = result.MaxAbsResidual.ToString("0.##########", CultureInfo.InvariantCulture);
                LastResultText = "离散补偿：已写入 JSON";
                ResultBrush = Brushes.SeaGreen;
                AppendLog("离散补偿完成：点数=" + DiscretePointCountText + " 脉冲范围=" + DiscreteMinPulseText + ".." + DiscreteMaxPulseText);
                AppendLog("写入配置：" + selection.ConfigPath);
            }
            catch (Exception ex)
            {
                LastResultText = "离散补偿异常：" + ex.Message;
                ResultBrush = Brushes.Firebrick;
                AppendLog("离散补偿异常：" + ex.GetType().Name + ": " + ex.Message);
            }
        }

        private string UpdateLinearCompensationJson(CalibrationFitResult result)
        {
            MotionConfigSelection selection = LoadSelectedMotionConfig();
            if (selection.Axis.LinearCompensationParameters == null)
            {
                selection.Axis.LinearCompensationParameters = new LinearCompensationParametersConfigure();
            }

            selection.Axis.LinearCompensationParameters.Enabled = true;
            selection.Axis.LinearCompensationParameters.ScaleFactor = result.ScaleFactor;
            selection.Axis.LinearCompensationParameters.ScaleOffset = 0.0;

            SaveMotionConfig(selection);

            return selection.ConfigPath;
        }

        private MotionConfigSelection LoadSelectedMotionConfig()
        {
            string configPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "MotionModule.json");
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
                    string.Equals(item.ItemNumber, SelectedProjectItemNumber, StringComparison.OrdinalIgnoreCase));
            if (project == null)
            {
                project = root.MotionConfigure.FirstOrDefault(item => item != null);
            }

            if (project == null)
            {
                throw new InvalidDataException("MotionModule.json 未找到可写入的项目配置。");
            }

            MotionAxisConfig axis = project.GetAxis(CurrentAxisIndex);
            if (axis == null)
            {
                throw new InvalidDataException("MotionModule.json 未找到 " + SelectedAxis + " 配置。");
            }

            axis.Validate(CurrentAxisIndex);

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
            List<double> commands = new List<double>();
            List<double> measured = new List<double>();

            foreach (string line in File.ReadLines(path))
            {
                if (TryParseCalibrationLine(line, out double command, out double actual))
                {
                    commands.Add(command);
                    measured.Add(actual);
                }
            }

            if (commands.Count < 2)
            {
                throw new InvalidDataException("CSV 至少需要两行有效数据。");
            }

            double sumX = 0;
            double sumY = 0;
            for (int i = 0; i < commands.Count; i++)
            {
                sumX += commands[i];
                sumY += measured[i];
            }

            double meanX = sumX / commands.Count;
            double meanY = sumY / measured.Count;
            double numerator = 0;
            double denominator = 0;

            for (int i = 0; i < commands.Count; i++)
            {
                double dx = commands[i] - meanX;
                double dy = measured[i] - meanY;
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
            for (int i = 0; i < commands.Count; i++)
            {
                double fitted = slope * commands[i] + intercept;
                double residual = measured[i] - fitted;
                squaredError += residual * residual;
            }

            return new CalibrationFitResult
            {
                Count = commands.Count,
                Slope = slope,
                Intercept = intercept,
                ScaleFactor = 1.0 / slope,
                RmsError = Math.Sqrt(squaredError / commands.Count)
            };
        }

        private static DiscreteCompensationResult CalculateDiscreteCompensation(string path, double effectiveScale)
        {
            if (effectiveScale <= 0)
            {
                throw new InvalidDataException("当前轴有效 scale 必须大于 0。");
            }

            List<DiscreteCompensationSample> samples = new List<DiscreteCompensationSample>();
            foreach (string line in File.ReadLines(path))
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
                double positiveResidual = samples[i].PositiveMeasured - samples[i].Command;
                double negativeResidual = samples[i].NegativeMeasured - samples[i].Command;
                positive[i] = ToCompensationPulse(-positiveResidual * effectiveScale);
                negative[i] = ToCompensationPulse(-negativeResidual * effectiveScale);

                minPulse = Min(minPulse, positive[i], negative[i]);
                maxPulse = Max(maxPulse, positive[i], negative[i]);
                maxAbsResidual = Math.Max(maxAbsResidual, Math.Abs(positiveResidual));
                maxAbsResidual = Math.Max(maxAbsResidual, Math.Abs(negativeResidual));
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
                EffectiveScale = effectiveScale
            };
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

        private static bool TryParseFlexibleDouble(string value, out double result)
        {
            return double.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out result) ||
                   double.TryParse(value, NumberStyles.Float, CultureInfo.CurrentCulture, out result);
        }

        private static string TranslateStatus(E_Turntable_Status status)
        {
            switch (status)
            {
                case E_Turntable_Status.ready:
                    return "就绪";
                case E_Turntable_Status.moving:
                    return "运动中";
                case E_Turntable_Status.stop:
                    return "停止";
                case E_Turntable_Status.alarm:
                    return "报警";
                default:
                    return status.ToString();
            }
        }

        private static string TranslateAlarm(int alarm)
        {
            if (alarm == 0)
            {
                return "正常";
            }

            List<string> descriptions = new List<string>();
            AddAlarmDescription(descriptions, alarm, 128, "未连接");
            AddAlarmDescription(descriptions, alarm, 256, "伺服使能异常");
            AddAlarmDescription(descriptions, alarm, 512, "复位异常");
            AddAlarmDescription(descriptions, alarm, 1024, "原点回归异常");
            AddAlarmDescription(descriptions, alarm, 2048, "绝对定位异常");
            AddAlarmDescription(descriptions, alarm, 4096, "相对定位异常");
            AddAlarmDescription(descriptions, alarm, 8192, "运动超时");
            AddAlarmDescription(descriptions, alarm, 16384, "限位触发");
            AddAlarmDescription(descriptions, alarm, 32768, "驱动器报警");
            AddAlarmDescription(descriptions, alarm, 65536, "配置或补偿参数异常");

            int knownMask = 128 | 256 | 512 | 1024 | 2048 | 4096 | 8192 | 16384 | 32768 | 65536;
            int unknown = alarm & ~knownMask;
            if (unknown != 0)
            {
                descriptions.Add("未知报警位 " + unknown.ToString(CultureInfo.InvariantCulture));
            }

            return descriptions.Count == 0 ? "未知报警" : string.Join("、", descriptions);
        }

        private static void AddAlarmDescription(List<string> descriptions, int alarm, int bit, string description)
        {
            if ((alarm & bit) != 0)
            {
                descriptions.Add(description);
            }
        }

        private static string TranslateHomeStatus(short homeStatus)
        {
            if ((homeStatus & 4) != 0)
            {
                return "回零失败";
            }

            if ((homeStatus & 8) != 0)
            {
                return "回零参数错误";
            }

            if ((homeStatus & 16) != 0)
            {
                return "原点开关未触发";
            }

            if ((homeStatus & 1) != 0)
            {
                return "回零中";
            }

            if ((homeStatus & 2) != 0)
            {
                return "回零成功";
            }

            return "未回零";
        }

        private static Brush GetHomeStatusBrush(short homeStatus)
        {
            if ((homeStatus & (4 | 8 | 16)) != 0)
            {
                return Brushes.Firebrick;
            }

            if ((homeStatus & 1) != 0)
            {
                return Brushes.SteelBlue;
            }

            if ((homeStatus & 2) != 0)
            {
                return Brushes.SeaGreen;
            }

            return Brushes.Gray;
        }

        private static Brush GetStatusBrush(E_Turntable_Status status)
        {
            switch (status)
            {
                case E_Turntable_Status.ready:
                    return Brushes.SeaGreen;
                case E_Turntable_Status.moving:
                    return Brushes.SteelBlue;
                case E_Turntable_Status.stop:
                    return Brushes.DarkOrange;
                case E_Turntable_Status.alarm:
                    return Brushes.Firebrick;
                default:
                    return Brushes.Gray;
            }
        }

        private static byte[] ParseIPv4(string value)
        {
            string[] parts = (value ?? string.Empty).Split('.');
            if (parts.Length != 4)
            {
                throw new FormatException("IP 地址必须为 IPv4 格式。");
            }

            byte[] ip = new byte[4];
            for (int i = 0; i < parts.Length; i++)
            {
                if (!byte.TryParse(parts[i], NumberStyles.Integer, CultureInfo.InvariantCulture, out ip[i]))
                {
                    throw new FormatException("IP 地址包含非法段：" + parts[i]);
                }
            }

            return ip;
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

        private int ParseTimeout()
        {
            int timeout = ParseInt(TimeoutText, "超时时间");
            if (timeout < -1)
            {
                throw new ArgumentOutOfRangeException("Timeout", "超时时间不能小于 -1。");
            }

            return timeout;
        }

        private uint ParseTimeoutAsUInt32()
        {
            int timeout = ParseTimeout();
            return timeout < 0 ? 5000U : (uint)timeout;
        }

        private void AppendLog(string message)
        {
            if (!System.Windows.Application.Current.Dispatcher.CheckAccess())
            {
                System.Windows.Application.Current.Dispatcher.Invoke(() => AppendLog(message));
                return;
            }

            string line = DateTime.Now.ToString("HH:mm:ss", CultureInfo.InvariantCulture) + "  " + message;
            LogEntries.Insert(0, line);
            while (LogEntries.Count > 300)
            {
                LogEntries.RemoveAt(LogEntries.Count - 1);
            }
        }

        private static void RunOnUi(Action action)
        {
            if (System.Windows.Application.Current.Dispatcher.CheckAccess())
            {
                action();
            }
            else
            {
                System.Windows.Application.Current.Dispatcher.Invoke(action);
            }
        }

        private void RaiseCommandStates()
        {
            ConnectCommand.RaiseCanExecuteChanged();
            DisconnectCommand.RaiseCanExecuteChanged();
            InitCommand.RaiseCanExecuteChanged();
            ServoResetCommand.RaiseCanExecuteChanged();
            StopCommand.RaiseCanExecuteChanged();
            JogNegativeCommand.RaiseCanExecuteChanged();
            JogPositiveCommand.RaiseCanExecuteChanged();
            HomeCommand.RaiseCanExecuteChanged();
            ZeroCommand.RaiseCanExecuteChanged();
            MoveRelativeCommand.RaiseCanExecuteChanged();
            MoveAbsoluteCommand.RaiseCanExecuteChanged();
            TriggerStartCommand.RaiseCanExecuteChanged();
            TriggerStopCommand.RaiseCanExecuteChanged();
            AutoHomeCommand.RaiseCanExecuteChanged();
            AutoIndexCommand.RaiseCanExecuteChanged();
            OpenCalibrationCommand.RaiseCanExecuteChanged();
            OpenConfigEditorCommand.RaiseCanExecuteChanged();
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

        public void Dispose()
        {
            pollTimer.Stop();
            if (IsConnected)
            {
                try
                {
                    controller.Disconnect();
                }
                catch
                {
                    // 关闭窗口时不再弹出硬件异常。
                }
            }
        }

        private sealed class StatusSnapshot
        {
            public E_Result StatusResult;
            public E_Turntable_Status Status;
            public int Alarm;
            public E_Result PositionResult;
            public double Position;
            public E_Result SpeedResult;
            public double Speed;
            public E_Result HomeStatusResult;
            public short HomeStatus;
        }

        private sealed class CalibrationFitResult
        {
            public int Count;
            public double Slope;
            public double Intercept;
            public double ScaleFactor;
            public double RmsError;
        }

        private sealed class MotionConfigSelection
        {
            public string ConfigPath;
            public DataContractJsonSerializer Serializer;
            public MotionModuleConfigRoot Root;
            public MotionAxisConfig Axis;
        }

        private sealed class DiscreteCompensationSample
        {
            public double Command;
            public double PositiveMeasured;
            public double NegativeMeasured;
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
        }
    }
}
