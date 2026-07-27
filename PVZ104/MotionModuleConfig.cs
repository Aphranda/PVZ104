using System;
using System.Runtime.Serialization;

namespace PVZ104
{
    [DataContract]
    public class MotionModuleConfigRoot
    {
        [DataMember(Name = "version")]
        public string Version { get; set; }

        [DataMember(Name = "motion_configure")]
        public MotionProjectConfig[] MotionConfigure { get; set; }
    }

    [DataContract]
    public class MotionProjectConfig
    {
        [DataMember(Name = "item_number")]
        public string ItemNumber { get; set; }

        [DataMember(Name = "turntable")]
        public MotionAxisGroupConfig Turntable { get; set; }

        [DataMember(Name = "Wiggler", EmitDefaultValue = false)]
        public MotionAxisGroupConfig Wiggler { get; set; }

        [DataMember(Name = "wiggler", EmitDefaultValue = false)]
        public MotionAxisGroupConfig WigglerLower { get; set; }

        public MotionAxisConfig GetAxis(int axisIndex)
        {
            MotionAxisConfig axis = null;
            if (Turntable != null)
            {
                axis = Turntable.GetAxis(axisIndex);
            }

            if (axis == null && Wiggler != null)
            {
                axis = Wiggler.GetAxis(axisIndex);
            }

            if (axis == null && WigglerLower != null)
            {
                axis = WigglerLower.GetAxis(axisIndex);
            }

            return axis;
        }

        public int GetConfiguredAxisCount()
        {
            int count = 0;
            for (int i = 0; i < 8; i++)
            {
                if (GetAxis(i) != null)
                {
                    count = i + 1;
                }
            }

            return count;
        }
    }

    [DataContract]
    public class MotionAxisGroupConfig
    {
        [DataMember(Name = "axis1")]
        public MotionAxisConfig Axis1 { get; set; }

        [DataMember(Name = "axis2", EmitDefaultValue = false)]
        public MotionAxisConfig Axis2 { get; set; }

        [DataMember(Name = "axis3", EmitDefaultValue = false)]
        public MotionAxisConfig Axis3 { get; set; }

        [DataMember(Name = "axis4", EmitDefaultValue = false)]
        public MotionAxisConfig Axis4 { get; set; }

        [DataMember(Name = "axis5", EmitDefaultValue = false)]
        public MotionAxisConfig Axis5 { get; set; }

        [DataMember(Name = "axis6", EmitDefaultValue = false)]
        public MotionAxisConfig Axis6 { get; set; }

        [DataMember(Name = "axis7", EmitDefaultValue = false)]
        public MotionAxisConfig Axis7 { get; set; }

        [DataMember(Name = "axis8", EmitDefaultValue = false)]
        public MotionAxisConfig Axis8 { get; set; }

        public MotionAxisConfig GetAxis(int axisIndex)
        {
            switch (axisIndex)
            {
                case 0: return Axis1;
                case 1: return Axis2;
                case 2: return Axis3;
                case 3: return Axis4;
                case 4: return Axis5;
                case 5: return Axis6;
                case 6: return Axis7;
                case 7: return Axis8;
                default: return null;
            }
        }
    }

    [DataContract]
    public class MotionAxisConfig
    {
        [DataMember(Name = "motor_base_configure")]
        public MotorBaseConfigure MotorBaseConfigure { get; set; }

        [DataMember(Name = "home_parameters")]
        public HomeParametersConfigure HomeParameters { get; set; }

        [DataMember(Name = "jog_parameters")]
        public JogParametersConfigure JogParameters { get; set; }

        [DataMember(Name = "move_parameters")]
        public MoveParametersConfigure MoveParameters { get; set; }

        [DataMember(Name = "trigger_parameters")]
        public TriggerParametersConfigure TriggerParameters { get; set; }

        [DataMember(Name = "discrete_compensation_parameters", EmitDefaultValue = false)]
        public CompensationParametersConfigure DiscreteCompensationParameters { get; set; }

        [DataMember(Name = "full_stroke_discrete_compensation_parameters", EmitDefaultValue = false)]
        public CompensationParametersConfigure FullStrokeDiscreteCompensationParameters { get; set; }

        [DataMember(Name = "compensation_parameters", EmitDefaultValue = false)]
        public CompensationParametersConfigure CompensationParameters { get; set; }

