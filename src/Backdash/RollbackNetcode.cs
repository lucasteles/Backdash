namespace Backdash;

/// <summary>
///     The session builder entrypoint.
///     used to create new netcode sessions.
/// </summary>
/// <seealso cref="INetcodeSession{TInput}" />
/// <seealso cref="NetcodeSessionBuilder{TInput}" />
public static class RollbackNetcode
{
    /// <inheritdoc
    ///     cref="NetcodeSessionBuilder.WithInputType{T}(System.Func{Backdash.NetcodeSessionBuilder.InputTypeSelector,Backdash.NetcodeSessionBuilder.InputTypeSelected{T}})" />
    public static NetcodeSessionBuilder<T> WithInputType<T>(
        Func<NetcodeSessionBuilder.InputTypeSelector, NetcodeSessionBuilder.InputTypeSelected<T>> selector)
        where T : unmanaged =>
        new NetcodeSessionBuilder().WithInputType(selector);

    /// <inheritdoc cref="NetcodeSessionBuilder.WithInputType{T}()" />
    public static NetcodeSessionBuilder<T> WithInputType<T>() where T : unmanaged, Enum =>
        WithInputType(x => x.Enum<T>());
}
