using System;
using System.Diagnostics;
using System.Threading;
using GC.Frame.Motion.Private;

namespace PVZ104
{
    public class MotionControl
    {
        // 向外暴露的属性
        public byte[] ipv4 = new byte[4];       // 定义当前ipv4地址
        public int alarmId = 0;                 // 定义当前报错ID

        // 内部使用的全局变量
        int NUM = 0;                            // 扫描轴数量
        short On = 1, Off = 0;                  // 开关定义

        // 轴函数定义
        Axis[] ax = new Axis[8];               // 定义8轴配置
        MotionPara[] mp = new MotionPara[8];   // 定义8轴参数
        HomePara[] hp = new HomePara[8];        // 定义8轴回零参数
        private readonly int[] axisAlarmIds = new int[8];
        private readonly short[] axisEncoderModes = new short[8];
        private readonly int[] lastPtpTargetPulse = new int[8];
        private readonly bool[] hasLastPtpTarget = new bool[8];

        //句柄定义
        private ushort DevHandle = 0;           // 定义控制器句柄
        private ushort[] axisHandle;            // 可控制单轴参数配置

        private readonly object nativeSync = new object(); // 厂家 DLL 调用串行锁

        bool isConnected = false;               // 定义连接状态
        
        FileConfiguration FileConfiguration = new FileConfiguration();

        public MotionConnectionDiagnostic LastConnectionDiagnostic { get; private set; } = MotionConnectionDiagnostic.Empty;

        public bool IsConnected
        {
            get { return isConnected; }
        }

        public bool IsInitialized
        {
            get
            {
                if (axisHandle == null || axisHandle.Length == 0)
                {
                    return false;
                }

                for (int i = 0; i < axisHandle.Length; i++)
                {
                    if (axisHandle[i] != 0)
                    {
                        return true;
                    }
                }

                return false;
            }
        }

        /// <summary>
        /// 初始化轴参数
        /// </summary>
        public MotionControl()
        {
            // 初始化轴定义，轴参数
            for (int i = 0; i < ax.Length; i++)
            {
                ax[i] = new Axis();
                mp[i] = new MotionPara();
                hp[i] = new HomePara();
                axisEncoderModes[i] = 256;
            }
        }

        private bool TryGetAxisIndex(Dimension dimension, out int axisIndex)
        {
            axisIndex = (int)dimension;
            return axisIndex >= 0 && axisIndex < ax.Length && axisIndex < mp.Length;
        }

        private bool TryGetAxisHandle(Dimension dimension, out int axisIndex, out ushort axisCurrentHandle)
        {
            axisCurrentHandle = 0;
            if (!TryGetAxisIndex(dimension, out axisIndex))
            {
                return false;
            }

            axisCurrentHandle = ax[axisIndex].AxisHandle;
            return axisCurrentHandle != 0;
        }

        private void ClearAllAlarmIds()
        {
            Array.Clear(axisAlarmIds, 0, axisAlarmIds.Length);
            alarmId = 0;
        }

        private int GetStoredAxisAlarmId(Dimension dimension)
        {
            if (!TryGetAxisIndex(dimension, out int axisIndex))
            {
                return alarmId;
            }

            return axisAlarmIds[axisIndex];
        }

        private void SetAxisAlarm(Dimension dimension, MotionAlarmFlags flag, bool enabled)
        {
            if (!TryGetAxisIndex(dimension, out int axisIndex))
            {
                return;
            }

            if (enabled)
            {
                axisAlarmIds[axisIndex] |= (int)flag;
            }
            else
            {
                axisAlarmIds[axisIndex] &= ~(int)flag;
            }

            alarmId = axisAlarmIds[axisIndex];
        }

        private int PublishAxisAlarmId(Dimension dimension, int value)
        {
            if (TryGetAxisIndex(dimension, out int axisIndex))
            {
                axisAlarmIds[axisIndex] = value;
            }

            alarmId = value;
            return value;
        }

        internal int BuildStatusAlarmId(Dimension dimension, Axis axis)
        {
            int current = GetStoredAxisAlarmId(dimension);

            if (axis.IsConnected)
            {
                current &= ~(int)MotionAlarmFlags.NotConnected;
            }
            else
            {
                current |= (int)MotionAlarmFlags.NotConnected;
            }

            if (axis.IsAlarming)
            {
                current |= (int)MotionAlarmFlags.Driver;
            }
            else
            {
                current &= ~(int)MotionAlarmFlags.Driver;
            }

            bool limitAlarm = (axis.NegArrived && axis.IsNegLmtActived) || (axis.PosArrived && axis.IsPosLmtActived);
            if (limitAlarm)
            {
                current |= (int)MotionAlarmFlags.Limit;
            }
            else
            {
                current &= ~(int)MotionAlarmFlags.Limit;
            }

            return PublishAxisAlarmId(dimension, current);
        }

        internal int BuildDisconnectedAlarmId(Dimension dimension)
        {
            return PublishAxisAlarmId(dimension, GetStoredAxisAlarmId(dimension) | (int)MotionAlarmFlags.NotConnected);
        }

        /// <summary>
        /// 连接运动控制卡
        /// </summary>
        /// <returns>连接状态</returns>
        public E_Result CardConnect(byte[] ipv4)
        {
            if (ipv4 == null || ipv4.Length != 4)
            {
                LastConnectionDiagnostic = new MotionConnectionDiagnostic(
                    ipv4,
                    null,
                    null,
                    null,
                    null,
                    null,
                    "IPv4 参数无效");
                return E_Result.E_INVALID_ARGUMENT;
            }

            short rtn = 0;
            if (isConnected)
            {
                LastConnectionDiagnostic = new MotionConnectionDiagnostic(
                    ipv4,
                    null,
                    null,
                    null,
                    null,
                    null,
                    "设备已连接");
                return E_Result.E_ALREADY_CONNECTED;
            }

            short? openByIpRtn = null;
            short? searchRtn = null;
            ushort? searchDevNum = null;
            short? openBySearchRtn = null;
            short? nativeLastError = null;
            string diagnosticMessage;

            try
            {
                // 指定IP连接时不依赖广播搜索结果。多IP/多网段网卡下，厂家搜索接口可能搜不到设备。
                lock (nativeSync)
                {
                    rtn = CNMCLib20.NMC_DevOpenByIP(ipv4, ref DevHandle);
                }
                openByIpRtn = rtn;
                if (rtn == 0)
                {
                    diagnosticMessage = "指定 IP 连接成功";
                }
                else
                {
                    nativeLastError = TryGetLastNativeError();

                    ushort devNum = 0; // 当前板卡可控轴数量
                    byte[] devInfo = new byte[4 * 84]; // 当前板卡相关信息-设备序号，识别字符串，描述符，板卡ID
                    short currentSearchRtn;
                    lock (nativeSync)
                    {
                        currentSearchRtn = CNMCLib20.NMC_DevSearch(CNMCLib20.TSearchMode.Ethernet, ref devNum, devInfo);
                    }
                    searchRtn = currentSearchRtn;
                    searchDevNum = devNum;
                    if (currentSearchRtn == 0 && devNum > 0)
                    {
                        lock (nativeSync)
                        {
                            rtn = CNMCLib20.NMC_DevOpen(0, ref DevHandle);
                        }
                        openBySearchRtn = rtn;
                        if (rtn == 0)
                        {
                            if (!TryReadDeviceIPv4(DevHandle, out byte[] openedIPv4))
                            {
                                CloseDeviceHandle();
                                rtn = -1;
                                diagnosticMessage = "指定 IP 失败，搜索兜底已打开设备，但无法读取设备 IP";
                            }
                            else if (!IsSameIPv4(ipv4, openedIPv4))
                            {
                                CloseDeviceHandle();
                                rtn = -1;
                                diagnosticMessage = "指定 IP 失败，搜索兜底打开到非目标 IP：" + FormatIPv4(openedIPv4);
                            }
                            else
                            {
                                diagnosticMessage = "指定 IP 失败，搜索兜底连接到目标 IP";
                            }
                        }
                        else
                        {
                            diagnosticMessage = "指定 IP 和搜索兜底打开均失败";
                        }
                    }
                    else
                    {
                        diagnosticMessage = currentSearchRtn == 0 ? "指定 IP 失败，搜索未发现设备" : "指定 IP 失败，搜索接口调用失败";
                    }

                    if (rtn != 0)
                    {
                        nativeLastError = TryGetLastNativeError();
                    }
                }
            }
            catch (Exception ex) when (ex is DllNotFoundException || ex is BadImageFormatException || ex is EntryPointNotFoundException)
            {
                isConnected = false;
                LastConnectionDiagnostic = new MotionConnectionDiagnostic(
                    ipv4,
                    openByIpRtn,
                    searchRtn,
                    searchDevNum,
                    openBySearchRtn,
                    nativeLastError,
                    ex.GetType().Name + ": " + ex.Message);
                return E_Result.E_FAILED;
            }

            isConnected = rtn == 0;
            LastConnectionDiagnostic = new MotionConnectionDiagnostic(
                ipv4,
                openByIpRtn,
                searchRtn,
                searchDevNum,
                openBySearchRtn,
                nativeLastError,
                diagnosticMessage);

            return _Result(isConnected);
        }

