using System.Runtime.InteropServices;

namespace Backdash.Network.Messages;

[Serializable, StructLayout(LayoutKind.Sequential)]
readonly record struct KeepAlive;
