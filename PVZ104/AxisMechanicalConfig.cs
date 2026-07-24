using System;

namespace PVZ104
{
    public class AxisMechanicalConfig
    {
        public const double Axis01MinScale = 8000;
        public const double Axis01MaxScale = 20000;

        public int AxisIndex { get; set; }
        public double Scale { get; set; }
        public bool IsPosLmtDown { get; set; }
        public bool IsNegLmtDown { get; set; }
        public short Encoder { get; set; }

        public string AxisName
        {
            get { return string.Format("Axis{0:00}", AxisIndex + 1); }
        }

        public void Validate()
        {
            if (Scale <= 0)
            {
                throw new ArgumentOutOfRangeException("Scale", "脉冲当量必须大于 0。");
            }

            if (AxisIndex == 0 && (Scale < Axis01MinScale || Scale > Axis01MaxScale))
            {
                throw new ArgumentOutOfRangeException(
                    "Scale",
                    string.Format("Axis01 脉冲当量必须在 {0}-{1} 范围内。", Axis01MinScale, Axis01MaxScale));
            }

            if (Encoder < 0)
            {
                throw new ArgumentOutOfRangeException("Encoder", "编码器模式不能为负数。");
            }
        }

        public AxisMechanicalConfig Clone()
        {
            return new AxisMechanicalConfig
            {
                AxisIndex = AxisIndex,
                Scale = Scale,
                IsPosLmtDown = IsPosLmtDown,
                IsNegLmtDown = IsNegLmtDown,
                Encoder = Encoder,
            };
        }
    }
}
