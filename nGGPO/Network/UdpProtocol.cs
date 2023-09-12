using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;

namespace nGGPO.Network;

public class UdpProtocol
{
    readonly Udp? udp;
    readonly Poll poll;
    readonly int queue;
    readonly IPEndPoint endpoint;
    readonly IReadOnlyList<UdpConnectStatus> connectStatus;

    State currentState;

    public enum State
    {
        Syncing,
        Synchronzied,
        Running,
        Disconnected,
    };

    public UdpProtocol(Udp udp, Poll poll, int queue, IPEndPoint endpoint,
        IReadOnlyList<UdpConnectStatus> connectStatus)
    {
        this.udp = udp;
        this.poll = poll;
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
}