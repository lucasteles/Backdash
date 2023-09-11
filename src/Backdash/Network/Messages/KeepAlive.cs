using System.Runtime.InteropServices;

namespace Backdash.Network.Messages;

[StructLayout(LayoutKind.Sequential)]
readonly record struct KeepAlive;