        [DataMember(Name = "linear_compensation_parameters", EmitDefaultValue = false)]
        public LinearCompensationParametersConfigure LinearCompensationParameters { get; set; }

        public void Validate(int axisIndex)
        {
            if (MotorBaseConfigure == null)
            {
                throw new InvalidOperationException(GetAxisName(axisIndex) + " 缺少 motor_base_configure。");
            }

            MotorBaseConfigure.Validate(axisIndex);

            if (MoveParameters == null)
            {
                throw new InvalidOperationException(GetAxisName(axisIndex) + " 缺少 move_parameters。");
            }

            MoveParameters.Validate(axisIndex);

            if (JogParameters == null)
            {
                throw new InvalidOperationException(GetAxisName(axisIndex) + " 缺少 jog_parameters。");
            }

            JogParameters.Validate(axisIndex);

            if (HomeParameters == null)
            {
                throw new InvalidOperationException(GetAxisName(axisIndex) + " 缺少 home_parameters。");
            }

            HomeParameters.Validate(axisIndex);

            if (LinearCompensationParameters != null)
            {
                LinearCompensationParameters.Validate(axisIndex, MotorBaseConfigure.Scale.Value);
            }

            if (TriggerParameters == null)
            {
                throw new InvalidOperationException(GetAxisName(axisIndex) + " 缺少 trigger_parameters。");
            }

            TriggerParameters.Validate(axisIndex);

            CompensationParametersConfigure compensation = GetFullStrokeDiscreteCompensationParameters();
            if (compensation != null)
            {
                compensation.Validate(axisIndex);
            }
        }

        public double GetEffectiveScale(int axisIndex)
        {
            double scale = MotorBaseConfigure.Scale.Value;
            if (LinearCompensationParameters == null)
            {
                return scale;
            }

            LinearCompensationParameters.Validate(axisIndex, scale);
            return LinearCompensationParameters.Apply(scale);
        }

        public CompensationParametersConfigure GetFullStrokeDiscreteCompensationParameters()
        {
            return DiscreteCompensationParameters ?? FullStrokeDiscreteCompensationParameters ?? CompensationParameters;
        }

        private static string GetAxisName(int axisIndex)
        {
            return string.Format("axis{0}", axisIndex + 1);
        }
    }

    [DataContract]
    public class MotorBaseConfigure
    {
        [DataMember(Name = "scale")]
        public double? Scale { get; set; }

        [DataMember(Name = "smooth")]
        public double? Smooth { get; set; }

        [DataMember(Name = "encoder")]
        public short? Encoder { get; set; }

        [DataMember(Name = "pos_lmt_down")]
        public bool? PosLmtDown { get; set; }

        [DataMember(Name = "neg_lmt_down")]
        public bool? NegLmtDown { get; set; }

        [DataMember(Name = "pos_lmt_enable", EmitDefaultValue = false)]
        public short? PosLmtEnable { get; set; }

        [DataMember(Name = "neg_lmt_enable", EmitDefaultValue = false)]
        public short? NegLmtEnable { get; set; }

        [DataMember(Name = "step_inv", EmitDefaultValue = false)]
        public short? StepInv { get; set; }

        [DataMember(Name = "step_mode", EmitDefaultValue = false)]
        public short? StepMode { get; set; }

        [DataMember(Name = "alarm_enable", EmitDefaultValue = false)]
        public short? AlarmEnable { get; set; }

        [DataMember(Name = "alarm_level", EmitDefaultValue = false)]
        public short? AlarmLevel { get; set; }

        [DataMember(Name = "soft_lmt_enable", EmitDefaultValue = false)]
        public short? SoftLmtEnable { get; set; }

        [DataMember(Name = "soft_lmt_pos", EmitDefaultValue = false)]
        public double? SoftLmtPos { get; set; }

        [DataMember(Name = "soft_lmt_neg", EmitDefaultValue = false)]
        public double? SoftLmtNeg { get; set; }

        [DataMember(Name = "estop_dec", EmitDefaultValue = false)]
        public double? EstopDec { get; set; }

        [DataMember(Name = "max_vel", EmitDefaultValue = false)]
        public double? MaxVel { get; set; }

        [DataMember(Name = "max_acc", EmitDefaultValue = false)]
        public double? MaxAcc { get; set; }

        [DataMember(Name = "pos_err", EmitDefaultValue = false)]
        public int? PosErr { get; set; }

