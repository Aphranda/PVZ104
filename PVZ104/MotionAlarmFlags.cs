using System;

namespace PVZ104
{
    [Flags]
    internal enum MotionAlarmFlags
    {
        None = 0,
        NotConnected = 128,
        ServoEnable = 256,
        Reset = 512,
        Home = 1024,
        AbsoluteMove = 2048,
        RelativeMove = 4096,
        Timeout = 8192,
        Limit = 16384,
        Driver = 32768,
        Configuration = 65536
    }
}
