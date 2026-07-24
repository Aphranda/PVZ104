using System;
using System.Linq;
using System.Threading;

namespace PVZ104
{
    public enum E_Turntable_Status { alarm = 0, moving = 1, ready = 2, stop = 3 }
    /// <summary>
    /// 轴号标识
    /// </summary>
    /// <remarks>
    /// Axis01 以竖直轴Z轴为中心轴，进行旋转运动。
    /// Axis02 以水平轴X/Y轴为旋转轴，进行旋转运动
    /// Axis03 以X轴为旋转中心，进行平移运动
    /// Axis04 以Y轴为旋转中心，进行平移运动
    /// Axis05 以Z轴为旋转中心，进行平移运动
    /// </remarks>
    public enum Dimension { Axis01 = 0, Axis02 = 1, Axis03 = 2, Axis04 = 3, Axis05 = 4 }
    public enum E_Result
    {
        E_SUCCESS = 0, // Success.
        E_FAILED = 1, // General Failure.

        //IO status feedback
        E_IO_EXCEPTION = 20001, // An I/O error occurs.
        E_PATH_TOO_LONG = 20002, // A path or fully qualified file
        E_FILE_NOT_FOUND = 20003, // An attempt to access a file that
        E_INVALID_ARGUMENT = 20004, // when a null reference is passed
        E_ARGUMENT_OUT_OF_RANGE = 20005, // The value of an argument is
        E_ABNORMAL_INPUT_FILE = 20006, // The format or content of the
        E_INVALID_SERIALPORT = 30001, // Invalid or noneExisted COM port.
        E_ALREADY_DISCONNECTED = 30002, // The hardware has been
        E_TIMEOUT = 30003, // Timed out. Unable to complete the
        E_BAD_TIMEOUT = 30004, // The timeout time is less than the
        E_SERIALPORT_TIMEOUT = 30005, // The operation of the serial port
        E_ABNORMAL_SERIALPORT = 30006, // Serial communication is abnormal.
        E_ALREADY_CONNECTED = 30007, // The S7 1200 already Connected

        //Authority status feedback
        E_UNAUTHORIZED_ACCESS = 40001, // Insufficient permissions to
        E_NO_DONGLE = 40002, // The driver of the dongle is not installed or the dongle is not inserted.
        E_UNAUTHORIZED = 40003, // Unauthorized function.
        E_EXPIRED_LICENSE = 40004, // Unauthorized function
        E_NotImplement_Function = 50001, // The function is not implemented.
        E_UNDEFINED = 99999, // Undefined or unknown error.
        E_ToDo = 20220515
    }
    public interface GTSApi
    {
        // 转动连接
        public E_Result Connect(byte[] ipv4);
        // 最近一次连接诊断
        public MotionConnectionDiagnostic LastConnectionDiagnostic { get; }
        // 最近一次连接诊断文本
        public string LastConnectionMessage { get; }
        // 转动断开
        public E_Result Disconnect();
        // 查询转动状态
        public E_Result GetStatus(Dimension dimension, out E_Turntable_Status status, out int alarmId);
        // 转动初始化
        public E_Result Init(Dimension dimension, bool init_flag, UInt32 ServoResetTimeDelay = 5000);
        // 转动寻零
        public E_Result Home(Dimension dimension, double speed, double offset, int timeout = -1);

        // 电机移动
        public E_Result MoveRelative(Dimension dimension, double speed, double position, int timeout = -1);

        // 电机移动
        public E_Result MoveAbsolute(Dimension dimension, double speed, double position, int timeout = -1);

        // 电机JOG
        public E_Result Jog(Dimension dimension, double speed, bool direction, int timeout = -1);

        // 连续触发
        public E_Result Trigger(Dimension dimension, double start, double stop, double step, int pulseWidth, int timeout = -1);

        // 连续触发停止
        public E_Result TriggerStop(Dimension dimension);