        public void Validate(int axisIndex)
        {
            if (!Scale.HasValue || Scale.Value <= 0)
            {
                throw new InvalidOperationException("axis" + (axisIndex + 1) + " scale 必须大于 0。");
            }

            if (axisIndex == 0 &&
                (Scale.Value < AxisMechanicalConfig.Axis01MinScale || Scale.Value > AxisMechanicalConfig.Axis01MaxScale))
            {
                throw new InvalidOperationException(
                    string.Format(
                        "axis1 scale 必须在 {0}-{1} 范围内。",
                        AxisMechanicalConfig.Axis01MinScale,
                        AxisMechanicalConfig.Axis01MaxScale));
            }

            if (!Smooth.HasValue)
            {
                throw new InvalidOperationException("axis" + (axisIndex + 1) + " smooth 缺失。");
            }

            if (!Encoder.HasValue)
            {
                throw new InvalidOperationException("axis" + (axisIndex + 1) + " encoder 缺失。");
            }

            if (!PosLmtEnable.HasValue || !NegLmtEnable.HasValue || !SoftLmtEnable.HasValue)
            {
                throw new InvalidOperationException("axis" + (axisIndex + 1) + " 限位启用配置缺失。");
            }

            if (!PosLmtDown.HasValue || !NegLmtDown.HasValue)
            {
                throw new InvalidOperationException("axis" + (axisIndex + 1) + " 限位电平配置缺失。");
            }

            ValidateBinary(PosLmtEnable, axisIndex, "pos_lmt_enable");
            ValidateBinary(NegLmtEnable, axisIndex, "neg_lmt_enable");
            ValidateBinary(StepInv, axisIndex, "step_inv");
            ValidateBinary(StepMode, axisIndex, "step_mode");
            ValidateBinary(AlarmEnable, axisIndex, "alarm_enable");
            ValidateBinary(AlarmLevel, axisIndex, "alarm_level");
            ValidateBinary(SoftLmtEnable, axisIndex, "soft_lmt_enable");

            if ((SoftLmtPos.HasValue && !SoftLmtNeg.HasValue) ||
                (!SoftLmtPos.HasValue && SoftLmtNeg.HasValue))
            {
                throw new InvalidOperationException("axis" + (axisIndex + 1) + " soft_lmt_pos 和 soft_lmt_neg 必须同时配置。");
            }

            if (SoftLmtPos.HasValue && SoftLmtPos.Value <= SoftLmtNeg.Value)
            {
                throw new InvalidOperationException("axis" + (axisIndex + 1) + " soft_lmt_pos 必须大于 soft_lmt_neg。");
            }

            if ((EstopDec.HasValue && EstopDec.Value <= 0) ||
                (MaxVel.HasValue && MaxVel.Value <= 0) ||
                (MaxAcc.HasValue && MaxAcc.Value <= 0))
            {
                throw new InvalidOperationException("axis" + (axisIndex + 1) + " estop_dec/max_vel/max_acc 必须大于 0。");
            }

            if (PosErr.HasValue && PosErr.Value < 0)
            {
                throw new InvalidOperationException("axis" + (axisIndex + 1) + " pos_err 不能为负数。");
            }
        }

        private static void ValidateBinary(short? value, int axisIndex, string name)
        {
            if (value.HasValue && value.Value != 0 && value.Value != 1)
            {
                throw new InvalidOperationException("axis" + (axisIndex + 1) + " " + name + " 只能为 0 或 1。");
            }
        }
    }

    [DataContract]
    public class HomeParametersConfigure
    {
        [DataMember(Name = "homeMode")]
        public short? HomeMode { get; set; }

        [DataMember(Name = "dir")]
        public short? Dir { get; set; }

        [DataMember(Name = "acc")]
        public double? Acc { get; set; }

        [DataMember(Name = "scan1stVel")]
        public double? Scan1stVel { get; set; }

        [DataMember(Name = "scan2ndVel")]
        public double? Scan2ndVel { get; set; }

        [DataMember(Name = "reScanEn")]
        public short? ReScanEn { get; set; }

        [DataMember(Name = "homeEdge")]
        public short? HomeEdge { get; set; }

        [DataMember(Name = "lmtEdge")]
        public short? LmtEdge { get; set; }

        [DataMember(Name = "zEdge")]
        public short? ZEdge { get; set; }

        [DataMember(Name = "iniRetPos")]
        public double? IniRetPos { get; set; }

        [DataMember(Name = "retSwOffset")]
        public double? RetSwOffset { get; set; }