        /// <summary>
        /// 断开运动控制卡
        /// </summary>
        /// <returns>
        /// 断开状态</returns>
        public E_Result CardDisconnect()
        {
            short rtn = 0;
            // 判断是否已经连接，关闭连接并改变连接状态
            if (isConnected)
            {
                short axisCloseRtn = CloseAxisHandles();
                lock (nativeSync)
                {
                    rtn = CNMCLib20.NMC_DevClose(ref DevHandle);
                }
                isConnected = false;
                DevHandle = 0;
                ClearRuntimeState();
                return _Result(axisCloseRtn == 0 && rtn == 0);
            }
            else
            {
                return E_Result.E_ALREADY_DISCONNECTED;
            }
        }

        /// <summary>
        /// 初始化运动控制卡
        /// </summary>
        /// <remarks>将运动配置内置，写死</remarks>
        /// <returns>初始化状态</returns>
        public E_Result CardInitial()
        {
            if (!isConnected)
            {
                return E_Result.E_ALREADY_DISCONNECTED;
            }

            ClearAllAlarmIds();
            short rtn = 0;

            CNMCLib20.TDevResourceInfo devInformation = new CNMCLib20.TDevResourceInfo();

            //获取控制器信息
            lock (nativeSync)
            {
                rtn = CNMCLib20.NMC_GetCardInfo(DevHandle, ref devInformation);
            }
            if (rtn != 0)
            {
                return E_Result.E_FAILED;
            }

            //获取轴号  
            NUM = devInformation.axisNum;
            int configuredAxisCount;
            try
            {
                configuredAxisCount = FileConfiguration.GetConfiguredAxisCount();
            }
            catch (Exception ex)
            {
                Debug.WriteLine("MotionModule.json 轴配置错误: " + ex.Message);
                return E_Result.E_FAILED;
            }

            if (configuredAxisCount <= 0)
            {
                return E_Result.E_FAILED;
            }

            //获取板卡IP
            ipv4 = devInformation.ipv4;

            // 首次进行生成操作，dll不会有反馈，此时以 JSON 配置轴数为准。
            if (NUM == 0)
            {
                NUM = configuredAxisCount;
            }

            NUM = Math.Min(Math.Min(NUM, configuredAxisCount), ax.Length);
            CloseAxisHandles();
            ushort[] openedAxisHandles = new ushort[NUM];

            // 开启单轴，并输出可控轴列表 257-264八轴控制。
            for (int i = 0; i < NUM; i++)
            {
                lock (nativeSync)
                {
                    rtn = CNMCLib20.NMC_MtOpen(DevHandle, (short)i, ref openedAxisHandles[i]);
                }
                if (rtn != 0 || openedAxisHandles[i] == 0)
                {
                    return E_Result.E_FAILED;
                }
            }

            axisHandle = openedAxisHandles;

            //读取默认运动参数
            for (int i = 0; i < NUM; i++)
            {
                try
                {
                    mp[i] = FileConfiguration.GetMotionPara(i, axisHandle[i]);
                    Debug.WriteLine("载入 JSON 参数");
                }
                catch (Exception ex)
                {
                    Debug.WriteLine("MotionModule.json 参数错误: " + ex.Message);
                    return E_Result.E_FAILED;
                }
            }
            //获取并配置各轴参数
            for (ushort i = 0; i < NUM; i++)
            {
                ax[i] = GetAxisPara(DevHandle, axisHandle[i], mp[i].Scale, mp[i].Smoth);
            }


            // 根据机械结构配置各轴参数
            AxisMechanicalConfig[] axisConfigs = FileConfiguration.GetAxisMechanicalConfigs(Math.Min(NUM, 4));
            for (int i = 0; i < axisConfigs.Length; i++)
            {
                mp[i].Scale = axisConfigs[i].Scale;
                ax[i].IsPoslmtDown = axisConfigs[i].IsPosLmtDown;
                ax[i].IsNeglmtDown = axisConfigs[i].IsNegLmtDown;
                axisEncoderModes[i] = axisConfigs[i].Encoder;
            }


            for (int i = 0; i < NUM; i++)
            {
                if (SetAxisPara((Dimension)i, BuildAxisPara(i)) != 0)
                {
                    return E_Result.E_FAILED;
                }
            }

            return E_Result.E_SUCCESS;
        }

        /// <summary>
        /// 复位运动控制卡
        /// </summary>
        /// <returns></returns>
        public E_Result CardReset()
        {
            short rtn = 0;
            if (isConnected)
            {
                lock (nativeSync)
                {
                    rtn = CNMCLib20.NMC_DevReset(DevHandle);
                }
                return _Result(rtn == 0);
            }
            else
            {
                return E_Result.E_ALREADY_DISCONNECTED;
            }
        }

        /// <summary>
        /// 清除错误
        /// </summary>
        /// <returns>清除错误状态</returns>
        public E_Result ClearError(Dimension dimension)
        {
            short rtn = 0;
            if (!TryGetAxisHandle(dimension, out _, out ushort axisCurrentHandle))
            {
                return E_Result.E_INVALID_ARGUMENT;
            }

            lock (nativeSync)
            {
                rtn = CNMCLib20.NMC_MtClrError(axisCurrentHandle);
            }
            return _Result(rtn == 0);
        }

        /// <summary>
        /// 伺服使能
        /// </summary>
        /// <remarks>运动控制底层逻辑模块-伺服使能</remarks>
        /// <returns>伺服使能状态</returns>
        public E_Result ServoEnable(Dimension dimension, bool Enable)
        {
            short rtn = 0;
            if (!TryGetAxisHandle(dimension, out _, out ushort axisCurrentHandle))
            {
                return E_Result.E_INVALID_ARGUMENT;
            }

            if (Enable)
            {
                // 开启使能
                lock (nativeSync)
                {
                    rtn = CNMCLib20.NMC_MtSetSvOn(axisCurrentHandle);
                }
                ClearError(dimension);
                return _Result(rtn == 0);
            }
            else
            {
                // 关闭使能
                lock (nativeSync)
                {
                    rtn = CNMCLib20.NMC_MtSetSvOff(axisCurrentHandle);
                }
                return _Result(rtn == 0);
            }

        }