        // 电机停止移动
        public E_Result Stop(Dimension dimension);
        // 获取电机速度
        public E_Result GetSpeed(Dimension dimension, out double speed);
        // 获取轴位置/角度
        public E_Result GetPosition(Dimension dimension, out double position);
        // 轴位置清零
        public E_Result Zero(Dimension dimension);
        // 多轴同时回零
        public E_Result HomeAll(Dimension[] dimensions, double[] speed, double[] offset, int timeout = -1);
        // 多轴同时移动
        public E_Result MoveAll(Dimension[] dimensions, double[] speed, double[] position, int timeout = -1);

        // 日志类，增加大型设备的传感器监测信息
    }
    public class AutoGCApi : GTSApi
    {
        private const int PollIntervalMs = 100;
        private const int StartGraceMs = 500;
        private const int DefaultMoveTimeoutMs = 60000;

        MotionControl motionControl = new MotionControl();
        public byte[] ipv4 = new byte[4];

        public string ProjectItemNumber
        {
            get { return motionControl.ProjectItemNumber; }
            set { motionControl.ProjectItemNumber = value; }
        }

        public string ActiveProjectItemNumber => motionControl.ActiveProjectItemNumber;

        public MotionConnectionDiagnostic LastConnectionDiagnostic => motionControl.LastConnectionDiagnostic;

        public string LastConnectionMessage => motionControl.LastConnectionDiagnostic.ToString();

        public string[] GetAvailableProjectItemNumbers()
        {
            return motionControl.GetAvailableProjectItemNumbers();
        }

        /// <summary>
        /// 连接高川运动控制卡
        /// </summary>
        /// <param name="ipAddress"></param>
        /// <param name="port"></param>
        /// <returns></returns>
        public E_Result Connect(byte[] ipv4)
        {
            return motionControl.CardConnect(ipv4);
        }

        /// <summary>
        /// 高川运动控制卡断开连接
        /// </summary>
        /// <returns></returns>
        public E_Result Disconnect()
        {
            return motionControl.CardDisconnect();
        }

        /// <summary>
        /// 初始化
        /// </summary>
        /// <param name="dimension">维度</param>
        /// <param name="servoResetTimeDelay">初始化时间</param>
        /// <returns></returns>
        public E_Result Init(Dimension dimension, bool init_flag, UInt32 servoResetTimeDelay = 5000)
        {
            if (!motionControl.IsConnected)
            {
                return E_Result.E_ALREADY_DISCONNECTED;
            }

            E_Result result = motionControl.CardInitial();
            if (result != E_Result.E_SUCCESS)
            {
                return result;
            }

            if (init_flag)
            {
                // IO 控制伺服驱动器供电；先建立轴句柄，才能可靠执行 Servo Off/On。
                result = motionControl.ServoEnable(dimension, false);
                if (result != E_Result.E_SUCCESS)
                {
                    return result;
                }

                result = motionControl.MotorIOControl(false, 0);
                if (result != E_Result.E_SUCCESS)
                {
                    return result;
                }

                result = motionControl.MotorIOControl(false, 1);
                if (result != E_Result.E_SUCCESS)
                {
                    return result;
                }

                SpinWait.SpinUntil(() => false, 2000);

                result = motionControl.MotorIOControl(true, 0);
                if (result != E_Result.E_SUCCESS)
                {
                    return result;
                }

                result = motionControl.MotorIOControl(true, 1);
                if (result != E_Result.E_SUCCESS)
                {
                    return result;
                }

                int resetDelay = servoResetTimeDelay > int.MaxValue ? int.MaxValue : (int)servoResetTimeDelay;
                SpinWait.SpinUntil(() => false, Math.Max(0, resetDelay - 2000));

                result = motionControl.CardInitial();
                if (result != E_Result.E_SUCCESS)
                {
                    return result;
                }

                return motionControl.ServoEnable(dimension, true);
            }
            else
            {
                result = motionControl.MotorIOControl(true, 0);
                if (result != E_Result.E_SUCCESS)
                {
                    return result;
                }

                result = motionControl.MotorIOControl(true, 1);
                if (result != E_Result.E_SUCCESS)
                {
                    return result;
                }

                return motionControl.ServoEnable(dimension, true);
            }

        }