        [DataMember(Name = "safeLen")]
        public double? SafeLen { get; set; }

        [DataMember(Name = "usePreSetPtpPara")]
        public short? UsePreSetPtpPara { get; set; }

        public void Validate(int axisIndex)
        {
            if (!HomeMode.HasValue ||
                !Dir.HasValue ||
                !Acc.HasValue ||
                !Scan1stVel.HasValue ||
                !Scan2ndVel.HasValue ||
                !ReScanEn.HasValue ||
                !HomeEdge.HasValue ||
                !LmtEdge.HasValue ||
                !ZEdge.HasValue ||
                !IniRetPos.HasValue ||
                !RetSwOffset.HasValue ||
                !SafeLen.HasValue ||
                !UsePreSetPtpPara.HasValue)
            {
                throw new InvalidOperationException("axis" + (axisIndex + 1) + " home_parameters 字段不完整。");
            }

            if (Acc.Value <= 0 || Scan1stVel.Value <= 0 || Scan2ndVel.Value <= 0 || SafeLen.Value <= 0)
            {
                throw new InvalidOperationException("axis" + (axisIndex + 1) + " home_parameters 存在非正数。");
            }
        }
    }

    [DataContract]
    public class JogParametersConfigure
    {
        [DataMember(Name = "acc")]
        public double? Acc { get; set; }

        [DataMember(Name = "dec")]
        public double? Dec { get; set; }

        [DataMember(Name = "smooth")]
        public double? Smooth { get; set; }

        public void Validate(int axisIndex)
        {
            if (!Acc.HasValue || !Dec.HasValue || !Smooth.HasValue)
            {
                throw new InvalidOperationException("axis" + (axisIndex + 1) + " jog_parameters 字段不完整。");
            }

            if (Acc.Value <= 0 || Dec.Value <= 0)
            {
                throw new InvalidOperationException("axis" + (axisIndex + 1) + " jog_parameters 加减速必须大于 0。");
            }
        }
    }

    [DataContract]
    public class MoveParametersConfigure
    {
        [DataMember(Name = "acc")]
        public double? Acc { get; set; }

        [DataMember(Name = "dec")]
        public double? Dec { get; set; }

        [DataMember(Name = "smooth")]
        public double? Smooth { get; set; }

        [DataMember(Name = "startVel")]
        public double? StartVel { get; set; }

        [DataMember(Name = "endVel")]
        public double? EndVel { get; set; }

        public void Validate(int axisIndex)
        {
            if (!Acc.HasValue || !Dec.HasValue || !Smooth.HasValue || !StartVel.HasValue || !EndVel.HasValue)
            {
                throw new InvalidOperationException("axis" + (axisIndex + 1) + " move_parameters 字段不完整。");
            }

            if (Acc.Value <= 0 || Dec.Value <= 0)
            {
                throw new InvalidOperationException("axis" + (axisIndex + 1) + " move_parameters 加减速必须大于 0。");
            }
        }
    }

    [DataContract]
    public class TriggerParametersConfigure
    {
        [DataMember(Name = "outputChn")]
        public short? OutputChn { get; set; }

        [DataMember(Name = "outputType")]
        public short? OutputType { get; set; }

        [DataMember(Name = "chnType")]
        public short? ChnType { get; set; }

        [DataMember(Name = "dir1No")]
        public short? Dir1No { get; set; }

        [DataMember(Name = "dir2No")]
        public short? Dir2No { get; set; }

        [DataMember(Name = "posSrc")]
        public short? PosSrc { get; set; }

        [DataMember(Name = "stLevel")]
        public short? StLevel { get; set; }

        [DataMember(Name = "errZone")]
        public short? ErrZone { get; set; }

        [DataMember(Name = "directOutZone")]
        public short? DirectOutZone { get; set; }

        [DataMember(Name = "vibrateRange")]
        public short? VibrateRange { get; set; }

        [DataMember(Name = "gateTime")]
        public int? GateTime { get; set; }

        [DataMember(Name = "minIntervalTime")]
        public int? MinIntervalTime { get; set; }

