using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using GC.Frame.Motion.Private;

namespace GTSStandardizationAPI
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
        public E_Result Connect();
        // 转动断开
        public E_Result Disconnect();
        // 查询转动状态
        public E_Result GetStatus(Dimension dimension, out E_Turntable_Status status, out int alarmId);
        // 转动初始化
        public E_Result Init(Dimension dimension, bool servo, UInt32 ServoResetTimeDelay);
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
    }
    public class AutoGCApi : GTSApi
    {

        MotionControl motionControl = new MotionControl();
        public byte[] ipv4 = new byte[4];
        /// <summary>
        /// 连接高川运动控制卡
        /// </summary>
        /// <param name="ipAddress"></param>
        /// <param name="port"></param>
        /// <returns></returns>
        public E_Result Connect()
        {
            return motionControl.CardConnect();
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
        /// 使能并且，初始化--------------TODO：加入外部初始化参数，每个轴单独加入
        /// </summary>
        /// <returns></returns>
        public E_Result Init(Dimension dimension, bool servo, UInt32 ServoResetTimeDelay)
        {
            motionControl.CardInitial();
            return motionControl.ServoEnable(dimension, true);
        }

        /// <summary>
        /// 获取当前状态
        /// </summary>
        /// <param name="dimension"></param>
        /// <param name="status"></param>
        /// <param name="alarmId">[0000 0000 0000 0000 ] [驱动器报警，限位告警,超时告警，相对定位告警，绝对定位告警，原点回归告警，复位告警，使能告警, 未连接报警]</param>
        /// <returns></returns>
        public E_Result GetStatus(Dimension dimension, out E_Turntable_Status status, out int alarmId)
        {
            alarmId = motionControl.alarmId;
            status = E_Turntable_Status.alarm;
            Axis axis = motionControl.MotorGetStatus(dimension);
            if (axis.IsRunning == true)
            {
                status = E_Turntable_Status.moving;
            }
            if (axis.IsArrive == true)
            {
                status = E_Turntable_Status.ready;
            }
            if (axis.IsAlarming == true)
            {
                status = E_Turntable_Status.alarm;
            }

            // 连接状态判断
            if (axis.IsConnected == true)
            {
                alarmId = alarmId & -129; // 将连接状态置位
            }
            else
            {
                alarmId = alarmId | 128;
            }

            // 判断限位状态
            if (axis.NegArrived == true || axis.PosArrived == true)
            {
                alarmId = alarmId | 16384;
            }
            else
            {
                alarmId = alarmId & -16385;
            }
            // 获取IPV4地址
            ipv4 = motionControl.ipv4;
            return E_Result.E_SUCCESS;
        }

        /// <summary>
        /// 获取当前角度值，需要脉冲转角度
        /// </summary>
        /// <param name="dimension"></param>
        /// <param name="position"></param>
        /// <returns></returns>
        public E_Result GetPosition(Dimension dimension, out double position)
        {
            Axis axis = motionControl.MotorGetStatus(dimension);
            position = axis.CurrentPos1;
            return E_Result.E_SUCCESS;
        }

        /// <summary>
        /// 获取当前角速度，脉冲频率转角速度
        /// </summary>
        /// <param name="dimension"></param>
        /// <param name="speed"></param>
        /// <returns></returns>
        public E_Result GetSpeed(Dimension dimension, out double speed)
        {
            Axis axis = motionControl.MotorGetStatus(dimension);
            speed = axis.CurrentVel;
            return E_Result.E_SUCCESS;
        }

        /// <summary>
        /// 原点复位，带系统偏置
        /// </summary>
        /// <param name="dimension"></param>
        /// <param name="speed"></param>
        /// <param name="timeout"></param>
        /// <returns></returns>
        public E_Result Home(Dimension dimension, double speed, double offset, int timeout = -1)
        {

            E_Result e_Result = motionControl.MotorHome(dimension, speed, offset);
            BlockingQuery(dimension, timeout);
            return e_Result;
        }

        /// <summary>
        /// 驱动相对移动
        /// </summary>
        /// <param name="dimension">轴号</param>
        /// <param name="speed">速度</param>
        /// <param name="position">位置</param>
        /// <param name="timeout"></param>
        /// <returns></returns>
        public E_Result MoveRelative(Dimension dimension, double speed, double position, int timeout = -1)
        {
            E_Result e_Result = motionControl.MotorRelative(dimension, speed, position);
            BlockingQuery(dimension, timeout);
            return e_Result;
        }

        /// <summary>
        /// 驱动绝对移动
        /// </summary>
        /// <param name="dimension">轴号</param>
        /// <param name="speed">速度</param>
        /// <param name="position">位置</param>
        /// <param name="timeout"></param>
        /// <returns></returns>
        public E_Result MoveAbsolute(Dimension dimension, double speed, double position, int timeout = -1)
        {
            E_Result e_Result = motionControl.MotorAbsolute(dimension, speed, position);
            BlockingQuery(dimension, timeout);
            return e_Result;
        }

        /// <summary>
        /// JOG
        /// </summary>
        /// <param name="dimension"></param>
        /// <param name="speed"></param>
        /// <param name="direction"></param>
        /// <param name="timeout"></param>
        /// <returns></returns>
        public E_Result Jog(Dimension dimension, double speed, bool direction, int timeout = -1)
        {
            E_Result e_Result = motionControl.MotorJog(dimension, speed, direction);
            BlockingQuery(dimension, timeout);
            return E_Result.E_SUCCESS;
        }


        public E_Result Trigger(Dimension dimension, double start, double stop, double step, int pulseWidth, int timeout = -1)
        {
            // 设置连续触发参数
            E_Result e_Result;
            e_Result = motionControl.MotorCompareHS2Para(dimension, pulseWidth);
            if (e_Result != E_Result.E_SUCCESS)
            {
                return e_Result;
            }

            // 设置连续脉冲数组
            double pulseNum = (stop - start) / step; // 脉冲数量


            double numFlag = pulseNum;
            while (numFlag > 0)
            {
                if (pulseNum <= 128)
                {
                    double[] posArray = new double[(int)pulseNum * 2];
                    for (int i = 0; i < (int)pulseNum; i++)
                    {
                        posArray[2 * i] = start + i * step;
                    }
                    motionControl.MotorCompareHs2Data(dimension, posArray);
                    break;
                }
                else
                {
                    double[] posArray = new double[128];
                    for (int i = 0; i < 128; i++)
                    {
                        posArray[i] = start + i * step + (pulseNum - numFlag) * i;
                    }
                    numFlag = numFlag - 128;
                    motionControl.MotorCompareHs2Data(dimension, posArray);
                }
            }

            // 检查参数
            CompareStatus compareStatus = new CompareStatus();
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
        /// Stop
        /// </summary>
        /// <param name="dimension"></param>
        /// <returns></returns>
        public E_Result Stop(Dimension dimension)
        {
            E_Result e_Result = motionControl.MotorStop(dimension);
            return e_Result;
        }

        public E_Result TriggerStop(Dimension dimension)
        {
            E_Result e_Result = motionControl.MotorCompareHS2Stop();
            return e_Result;
        }

        /// <summary>
        /// 轮询阻塞查询
        /// </summary>
        /// <param name="second"></param>
        /// <param name="dimension"></param>
        /// <returns></returns>
        public E_Result BlockingQuery(Dimension dimension, int second)
        {
            int block_flag = 0;
            while (second > block_flag)
            {
                Axis axis = motionControl.MotorGetStatus(dimension);
                bool result = false;

                result = axis.IsRunning;

                SpinWait.SpinUntil(() => !result, 1000); // 延时1S
                if (!result)
                {
                    return E_Result.E_SUCCESS;
                }
                block_flag++;
            }
            return E_Result.E_TIMEOUT;
        }

    }
}