        /// <summary>
        /// 获取当前状态
        /// </summary>
        /// <param name="dimension">维度</param>
        /// <param name="status">状态</param>
        /// <param name="alarmId">[0000 0000 0000 0000 ] [驱动器报警，限位告警,超时告警，相对定位告警，绝对定位告警，原点回归告警，复位告警，使能告警, 未连接报警]</param>
        /// <returns></returns>
        public E_Result GetStatus(Dimension dimension, out E_Turntable_Status status, out int alarmId)
        {
            status = E_Turntable_Status.alarm;
            E_Result result = motionControl.TryMotorGetStatus(dimension, out Axis axis);
            if (result != E_Result.E_SUCCESS)
            {
                alarmId = motionControl.BuildDisconnectedAlarmId(dimension);
                return result;
            }

            if (axis.IsRunning)
            {
                status = E_Turntable_Status.moving;
            }
            if (axis.IsArrive)
            {
                status = E_Turntable_Status.ready;
            }
            if (axis.IsAlarming)
            {
                status = E_Turntable_Status.alarm;
            }

            alarmId = motionControl.BuildStatusAlarmId(dimension, axis);

            // 获取IPV4地址
            ipv4 = motionControl.ipv4;
            return E_Result.E_SUCCESS;
        }

        /// <summary>
        /// 获取当前角度值
        /// </summary>
        /// <param name="dimension">维度</param>
        /// <param name="position">位置/角度</param>
        /// <returns></returns>
        public E_Result GetPosition(Dimension dimension, out double position)
        {
            E_Result result = motionControl.TryMotorGetStatus(dimension, out Axis axis);
            position = axis.CurrentPos1;
            return result;
        }

        /// <summary>
        /// 获取当前角速度
        /// </summary>
        /// <param name="dimension">维度</param>
        /// <param name="speed">速度</param>
        /// <returns></returns>
        public E_Result GetSpeed(Dimension dimension, out double speed)
        {
            E_Result result = motionControl.TryMotorGetStatus(dimension, out Axis axis);
            speed = axis.CurrentVel;
            return result;
        }

        /// <summary>
        /// 原点复位，带系统偏置
        /// </summary>
        /// <param name="dimension">维度</param>
        /// <param name="speed">速度</param>
        /// <param name="timeout">超时时间</param>
        /// <returns></returns>
        public E_Result Home(Dimension dimension, double speed, double offset, int timeout = -1)
        {
            if (speed <= 0)
            {
                return E_Result.E_INVALID_ARGUMENT;
            }

            // 执行Home时，当前位置是不可靠的，所以使用最大行程440
            int finalTimeout = CalculateTimeout(dimension, timeout, speed);

            // 驱动轴原点复位
            E_Result e_Result = motionControl.MotorHome(dimension, speed, offset);
            if (e_Result != E_Result.E_SUCCESS)
            {
                return e_Result;
            }

            // 回零使用厂家回零状态位判断完成，避免普通运动状态未刷新导致提前返回。
            e_Result = BlockingHomeQuery(dimension, finalTimeout);
            Thread.Sleep(1500);
            return e_Result;
        }

        /// <summary>
        /// 相对移动
        /// </summary>
        /// <param name="dimension">轴号</param>
        /// <param name="speed">速度</param>
        /// <param name="position">位置</param>
        /// <param name="timeout">超时时间</param>
        /// <returns></returns>
        public E_Result MoveRelative(Dimension dimension, double speed, double position, int timeout = -1)
        {
            if (speed <= 0)
            {
                return E_Result.E_INVALID_ARGUMENT;
            }

            // 计算内置超时时间
            int finalTimeout = CalculateTimeout(dimension, timeout, speed, position);

            // 驱动轴相对运动
            E_Result e_Result = motionControl.MotorRelative(dimension, speed, position);
            if (e_Result != E_Result.E_SUCCESS)
            {
                return e_Result;
            }

            // 轮询运动状态
            return BlockingQuery(dimension, finalTimeout);
        }

