using System;
using System.Linq;

namespace PVZ104
{
    public sealed class MotionConnectionDiagnostic
    {
        public MotionConnectionDiagnostic(
            byte[] requestedIpv4,
            short? openByIpReturnCode,
            short? searchReturnCode,
            ushort? searchedDeviceCount,
            short? openBySearchReturnCode,
            short? nativeLastErrorCode,
            string message)
        {
            RequestedIpv4 = requestedIpv4 == null ? Array.Empty<byte>() : (byte[])requestedIpv4.Clone();
            OpenByIpReturnCode = openByIpReturnCode;
            SearchReturnCode = searchReturnCode;
            SearchedDeviceCount = searchedDeviceCount;
            OpenBySearchReturnCode = openBySearchReturnCode;
            NativeLastErrorCode = nativeLastErrorCode;
            Message = message ?? string.Empty;
        }

        public byte[] RequestedIpv4 { get; }

        public short? OpenByIpReturnCode { get; }

        public short? SearchReturnCode { get; }

        public ushort? SearchedDeviceCount { get; }

        public short? OpenBySearchReturnCode { get; }

        public short? NativeLastErrorCode { get; }

        public string Message { get; }

        public static MotionConnectionDiagnostic Empty { get; } =
            new MotionConnectionDiagnostic(null, null, null, null, null, null, "尚未执行连接");

        public override string ToString()
        {
            string ipText = RequestedIpv4.Length == 4 ? string.Join(".", RequestedIpv4.Select(v => v.ToString())) : "Invalid";
            return $"{Message}; IP={ipText}; OpenByIP={Format(OpenByIpReturnCode)}; Search={Format(SearchReturnCode)}; Count={Format(SearchedDeviceCount)}; OpenBySearch={Format(OpenBySearchReturnCode)}; LastErr={Format(NativeLastErrorCode)}";
        }

        private static string Format<T>(T? value) where T : struct
        {
            return value.HasValue ? value.Value.ToString() : "N/A";
        }
    }
}
