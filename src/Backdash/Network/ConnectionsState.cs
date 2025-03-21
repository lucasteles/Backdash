using Backdash.Network.Messages;

namespace Backdash.Network;

sealed class ConnectionsState
{
    public int Length => Statuses.Length;
    public readonly ConnectStatus[] Statuses;

    public ConnectionsState(int size, Frame lastFrame)
    {
        Statuses = new ConnectStatus[size];
        ConnectStatus defaultStatus = new()
        {
            LastFrame = lastFrame,
        };

        Array.Fill(Statuses, defaultStatus);
    }

    public ConnectionsState(int size) : this(size, Frame.Null) { }
    public ref ConnectStatus this[in PlayerHandle player] => ref Statuses[player.QueueIndex];
    public ref ConnectStatus this[int index] => ref Statuses[index];
    public bool IsKnown(in PlayerHandle player) => player.QueueIndex >= 0 && player.QueueIndex < Length;

    public bool IsConnected(in PlayerHandle player) =>
        IsKnown(in player) && !Statuses[player.QueueIndex].Disconnected;

    public void CopyTo(Span<ConnectStatus> buffer) => Statuses.CopyTo(buffer);

    public Span<ConnectStatus> AsSpan() => Statuses;

    public static implicit operator Span<ConnectStatus>(ConnectionsState state) => state.AsSpan();
    public static implicit operator ReadOnlySpan<ConnectStatus>(ConnectionsState state) => state.AsSpan();
}
