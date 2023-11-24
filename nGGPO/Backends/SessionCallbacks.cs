namespace nGGPO.Backends;

public readonly struct SaveGameState<TState> where TState : struct
{
    public readonly TState State;
    public readonly int Checksum;

    public SaveGameState(TState state, int checksum = 0)
    {
        State = state;
        Checksum = checksum;
    }
}

/*
 * The SessionCallbacks structure contains the callback functions that
 * your application must implement.  nGGPO will periodically call these
 * functions during the game.  All callback functions must be implemented.
 */
public interface ISessionCallbacks<TGameState> where TGameState : struct
{

    /*
     The client should allocate a buffer, copy the
     * entire contents of the current game state into it, and copy the
     * length into the *len parameter.  Optionally, the client can compute
     * a checksum of the data and store it in the *checksum argument.
     */
    bool SaveGameState(int frame, out SaveGameState<TGameState> buffer);

    /*
     * nGGPO will call this function at the beginning
     * of a rollback.  The buffer and len parameters contain a previously
     * saved state returned from the save_game_state function.  The client
     * should make the current game state match the state contained in the
     * buffer.
     */
    bool LoadGameState(in TGameState buffer);

    /*
     * Used in diagnostic testing.  The client should use
     * the ggpo_log function to write the contents of the specified save
     * state in a human readable form.
     */
    bool LogGameState(string filename, in TGameState buffer);

    /*
     * advance_frame - Called during a rollback.  You should advance your game
     * state by exactly one frame.  Before each frame, call ggpo_synchronize_input
     * to retrieve the inputs you should use for that frame.  After each frame,
     * you should call ggpo_advance_frame to notify GGPO.net that you're
     * finished.
     *
     * The flags parameter is reserved.  It can safely be ignored at this time.
     */
    bool AdvanceFrame(int flags);

    /*
     * Notification that something has happened.  See the GGPOEventCode
     * structure above for more information.
     */
    bool OnEvent<TEvent>(TEvent info) where TEvent : INetCodeEvent;
}