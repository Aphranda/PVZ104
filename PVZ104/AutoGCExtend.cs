namespace PVZ104
{
    public static class AutoGCExtend
    {
        // 原点复位，带系统偏置
        public static E_Result Reset(this GTSApi gTS, Dimension dimension, double hightSpeed, double lowSpeed, double offset, int timeout = -1)
        {
            E_Result result = gTS.GetPosition(dimension, out double position);
            if (result != E_Result.E_SUCCESS)
            {
                return result;
            }

            double physicalZero;
            if (position % 360 > 180)
            {
                physicalZero = (int)(position / 360) * 360 + 360 - offset;
                result = gTS.MoveAbsolute(dimension, hightSpeed, physicalZero, timeout);
                if (result != E_Result.E_SUCCESS)
                {
                    return result;
                }

                result = gTS.Zero(dimension);
                if (result != E_Result.E_SUCCESS)
                {
                    return result;
                }
            }
            else
            {
                physicalZero = (int)(position / 360) * 360 - offset;
                result = gTS.MoveAbsolute(dimension, hightSpeed, physicalZero, timeout);
                if (result != E_Result.E_SUCCESS)
                {
                    return result;
                }
            }

            return gTS.Home(dimension, lowSpeed, offset, timeout);
        }

        // 继续移动
        public static void GoON(this GTSApi gTS)
        {
        }

        // 多轴同时回零
        public static E_Result HomeAll(this GTSApi gts, Dimension[] dimensions, double[] speed, double[] offset, int timeout = -1)
        {
            return gts.HomeAll(dimensions, speed, offset, timeout);
        }

        // 多轴同时移动
        public static E_Result MoveAll(this GTSApi gts, Dimension[] dimensions, double[] speed, double[] position, int timeout = -1)
        {
            return gts.MoveAll(dimensions, speed, position, timeout);
        }
    }
}