        /// <summary>
        /// 原点复位
        /// </summary>
        /// <param name="dimension">单轴维度</param>
        /// <param name="speed">原点复位</param>
        /// <param name="offset">原点复位偏移</param>
        /// <returns></returns>
        public E_Result MotorHome(Dimension dimension, double speed, double offset)
        {
            short rtn = 0;
            if (!TryGetAxisHandle(dimension, out int currentAxis, out ushort axisCurrentHandle))
            {
                return E_Result.E_INVALID_ARGUMENT;
            }

            HomePara homePara;
            try
            {
                homePara = FileConfiguration.GetHomePara(currentAxis);
            }
            catch (Exception ex)
            {
                Debug.WriteLine("MotionModule.json 回零参数错误: " + ex.Message);
                SetAxisAlarm(dimension, MotionAlarmFlags.Home, true);
                return E_Result.E_FAILED;
            }


            // 实例化单轴配置
            ax[currentAxis].HomeMode = homePara.HomeMode;
            ax[currentAxis].IsNegHome = homePara.IsNegHome;
            ax[currentAxis].HomeMaxPos = homePara.HomeMaxPos;
            ax[currentAxis].SearchHomeVel = speed;
            ax[currentAxis].HomeAcc = homePara.HomeAcc;
            ax[currentAxis].HomeBackVel = homePara.HomeBackVel;
            ax[currentAxis].HomeOffset = offset;
            ax[currentAxis].HomeOffsetBegin = homePara.HomeOffsetBegin;
            ax[currentAxis].HomeOffsetLmt = homePara.HomeOffsetLmt;
            ax[currentAxis].IsHomeTwice = homePara.IsHomeTwice;
            ax[currentAxis].IsZUp = homePara.IsZUp;
            ax[currentAxis].IsHomeUp = homePara.IsHomeUp;
            ax[currentAxis].IsLmtUp = homePara.IsLmtUp;
            ax[currentAxis].IsHomeZero = homePara.IsHomeZero;
            mp[currentAxis].ZeroPos = ax[currentAxis].IsHomeZero == true ? 0 : 1;

            // 设置回零参数
            CNMCLib20.THomeSetting homepara;
            homepara.mode = ax[currentAxis].HomeMode;
            homepara.dir = (short)(ax[currentAxis].IsNegHome == true ? 0 : 1);
            homepara.acc = ax[currentAxis].HomeAcc * mp[currentAxis].Scale / 1000000;
            homepara.homeEdge = ax[currentAxis].IsHomeUp == true ? (byte)1 : (byte)0;
            homepara.zEdge = ax[currentAxis].IsZUp == true ? (byte)1 : (byte)0;
            homepara.lmtEdge = ax[currentAxis].IsLmtUp == true ? (byte)1 : (byte)0;
            homepara.offset = (int)(ax[currentAxis].HomeOffset * mp[currentAxis].Scale);
            homepara.scan1stVel = ax[currentAxis].SearchHomeVel * mp[currentAxis].Scale / 1000;
            homepara.scan2ndVel = ax[currentAxis].HomeBackVel * mp[currentAxis].Scale / 1000;
            homepara.reScanEn = ax[currentAxis].IsHomeTwice == true ? (byte)1 : (byte)0;
            homepara.iniRetPos = (int)(ax[currentAxis].HomeOffsetBegin * mp[currentAxis].Scale);
            homepara.retSwOffset = (int)(ax[currentAxis].HomeOffsetLmt * mp[currentAxis].Scale);
            homepara.safeLen = (int)(ax[currentAxis].HomeMaxPos * mp[currentAxis].Scale);
            homepara.usePreSetPtpPara = 0;
            homepara.reserved0 = 0;
            homepara.reserved1 = 0;
            homepara.reserved2 = 0;


            lock (nativeSync)
            {
                rtn = CNMCLib20.NMC_MtSetHomePara(axisCurrentHandle, ref homepara);
                if (rtn != 0)
                {
                    // 回零参数错误
                    SetAxisAlarm(dimension, MotionAlarmFlags.Home, true);
                    return E_Result.E_FAILED;
                }
                rtn = CNMCLib20.NMC_MtHome(axisCurrentHandle);
                if (rtn != 0)
                {
                    // 回零启动失败
                    SetAxisAlarm(dimension, MotionAlarmFlags.Home, true);
                    return E_Result.E_FAILED;
                }
            }
            SetAxisAlarm(dimension, MotionAlarmFlags.Home, false);
            return E_Result.E_SUCCESS;
        }

        /// <summary>
        /// 原点复位状态
        /// </summary>
        /// <param name="dimension">单轴维度</param>
        /// <returns>0: 回零停止 1:回零中，2：回零成功，4：回零失败，8：回零参数，16 开关失效</returns>
        public short MotorHomeStatus(Dimension dimension)
        {
            TryMotorHomeStatus(dimension, out short homests);
            return homests;
        }

        public E_Result TryMotorHomeStatus(Dimension dimension, out short homests)
        {
            homests = 0;
            if (!isConnected)
            {
                return E_Result.E_ALREADY_DISCONNECTED;
            }

            if (!TryGetAxisHandle(dimension, out _, out ushort axisCurrentHandle))
            {
                return E_Result.E_INVALID_ARGUMENT;
            }

            short rtn;
            lock (nativeSync)
            {
                // 获取原点复位状态
                rtn = CNMCLib20.NMC_MtGetHomeSts(axisCurrentHandle, ref homests);
            }
            return _Result(rtn == 0);
        }

        /// <summary>
        /// 停止原点复位
        /// </summary>
        /// <param name="dimension">维度</param>
        /// <returns></returns>
        public E_Result MotorHomeStop(Dimension dimension)
        {
            short rtn = 0;
            if (!TryGetAxisHandle(dimension, out _, out ushort axisCurrentHandle))
            {
                return E_Result.E_INVALID_ARGUMENT;
            }

            lock (nativeSync)
            {
                // 停止原点复位
                rtn = CNMCLib20.NMC_MtHomeStop(axisCurrentHandle);
            }
            return _Result(rtn == 0);
        }

        /// <summary>
        /// 位置清零
        /// </summary>
        /// <param name="dimension">维度</param>
        /// <returns></returns>
        public E_Result MotorZero(Dimension dimension)
        {
            short rtn = 0;
            if (!TryGetAxisHandle(dimension, out _, out ushort axisCurrentHandle))
            {
                return E_Result.E_INVALID_ARGUMENT;
            }

            lock (nativeSync)
            {
                // 将当前位置进行清除
                rtn = CNMCLib20.NMC_MtZeroPos(axisCurrentHandle);
            }
            return _Result(rtn == 0);
        }

        /// <summary>
        /// JOG运动
        /// </summary>
        /// <param name="dimension">JOG的维度</param>
        /// <param name="speed">JOG的速度</param>
        /// <param name="direction">JOG的方向; True顺时针，False逆时针</param>
        /// <returns></returns>
        public E_Result MotorJog(Dimension dimension, double speed, bool direction)
        {
            if (!TryGetAxisHandle(dimension, out int currentAxis, out ushort axisCurrentHandle))
            {
                return E_Result.E_INVALID_ARGUMENT;
            }

            // 实例化运动配置
            // mp[currentAxis] = GetMotionPara(ax[currentAxis].AxisHandle);

            // 设置运动参数
            mp[currentAxis].Mode = 0;
            mp[currentAxis].Vel = speed;
            mp[currentAxis].Direction = direction;
            try
            {
                JogParametersConfigure jogParameters = FileConfiguration.GetJogParameters(currentAxis);
                mp[currentAxis].Acc = jogParameters.Acc.Value;
                mp[currentAxis].Dec = jogParameters.Dec.Value;
                ax[currentAxis].Smooth = jogParameters.Smooth.Value;
            }
            catch (Exception ex)
            {
                Debug.WriteLine("MotionModule.json JOG 参数错误: " + ex.Message);
                return E_Result.E_FAILED;
            }

            // 判断方向
            int dirIndex = mp[currentAxis].Direction == true ? -1 : 1;

            short rtn = 0;
            CNMCLib20.TJogPara jogPara;
            if (mp[currentAxis].Mode == 0)
            {
                lock (nativeSync)
                {
                    //jog
                    rtn = CNMCLib20.NMC_MtSetPrfMode(axisCurrentHandle, CNMCLib20.MT_JOG_PRF_MODE);
                    if (rtn != 0)
                    {
                        // JOG运动模式设置失败
                        return E_Result.E_FAILED;
                    }
                    jogPara.acc = mp[currentAxis].Acc * mp[currentAxis].Scale / 1000000;
                    jogPara.dec = mp[currentAxis].Dec * mp[currentAxis].Scale / 1000000;
                    jogPara.smoothCoef = ax[currentAxis].Smooth;
                    rtn = CNMCLib20.NMC_MtSetJogPara(axisCurrentHandle, ref jogPara);
                    if (rtn != 0)
                    {
                        // JOG运动参数设置失败
                        return E_Result.E_FAILED;
                    }
                    rtn = CNMCLib20.NMC_MtSetVel(axisCurrentHandle, dirIndex * mp[currentAxis].Vel * mp[currentAxis].Scale / 1000);

                    if (rtn != 0)
                    {
                        // JOG运动的最高速度设置失败
                        return E_Result.E_FAILED;
                    }
                    rtn = CNMCLib20.NMC_MtUpdate(axisCurrentHandle);
                    if (rtn != 0)
                    {
                        // JOG启动失败
                        return E_Result.E_FAILED;
                    }
                }
            }
            return E_Result.E_SUCCESS;
        }

