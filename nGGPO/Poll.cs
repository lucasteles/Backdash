using System.Diagnostics;
using nGGPO.Types;

namespace nGGPO;

public interface IPollSink
{
    bool OnHandlePoll(object? value);
    bool OnMsgPoll(object? value);
    bool OnPeriodicPoll(object? value, long lastFired);
    Task<bool> OnLoopPoll(object? value);
}

public abstract class PollSink : IPollSink
{
    public virtual bool OnHandlePoll(object? value) => true;
    public virtual bool OnMsgPoll(object? value) => true;
    public virtual bool OnPeriodicPoll(object? value, long lastFired) => true;
    public virtual Task<bool> OnLoopPoll(object? value) => Task.FromResult(true);
}

public class Poll
{
    public const int MaxPollableHandles = 64;

    protected long StartTime;
    protected int HandleCount;
    protected Handle[] Handles = new Handle[MaxPollableHandles];
    protected PollSinkCb[] HandleSinks = new PollSinkCb[MaxPollableHandles];

    protected StaticBuffer<PollSinkCb> MsgSinks = new();
    protected StaticBuffer<PollSinkCb> LoopSinks = new();
    protected StaticBuffer<PollPeriodicSinkCb> PeriodicSinks = new();

    public void RegisterHandle(IPollSink sink, Handle h, object? cookie = null)
    {
        Trace.Assert(HandleCount < MaxPollableHandles - 1);

        Handles[HandleCount] = h;
        HandleSinks[HandleCount] = new(sink, cookie);
        HandleCount++;
    }

    public void RegisterMsgLoop(IPollSink sink, object? cookie = null)
    {
        MsgSinks.PushBack(new PollSinkCb(sink, cookie));
    }

    public void RegisterLoop(IPollSink sink, object? cookie = null)
    {
        LoopSinks.PushBack(new PollSinkCb(sink, cookie));
    }

    public void RegisterPeriodic(IPollSink sink, int interval, object? cookie = null)
    {
        PeriodicSinks.PushBack(new PollPeriodicSinkCb(sink, cookie, interval));
    }

    public async Task<bool> Pump(long timeout)
    {
        int i, res;
        var finished = false;

        if (StartTime == 0)
            StartTime = Platform.GetCurrentTimeMS();

        var elapsed = Platform.GetCurrentTimeMS() - StartTime;
        var maxwait = ComputeWaitTime(elapsed);
        if (maxwait != long.MaxValue)
        {
            timeout = Math.Min(timeout, maxwait);
        }

        // ????/
        // res = WaitForMultipleObjects(_handle_count, _handles, false, timeout);
        // if (res >= WAIT_OBJECT_0 && res < WAIT_OBJECT_0 + _handle_count)
        // {
        //     i = res - WAIT_OBJECT_0;
        //     finished = !_handle_sinks[i].Sink.OnHandlePoll(_handle_sinks[i].Cookie) || finished;
        // }

        for (i = 0; i < MsgSinks.Size; i++)
        {
            var cb = MsgSinks[i];
            finished = !cb.Sink.OnMsgPoll(cb.Cookie) || finished;
        }

        for (i = 0; i < PeriodicSinks.Size; i++)
        {
            var cb = PeriodicSinks[i];
            if (cb.Interval + cb.LastFired <= elapsed)
            {
                cb.LastFired = (elapsed / cb.Interval) * cb.Interval;
                finished = !cb.Sink.OnPeriodicPoll(cb.Cookie, cb.LastFired) || finished;
            }
        }

        for (i = 0; i < LoopSinks.Size; i++)
        {
            var cb = LoopSinks[i];
            finished = !await cb.Sink.OnLoopPoll(cb.Cookie) || finished;
        }

        return finished;
    }


    public async Task Run(long timeout = 100)
    {
        while (await Pump(timeout))
        {
        }
    }

    protected long ComputeWaitTime(long elapsed)
    {
        var waitTime = long.MaxValue;
        var count = PeriodicSinks.Size;

        if (count <= 0) return waitTime;

        for (var i = 0; i < count; i++)
        {
            var cb = PeriodicSinks[i];
            var timeout = cb.Interval + cb.LastFired - elapsed;
            if (waitTime == long.MaxValue || timeout < waitTime)
                waitTime = Math.Max(timeout, 0);
        }

        return waitTime;
    }

    protected class PollSinkCb
    {
        public IPollSink Sink { get; set; }
        public object? Cookie { get; set; }

        public PollSinkCb(IPollSink sink, object? cookie)
        {
            Sink = sink;
            Cookie = cookie;
        }
    }

    protected class PollPeriodicSinkCb : PollSinkCb
    {
        public int Interval { get; set; }
        public long LastFired { get; set; }

        public PollPeriodicSinkCb(IPollSink sink, object? cookie, int lastFired)
            : base(sink, cookie)
        {
            LastFired = lastFired;
            Interval = 0;
        }
    }
}