        /// <summary>
        /// 绝对移动
        /// </summary>
        /// <param name="dimension">轴号</param>
        /// <param name="speed">速度</param>
        /// <param name="position">位置</param>
        /// <param name="timeout">超时时间</param>
        /// <returns></returns>
        public E_Result MoveAbsolute(Dimension dimension, double speed, double position, int timeout = -1)
        {
            if (speed <= 0)
            {
                return E_Result.E_INVALID_ARGUMENT;
            }

            // 计算内置超时时间
            double currentPosition = 0;
            E_Result positionResult = GetPosition(dimension, out currentPosition);
            if (positionResult != E_Result.E_SUCCESS)
            {
                return positionResult;
            }

            double movePosition = Math.Abs(currentPosition - position);
            int finalTimeout = CalculateTimeout(dimension, timeout, speed, movePosition);

            // 驱动轴绝对运动
            E_Result e_Result = motionControl.MotorAbsolute(dimension, speed, position);
            if (e_Result != E_Result.E_SUCCESS)
            {
                return e_Result;
            }

            // 轮询运动状态
            return BlockingQuery(dimension, finalTimeout);
        }

        /// <summary>
        /// JOG
        /// </summary>
        /// <param name="dimension">维度</param>
        /// <param name="speed">速度</param>
        /// <param name="direction">方向</param>
        /// <param name="timeout">超时时间</param>
        /// <returns></returns>
        public E_Result Jog(Dimension dimension, double speed, bool direction, int timeout = -1)
        {
            if (speed <= 0)
            {
                return E_Result.E_INVALID_ARGUMENT;
            }

            // 驱动轴以速度模式运行
            E_Result e_Result = motionControl.MotorJog(dimension, speed, direction);
            if (e_Result != E_Result.E_SUCCESS || timeout < 0)
            {
                return e_Result;
            }

            // JOG 是速度模式，不能用点位运动的到位状态判断完成。
            E_Result waitResult = BlockingJogQuery(dimension, timeout);
            if (waitResult != E_Result.E_SUCCESS)
            {
                motionControl.MotorStop(dimension);
            }

            return waitResult;
        }

        /// <summary>
        /// 开始连续触发
        /// </summary>
        /// <param name="dimension">维度</param>
        /// <param name="start">开始位置</param>
        /// <param name="stop">停止位置</param>
        /// <param name="step">触发步长</param>
        /// <param name="pulseWidth">脉冲宽度us</param>
        /// <param name="timeout">超时时间</param>
        /// <returns></returns>
        public E_Result Trigger(Dimension dimension, double start, double stop, double step, int pulseWidth, int timeout = -1)
        {
            if (step <= 0 || stop <= start || pulseWidth <= 0)
            {
                return E_Result.E_INVALID_ARGUMENT;
            }

            // 设置连续触发参数
            E_Result e_Result;
            e_Result = motionControl.MotorCompareHS2Para(dimension, pulseWidth);
            if (e_Result != E_Result.E_SUCCESS)
            {
                return e_Result;
            }

            // 设置连续脉冲数组
            int pulseNum = (int)((stop - start) / step); // 脉冲数量
            if (pulseNum <= 0)
            {
                return E_Result.E_INVALID_ARGUMENT;
            }

            if (pulseNum <= 128)
            {
                double[] posArray = new double[pulseNum * 2]; //256
                for (int i = 0; i < pulseNum; i++)
                {
                    posArray[2 * i] = start + i * step;
                }

                e_Result = motionControl.MotorCompareHs2Data(dimension, posArray);
                if (e_Result != E_Result.E_SUCCESS)
                {
                    return e_Result;
                }
            }
            else if (pulseNum > 128)
            {
                int residualPulse = pulseNum;
                double currentPosition = start;
                while (currentPosition < stop)
                {
                    if (residualPulse >= 128)
                    {
                        double[] posArray = new double[256]; //256
                        for (int i = 0; i < 128; i++)
                        {
                            posArray[2 * i] = currentPosition + i * step;
                        }
                        e_Result = motionControl.MotorCompareHs2Data(dimension, posArray);
                        if (e_Result != E_Result.E_SUCCESS)
                        {
                            return e_Result;
                        }
                    }
                    else if (residualPulse < 128)
                    {
                        double[] posArray = new double[residualPulse * 2]; //256
                        for (int i = 0; i < residualPulse; i++)
                        {
                            posArray[2 * i] = currentPosition + i * step;
                        }
                        e_Result = motionControl.MotorCompareHs2Data(dimension, posArray);
                        if (e_Result != E_Result.E_SUCCESS)
                        {
                            return e_Result;
                        }
                    }
                    residualPulse = residualPulse - 128;
                    currentPosition = currentPosition + 128 * step;
                }
            }

            // 检查参数
            HS2CompareStatus compareStatus = new HS2CompareStatus();
            e_Result = motionControl.MotorCompareHS2Status(out compareStatus);

            if (e_Result != E_Result.E_SUCCESS)
            {
                return e_Result;
            }
            if (compareStatus.usedSpace != pulseNum)
            {
                return E_Result.E_FAILED;
            }

            // 启动连续触发
            e_Result = motionControl.MotorCompareHS2Start();
            if (e_Result != E_Result.E_SUCCESS)
            {
                return e_Result;
            }

            return E_Result.E_SUCCESS;
        }

