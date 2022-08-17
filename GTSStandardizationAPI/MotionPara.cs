using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GTSStandardizationAPI
{
    /// <summary>
    /// 控制模式
    /// </summary>
    public enum ENCMODE : int
    {
        OUTAB, //外部，AB相90度差
        OUTPD, //外部，脉冲加方向
        OUTPG, //外部，正负脉冲
        OUTABFU,//外部，AB相90度差（负逻辑）
        OUTPDFU,//外部，脉冲加方向（负逻辑）
        OUTPGFU,//外部，正负脉冲（负逻辑）
        INAB,//内部，AB相90度差
        INPD,//内部，脉冲加方向
        INPG,//内部，正负脉冲
        INABFU,//内部，AB相90度差（负逻辑）
        INPDFU,//内部，脉冲加方向（负逻辑）
        INPGFU,//内部，正负脉冲（负逻辑）
    }
    /// <summary>
    /// 记录轴配置的类
    /// </summary>
    public class Axis
    {
        private UInt16 axisHandle;
        private bool isPosLmtActived = false;
        private bool isNegLmtActived = false;
        private bool isAlarmActived = false;
        private int stepMode;
        private bool isAxisOn;
        private bool isAlarming;
        private bool isAlarmDown = true;
        private bool isRunning;
        private bool isPosErr;
        private bool negArrived;
        private bool posSoftArrived;
        private bool negSoftArrived;
        private bool posArrived;
        private bool isArrive;
        private bool isError;
        private double currentPos0;
        private double currentPos1;
        private double currentVel;
        private bool isNeglmtDown = false;
        private bool isPoslmtDown = true;
        private bool isSoftLmtActived = false;
        private double safePoslmtPos;
        private double safelNeglmtPos;
        private double smooth;
        private double stopDec;
        private double maxaAcc;
        private double maxVel;
        private int maxPosErr;
        private bool isEncNeged;
        private ENCMODE encMode;
        private double homeMaxPos;
        private double searchHomeVel;
        private double homeBackVel;
        private double homeOffset;
        private double homeAcc;
        private bool isNegHome = true;
        private bool isHomeTwice = false;
        private bool isHomeZero = true;
        private bool isHomeUp = false;
        private bool isLmtUp = false;
        private bool isZUp = false;
        private double scale;
        private short homeMode;
        private double homeOffsetBegin;
        private double homeOffsetLmt;
        private bool isConnected = false;
        /// <summary>
        /// 轴句柄
        /// </summary>
        public UInt16 AxisHandle { get => axisHandle; set => axisHandle = value; }
        /// <summary>
        /// 正限位是否激活
        /// </summary>
        public bool IsPosLmtActived { get => isPosLmtActived; set => isPosLmtActived = value; }
        /// <summary>
        /// 负限位是否激活
        /// </summary>
        public bool IsNegLmtActived { get => isNegLmtActived; set => isNegLmtActived = value; }
        /// <summary>
        /// 驱动器报警是否激活
        /// </summary>
        public bool IsAlarmActived { get => isAlarmActived; set => isAlarmActived = value; }
        /// <summary>
        /// 脉冲模式，0，脉冲加方向，1，正负脉冲
        /// </summary>
        public int StepMode { get => stepMode; set => stepMode = value; }
        /// <summary>
        /// 驱动器是否使能
        /// </summary>
        public bool IsAxisOn { get => isAxisOn; set => isAxisOn = value; }
        /// <summary>
        /// 是否正在报警
        /// </summary>
        public bool IsAlarming { get => isAlarming; set => isAlarming = value; }

        /// <summary>
        /// 是否正在运动
        /// </summary>
        public bool IsRunning { get => isRunning; set => isRunning = value; }
        /// <summary>
        /// 是否位置越限
        /// </summary>
        public bool IsPosErr { get => isPosErr; set => isPosErr = value; }
        /// <summary>
        /// 负限位是否触发
        /// </summary>
        public bool NegArrived { get => negArrived; set => negArrived = value; }
        /// <summary>
        /// 正限位是否触发
        /// </summary>
        public bool PosArrived { get => posArrived; set => posArrived = value; }

        /// <summary>
        /// 是否运动到位
        /// </summary>
        public bool IsArrive { get => isArrive; set => isArrive = value; }
        /// <summary>
        /// 是否运动报错
        /// </summary>
        public bool IsError { get => isError; set => isError = value; }
        /// <summary>
        /// 当前计数器位置
        /// </summary>
        public double CurrentPos0 { get => currentPos0; set => currentPos0 = value; }
        /// <summary>
        /// 当前编码器位置
        /// </summary>
        public double CurrentPos1 { get => currentPos1; set => currentPos1 = value; }
        /// <summary>
        /// 当前速度
        /// </summary>
        public double CurrentVel { get => currentVel; set => currentVel = value; }
        /// <summary>
        /// 正限位是否为低电平触发
        /// </summary>
        public bool IsPoslmtDown { get => isPoslmtDown; set => isPoslmtDown = value; }
        /// <summary>
        /// 负限位是否为低电平触发
        /// </summary>
        public bool IsNeglmtDown { get => isNeglmtDown; set => isNeglmtDown = value; }
        /// <summary>
        /// 软限位是否激活
        /// </summary>
        public bool IsSoftLmtActived { get => isSoftLmtActived; set => isSoftLmtActived = value; }
        /// <summary>
        /// 软限位安全位置正
        /// </summary>
        public double SafePoslmtPos { get => safePoslmtPos; set => safePoslmtPos = value; }
        /// <summary>
        /// 软限位安全位置负
        /// </summary>
        public double SafelNeglmtPos { get => safelNeglmtPos; set => safelNeglmtPos = value; }
        /// <summary>
        /// 平滑系数
        /// </summary>
        public double Smooth { get => smooth; set => smooth = value; }
        /// <summary>
        /// 急停减速度
        /// </summary>
        public double StopDec { get => stopDec; set => stopDec = value; }
        /// <summary>
        /// 最大加速度
        /// </summary>
        public double MaxaAcc { get => maxaAcc; set => maxaAcc = value; }
        /// <summary>
        /// 最大速度
        /// </summary>
        public double MaxVel { get => maxVel; set => maxVel = value; }
        /// <summary>
        /// 允许的最大位置误差
        /// </summary>
        public int MaxPosErr { get => maxPosErr; set => maxPosErr = value; }
        /// <summary>
        /// 编码器计数模式
        /// </summary>
        public ENCMODE EncMode { get => encMode; set => encMode = value; }
        /// <summary>
        /// 指令脉冲是否取反
        /// </summary>
        public bool IsEncNeged { get => isEncNeged; set => isEncNeged = value; }
        /// <summary>
        /// 原点最大搜索距离
        /// </summary>
        public double HomeMaxPos { get => homeMaxPos; set => homeMaxPos = value; }
        /// <summary>
        /// 搜索速度
        /// </summary>
        public double SearchHomeVel { get => searchHomeVel; set => searchHomeVel = value; }
        /// <summary>
        /// 原点返回速度
        /// </summary>
        public double HomeBackVel { get => homeBackVel; set => homeBackVel = value; }
        /// <summary>
        /// 回零偏移量
        /// </summary>
        public double HomeOffset { get => homeOffset; set => homeOffset = value; }
        /// <summary>
        /// 回零加速度
        /// </summary>
        public double HomeAcc { get => homeAcc; set => homeAcc = value; }
        /// <summary>
        /// 是否负向回零
        /// </summary>
        public bool IsNegHome { get => isNegHome; set => isNegHome = value; }
        /// <summary>
        /// 是否二次回零
        /// </summary>
        public bool IsHomeTwice { get => isHomeTwice; set => isHomeTwice = value; }
        /// <summary>
        /// 是否位置需要清零
        /// </summary>
        public bool IsHomeZero { get => isHomeZero; set => isHomeZero = value; }
        /// <summary>
        /// 原点信号是否上升沿触发
        /// </summary>
        public bool IsHomeUp { get => isHomeUp; set => isHomeUp = value; }
        /// <summary>
        /// 限位信号是否上升沿触发
        /// </summary>
        public bool IsLmtUp { get => isLmtUp; set => isLmtUp = value; }
        /// <summary>
        /// Z向信号是否上升沿触发
        /// </summary>
        public bool IsZUp { get => isZUp; set => isZUp = value; }
        /// <summary>
        /// 是否是低电平触发驱动器报警
        /// </summary>
        public bool IsAlarmDown { get => isAlarmDown; set => isAlarmDown = value; }
        /// <summary>
        /// 负向软限位是否触发
        /// </summary>
        public bool NegSoftArrived { get => negSoftArrived; set => negSoftArrived = value; }
        /// <summary>
        /// 正向软限位是否触发
        /// </summary>
        public bool PosSoftArrived { get => posSoftArrived; set => posSoftArrived = value; }
        /// <summary>
        /// 轴当量
        /// </summary>
        public double Scale { get => scale; set => scale = value; }
        /// <summary>
        /// 回零模式选择
        /// </summary>
        public short HomeMode { get => homeMode; set => homeMode = value; }
        /// <summary>
        /// 起始反向距离
        /// </summary>
        public double HomeOffsetBegin { get => homeOffsetBegin; set => homeOffsetBegin = value; }
        /// <summary>
        /// 离开开关距离
        /// </summary>
        public double HomeOffsetLmt { get => homeOffsetLmt; set => homeOffsetLmt = value; }

        /// <summary>
        /// 是否连接查询
        /// </summary>
        public bool IsConnected { get => isConnected; set => isConnected = value; }
    }

    /// <summary>
    /// 运动参数类
    /// </summary>
    public class MotionPara
    {
        private ushort axisNumber = 0;
        private double pos = 36;
        private double vel = 10;
        private double acc = 100;
        private double dec = 100;
        private double jumpVel = 0.0;
        private double endVel = 0.0;
        private int mode = 0;
        private double scale = 18000;
        private int zeroPos = 1;
        private double smoth = 100;
        private bool direction = true;

        public ushort AxisNumber { get => axisNumber; set => axisNumber = value; }
        public double Pos { get => pos; set => pos = value; }
        public double Vel { get => vel; set => vel = value; }
        public double Acc { get => acc; set => acc = value; }
        public double Dec { get => dec; set => dec = value; }
        public double JumpVel { get => jumpVel; set => jumpVel = value; }
        public double EndVel { get => endVel; set => endVel = value; }
        public int Mode { get => mode; set => mode = value; }
        public double Scale { get => scale; set => scale = value; }
        public int ZeroPos { get => zeroPos; set => zeroPos = value; }
        public double Smoth { get => smoth; set => smoth = value; }
        public bool Direction { get => direction; set => direction = value; }
    }
    /// <summary>
    /// 多轴参数类
    /// </summary>
    public class AxisPara
    {
        // 限位激活与电平 1:有效, 0:无效; 1:高电平, 0:低电平。
        private short poslmt = 1;
        private short neglmt = 1;
        private short alarmEnable = 0;

        private short poslmtlev = 1;
        private short neglmtlev = 1;
        private short alarmLevel = 1;

        // 脉冲输出模式
        private short stepInv = 0;
        private short stepMode = 0;

        // 软限位激活与设置
        private short softlmt = 1;
        private double softlmtpos = 32768;
        private double softlmtneg = -32768;

        // 安全数值设置
        private double estpDec = 36;    // 急停减速度
        private double maxVel = 72;     // 最大速度
        private double maxAcc = 36;     // 最大加速度

        // 其他参数
        private Int32 posErr = 0;       // 最大误差设定
        private double smooth = 100;    // 平滑系数
        private double scale = 18000;    // 轴当量，减速比单位转换使用

        private double zeroPos = 0;     // 是否清零

        private short encoder = 256;      // 编码器模式 内部计数：0x0100 外部计数：0x0000 外部取反：0x1000

        public short Poslmt { get => poslmt; set => poslmt = value; }
        public short Neglmt { get => neglmt; set => neglmt = value; }
        public short AlarmEnable { get => alarmEnable; set => alarmEnable = value; }
        public short Poslmtlev { get => poslmtlev; set => poslmtlev = value; }
        public short Neglmtlev { get => neglmtlev; set => neglmtlev = value; }
        public short AlarmLevel { get => alarmLevel; set => alarmLevel = value; }
        public short StepInv { get => stepInv; set => stepInv = value; }
        public short StepMode { get => stepMode; set => stepMode = value; }
        public short Softlmt { get => softlmt; set => softlmt = value; }
        public double Softlmtpos { get => softlmtpos; set => softlmtpos = value; }
        public double Softlmtneg { get => softlmtneg; set => softlmtneg = value; }
        public double EstpDec { get => estpDec; set => estpDec = value; }
        public double MaxVel { get => maxVel; set => maxVel = value; }
        public double MaxAcc { get => maxAcc; set => maxAcc = value; }
        public int PosErr { get => posErr; set => posErr = value; }
        public double Smooth { get => smooth; set => smooth = value; }
        public double Scale { get => scale; set => scale = value; }
        public double ZeroPos { get => zeroPos; set => zeroPos = value; }
        public short Encoder { get => encoder; set => encoder = value; }

    }

    /// <summary>
    /// 原点复位参数类
    /// </summary>
    public class HomePara
    {
        private short homeMode = 3;             // 原点复位模式
        private bool isNegHome = true;         // 是否反向回零
        private double homeMaxPos = 360;        // 复位最大距离
        private double serchHomeVel = 10;       // 搜索速度
        private double homeAcc = 1000;          // 搜索加速度
        private double homeBackVel = 1;         // 原点返回速度
        private double homeOffset = 0;          // 原点偏移
        private double homeOffsetBegin = 0;     // 起始反向距离
        private double homeOffsetLmt = 0;       // 反向运动离开开关距离
        private bool isHomeTwice = false;       // 是否二次回零
        private bool isZUp = true;              // Z上升沿触发
        private bool isHomeUp = true;          // 原点输入上升沿触发
        private bool isLmtUp = true;            // 限位上升沿触发
        private bool isHomeZero = false;        // 原点清零

        public short HomeMode { get => homeMode; set => homeMode = value; }
        public bool IsNegHome { get => isNegHome; set => isNegHome = value; }
        public double HomeMaxPos { get => homeMaxPos; set => homeMaxPos = value; }
        public double SerchHomeVel { get => serchHomeVel; set => serchHomeVel = value; }
        public double HomeAcc { get => homeAcc; set => homeAcc = value; }
        public double HomeBackVel { get => homeBackVel; set => homeBackVel = value; }
        public double HomeOffset { get => homeOffset; set => homeOffset = value; }
        public double HomeOffsetBegin { get => homeOffsetBegin; set => homeOffsetBegin = value; }
        public double HomeOffsetLmt { get => homeOffsetLmt; set => homeOffsetLmt = value; }
        public bool IsHomeTwice { get => isHomeTwice; set => isHomeTwice = value; }
        public bool IsZUp { get => isZUp; set => isZUp = value; }
        public bool IsHomeUp { get => isHomeUp; set => isHomeUp = value; }
        public bool IsLmtUp { get => isLmtUp; set => isLmtUp = value; }
        public bool IsHomeZero { get => isHomeZero; set => isHomeZero = value; }
    }

    /// <summary>
    /// 高速触发参数类
    /// </summary>
    public class ComparaParaHS2
    {
        private short outputChn = 0;
        private short outputType = 0;
        private short chnType = 1;
        private short dir1No = 0;
        private short dir2No = -1;
        private short posSrc = 1;
        private short stLevel = 0;
        private short errZone = 1;
        private short directOutZone = 10;
        private short vibrateRange = 10;
        private int gateTime = 10;
        private int minIntervalTime = 0;
        private int out2Delay = 0;

        public short OutputChn { get => outputChn; set => outputChn = value; }
        public short OutputType { get => outputType; set => outputType = value; }
        public short ChnType { get => chnType; set => chnType = value; }
        public short Dir1No { get => dir1No; set => dir1No = value; }
        public short Dir2No { get => dir2No; set => dir2No = value; }
        public short PosSrc { get => posSrc; set => posSrc = value; }
        public short StLevel { get => stLevel; set => stLevel = value; }
        public short ErrZone { get => errZone; set => errZone = value; }
        public short DirectOutZone { get => directOutZone; set => directOutZone = value; }
        public short VibrateRange { get => vibrateRange; set => vibrateRange = value; }
        public int GateTime { get => gateTime; set => gateTime = value; }
        public int MinIntervalTime { get => minIntervalTime; set => minIntervalTime = value; }
        public int Out2Delay { get => out2Delay; set => out2Delay = value; }
    }

    /// <summary>
    /// 高速触发状态类
    /// </summary>
    public class CompareStatus
    {
        public short sts;              // 运行状态，0 空闲 1 忙
        public int freeSpace;         // 控制器剩余空间
        public int usedSpace;         // 剩余位置比较点
        public int outCount;          // 已经输出的个数
    }
}
