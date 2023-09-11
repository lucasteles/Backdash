using System.Runtime.InteropServices;

namespace nGGPO.Network;

static class Serializer
{
    public static byte[] Encode<T>(T message) where T : struct
    {
        var size = Marshal.SizeOf(message);
        var body = new byte[size];
        var ptr = Marshal.AllocHGlobal(size);
        Marshal.StructureToPtr(message, ptr, true);
        Marshal.Copy(ptr, body, 0, size);
        Marshal.FreeHGlobal(ptr);
        return body;
    }

    public static T Decode<T>(byte[] body) where T : struct
    {
        var size = Marshal.SizeOf<T>();
        var ptr = Marshal.AllocHGlobal(size);
        Marshal.Copy(body, 0, ptr, size);
        var response = (T) Marshal.PtrToStructure(ptr, typeof(T));
        Marshal.FreeHGlobal(ptr);
        return response;
    }

    public static int SizeOf<T>() where T : struct => Marshal.SizeOf<T>();
}