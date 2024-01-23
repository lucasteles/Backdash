using System.Runtime.InteropServices;

namespace nGGPO.Network.Messages;

[StructLayout(LayoutKind.Sequential)]
readonly record struct KeepAlive;
