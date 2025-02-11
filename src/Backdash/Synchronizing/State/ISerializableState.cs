using Backdash.Serialization.Buffer;

namespace Backdash.Synchronizing.State;

interface ISerializableState
{
    void Serialize(BinaryBufferWriter writer);
}
