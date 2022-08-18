using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GTSStandardizationAPI
{
    public static class AutoGCExtend
    {
        // 原点复位，带系统偏置
        public static void Reset(this GTSApi gTS, Dimension dimension, double hightSpeed, double lowSpeed, double offset, int timeout = -1)
        {
            MotionControl motionControl = new MotionControl();
            double position;
            gTS.GetPosition(dimension, out position);
            if (position > 180)
            {
                gTS.MoveAbsolute(dimension, hightSpeed, 360, timeout);
                motionControl.MotorZero(dimension);
            }
            else
            {
                gTS.MoveAbsolute(dimension, hightSpeed, 0, timeout);
            }
            gTS.Home(dimension, lowSpeed, offset, timeout);
        }
        // 继续移动
        public static void GoON(this GTSApi gTS)
        {

        }
        // 多轴同时移动
        public static void MoveAll(this GTSApi gts, double speed, double position, int timeout = -1)
        {

        }
    }
}
