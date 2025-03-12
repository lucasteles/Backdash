using System.Runtime.InteropServices;

namespace Backdash.Network.Messages;

[Serializable, StructLayout(LayoutKind.Sequential, Size = 1)]
readonly record struct KeepAlive;