        /// <summary>
        /// 运动停止
        /// </summary>
        /// <param name="dimension">维度</param>
        /// <returns></returns>
        public E_Result Stop(Dimension dimension)
        {
            E_Result e_Result = motionControl.MotorStop(dimension);
            return e_Result;
        }

        /// <summary>
        /// 连续触发停止
        /// </summary>
        /// <param name="dimension">维度</param>
        /// <returns></returns>
        public E_Result TriggerStop(Dimension dimension)
        {
            E_Result e_Result = motionControl.MotorCompareHS2Stop();
            return e_Result;
        }

        /// <summary>
        /// 轴位置清零
        /// </summary>
        /// <param name="dimension">维度</param>
        /// <returns></returns>
        public E_Result Zero(Dimension dimension)
        {
            return motionControl.MotorZero(dimension);
        }

        /// <summary>
        /// 多轴同时回零
        /// </summary>
        public E_Result HomeAll(Dimension[] dimensions, double[] speed, double[] offset, int timeout = -1)
        {
            if (dimensions == null || speed == null || offset == null ||
                dimensions.Length == 0 ||
                dimensions.Length != speed.Length || dimensions.Length != offset.Length)
            {
                return E_Result.E_INVALID_ARGUMENT;
            }

            for (int i = 0; i < dimensions.Length; i++)
            {
                if (speed[i] <= 0)
                {
                    return E_Result.E_INVALID_ARGUMENT;
                }

                E_Result result = motionControl.MotorHome(dimensions[i], speed[i], offset[i]);
                if (result != E_Result.E_SUCCESS)
                {
                    StopStartedAxes(dimensions, i, isHome: true);
                    return result;
                }

                Thread.Sleep(100);
            }

            int finalTimeout = timeout < 0 ? DefaultMoveTimeoutMs : timeout;
            for (int i = 0; i < dimensions.Length; i++)
            {
                finalTimeout = Math.Max(finalTimeout, CalculateTimeout(dimensions[i], timeout, speed[i]));
            }

            E_Result waitResult = BlockingHomeQuery(dimensions, finalTimeout);
            if (waitResult != E_Result.E_SUCCESS)
            {
                StopStartedAxes(dimensions, dimensions.Length, isHome: true);
            }

            return waitResult;
        }

        /// <summary>
        /// 多轴同时移动
        /// </summary>
        public E_Result MoveAll(Dimension[] dimensions, double[] speed, double[] position, int timeout = -1)
        {
            if (dimensions == null || speed == null || position == null ||
                dimensions.Length == 0 ||
                dimensions.Length != speed.Length || dimensions.Length != position.Length)
            {
                return E_Result.E_INVALID_ARGUMENT;
            }

            double[] currentPosition = new double[dimensions.Length];
            int[] timeoutArray = new int[dimensions.Length];

            for (int i = 0; i < dimensions.Length; i++)
            {
                if (speed[i] <= 0)
                {
                    return E_Result.E_INVALID_ARGUMENT;
                }

                E_Result positionResult = GetPosition(dimensions[i], out currentPosition[i]);
                if (positionResult != E_Result.E_SUCCESS)
                {
                    return positionResult;
                }
            }

            for (int i = 0; i < dimensions.Length; i++)
            {
                E_Result result = motionControl.MotorAbsolute(dimensions[i], speed[i], position[i]);
                if (result != E_Result.E_SUCCESS)
                {
                    StopStartedAxes(dimensions, i, isHome: false);
                    return result;
                }

                Thread.Sleep(100);
            }

            for (int i = 0; i < dimensions.Length; i++)
            {
                timeoutArray[i] = CalculateTimeout(dimensions[i], timeout, speed[i], Math.Abs(position[i] - currentPosition[i]));
            }

            E_Result waitResult = motionControl.BlockingQuery(dimensions, timeoutArray.Max());
            if (waitResult != E_Result.E_SUCCESS)
            {
                StopStartedAxes(dimensions, dimensions.Length, isHome: false);
            }

            return waitResult;
        }