        /// <summary>
        /// 单轴停止运行
        /// </summary>
        /// <param name="dimension">单轴维度</param>
        /// <returns>单轴停止状态</returns>
        public E_Result MotorStop(Dimension dimension)
        {
            short rtn = 0;
            if (!TryGetAxisHandle(dimension, out _, out ushort axisCurrentHandle))
            {
                return E_Result.E_INVALID_ARGUMENT;
            }

            lock (nativeSync)
            {
                rtn = CNMCLib20.NMC_MtStop(axisCurrentHandle);
            }
            return _Result(rtn == 0);
        }

        /// <summary>
        /// 单轴相对运动
        /// </summary>
        /// <param name="dimension">维度</param>
        /// <param name="speed">运行速度</param>
        /// <param name="position">运行位置</param>
        /// <returns></returns>
        public E_Result MotorRelative(Dimension dimension, double speed, double position)
        {
            short rtn = 0;
            CNMCLib20.TPtpPara ptpPara;

            if (!TryGetAxisHandle(dimension, out int currentAxis, out ushort axisCurrentHandle))
            {
                return E_Result.E_INVALID_ARGUMENT;
            }

            // 相对运动
            // 获取当前轴配置
            // mp[currentAxis] = GetMotionPara(ax[currentAxis].AxisHandle);

            //设置运动参数
            mp[currentAxis].Mode = 1;
            mp[currentAxis].Vel = speed;
            mp[currentAxis].Pos = position;

            int targetPulse;

            lock (nativeSync)
            {
                // 获取当前轴详细参数
                ax[currentAxis] = GetAxisPara(DevHandle, axisHandle[currentAxis], mp[currentAxis].Scale, mp[currentAxis].Smoth);
                // 获取当前位置
                double newpos = ax[currentAxis].CurrentPos0 * mp[currentAxis].Scale;
                targetPulse = ToPulse(mp[currentAxis].Pos * mp[currentAxis].Scale + newpos);

                // step1--设置模式
                rtn = CNMCLib20.NMC_MtSetPrfMode(axisCurrentHandle, CNMCLib20.MT_PTP_PRF_MODE);
                if (rtn != 0)
                {
                    SetAxisAlarm(dimension, MotionAlarmFlags.RelativeMove, true);
                    // PTOP运动模式设置失败
                    return E_Result.E_FAILED;
                }
                //step2--设置参数
                ptpPara.acc = mp[currentAxis].Acc * mp[currentAxis].Scale / 1000000;
                ptpPara.dec = mp[currentAxis].Dec * mp[currentAxis].Scale / 1000000;
                ptpPara.smoothCoef = (short)ax[currentAxis].Smooth;
                ptpPara.startVel = mp[currentAxis].JumpVel * mp[currentAxis].Scale / 1000;
                ptpPara.endVel = mp[currentAxis].EndVel * mp[currentAxis].Scale / 1000;
                ptpPara.dummy1 = 0;
                ptpPara.dummy2 = 0;
                ptpPara.dummy3 = 0;
                rtn = CNMCLib20.NMC_MtSetPtpPara(axisCurrentHandle, ref ptpPara);
                if (rtn != 0)
                {
                    // PTOP运动参数设置失败;
                    SetAxisAlarm(dimension, MotionAlarmFlags.RelativeMove, true);
                    return E_Result.E_FAILED;
                }
                //step3--设置速度
                rtn = CNMCLib20.NMC_MtSetVel(axisCurrentHandle, mp[currentAxis].Vel * mp[currentAxis].Scale / 1000);
                if (rtn != 0)
                {
                    // PTOP运动的最高速度设置失败;
                    SetAxisAlarm(dimension, MotionAlarmFlags.RelativeMove, true);
                    return E_Result.E_FAILED;
                }
                //step4--设置目标位置
                rtn = CNMCLib20.NMC_MtSetPtpTgtPos(axisCurrentHandle, targetPulse);

                if (rtn != 0)
                {
                    // PTOP目标位置设置失败;
                    SetAxisAlarm(dimension, MotionAlarmFlags.RelativeMove, true);
                    return E_Result.E_FAILED;
                }

                RememberPtpTarget(currentAxis, targetPulse);

                //step5--启动运动
                rtn = CNMCLib20.NMC_MtUpdate(axisCurrentHandle);
                if (rtn != 0)
                {
                    // PTOP启动失败;
                    SetAxisAlarm(dimension, MotionAlarmFlags.RelativeMove, true);
                    return E_Result.E_FAILED;
                }
            }

            SetAxisAlarm(dimension, MotionAlarmFlags.RelativeMove, false);
            return E_Result.E_SUCCESS;
        }

        /// <summary>
        /// 单轴绝对运动
        /// </summary>
        /// <param name="dimension">维度</param>
        /// <param name="speed">运行速度</param>
        /// <param name="position">运行位置</param>
        /// <returns></returns>
        public E_Result MotorAbsolute(Dimension dimension, double speed, double position)
        {
            short rtn = 0;
            if (!TryGetAxisHandle(dimension, out int currentAxis, out ushort axisCurrentHandle))
            {
                return E_Result.E_INVALID_ARGUMENT;
            }

            // 获取当前轴配置
            // mp[currentAxis] = GetMotionPara(ax[currentAxis].AxisHandle);

            //设置运动参数
            mp[currentAxis].Mode = 2;
            mp[currentAxis].Vel = speed;
            mp[currentAxis].Pos = position;


            int targetPulse = ToPulse(mp[currentAxis].Pos * mp[currentAxis].Scale);

            lock (nativeSync)
            {
                //绝对运动
                //step1--设置模式
                rtn = CNMCLib20.NMC_MtSetPrfMode(axisCurrentHandle, CNMCLib20.MT_PTP_PRF_MODE);
                if (rtn != 0)
                {
                    // PTOP运动模式设置失败;
                    SetAxisAlarm(dimension, MotionAlarmFlags.AbsoluteMove, true);
                    return E_Result.E_FAILED;
                }
                //step2--设置参数
                CNMCLib20.TPtpPara ptpPara;
                ptpPara.acc = mp[currentAxis].Acc * mp[currentAxis].Scale / 1000000;
                ptpPara.dec = mp[currentAxis].Dec * mp[currentAxis].Scale / 1000000;
                ptpPara.smoothCoef = (short)ax[currentAxis].Smooth;
                ptpPara.startVel = mp[currentAxis].JumpVel * mp[currentAxis].Scale / 1000;
                ptpPara.endVel = mp[currentAxis].EndVel * mp[currentAxis].Scale / 1000;
                ptpPara.dummy1 = 0;
                ptpPara.dummy2 = 0;
                ptpPara.dummy3 = 0;
                rtn = CNMCLib20.NMC_MtSetPtpPara(axisCurrentHandle, ref ptpPara);
                if (rtn != 0)
                {
                    // PTOP运动参数设置失败;
                    SetAxisAlarm(dimension, MotionAlarmFlags.AbsoluteMove, true);
                    return E_Result.E_FAILED;
                }
                //step3--设置速度
                rtn = CNMCLib20.NMC_MtSetVel(axisCurrentHandle, mp[currentAxis].Vel * mp[currentAxis].Scale / 1000);
                if (rtn != 0)
                {
                    // PTOP运动的最高速度设置失败;
                    SetAxisAlarm(dimension, MotionAlarmFlags.AbsoluteMove, true);
                    return E_Result.E_FAILED;
                }
                //step4--设置目标位置
                rtn = CNMCLib20.NMC_MtSetPtpTgtPos(axisCurrentHandle, targetPulse);
                if (rtn != 0)
                {
                    // PTOP目标位置设置失败;
                    SetAxisAlarm(dimension, MotionAlarmFlags.AbsoluteMove, true);
                    return E_Result.E_FAILED;
                }

                RememberPtpTarget(currentAxis, targetPulse);

                //step5--启动运动
                rtn = CNMCLib20.NMC_MtUpdate(axisCurrentHandle);
                if (rtn != 0)
                {
                    // PTOP启动失败
                    SetAxisAlarm(dimension, MotionAlarmFlags.AbsoluteMove, true);
                    return E_Result.E_FAILED;
                }
            }
            SetAxisAlarm(dimension, MotionAlarmFlags.AbsoluteMove, false);
            return E_Result.E_SUCCESS;
        }