        public void Validate(int axisIndex)
        {
            if (!OutputChn.HasValue ||
                !OutputType.HasValue ||
                !ChnType.HasValue ||
                !Dir1No.HasValue ||
                !Dir2No.HasValue ||
                !PosSrc.HasValue ||
                !StLevel.HasValue ||
                !ErrZone.HasValue ||
                !DirectOutZone.HasValue ||
                !VibrateRange.HasValue ||
                !GateTime.HasValue ||
                !MinIntervalTime.HasValue)
            {
                throw new InvalidOperationException("axis" + (axisIndex + 1) + " trigger_parameters 字段不完整。");
            }

            if (OutputChn.Value < 0 ||
                ErrZone.Value < 0 ||
                DirectOutZone.Value < 0 ||
                VibrateRange.Value < 0 ||
                GateTime.Value < 0 ||
                MinIntervalTime.Value < 0)
            {
                throw new InvalidOperationException("axis" + (axisIndex + 1) + " trigger_parameters 存在非法负数。");
            }
        }
    }

    [DataContract]
    public class CompensationParametersConfigure
    {
        [DataMember(Name = "num")]
        public int? Num { get; set; }

        [DataMember(Name = "startPos")]
        public int? StartPos { get; set; }

        [DataMember(Name = "cmpLen")]
        public int? CmpLen { get; set; }

        [DataMember(Name = "pCmpPos")]
        public short[] PCmpPos { get; set; }

        [DataMember(Name = "nCmpPos")]
        public short[] NCmpPos { get; set; }

        public void Validate(int axisIndex)
        {
            if (!Num.HasValue || !StartPos.HasValue || !CmpLen.HasValue)
            {
                throw new InvalidOperationException("axis" + (axisIndex + 1) + " discrete_compensation_parameters 字段不完整。");
            }

            if (Num.Value < 2 || Num.Value > 360)
            {
                throw new InvalidOperationException("axis" + (axisIndex + 1) + " discrete_compensation_parameters num 必须在 2 到 360 之间。");
            }

            if (CmpLen.Value <= 0)
            {
                throw new InvalidOperationException("axis" + (axisIndex + 1) + " discrete_compensation_parameters cmpLen 必须大于 0。");
            }

            ValidateArray(axisIndex, "pCmpPos", PCmpPos, Num.Value);
            ValidateArray(axisIndex, "nCmpPos", NCmpPos, Num.Value);
        }

        public short[] GetPositiveCompensationArray()
        {
            return ExpandArray(PCmpPos, Num.Value);
        }

        public short[] GetNegativeCompensationArray()
        {
            return ExpandArray(NCmpPos, Num.Value);
        }

        private static void ValidateArray(int axisIndex, string fieldName, short[] values, int num)
        {
            if (values == null || values.Length == 0)
            {
                throw new InvalidOperationException("axis" + (axisIndex + 1) + " discrete_compensation_parameters " + fieldName + " 不能为空。");
            }

            if (values.Length != 1 && values.Length != num)
            {
                throw new InvalidOperationException(
                    "axis" + (axisIndex + 1) + " discrete_compensation_parameters " + fieldName + " 长度必须为 1 或 num。");
            }
        }

        private static short[] ExpandArray(short[] values, int num)
        {
            if (values.Length == num)
            {
                return values;
            }

            short[] expanded = new short[num];
            for (int i = 0; i < expanded.Length; i++)
            {
                expanded[i] = values[0];
            }

            return expanded;
        }
    }

    [DataContract]
    public class LinearCompensationParametersConfigure
    {
        [DataMember(Name = "enabled")]
        public bool? Enabled { get; set; }

        [DataMember(Name = "scale_factor")]
        public double? ScaleFactor { get; set; }

        [DataMember(Name = "scale_offset")]
        public double? ScaleOffset { get; set; }

        public double Apply(double baseScale)
        {
            if (Enabled.HasValue && !Enabled.Value)
            {
                return baseScale;
            }

            double factor = ScaleFactor.HasValue ? ScaleFactor.Value : 1.0;
            double offset = ScaleOffset.HasValue ? ScaleOffset.Value : 0.0;
            return (baseScale + offset) * factor;
        }

        public void Validate(int axisIndex, double baseScale)
        {
            double factor = ScaleFactor.HasValue ? ScaleFactor.Value : 1.0;
            if (factor <= 0)
            {
                throw new InvalidOperationException("axis" + (axisIndex + 1) + " linear_compensation_parameters scale_factor 必须大于 0。");
            }

            double effectiveScale = Apply(baseScale);
            if (effectiveScale <= 0)
            {
                throw new InvalidOperationException("axis" + (axisIndex + 1) + " 线性补偿后的 scale 必须大于 0。");
            }
        }
    }
}
