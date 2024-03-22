using Backdash.Data;

namespace Backdash.Sync.State;

sealed class EmptyChecksumStore : IChecksumStore
{
    public int GetChecksum(in Frame frame) => 0;
    public (Frame, int) LastChecksum() => (Frame.Null, 0);
}