        /// <summary>
        /// 设置单轴螺距补偿(1°补偿一次)
        /// </summary>
        /// <param name="dimension"></param>
        /// <param name="comPos"></param>
        /// <param name="comNeg"></param>
        /// <returns></returns>
        public E_Result MotorCompensationPara(Dimension dimension, short[] comPos, short[] comNeg)
        {
            short rtn = 0;
            if (!TryGetAxisHandle(dimension, out int currentAxis, out ushort axisCurrentHandle))
            {
                return E_Result.E_INVALID_ARGUMENT;
            }

            lock (nativeSync)
            {
                rtn = CNMCLib20.NMC_MtSetLeadScrewCompPara(axisCurrentHandle, 360, 0, 360 * (int)mp[currentAxis].Scale, comPos, comNeg);
            }
            return _Result(rtn == 0);
        }

        /// <summary>
        /// 启动单轴螺距补偿
        /// </summary>
        /// <param name="dimension"></param>
        /// <returns></returns>
        public E_Result MotorCompensationEnable(Dimension dimension, bool enable)
        {
            short rtn = 0;
            if (!TryGetAxisHandle(dimension, out _, out ushort axisCurrentHandle))
            {
                return E_Result.E_INVALID_ARGUMENT;
            }

            short sw = enable == true ? (short)1 : (short)0;
            lock (nativeSync)
            {
                rtn = CNMCLib20.NMC_MtEnableLeadScrew(axisCurrentHandle, sw);
            }
            return _Result(rtn == 0);
        }

        /// <summary>
        /// 高速二维位置比较参数设置
        /// </summary>
        /// <param name="dimension">维度</param>
        /// <param name="pulseWidth">脉冲宽度</param>
        /// <returns></returns>
        public E_Result MotorCompareHS2Para(Dimension dimension, int pulseWidth)
        {
            if (!isConnected)
            {
                return E_Result.E_ALREADY_DISCONNECTED;
            }

            if (!TryGetAxisIndex(dimension, out int axisIndex) || pulseWidth <= 0)
            {
                return E_Result.E_INVALID_ARGUMENT;
            }

            short rtn = 0;
            short group = 0;
            short currentAxis = (short)axisIndex;

            // 停止高速位置比较,以防止上次异常，没有关闭
            MotorCompareHS2Stop();

            TriggerParametersConfigure triggerParameters;
            try
            {
                triggerParameters = FileConfiguration.GetTriggerParameters(axisIndex);
            }
            catch (Exception ex)
            {
                Debug.WriteLine("MotionModule.json 触发参数错误: " + ex.Message);
                return E_Result.E_FAILED;
            }

            if (triggerParameters == null ||
                !triggerParameters.Dir2No.HasValue ||
                !triggerParameters.OutputChn.HasValue ||
                !triggerParameters.OutputType.HasValue ||
                !triggerParameters.ChnType.HasValue ||
                !triggerParameters.PosSrc.HasValue ||
                !triggerParameters.StLevel.HasValue ||
                !triggerParameters.ErrZone.HasValue ||
                !triggerParameters.DirectOutZone.HasValue ||
                !triggerParameters.VibrateRange.HasValue ||
                !triggerParameters.MinIntervalTime.HasValue)
            {
                return E_Result.E_FAILED;
            }

            // 定义比较参数
            CNMCLib20.TComp2DimensParamEx comp2DimensParamEx = new CNMCLib20.TComp2DimensParamEx(true);
            comp2DimensParamEx.dir1No = currentAxis;
            comp2DimensParamEx.dir2No = triggerParameters.Dir2No.Value;
            comp2DimensParamEx.outputChn = triggerParameters.OutputChn.Value;
            comp2DimensParamEx.outputType = triggerParameters.OutputType.Value;
            comp2DimensParamEx.chnType = triggerParameters.ChnType.Value;
            comp2DimensParamEx.posSrc = triggerParameters.PosSrc.Value;
            comp2DimensParamEx.stLevel = triggerParameters.StLevel.Value;
            comp2DimensParamEx.errZone = triggerParameters.ErrZone.Value;
            comp2DimensParamEx.directOutZone = triggerParameters.DirectOutZone.Value;
            comp2DimensParamEx.vibrateRange = triggerParameters.VibrateRange.Value;
            comp2DimensParamEx.gateTime = pulseWidth;
            comp2DimensParamEx.minIntervalTime = triggerParameters.MinIntervalTime.Value;



            lock (nativeSync)
            {
                rtn = CNMCLib20.NMC_Comp2DimensSetParamEx(DevHandle, group, ref comp2DimensParamEx, 0);
            }

            if (rtn != 0)
            {
                // 二维位置比较参数设置错误

                return E_Result.E_FAILED;
            }
            return E_Result.E_SUCCESS;
        }

        /// <summary>
        /// 高速二维位置比较数据设置
        /// </summary>
        /// <param name="dimension">维度</param>
        /// <param name="posArray"> 目标数组</param>
        /// <returns></returns>
        public E_Result MotorCompareHs2Data(Dimension dimension, double[] posRangleArray)
        {
            if (!isConnected)
            {
                return E_Result.E_ALREADY_DISCONNECTED;
            }

            if (!TryGetAxisIndex(dimension, out int currentAxis))
            {
                return E_Result.E_INVALID_ARGUMENT;
            }

            if (posRangleArray == null || posRangleArray.Length == 0 || posRangleArray.Length % 2 != 0)
            {
                return E_Result.E_INVALID_ARGUMENT;
            }

            int[] posArray = new int[posRangleArray.Length];

            for (int i = 0; i < posRangleArray.Length; i++)
            {
                posArray[i] = (int)(posRangleArray[i] * mp[currentAxis].Scale);
            }

            short rtn = 0;
            short grounp = 0;
            lock (nativeSync)
            {
                rtn = CNMCLib20.NMC_Comp2DimensSetData(DevHandle, grounp, posArray, (short)(posArray.Length / 2), 0);
            }
            if (rtn != 0)
            {

                // 二维位置比较点位设置错误
                return E_Result.E_FAILED;
            }

            return E_Result.E_SUCCESS;
        }

        /// <summary>
        /// 高速二维位置比较状态
        /// </summary>
        /// <param name="compareStatus">比较参数</param>
        /// <returns></returns>
        public E_Result MotorCompareHS2Status(out HS2CompareStatus compareStatus)
        {
            short rtn = 0;
            short grounp = 0;
            compareStatus = new HS2CompareStatus();
            CNMCLib20.TComp2DimensSts comp2DimensSts = new CNMCLib20.TComp2DimensSts();
            lock (nativeSync)
            {
                rtn = CNMCLib20.NMC_Comp2DimensStatusEx(DevHandle, grounp, ref comp2DimensSts, 0);
            }

            compareStatus.sts = comp2DimensSts.sts;
            compareStatus.freeSpace = comp2DimensSts.freeSpace;
            compareStatus.usedSpace = comp2DimensSts.usedSpace;
            compareStatus.outCount = comp2DimensSts.outCount;
            return _Result(rtn == 0);
        }

        /// <summary>
        /// 开始高速二维位置比较
        /// </summary>
        /// <returns></returns>
        public E_Result MotorCompareHS2Start()
        {
            short rtn = 0;
            short group = 0;

            lock (nativeSync)
            {
                rtn = CNMCLib20.NMC_Comp2DimensOnoff(DevHandle, group, On, 0);
            }
            if (rtn != 0)
            {

                // 二维位置比较开启错误
                return E_Result.E_FAILED;
            }
            return E_Result.E_SUCCESS;
        }

        /// <summary>
        /// 停止高速二维位置比较
        /// </summary>
        /// <returns></returns>
        public E_Result MotorCompareHS2Stop()
        {
            short rtn = 0;
            short grounp = 0;
            int[] posArray = new int[0];
            lock (nativeSync)
            {
                rtn = CNMCLib20.NMC_Comp2DimensOnoff(DevHandle, grounp, Off, 0);
                CNMCLib20.NMC_Comp2DimensSetData(DevHandle, grounp, posArray, (short)(posArray.Length / 2), 0);
            }
            return _Result(rtn == 0);
        }