        /// <summary>
        /// 轮询阻塞查询
        /// </summary>
        /// <param name="dimension">维度</param>
        /// <param name="timeout">轮询延时</param>
        /// <returns></returns>
        public E_Result BlockingQuery(Dimension dimension, int timeout)
        {
            System.Diagnostics.Stopwatch stopwatch = System.Diagnostics.Stopwatch.StartNew();
            bool observedRunning = false;

            while (stopwatch.ElapsedMilliseconds <= timeout)
            {
                E_Result result = motionControl.TryMotorGetStatus(dimension, out Axis axis);
                if (result != E_Result.E_SUCCESS)
                {
                    return result;
                }

                if (IsMotionFault(axis))
                {
                    return E_Result.E_FAILED;
                }

                if (axis.IsRunning)
                {
                    observedRunning = true;
                }
                else if (observedRunning)
                {
                    return ValidatePtpCompletion(dimension, axis);
                }
                else if (stopwatch.ElapsedMilliseconds >= StartGraceMs)
                {
                    return ValidatePtpCompletion(dimension, axis);
                }

                Thread.Sleep(PollIntervalMs);
            }
            return E_Result.E_TIMEOUT;
        }

        private E_Result ValidatePtpCompletion(Dimension dimension, Axis axis)
        {
            if (!axis.IsArrive)
            {
                return E_Result.E_FAILED;
            }

            E_Result result = motionControl.TryValidatePtpCompletion(dimension, axis, out bool isArrived);
            if (result != E_Result.E_SUCCESS)
            {
                return result;
            }

            return isArrived ? E_Result.E_SUCCESS : E_Result.E_FAILED;
        }

        private E_Result BlockingJogQuery(Dimension dimension, int timeout)
        {
            if (timeout < 0)
            {
                return E_Result.E_SUCCESS;
            }

            System.Diagnostics.Stopwatch stopwatch = System.Diagnostics.Stopwatch.StartNew();
            bool observedRunning = false;

            while (stopwatch.ElapsedMilliseconds <= timeout)
            {
                E_Result result = motionControl.TryMotorGetStatus(dimension, out Axis axis);
                if (result != E_Result.E_SUCCESS)
                {
                    return result;
                }

                if (IsMotionFault(axis))
                {
                    return E_Result.E_FAILED;
                }

                if (axis.IsRunning)
                {
                    observedRunning = true;
                }
                else if (observedRunning)
                {
                    return E_Result.E_SUCCESS;
                }
                else if (stopwatch.ElapsedMilliseconds >= StartGraceMs)
                {
                    return E_Result.E_FAILED;
                }

                Thread.Sleep(PollIntervalMs);
            }

            return E_Result.E_TIMEOUT;
        }

        private E_Result BlockingHomeQuery(Dimension dimension, int timeout)
        {
            System.Diagnostics.Stopwatch stopwatch = System.Diagnostics.Stopwatch.StartNew();
            bool observedHomeRunning = false;

            while (stopwatch.ElapsedMilliseconds <= timeout)
            {
                E_Result result = motionControl.TryMotorHomeStatus(dimension, out short homeStatus);
                if (result != E_Result.E_SUCCESS)
                {
                    return result;
                }

                HomeWaitState homeResult = EvaluateHomeStatus(homeStatus, ref observedHomeRunning);
                if (homeResult == HomeWaitState.Done)
                {
                    return E_Result.E_SUCCESS;
                }

                if (homeResult == HomeWaitState.Failed)
                {
                    return E_Result.E_FAILED;
                }

                if (!observedHomeRunning && stopwatch.ElapsedMilliseconds >= StartGraceMs)
                {
                    return E_Result.E_FAILED;
                }

                Thread.Sleep(PollIntervalMs);
            }

            return E_Result.E_TIMEOUT;
        }

