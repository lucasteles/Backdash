using Backdash.Data;

namespace Backdash.Sync.State;

/// <summary>
/// Stores checksum state information
/// </summary>
public interface IChecksumStore
{
    /// <summary>
    ///  Reads state checksum of <paramref name="frame"/>
    /// <param name="frame">Requested frame</param>
    /// </summary>
    int GetChecksum(in Frame frame);
}