        /// <summary>
        /// IO控制
        /// </summary>
        /// <param name="Switch">开关</param>
        /// <param name="index">IO位置</param>
        /// <returns></returns>
        public E_Result MotorIOControl(bool Switch, short index)
        {
            int level = Switch == true ? 0 : 1;
            short rtn = 0;
            lock (nativeSync)
            {
                rtn = CNMCLib20.NMC_SetDOBit(DevHandle, index, (short)level);
            }
            return _Result(rtn == 0);
        }

        /// <summary>
        /// 返回当前轴状态
        /// </summary>
        /// <param name="dimension">维度</param>
        /// <returns></returns>
        public Axis MotorGetStatus(Dimension dimension)
        {
            TryMotorGetStatus(dimension, out Axis axis);
            return axis;
        }

        public E_Result TryMotorGetStatus(Dimension dimension, out Axis axis)
        {
            axis = new Axis();
            if (!isConnected)
            {
                return E_Result.E_ALREADY_DISCONNECTED;
            }

            short rtn = 0;
            if (!TryGetAxisHandle(dimension, out int currentAxis, out ushort axisCurrentHandle))
            {
                return E_Result.E_INVALID_ARGUMENT;
            }

            int motionIOstatus = 256;
            lock (nativeSync)
            {
                axis = GetAxisPara(DevHandle, axisCurrentHandle, mp[currentAxis].Scale, mp[currentAxis].Smoth, out short nativeReadRtn);
                if (nativeReadRtn != 0)
                {
                    return E_Result.E_FAILED;
                }

                rtn = CNMCLib20.NMC_MtGetMotionIO(axisCurrentHandle, ref motionIOstatus);
                if (rtn != 0)
                {
                    return E_Result.E_FAILED;
                }
            }

            axis.IsConnected = true;
            if ((motionIOstatus & (1 << 8)) != 0)
            {
                axis.IsAlarming = true;
            }
            else
            {
                axis.IsAlarming = false;
            }

            axis.PosArrived = (motionIOstatus & (1 << 6)) != 0;
            axis.NegArrived = (motionIOstatus & (1 << 7)) != 0;
            BuildStatusAlarmId(dimension, axis);
            return E_Result.E_SUCCESS;
        }

        internal E_Result TryValidatePtpCompletion(Dimension dimension, Axis axis, out bool isArrived)
        {
            isArrived = false;
            if (!TryGetAxisHandle(dimension, out int currentAxis, out ushort axisCurrentHandle))
            {
                return E_Result.E_INVALID_ARGUMENT;
            }

            int nativeTargetPulse = 0;
            int commandPulse = 0;
            int actualPulse = 0;
            int arrivalBand = 0;
            int stableTime = 0;
            short targetRtn;
            short cmdRtn;
            short actualRtn;
            short arrivalRtn;

            lock (nativeSync)
            {
                targetRtn = CNMCLib20.NMC_MtGetPtpTgtPos(axisCurrentHandle, ref nativeTargetPulse);
                cmdRtn = CNMCLib20.NMC_MtGetCmdPos(axisCurrentHandle, ref commandPulse);
                actualRtn = CNMCLib20.NMC_MtGetAxisPos(axisCurrentHandle, ref actualPulse);
                arrivalRtn = CNMCLib20.NMC_MtGetAxisArrivalPara(axisCurrentHandle, ref arrivalBand, ref stableTime);
            }

            if (cmdRtn != 0 || actualRtn != 0)
            {
                return E_Result.E_FAILED;
            }

            int targetPulse = targetRtn == 0
                ? nativeTargetPulse
                : GetRememberedPtpTarget(currentAxis, commandPulse);

            int tolerancePulse = GetArrivalTolerancePulse(axis, arrivalRtn == 0 ? arrivalBand : 0);
            bool commandArrived = Math.Abs(commandPulse - targetPulse) <= tolerancePulse;
            bool actualArrived = Math.Abs(actualPulse - targetPulse) <= tolerancePulse;
            isArrived = axis.IsArrive && commandArrived && actualArrived;
            return E_Result.E_SUCCESS;
        }


        /// <summary>
        /// 获取各轴参数
        /// </summary>
        /// <param name="devhandle">设备句柄</param>
        /// <param name="axisHandle">轴句柄</param>
        /// <param name="scale">轴当量</param>
        /// <param name="somthtime">轴平滑系数</param>
        /// <returns>返回轴配置</returns>
        private Axis GetAxisPara(UInt16 devhandle, UInt16 axisHandle, double scale, double somthtime)
        {
            return GetAxisPara(devhandle, axisHandle, scale, somthtime, out _);
        }