        private E_Result BlockingHomeQuery(Dimension[] dimensions, int timeout)
        {
            if (dimensions == null || dimensions.Length == 0)
            {
                return E_Result.E_INVALID_ARGUMENT;
            }

            System.Diagnostics.Stopwatch stopwatch = System.Diagnostics.Stopwatch.StartNew();
            bool[] observedHomeRunning = new bool[dimensions.Length];

            while (stopwatch.ElapsedMilliseconds <= timeout)
            {
                bool allDone = true;

                for (int i = 0; i < dimensions.Length; i++)
                {
                    E_Result result = motionControl.TryMotorHomeStatus(dimensions[i], out short homeStatus);
                    if (result != E_Result.E_SUCCESS)
                    {
                        return result;
                    }

                    HomeWaitState homeResult = EvaluateHomeStatus(homeStatus, ref observedHomeRunning[i]);
                    if (homeResult == HomeWaitState.Failed)
                    {
                        return E_Result.E_FAILED;
                    }

                    if (homeResult != HomeWaitState.Done)
                    {
                        allDone = false;
                    }
                }

                if (allDone)
                {
                    return E_Result.E_SUCCESS;
                }

                if (stopwatch.ElapsedMilliseconds >= StartGraceMs)
                {
                    for (int i = 0; i < observedHomeRunning.Length; i++)
                    {
                        if (!observedHomeRunning[i])
                        {
                            return E_Result.E_FAILED;
                        }
                    }
                }

                Thread.Sleep(PollIntervalMs);
            }

            return E_Result.E_TIMEOUT;
        }

        private enum HomeWaitState
        {
            Waiting,
            Done,
            Failed
        }

        private static HomeWaitState EvaluateHomeStatus(short homeStatus, ref bool observedHomeRunning)
        {
            if ((homeStatus & 2) != 0)
            {
                return HomeWaitState.Done;
            }

            if ((homeStatus & (4 | 8 | 16)) != 0)
            {
                return HomeWaitState.Failed;
            }

            if ((homeStatus & 1) != 0)
            {
                observedHomeRunning = true;
            }

            return HomeWaitState.Waiting;
        }

        private static bool IsMotionFault(Axis axis)
        {
            return axis.IsAlarming ||
                   axis.IsError ||
                   (axis.NegArrived && axis.IsNegLmtActived) ||
                   (axis.PosArrived && axis.IsPosLmtActived);
        }

        private void StopStartedAxes(Dimension[] dimensions, int startedCount, bool isHome)
        {
            for (int i = 0; i < startedCount; i++)
            {
                if (isHome)
                {
                    motionControl.MotorHomeStop(dimensions[i]);
                }

                motionControl.MotorStop(dimensions[i]);
            }
        }


        /// <summary>
        /// 自动计算延时
        /// </summary>
        /// <param name="dimension">转台维度</param>
        /// <param name="inputTimeout">输入的时间</param>
        /// <param name="speed">转台速度</param>
        /// <param name="position">转台位置</param>
        /// <returns></returns>
        private int CalculateTimeout(Dimension dimension, int inputTimeout, double speed, double position = 440)
        {
            if (speed <= 0)
            {
                return DefaultMoveTimeoutMs;
            }

            double seconds = Math.Abs(position) / speed + 10;
            int calculatedTimeout = seconds >= int.MaxValue / 1000.0
                ? int.MaxValue
                : (int)Math.Ceiling(seconds * 1000);

            // 外部 timeout 只能延长等待时间，不能短于内部估算的安全时间。
            return inputTimeout < 0 ? calculatedTimeout : Math.Max(calculatedTimeout, inputTimeout);
        }
    }
}
