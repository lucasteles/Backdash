using System;
using System.Diagnostics;
using System.Threading.Tasks;
using nGGPO.Types;

namespace nGGPO;

public interface IPollLoopSink
{
    Task<bool> OnLoopPoll(object? value);
}

public interface IPollPeriodicSink
{
    bool OnPeriodicPoll(object? value, long lastFired);
}

public interface IPollMsgSink
{
    bool OnMsgPoll(object? value);
}

public interface IPollHandleSink
{
    bool OnHandlePoll(object? value);
}

class Poll
{
    long StartTime;
    int HandleCount;
    Handle[] Handles = new Handle[Max.PollableHandles];

    StaticBuffer<PollSinkCb<IPollHandleSink>> HandleSinks = new(Max.PollableHandles);
    StaticBuffer<PollSinkCb<IPollMsgSink>> MsgSinks = new();
    StaticBuffer<PollSinkCb<IPollLoopSink>> LoopSinks = new();
    StaticBuffer<PollPeriodicSinkCb> PeriodicSinks = new();

    public void RegisterHandle(IPollHandleSink sink, Handle h, object? cookie = null)
    {
        Trace.Assert(HandleCount < Max.PollableHandles - 1);

        Handles[HandleCount] = h;
        HandleSinks[HandleCount] = new(sink, cookie);
        HandleCount++;
    }

    public void RegisterMsgLoop(IPollMsgSink sink, object? cookie = null) =>
        MsgSinks.PushBack(new(sink, cookie));

    public void RegisterLoop(IPollLoopSink sink, object? cookie = null) =>
        LoopSinks.PushBack(new(sink, cookie));

    public void RegisterPeriodic(IPollPeriodicSink sink, int interval, object? cookie = null) =>
        PeriodicSinks.PushBack(new(sink, cookie, interval));

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

    protected class PollSinkCb<TSink>
    {
        public TSink Sink { get; set; }
        public object? Cookie { get; set; }

        public PollSinkCb(TSink sink, object? cookie)
        {
            Sink = sink;
            Cookie = cookie;
        }
    }

    protected class PollPeriodicSinkCb : PollSinkCb<IPollPeriodicSink>
    {
        public int Interval { get; set; }
        public long LastFired { get; set; }

        public PollPeriodicSinkCb(IPollPeriodicSink sink, object? cookie, int lastFired)
            : base(sink, cookie)
        {
            LastFired = lastFired;
            Interval = 0;
        }
    }
}