        private Axis GetAxisPara(UInt16 devhandle, UInt16 axisHandle, double scale, double somthtime, out short nativeReadRtn)
        {
            lock (nativeSync)
            {
                nativeReadRtn = 0;
                short rtn = 0;
                short axsists = 0;
                int postemp = 0;
                short posswt = 0, negswt = 0, swt = 0, swt1 = 0;
                short encmode = 0;
                int posiii = 0, negggg = 0;
                int poserr = 0;
                short posi = 0, neg = 0, posii = 0, negg = 0;
                double nowvel = 0.00;
                double scaletemp = 1.00;  //轴当量
                CNMCLib20.THomeSetting homepara = new CNMCLib20.THomeSetting();
                CNMCLib20.TSafePara safePara = new CNMCLib20.TSafePara();
                Axis temp = new Axis();
                temp.AxisHandle = axisHandle;
                scaletemp = scale;
                if (scaletemp == 0)
                {
                    scaletemp = 1;
                }
                temp.Scale = scaletemp;
                //获取限位激活配置           
                rtn = CNMCLib20.NMC_MtGetLmtOnOff(axisHandle, ref posswt, ref negswt);
                RememberNativeError(ref nativeReadRtn, rtn);
                temp.IsNegLmtActived = (negswt == 1) ? true : false;
                temp.IsPosLmtActived = (posswt == 1) ? true : false;
                //获取限位触发电平配置          
                rtn = CNMCLib20.NMC_MtGetLmtSns(axisHandle, ref posswt, ref negswt);
                RememberNativeError(ref nativeReadRtn, rtn);
                temp.IsNeglmtDown = (negswt == 0) ? true : false;
                temp.IsPoslmtDown = (posswt == 0) ? true : false;
                //获取驱动器报警配置
                rtn = CNMCLib20.NMC_MtGetAlarmOnOff(axisHandle, ref swt);
                RememberNativeError(ref nativeReadRtn, rtn);
                temp.IsAlarmActived = (swt == 1) ? true : false;
                //获取驱动器报警的电平配置
                rtn = CNMCLib20.NMC_MtGetAlarmSns(axisHandle, ref swt);
                RememberNativeError(ref nativeReadRtn, rtn);
                temp.IsAlarmDown = (swt == 0) ? true : false;
                //获取脉冲模式
                rtn = CNMCLib20.NMC_MtGetStepMode(axisHandle, ref swt, ref swt1);
                RememberNativeError(ref nativeReadRtn, rtn);
                temp.IsEncNeged = (swt == 1) ? true : false;
                temp.StepMode = swt1;
                //获取轴状态
                rtn = CNMCLib20.NMC_MtGetSts(axisHandle, ref axsists);
                RememberNativeError(ref nativeReadRtn, rtn);
                //取bit0,0静止,1运动
                temp.IsRunning = (axsists & (1 << 0)) == 0 ? false : true;
                //取bit1,位置到达,0,未到达,1到达
                temp.IsArrive = (axsists & (1 << 1)) == 0 ? false : true;
                //取bit2,运动是否出错,0,未出错,1,出错
                temp.IsError = (axsists & (1 << 2)) == 0 ? false : true;
                //取bit3,是否使能中
                temp.IsAxisOn = (axsists & (1 << 3)) == 0 ? false : true;
                //取bit6,正向限位是否触发
                temp.PosArrived = (axsists & (1 << 6)) == 0 ? false : true;
                //取bit7,负向限位是否触发
                temp.NegArrived = (axsists & (1 << 7)) == 0 ? false : true;
                //取bit8,正向软限位是否触发
                temp.PosSoftArrived = (axsists & (1 << 8)) == 0 ? false : true;
                //取bit9,负向软限位是否触发
                temp.NegSoftArrived = (axsists & (1 << 9)) == 0 ? false : true;
                //取bit10,驱动器是否报警
                temp.IsAlarming = (axsists & (1 << 10)) == 0 ? false : true;
                //取bit11,位置是否超过误差极限
                temp.IsPosErr = (axsists & (1 << 11)) == 0 ? false : true;
                //获取轴当前位置,脉冲转换为mm
                rtn = CNMCLib20.NMC_MtGetPrfPos(axisHandle, ref postemp);
                RememberNativeError(ref nativeReadRtn, rtn);
                temp.CurrentPos0 = postemp / scaletemp;
                //获取轴当前编码器位置,脉冲转化为mm
                rtn = CNMCLib20.NMC_MtGetAxisPos(axisHandle, ref postemp);
                RememberNativeError(ref nativeReadRtn, rtn);
                temp.CurrentPos1 = postemp / scaletemp;
                //获取当前轴速度,mm/s
                rtn = CNMCLib20.NMC_MtGetPrfVel(axisHandle, ref nowvel);
                RememberNativeError(ref nativeReadRtn, rtn);
                temp.CurrentVel = nowvel * 1000 / scaletemp;
                //获取是否为低电平触发硬限位停止的配置   
                //读取触发是否停止配置
                rtn = CNMCLib20.NMC_MtGetLmtOnOff(axisHandle, ref posi, ref neg);
                RememberNativeError(ref nativeReadRtn, rtn);
                //读取限位触发电平配置
                rtn = CNMCLib20.NMC_MtGetLmtSns(axisHandle, ref posii, ref negg);
                RememberNativeError(ref nativeReadRtn, rtn);
                temp.IsPoslmtDown = (posi == 1 && posii == 0) == true ? true : false;
                temp.IsNeglmtDown = (neg == 1 && negg == 0) == true ? true : false;
                //获取软限位配置
                //是否激活软限位
                rtn = CNMCLib20.NMC_MtGetSwLmtOnOff(axisHandle, ref swt);
                RememberNativeError(ref nativeReadRtn, rtn);
                temp.IsSoftLmtActived = (swt == 1) ? true : false;
                rtn = CNMCLib20.NMC_MtGetSwLmtValue(axisHandle, ref posiii, ref negggg);
                RememberNativeError(ref nativeReadRtn, rtn);
                temp.SafePoslmtPos = posiii / scaletemp;
                temp.SafelNeglmtPos = negggg / scaletemp;
                //读取轴运动安全参数
                rtn = CNMCLib20.NMC_MtGetSafePara(axisHandle, ref safePara);
                RememberNativeError(ref nativeReadRtn, rtn);
                //单位mm/s^2
                temp.StopDec = safePara.estpDec * 1000 * 1000 / scaletemp;
                //单位mm/s
                temp.MaxVel = safePara.maxVel * 1000 / scaletemp;
                //单位mm/s^2
                temp.MaxaAcc = safePara.maxAcc * 1000 * 1000 / scaletemp;
                //平滑系数
                temp.Smooth = somthtime;
                //最大位置误差读取,单位脉冲
                rtn = CNMCLib20.NMC_MtGetPosErrLmt(axisHandle, ref poserr);
                RememberNativeError(ref nativeReadRtn, rtn);
                temp.MaxPosErr = poserr;
                //获取编码器计数方式，外部还是内部，以及计数方向
                rtn = CNMCLib20.NMC_GetEncMode(devhandle, (short)axisHandle, ref encmode);
                RememberNativeError(ref nativeReadRtn, rtn);
                switch (encmode)
                {
                    case 0://外部编码器反馈，AB相90度差正
                        temp.EncMode = ENCMODE.OUTAB;
                        break;
                    case 1024:
                        temp.EncMode = ENCMODE.OUTPD;
                        break;
                    case 2048:
                        temp.EncMode = ENCMODE.OUTPG;
                        break;
                    case 4096:
                        temp.EncMode = ENCMODE.OUTABFU;
                        break;
                    case 17408:
                        temp.EncMode = ENCMODE.OUTPDFU;
                        break;
                    case 6144:
                        temp.EncMode = ENCMODE.OUTPGFU;
                        break;
                    case 256://内部编码器反馈，AB相90度差正
                        temp.EncMode = ENCMODE.INAB;
                        break;
                    case 1280:
                        temp.EncMode = ENCMODE.INPD;
                        break;
                    case 2304:
                        temp.EncMode = ENCMODE.INPG;
                        break;
                    case 4352:
                        temp.EncMode = ENCMODE.INABFU;
                        break;
                    case 17664:
                        temp.EncMode = ENCMODE.INPDFU;
                        break;
                    case 6400:
                        temp.EncMode = ENCMODE.INPGFU;
                        break;
                    default:
                        temp.EncMode = ENCMODE.INAB;
                        break;
                }
                //读取回零参数
                rtn = CNMCLib20.NMC_MtGetHomePara(axisHandle, ref homepara);
                RememberNativeError(ref nativeReadRtn, rtn);
                temp.HomeMaxPos = homepara.safeLen / scaletemp;
                temp.HomeAcc = homepara.acc * 1000000 / scaletemp;
                temp.SearchHomeVel = homepara.scan1stVel * 1000 / scaletemp;
                temp.HomeOffset = homepara.offset / scaletemp;
                temp.HomeBackVel = homepara.scan2ndVel * 1000 / scaletemp;//低速回零速度
                temp.IsNegHome = (homepara.dir == 0) ? true : false;
                temp.HomeMode = homepara.mode;
                temp.IsHomeTwice = homepara.reScanEn != 0;
                temp.IsZUp = homepara.zEdge != 0;
                temp.IsLmtUp = homepara.lmtEdge != 0;
                temp.IsHomeUp = homepara.homeEdge != 0;
                temp.HomeOffsetBegin = homepara.iniRetPos / scaletemp;
                temp.HomeOffsetLmt = homepara.retSwOffset / scaletemp;
                return temp;
            }
        }

        /// <summary>
        /// 设置各轴参数
        /// </summary>
        /// <param name="dimension">维度</param>
        /// <param name="axisPara">单轴配置参数</param>
        /// <returns></returns>
        private short SetAxisPara(Dimension dimension, AxisPara axisPara)
        {
            if (!TryGetAxisHandle(dimension, out int currentAxis, out ushort axisCurrentHandle))
            {
                return -1;
            }

            int scale = (int)mp[currentAxis].Scale;
            CNMCLib20.TSafePara safePara;
            short rtn = 0;
            lock (nativeSync)
            {
                //正负限位激活
                rtn = CNMCLib20.NMC_MtLmtOnOff(axisCurrentHandle, axisPara.Poslmt, axisPara.Neglmt);
                if (rtn != 0) return rtn;
                //正负限位电平配置
                rtn = CNMCLib20.NMC_MtLmtSns(axisCurrentHandle, axisPara.Poslmtlev, axisPara.Neglmtlev);
                if (rtn != 0) return rtn;
                //报警激活
                rtn = CNMCLib20.NMC_MtAlarmOnOff(axisCurrentHandle, axisPara.AlarmEnable);
                if (rtn != 0) return rtn;
                //报警电平配置
                rtn = CNMCLib20.NMC_MtAlarmSns(axisCurrentHandle, axisPara.AlarmLevel);
                if (rtn != 0) return rtn;
                //指令脉冲取反激活,脉冲模式设置
                rtn = CNMCLib20.NMC_MtSetStepMode(axisCurrentHandle, axisPara.StepInv, axisPara.StepMode);
                if (rtn != 0) return rtn;
                //软限位是否激活
                rtn = CNMCLib20.NMC_MtSwLmtOnOff(axisCurrentHandle, axisPara.Softlmt);
                if (rtn != 0) return rtn;
                //软件限位设置
                rtn = CNMCLib20.NMC_MtSwLmtValue(axisCurrentHandle, (int)(axisPara.Softlmtpos * scale), (int)(axisPara.Softlmtneg * scale));
                if (rtn != 0) return rtn;

                //运动安全参数设置
                safePara.estpDec = axisPara.EstpDec * scale / 1000000;
                safePara.maxAcc = axisPara.MaxAcc * scale / 1000000;
                safePara.maxVel = axisPara.MaxVel * scale / 1000;
                rtn = CNMCLib20.NMC_MtSetSafePara(axisCurrentHandle, ref safePara);
                if (rtn != 0) return rtn;

                //最大位置误差设定,单位脉冲
                rtn = CNMCLib20.NMC_MtSetPosErrLmt(axisCurrentHandle, axisPara.PosErr);
                if (rtn != 0) return rtn;
            }

            //平滑系数保存
            if (rtn != 0) return rtn;
            ax[currentAxis].Smooth = axisPara.Smooth;

            //轴当量保存 
            ax[currentAxis].Scale = axisPara.Scale;

            lock (nativeSync)
            {
                //编码器模式设置
                rtn = CNMCLib20.NMC_SetEncMode(DevHandle, (short)axisCurrentHandle, axisPara.Encoder);
            }
            return rtn;
        }

