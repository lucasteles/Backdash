using nGGPO.Data;

namespace nGGPO.Network.Protocol.Internal;

interface IProtocolEventDispatcher
{
    void Enqueue(ProtocolEvent evt);
    void Enqueue(ProtocolEventData evt);
    bool GetEvent(out ProtocolEventData? evt);
}

sealed class ProtocolEventDispatcher(IProtocolLogger logger) : IProtocolEventDispatcher
{
    readonly CircularBuffer<ProtocolEventData> eventQueue = new();

    public void Enqueue(ProtocolEvent evt) => Enqueue(new ProtocolEventData(evt));

    public void Enqueue(ProtocolEventData evt)
    {
        logger.LogEvent("Queuing event", evt);
        eventQueue.Push(evt);
    }

    public bool GetEvent(out ProtocolEventData? evt)
    {
        if (eventQueue.IsEmpty)
        {
            evt = default;
            return false;
        }

        evt = eventQueue.Pop();
        return true;
    }
}
