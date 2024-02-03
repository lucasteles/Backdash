﻿using nGGPO.Core;
using nGGPO.Network.Client;
using nGGPO.Network.Messages;
using nGGPO.Network.Protocol.Messaging;

namespace nGGPO.Network.Protocol;

interface IUdpClientFactory
{
    IUdpClient<ProtocolMessage> CreateClient(
        int port,
        bool enableEndianness,
        IUdpObserver<ProtocolMessage> observer,
        ILogger logger
    );
}

sealed class UdpClientFactory : IUdpClientFactory
{
    public IUdpClient<ProtocolMessage> CreateClient(
        int port,
        bool enableEndianness,
        IUdpObserver<ProtocolMessage> observer,
        ILogger logger
    )
    {
        UdpClient<ProtocolMessage> udpClient = new(
            new UdpSocket(port),
            new ProtocolMessageBinarySerializer
            {
                Network = enableEndianness,
            },
            observer,
            logger
        );

        return udpClient;
    }
}