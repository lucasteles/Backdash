namespace Backdash.Network.Protocol.Events;

interface IProtocolEventDispatcher
{
    void Enqueue(ProtocolEvent evt);
    void Enqueue(ProtocolEventData evt);
    bool GetEvent(out ProtocolEventData? evt);
}

sealed class ProtocolEventDispatcher(IProtocolLogger logger) : IProtocolEventDispatcher
{
    readonly Queue<ProtocolEventData> eventQueue = new(64);

    public void Enqueue(ProtocolEvent evt) => Enqueue(new ProtocolEventData(evt));

    public void Enqueue(ProtocolEventData evt)
    {
        logger.LogEvent("Queuing event", evt);
        eventQueue.Enqueue(evt);
    }

    public bool GetEvent(out ProtocolEventData? evt)
    {
        if (eventQueue.Count is 0)
        {
            evt = default;
            return false;
        }

        evt = eventQueue.Dequeue();
        return true;
    }
}
