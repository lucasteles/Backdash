using Backdash.Network.Messages;
using Backdash.Serialization;

namespace Backdash.Network.Protocol.Messaging;

sealed class ProtocolMessageBinarySerializer : SerializableTypeBinarySerializer<ProtocolMessage>;
