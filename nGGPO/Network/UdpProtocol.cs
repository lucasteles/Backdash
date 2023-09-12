using System;
using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;
using nGGPO.Network.Messages;

namespace nGGPO.Network;

public class UdpProtocol : IPollLoopSink
{
    readonly Udp? udp;
    readonly int queue;
    readonly IPEndPoint endpoint;
    readonly IReadOnlyList<ConnectStatus> connectStatus;

    State currentState;

    public enum State
    {
        Syncing,
        Synchronzied,
        Running,
        Disconnected,
    };

    public UdpProtocol(Udp udp, int queue, IPEndPoint endpoint,
        IReadOnlyList<ConnectStatus> connectStatus)
    {
        this.udp = udp;
        this.queue = queue;
        this.endpoint = endpoint;
        this.connectStatus = connectStatus;
    }

    public int DisconnectTimeout { get; set; }
    public int DisconnectNotifyStart { get; set; }

    public void Synchronize()
    {
        if (udp is null) return;
        currentState = State.Syncing;
        throw new NotImplementedException();
    }

    public bool IsInitialized()
    {
        throw new NotImplementedException();
    }

    public void SendInput<TInput>(GameInput<TInput> input) where TInput : struct
    {
        throw new NotImplementedException();
    }

    public bool HandlesMsg(IPEndPoint from, UdpMsg msg)
    {
        throw new NotImplementedException();
    }

    public void OnMsg(UdpMsg msg, int len)
    {
        throw new NotImplementedException();
    }

    public Task<bool> OnLoopPoll(object? value)
    {
        throw new NotImplementedException();
    }
}