        /// <summary>
        /// 获取单轴配置
        /// </summary>
        /// <param name="axishandle">单轴句柄</param>
        /// <returns></returns>
        private MotionPara GetMotionPara(ushort axishandle)
        {
            MotionPara motionPara = new MotionPara();
            motionPara.AxisNumber = axishandle;
            return motionPara;
        }

        private AxisPara BuildAxisPara(int axisIndex)
        {
            AxisPara axisPara = new AxisPara();
            axisPara.Scale = mp[axisIndex].Scale;
            axisPara.Smooth = mp[axisIndex].Smoth;
            axisPara.Poslmtlev = ax[axisIndex].IsPoslmtDown ? (short)0 : (short)1;
            axisPara.Neglmtlev = ax[axisIndex].IsNeglmtDown ? (short)0 : (short)1;
            axisPara.Encoder = axisEncoderModes[axisIndex];
            return axisPara;
        }

        private static int ToPulse(double pulse)
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

        private void RememberPtpTarget(int axisIndex, int targetPulse)
        {
            if (axisIndex < 0 || axisIndex >= lastPtpTargetPulse.Length)
            {
                return;
            }

            lastPtpTargetPulse[axisIndex] = targetPulse;
            hasLastPtpTarget[axisIndex] = true;
        }

        private int GetRememberedPtpTarget(int axisIndex, int fallbackPulse)
        {
            if (axisIndex < 0 ||
                axisIndex >= lastPtpTargetPulse.Length ||
                !hasLastPtpTarget[axisIndex])
            {
                return fallbackPulse;
            }

            return lastPtpTargetPulse[axisIndex];
        }

        private static int GetArrivalTolerancePulse(Axis axis, int nativeArrivalBand)
        {
            if (nativeArrivalBand > 0)
            {
                return nativeArrivalBand;
            }

            if (axis.MaxPosErr > 0)
            {
                return axis.MaxPosErr;
            }

            return 1;
        }

        private short CloseAxisHandles()
        {
            short firstError = 0;
            if (axisHandle != null)
            {
                for (int i = 0; i < axisHandle.Length; i++)
                {
                    ushort currentHandle = axisHandle[i];
                    if (currentHandle != 0)
                    {
                        short rtn;
                        lock (nativeSync)
                        {
                            rtn = CNMCLib20.NMC_MtClose(ref currentHandle);
                        }
                        if (firstError == 0 && rtn != 0)
                        {
                            firstError = rtn;
                        }
                    }

                    axisHandle[i] = 0;
                }
            }

            for (int i = 0; i < ax.Length; i++)
            {
                ax[i].AxisHandle = 0;
                ax[i].IsConnected = false;
            }

            return firstError;
        }

        private void ClearRuntimeState()
        {
            axisHandle = null;
            NUM = 0;
            ipv4 = new byte[4];
            ClearAllAlarmIds();
            Array.Clear(lastPtpTargetPulse, 0, lastPtpTargetPulse.Length);
            Array.Clear(hasLastPtpTarget, 0, hasLastPtpTarget.Length);
        }

        private void CloseDeviceHandle()
        {
            if (DevHandle != 0)
            {
                ushort handle = DevHandle;
                lock (nativeSync)
                {
                    CNMCLib20.NMC_DevClose(ref handle);
                }
                DevHandle = 0;
            }
        }

        private bool TryReadDeviceIPv4(ushort devHandle, out byte[] openedIPv4)
        {
            openedIPv4 = null;
            CNMCLib20.TDevResourceInfo devInformation = new CNMCLib20.TDevResourceInfo();
            short rtn;
            lock (nativeSync)
            {
                rtn = CNMCLib20.NMC_GetCardInfo(devHandle, ref devInformation);
            }
            if (rtn != 0 || devInformation.ipv4 == null || devInformation.ipv4.Length < 4)
            {
                return false;
            }

            openedIPv4 = new byte[4];
            Array.Copy(devInformation.ipv4, openedIPv4, openedIPv4.Length);
            return true;
        }

        private static bool IsSameIPv4(byte[] left, byte[] right)
        {
            return left != null &&
                   right != null &&
                   left.Length >= 4 &&
                   right.Length >= 4 &&
                   left[0] == right[0] &&
                   left[1] == right[1] &&
                   left[2] == right[2] &&
                   left[3] == right[3];
        }

        private static string FormatIPv4(byte[] address)
        {
            if (address == null || address.Length < 4)
            {
                return "<unknown>";
            }

            return string.Format("{0}.{1}.{2}.{3}", address[0], address[1], address[2], address[3]);
        }

        private static void RememberNativeError(ref short firstError, short currentRtn)
        {
            if (firstError == 0 && currentRtn != 0)
            {
                firstError = currentRtn;
            }
        }


        /// <summary>
        /// 判断当前操作是否成功
        /// </summary>
        /// <param name="status">操作状态</param>
        /// <returns></returns>
        private E_Result _Result(bool status)
        {
            if (status)
            {
                return E_Result.E_SUCCESS;
            }
            else
            {
                return E_Result.E_FAILED;
            }
        }

        private short? TryGetLastNativeError()
        {
            try
            {
                lock (nativeSync)
                {
                    return CNMCLib20.NMC_GetLastErr();
                }
            }
            catch (Exception)
            {
                return null;
            }
        }


        // 多轴轮询
        public E_Result BlockingQuery(Dimension[] dimensions, int timeout)
        {
            if (dimensions == null || dimensions.Length == 0)
            {
                return E_Result.E_INVALID_ARGUMENT;
            }

            Stopwatch stopwatch = Stopwatch.StartNew();
            bool[] observedRunning = new bool[dimensions.Length];

            while (stopwatch.ElapsedMilliseconds <= timeout)
            {
                bool allStopped = true;
                for (int i = 0; i < dimensions.Length; i++)
                {
                    E_Result result = TryMotorGetStatus(dimensions[i], out Axis axis);
                    if (result != E_Result.E_SUCCESS)
                    {
                        return result;
                    }

                    bool activeLimit = (axis.NegArrived && axis.IsNegLmtActived) || (axis.PosArrived && axis.IsPosLmtActived);
                    if (axis.IsAlarming || axis.IsError || activeLimit)
                    {
                        return E_Result.E_FAILED;
                    }

                    if (axis.IsRunning)
                    {
                        observedRunning[i] = true;
                        allStopped = false;
                    }
                    else if (observedRunning[i])
                    {
                        E_Result validateResult = ValidatePtpAxis(dimensions[i], axis, out bool isArrived);
                        if (validateResult != E_Result.E_SUCCESS)
                        {
                            return validateResult;
                        }

                        if (!isArrived)
                        {
                            return E_Result.E_FAILED;
                        }
                    }
                    else if (!observedRunning[i])
                    {
                        if (stopwatch.ElapsedMilliseconds < 500)
                        {
                            allStopped = false;
                        }
                        else if (!axis.IsArrive)
                        {
                            return E_Result.E_FAILED;
                        }
                        else
                        {
                            E_Result validateResult = ValidatePtpAxis(dimensions[i], axis, out bool isArrived);
                            if (validateResult != E_Result.E_SUCCESS)
                            {
                                return validateResult;
                            }

                            if (!isArrived)
                            {
                                return E_Result.E_FAILED;
                            }
                        }
                    }
                }

                if (allStopped)
                {
                    return E_Result.E_SUCCESS;
                }

                Thread.Sleep(100);
            }
            return E_Result.E_TIMEOUT;
        }

        private E_Result ValidatePtpAxis(Dimension dimension, Axis axis, out bool isArrived)
        {
            isArrived = false;
            if (!axis.IsArrive)
            {
                return E_Result.E_SUCCESS;
            }

            return TryValidatePtpCompletion(dimension, axis, out isArrived);
        }
    }
}
