using System.Linq;
using System.Threading;

namespace PVZ104
{
    public static class AutoGCExtend
    {
        // 原点复位，带系统偏置
        public static E_Result Reset(this GTSApi gTS, Dimension dimension, double hightSpeed, double lowSpeed, double offset, int timeout = -1)
        {
            E_Result result;
            MotionControl motionControl = new MotionControl();
            double position;
            double physicalZero;
            gTS.GetPosition(dimension, out position);

            if (position % 360 > 180)
            {
                physicalZero = (int)(position / 360) * 360 + 360 - offset;
                gTS.MoveAbsolute(dimension, hightSpeed, physicalZero, timeout);
                motionControl.MotorZero(dimension);
            }
            else
            {
                physicalZero = (int)(position / 360) * 360 - offset;
                gTS.MoveAbsolute(dimension, hightSpeed, physicalZero, timeout);
            }
            result = gTS.Home(dimension, lowSpeed, offset, timeout);

            return result;
        }
        // 继续移动
        public static void GoON(this GTSApi gTS)
        {

        }

        // 多轴同时回零
        public static E_Result HomeAll(this GTSApi gts, Dimension[] dimensions, double[] speed, double[] offset, int timeout = -1)
        {
            MotionControl motionControl = new MotionControl();

            // 执行多轴原点复位
            for (int i = 0; i < dimensions.Length; i++)
            {
                motionControl.MotorHome(dimensions[i], speed[i], offset[i]);
                // 各轴启动间隔100ms
                SpinWait.SpinUntil(() => false, 100);
            }
            // 轮询多轴复位是否完成,原点复位不知道当前位置，默认给60s复位时间
            motionControl.BlockingQuery(dimensions, 60000);
            return E_Result.E_SUCCESS;
        }

        // 多轴同时移动
        public static E_Result MoveAll(this GTSApi gts, Dimension[] dimensions, double[] speed, double[] position, int timeout = -1)
        {
            MotionControl motionControl = new MotionControl();


            double[] currentPosition = new double[dimensions.Length];
            int[] timeoutArray = new int[dimensions.Length];

            // 执行多轴同时运行
            for (int i = 0; i < dimensions.Length; i++)
            {
                // 获取单轴当前位置
                gts.GetPosition(dimensions[i], out currentPosition[i]);

                // 单轴运行
                motionControl.MotorAbsolute(dimensions[i], speed[i], position[i]);

                // 各轴启动间隔100ms
                SpinWait.SpinUntil(() => false, 100);
            }

            // 获取阻塞时间
            for (int i = 0; i < dimensions.Length; i++)
            {
                timeoutArray[i] = (int)(((position[i] - currentPosition[i]) / speed[i] + 10) * 1000);
            }

            //轮询复位是否完成
            motionControl.BlockingQuery(dimensions, timeoutArray.Max());
            return E_Result.E_SUCCESS;
        }
    }
}
