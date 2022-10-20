using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using GC.Frame.Motion.Private;
using System.Diagnostics;

namespace GTSStandardizationAPI
{
    public class MotionControl
    {
        // 向外暴露的属性
        public byte[] ipv4 = new byte[4];       // 定义当前ipv4地址
        public int alarmId = 0;                 // 定义当前报错ID

        // 内部使用的全局变量
        int NUM = 0;                            // 扫描轴数量
        short On = 1, Off = 0;                  // 开关定义
        Axis[] ax = new Axis[12];               // 定义12轴配置
        MotionPara[] mp = new MotionPara[12];   // 定义12轴参数
        private UInt16 DevHandle = 0;           // 定义控制器句柄
        private int currentAxis = 0;            // 定义当前轴index
        private ushort[] axisHandle;            // 可控制单轴参数配置
        private UInt16 AxisCurrentHandle = 0;   // 定义当前轴句柄

        object obj = new object();              // 定义
        bool isConnected = false;               // 定义连接状态
        CNMCLib20.TDevResourceInfo devInformation = new CNMCLib20.TDevResourceInfo();

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
            }
        }

        /// <summary>
        /// 连接运动控制卡
        /// </summary>
        /// <returns>连接状态</returns>
        public E_Result CardConnect(byte[] ipv4)
        {
            short rtn = 0;
            ushort devNum = 0; // 当前板卡可控轴数量
            byte[] devInfo = new byte[4 * 84]; // 当前板卡相关信息-设备序号，识别字符串，描述符，板卡ID
            rtn = CNMCLib20.NMC_DevSearch(CNMCLib20.TSearchMode.Ethernet, ref devNum, devInfo);
            if (rtn == 0 && devNum > 0)
            {
                // 打开控制器, 并获取全局句柄Devhandle
                rtn = CNMCLib20.NMC_DevOpenByIP(ipv4, ref DevHandle);
                if (rtn != 0)
                {
                    rtn = CNMCLib20.NMC_DevOpen(0, ref DevHandle);
                }
                if (rtn != 0)
                {
                    return _Result(rtn == 0);
                }
            }
            // 改变连接状态
            isConnected = true;

            // 返回连接状态
            return _Result(rtn == 0);
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
                rtn = CNMCLib20.NMC_DevClose(ref DevHandle);
                isConnected = false;
                return _Result(rtn == 0);
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
            alarmId = 0;
            short rtn = 0;

            //获取控制器信息
            rtn = CNMCLib20.NMC_GetCardInfo(DevHandle, ref devInformation);

            //获取轴号  
            NUM = devInformation.axisNum;

            //获取板卡IP
            ipv4 = devInformation.ipv4;

            // 首次进行生成操作，dll不会有反馈，需要对NUM赋初值
            if (NUM == 0)
            {
                NUM = 8;
            }
            axisHandle = new ushort[NUM];

            // 开启单轴，并输出可控轴列表 257-264八轴控制。
            for (int i = 0; i < NUM; i++)
            {
                rtn = CNMCLib20.NMC_MtOpen(DevHandle, (short)i, ref axisHandle[i]);
            }


            //读取默认运动参数
            for (int i = 0; i < NUM; i++)
            {
                mp[i] = GetMotionPara(axisHandle[i]);
            }
            //获取并配置各轴参数
            for (ushort i = 0; i < NUM; i++)
            {
                ax[i] = GetAxisPara(DevHandle, axisHandle[i], mp[i].Scale, mp[i].Smoth);
            }


            // TODO 根据机械结构配置各轴参数
            mp[0].Scale = 18000;
            mp[1].Scale = 455000;
            ax[0].IsPoslmtDown = false;
            ax[0].IsPoslmtDown = false;
            ax[1].IsPoslmtDown = true;
            ax[1].IsNeglmtDown = true;

            AxisPara axisPara = new AxisPara();

            SetAxisPara(Dimension.Axis02, axisPara);
            return _Result(rtn == 0);
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
                rtn = CNMCLib20.NMC_DevReset(DevHandle);
                return _Result(rtn == 0);
            }
            else
            {
                return _Result(rtn == 0);
            }
        }

        /// <summary>
        /// 清除错误
        /// </summary>
        /// <returns>清除错误状态</returns>
        public E_Result ClearError(Dimension dimension)
        {
            short rtn = 0;
            AxisCurrentHandle = ax[(int)dimension].AxisHandle;
            rtn = CNMCLib20.NMC_MtClrError(AxisCurrentHandle);
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
            AxisCurrentHandle = ax[(int)dimension].AxisHandle;
            if (Enable)
            {
                // 开启使能
                rtn = CNMCLib20.NMC_MtSetSvOn(AxisCurrentHandle);
                ClearError(dimension);
                return _Result(rtn == 0);
            }
            else
            {
                // 关闭使能
                rtn = CNMCLib20.NMC_MtSetSvOff(AxisCurrentHandle);
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
            // 以dimension定义当前使用轴index
            currentAxis = (int)dimension;

            // 获取当前轴句柄
            AxisCurrentHandle = ax[currentAxis].AxisHandle;

            // 实例化原点复位参数
            HomePara homePara = new HomePara();

            // 实例化运动配置
            // mp[currentAxis] = GetMotionPara(AxisCurrentHandle);

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


            rtn = CNMCLib20.NMC_MtSetHomePara(ax[currentAxis].AxisHandle, ref homepara);
            if (rtn != 0)
            {
                // 回零参数错误
                alarmId = alarmId | 1024;
                return E_Result.E_FAILED;
            }
            rtn = CNMCLib20.NMC_MtHome(ax[currentAxis].AxisHandle);
            if (rtn != 0)
            {
                // 回零启动失败
                alarmId = alarmId | 1024;
                return E_Result.E_FAILED;
            }
            alarmId = alarmId & -1025;
            return E_Result.E_SUCCESS;
        }

        /// <summary>
        /// 原点复位状态
        /// </summary>
        /// <param name="dimension">单轴维度</param>
        /// <returns>0: 回零停止 1:回零中，2：回零成功，4：回零失败，8：回零参数，16 开关失效</returns>
        public short MotorHomeStatus(Dimension dimension)
        {
            short homests = 0;
            currentAxis = (int)dimension;
            ushort homedle = ax[currentAxis].AxisHandle;

            // 获取原点复位状态
            CNMCLib20.NMC_MtGetHomeSts(homedle, ref homests);
            return homests;
        }

        /// <summary>
        /// 停止原点复位
        /// </summary>
        /// <param name="dimension">维度</param>
        /// <returns></returns>
        public E_Result MotorHomeStop(Dimension dimension)
        {
            short rtn = 0;
            currentAxis = (int)dimension;
            ushort homedle = ax[currentAxis].AxisHandle;

            // 停止原点复位
            rtn = CNMCLib20.NMC_MtHomeStop(homedle);
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
            currentAxis = (int)dimension;
            // 将当前位置进行清除
            rtn = CNMCLib20.NMC_MtZeroPos(ax[currentAxis].AxisHandle);
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
            currentAxis = (int)dimension;

            // 实例化运动配置
            // mp[currentAxis] = GetMotionPara(ax[currentAxis].AxisHandle);

            // 设置运动参数
            mp[currentAxis].Mode = 0;
            mp[currentAxis].Vel = speed;
            mp[currentAxis].Direction = direction;

            // 判断方向
            int dirIndex = mp[currentAxis].Direction == true ? -1 : 1;

            short rtn = 0;
            CNMCLib20.TJogPara jogPara;
            if (mp[currentAxis].Mode == 0)
            {
                //jog
                rtn = CNMCLib20.NMC_MtSetPrfMode(ax[currentAxis].AxisHandle, CNMCLib20.MT_JOG_PRF_MODE);
                if (rtn != 0)
                {
                    // JOG运动模式设置失败
                    return E_Result.E_FAILED;
                }
                jogPara.acc = mp[currentAxis].Acc * mp[currentAxis].Scale / 1000000;
                jogPara.dec = mp[currentAxis].Dec * mp[currentAxis].Scale / 1000000;
                jogPara.smoothCoef = ax[currentAxis].Smooth;
                rtn = CNMCLib20.NMC_MtSetJogPara(ax[currentAxis].AxisHandle, ref jogPara);
                if (rtn != 0)
                {
                    // JOG运动参数设置失败
                    return E_Result.E_FAILED;
                }
                rtn = CNMCLib20.NMC_MtSetVel(ax[currentAxis].AxisHandle, dirIndex * mp[currentAxis].Vel * mp[currentAxis].Scale / 1000);

                if (rtn != 0)
                {
                    // JOG运动的最高速度设置失败
                    return E_Result.E_FAILED;
                }
                rtn = CNMCLib20.NMC_MtUpdate(ax[currentAxis].AxisHandle);
                if (rtn != 0)
                {
                    // JOG启动失败
                    return E_Result.E_FAILED;
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
            currentAxis = (int)dimension;
            rtn = CNMCLib20.NMC_MtStop(ax[currentAxis].AxisHandle);
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

            currentAxis = (int)dimension;

            // 相对运动
            // 获取当前轴配置
            // mp[currentAxis] = GetMotionPara(ax[currentAxis].AxisHandle);

            //设置运动参数
            mp[currentAxis].Mode = 1;
            mp[currentAxis].Vel = speed;
            mp[currentAxis].Pos = position;

            // 获取当前轴详细参数
            ax[currentAxis] = GetAxisPara(DevHandle, axisHandle[currentAxis], mp[currentAxis].Scale, mp[currentAxis].Smoth);
            // 获取当前位置
            double newpos = ax[currentAxis].CurrentPos0 * mp[currentAxis].Scale;


            // step1--设置模式
            rtn = CNMCLib20.NMC_MtSetPrfMode(ax[currentAxis].AxisHandle, CNMCLib20.MT_PTP_PRF_MODE);
            if (rtn != 0)
            {
                alarmId = alarmId | 4096;
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
            rtn = CNMCLib20.NMC_MtSetPtpPara(ax[currentAxis].AxisHandle, ref ptpPara);
            if (rtn != 0)
            {
                // PTOP运动参数设置失败;
                alarmId = alarmId | 4096;
                return E_Result.E_FAILED;
            }
            //step3--设置速度
            rtn = CNMCLib20.NMC_MtSetVel(ax[currentAxis].AxisHandle, mp[currentAxis].Vel * mp[currentAxis].Scale / 1000);
            if (rtn != 0)
            {
                // PTOP运动的最高速度设置失败;
                alarmId = alarmId | 4096;
                return E_Result.E_FAILED;
            }
            //step4--设置目标位置
            rtn = CNMCLib20.NMC_MtSetPtpTgtPos(ax[currentAxis].AxisHandle, (int)(mp[currentAxis].Pos * mp[currentAxis].Scale + newpos));

            if (rtn != 0)
            {
                // PTOP目标位置设置失败;
                alarmId = alarmId | 4096;
                return E_Result.E_FAILED;
            }
            //step5--启动运动
            rtn = CNMCLib20.NMC_MtUpdate(ax[currentAxis].AxisHandle);
            if (rtn != 0)
            {
                // PTOP启动失败;
                alarmId = alarmId | 4096;
                return E_Result.E_FAILED;
            }
            alarmId = alarmId & -4097;
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
            currentAxis = (int)dimension;

            // 获取当前轴配置
            // mp[currentAxis] = GetMotionPara(ax[currentAxis].AxisHandle);

            //设置运动参数
            mp[currentAxis].Mode = 2;
            mp[currentAxis].Vel = speed;
            mp[currentAxis].Pos = position;


            //绝对运动
            //step1--设置模式
            rtn = CNMCLib20.NMC_MtSetPrfMode(ax[currentAxis].AxisHandle, CNMCLib20.MT_PTP_PRF_MODE);
            if (rtn != 0)
            {
                // PTOP运动模式设置失败;
                alarmId = alarmId | 2048;
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
            rtn = CNMCLib20.NMC_MtSetPtpPara(ax[currentAxis].AxisHandle, ref ptpPara);
            if (rtn != 0)
            {
                // PTOP运动参数设置失败;
                alarmId = alarmId | 2048;
                return E_Result.E_FAILED;
            }
            //step3--设置速度
            rtn = CNMCLib20.NMC_MtSetVel(ax[currentAxis].AxisHandle, mp[currentAxis].Vel * mp[currentAxis].Scale / 1000);
            if (rtn != 0)
            {
                // PTOP运动的最高速度设置失败;
                alarmId = alarmId | 2048;
                return E_Result.E_FAILED;
            }
            //step4--设置目标位置
            rtn = CNMCLib20.NMC_MtSetPtpTgtPos(ax[currentAxis].AxisHandle, (int)(mp[currentAxis].Pos * mp[currentAxis].Scale));
            if (rtn != 0)
            {
                // PTOP目标位置设置失败;
                alarmId = alarmId | 2048;
                return E_Result.E_FAILED;
            }
            //step5--启动运动
            rtn = CNMCLib20.NMC_MtUpdate(ax[currentAxis].AxisHandle);
            if (rtn != 0)
            {
                // PTOP启动失败
                alarmId = alarmId | 2048;
                return E_Result.E_FAILED;
            }
            alarmId = alarmId & -2049;
            return E_Result.E_SUCCESS;
        }

        /// <summary>
        /// 高速二维位置比较参数设置
        /// </summary>
        /// <param name="dimension">维度</param>
        /// <param name="pulseWidth">脉冲宽度</param>
        /// <returns></returns>
        public E_Result MotorCompareHS2Para(Dimension dimension, int pulseWidth)
        {
            short rtn = 0;
            short group = 0;
            short currentAxis = (short)dimension;

            // 停止高速位置比较,以防止上次异常，没有关闭
            MotorCompareHS2Stop();

            // 实例化比较参数
            ComparaParaHS2 comparaParaHS2 = new ComparaParaHS2();

            // 定义比较参数
            CNMCLib20.TComp2DimensParamEx comp2DimensParamEx = new CNMCLib20.TComp2DimensParamEx();
            comp2DimensParamEx.dir1No = currentAxis;
            comp2DimensParamEx.dir2No = comparaParaHS2.Dir2No;
            comp2DimensParamEx.outputChn = comparaParaHS2.OutputChn;
            comp2DimensParamEx.outputType = comparaParaHS2.OutputType;
            comp2DimensParamEx.chnType = comparaParaHS2.ChnType;
            comp2DimensParamEx.posSrc = comparaParaHS2.PosSrc;
            comp2DimensParamEx.stLevel = comparaParaHS2.StLevel;
            comp2DimensParamEx.errZone = comparaParaHS2.ErrZone;
            comp2DimensParamEx.directOutZone = comparaParaHS2.DirectOutZone;
            comp2DimensParamEx.vibrateRange = comparaParaHS2.VibrateRange; ;
            comp2DimensParamEx.gateTime = pulseWidth;
            comp2DimensParamEx.minIntervalTime = comparaParaHS2.MinIntervalTime;



            rtn = CNMCLib20.NMC_Comp2DimensSetParamEx(DevHandle, group, ref comp2DimensParamEx, 0);

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
            currentAxis = (int)dimension;
            int[] posArray = new int[posRangleArray.Length];

            for (int i = 0; i < posRangleArray.Length; i++)
            {
                posArray[i] = (int)(posRangleArray[i] * mp[currentAxis].Scale);
            }

            short rtn = 0;
            short grounp = 0;
            rtn = CNMCLib20.NMC_Comp2DimensSetData(DevHandle, grounp, posArray, (short)(posArray.Length / 2), 0);
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
        public E_Result MotorCompareHS2Status(out CompareStatus compareStatus)
        {
            short rtn = 0;
            short grounp = 0;
            compareStatus = new CompareStatus();
            CNMCLib20.TComp2DimensSts comp2DimensSts = new CNMCLib20.TComp2DimensSts();
            rtn = CNMCLib20.NMC_Comp2DimensStatusEx(DevHandle, grounp, ref comp2DimensSts, 0);

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

            rtn = CNMCLib20.NMC_Comp2DimensOnoff(DevHandle, group, On, 0);
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
            rtn = CNMCLib20.NMC_Comp2DimensOnoff(DevHandle, grounp, Off, 0);
            CNMCLib20.NMC_Comp2DimensSetData(DevHandle, grounp, posArray, (short)(posArray.Length / 2), 0);
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
            rtn = CNMCLib20.NMC_SetDOBit(DevHandle, index, (short)level);
            return _Result(rtn == 0);
        }

        /// <summary>
        /// 返回当前轴状态
        /// </summary>
        /// <param name="dimension">维度</param>
        /// <returns></returns>
        public Axis MotorGetStatus(Dimension dimension)
        {
            short rtn = 0;
            currentAxis = (int)dimension;
            Axis axis = GetAxisPara(DevHandle, ax[currentAxis].AxisHandle, mp[currentAxis].Scale, mp[currentAxis].Smoth);
            int motionIOstatus = 256;
            rtn = CNMCLib20.NMC_MtGetMotionIO(ax[currentAxis].AxisHandle, ref motionIOstatus);
            if (rtn == 0)
            {
                axis.IsConnected = true;
            }
            string motionIObyte = Convert.ToString(motionIOstatus, 2).PadLeft(4, '0').Substring(5);
            if (motionIObyte[0] == '1')
            {
                axis.IsAlarming = true;
                alarmId = 32768;
            }
            else
            {
                axis.IsAlarming = false;
                alarmId = 0;
            }
            if (motionIObyte[2] == '1')
            {
                axis.PosArrived = true;
            }
            else
            {
                axis.PosArrived = false;
            }
            if (motionIObyte[3] == '1')
            {
                axis.NegArrived = true;
            }
            else
            {
                axis.NegArrived = false;
            }
            return axis;
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
            lock (obj)
            {
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
                temp.IsNegLmtActived = (negswt == 1) ? true : false;
                temp.IsPosLmtActived = (posswt == 1) ? true : false;
                //获取限位触发电平配置          
                rtn = CNMCLib20.NMC_MtGetLmtSns(axisHandle, ref posswt, ref negswt);
                temp.IsNeglmtDown = (negswt == 0) ? true : false;
                temp.IsPoslmtDown = (posswt == 0) ? true : false;
                //获取驱动器报警配置
                rtn = CNMCLib20.NMC_MtGetAlarmOnOff(axisHandle, ref swt);
                temp.IsAlarmActived = (swt == 1) ? true : false;
                //获取驱动器报警的电平配置
                rtn = CNMCLib20.NMC_MtGetAlarmSns(axisHandle, ref swt);
                temp.IsAlarmDown = (swt == 0) ? true : false;
                //获取脉冲模式
                rtn = CNMCLib20.NMC_MtGetStepMode(axisHandle, ref swt, ref swt1);
                temp.IsEncNeged = (swt == 1) ? true : false;
                temp.StepMode = swt1;
                //获取轴状态
                rtn = CNMCLib20.NMC_MtGetSts(axisHandle, ref axsists);
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
                temp.CurrentPos0 = postemp / scaletemp;
                //获取轴当前编码器位置,脉冲转化为mm
                rtn = CNMCLib20.NMC_MtGetAxisPos(axisHandle, ref postemp);
                temp.CurrentPos1 = postemp / scaletemp;
                //获取当前轴速度,mm/s
                rtn = CNMCLib20.NMC_MtGetPrfVel(axisHandle, ref nowvel);
                temp.CurrentVel = nowvel * 1000 / scaletemp;
                //获取是否为低电平触发硬限位停止的配置   
                //读取触发是否停止配置
                rtn = CNMCLib20.NMC_MtGetLmtOnOff(axisHandle, ref posi, ref neg);
                //读取限位触发电平配置
                rtn = CNMCLib20.NMC_MtGetLmtSns(axisHandle, ref posii, ref negg);
                temp.IsPoslmtDown = (posi == 1 && posii == 0) == true ? true : false;
                temp.IsNeglmtDown = (neg == 1 && negg == 0) == true ? true : false;
                //获取软限位配置
                //是否激活软限位
                rtn = CNMCLib20.NMC_MtGetSwLmtOnOff(axisHandle, ref swt);
                temp.IsSoftLmtActived = (swt == 1) ? true : false;
                rtn = CNMCLib20.NMC_MtGetSwLmtValue(axisHandle, ref posiii, ref negggg);
                temp.SafePoslmtPos = posiii / scaletemp;
                temp.SafelNeglmtPos = negggg / scaletemp;
                //读取轴运动安全参数
                rtn = CNMCLib20.NMC_MtGetSafePara(axisHandle, ref safePara);
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
                temp.MaxPosErr = poserr;
                //获取编码器计数方式，外部还是内部，以及计数方向
                rtn = CNMCLib20.NMC_GetEncMode(devhandle, (short)axisHandle, ref encmode);
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
                temp.HomeMaxPos = homepara.safeLen / scaletemp;
                temp.HomeAcc = homepara.acc * 1000000 / scaletemp;
                temp.SearchHomeVel = homepara.scan1stVel * 1000 / scaletemp;
                temp.HomeOffset = homepara.offset / scaletemp;
                temp.HomeBackVel = homepara.scan2ndVel * 1000 / scaletemp;//低速回零速度
                temp.IsNegHome = (homepara.dir == 0) ? true : false;
                temp.HomeMode = homepara.mode;
                temp.IsHomeTwice = homepara.reScanEn == '1' ? true : false;
                temp.IsZUp = homepara.zEdge == '1' ? true : false;
                temp.IsLmtUp = homepara.lmtEdge == '1' ? true : false;
                temp.IsHomeUp = homepara.homeEdge == '1' ? true : false;
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
            AxisCurrentHandle = ax[(int)dimension].AxisHandle;
            int scale = (int)mp[currentAxis].Scale;
            CNMCLib20.TSafePara safePara;
            short rtn = 0;
            //正负限位激活
            rtn = CNMCLib20.NMC_MtLmtOnOff(AxisCurrentHandle, axisPara.Poslmt, axisPara.Neglmt);
            if (rtn != 0) return rtn;
            //正负限位电平配置
            rtn = CNMCLib20.NMC_MtLmtSns(AxisCurrentHandle, axisPara.Poslmtlev, axisPara.Neglmtlev);
            if (rtn != 0) return rtn;
            //报警激活
            rtn = CNMCLib20.NMC_MtAlarmOnOff(AxisCurrentHandle, axisPara.AlarmEnable);
            if (rtn != 0) return rtn;
            //报警电平配置
            rtn = CNMCLib20.NMC_MtAlarmSns(AxisCurrentHandle, axisPara.AlarmLevel);
            if (rtn != 0) return rtn;
            //指令脉冲取反激活,脉冲模式设置
            rtn = CNMCLib20.NMC_MtSetStepMode(AxisCurrentHandle, axisPara.StepInv, axisPara.StepMode);
            if (rtn != 0) return rtn;
            //软限位是否激活
            rtn = CNMCLib20.NMC_MtSwLmtOnOff(AxisCurrentHandle, axisPara.Softlmt);
            if (rtn != 0) return rtn;
            //软件限位设置
            rtn = CNMCLib20.NMC_MtSwLmtValue(AxisCurrentHandle, (int)(axisPara.Softlmtpos * scale), (int)(axisPara.Softlmtneg * scale));
            if (rtn != 0) return rtn;

            //运动安全参数设置
            safePara.estpDec = axisPara.EstpDec * scale / 1000000;
            safePara.maxAcc = axisPara.MaxAcc * scale / 1000000;
            safePara.maxVel = axisPara.MaxVel * scale / 1000;
            rtn = CNMCLib20.NMC_MtSetSafePara(AxisCurrentHandle, ref safePara);
            if (rtn != 0) return rtn;

            //最大位置误差设定,单位脉冲
            rtn = CNMCLib20.NMC_MtSetPosErrLmt(AxisCurrentHandle, axisPara.PosErr);
            if (rtn != 0) return rtn;

            //平滑系数保存
            if (rtn != 0) return rtn;
            ax[currentAxis].Smooth = Convert.ToDouble(axisPara.Smooth);

            //轴当量保存 
            ax[currentAxis].Scale = axisPara.Scale;

            //编码器模式设置
            rtn = CNMCLib20.NMC_SetEncMode(DevHandle, (short)AxisCurrentHandle, axisPara.Encoder);
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

    }
}
