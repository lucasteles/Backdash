using System.Diagnostics;
using Backdash.Core;
using Backdash.Data;

namespace Backdash.Synchronizing.Input;

sealed class InputQueue<TInput> where TInput : unmanaged
{
    readonly Logger logger;
    readonly GameInput<TInput>[] inputs;
    int length;
    int head, tail;
    bool firstFrame;
    Frame firstIncorrectFrame;
    Frame lastUserAddedFrame, lastAddedFrame, lastFrameRequested;
    GameInput<TInput> prediction;
    readonly int id;
    public int LocalFrameDelay { get; set; }
    public InputQueue(int id, int queueSize, Logger logger)
    {
        this.id = id;
        this.logger = logger;
        length = LocalFrameDelay = 0;
        head = tail = 0;
        firstFrame = true;
        lastUserAddedFrame = Frame.Null;
        firstIncorrectFrame = Frame.Null;
        lastFrameRequested = Frame.Null;
        lastAddedFrame = Frame.Null;
        prediction = new();
        inputs = new GameInput<TInput>[queueSize];
        Array.Fill(inputs, new(Frame.Zero));
    }
    int BackIndex => head == 0 ? inputs.Length - 1 : head - 1;
    ref GameInput<TInput> Back => ref inputs[BackIndex];
    ref GameInput<TInput> Front => ref inputs[tail];
    ref GameInput<TInput> AtFrame(Frame frame) => ref inputs[frame.Number % inputs.Length];
    ref GameInput<TInput> NextAfter(int offset) => ref inputs[(offset + tail) % inputs.Length];
    void Push(GameInput<TInput> input)
    {
        // Add the frame to the back of the queue
        inputs[head] = input;
        head = (head + 1) % inputs.Length;
        length++;
        Trace.Assert(length <= inputs.Length);
    }
    void Skip(int offset)
    {
        Trace.Assert(offset >= 0);
        tail = (tail + offset) % inputs.Length;
        length -= offset;
        Trace.Assert(length >= 0);
    }
    void Reset() => tail = head;
    public Frame FirstIncorrectFrame => firstIncorrectFrame;
    public void DiscardConfirmedFrames(Frame frame)
    {
        Trace.Assert(frame >= Frame.Zero);
        if (lastFrameRequested.IsNotNull)
            frame = Frame.Min(in frame, in lastFrameRequested);
        logger.Write(LogLevel.Trace,
            $"Queue {id} => discarding confirmed frames up to {frame} (last add:{lastAddedFrame} len:{length} front:{Front.Frame} back:{Back.Frame})");
        if (frame >= lastAddedFrame)
            Reset();
        else
        {
            var offset = frame.Number - Front.Frame.Number + 1;
            logger.Write(LogLevel.Trace, $"Queue {id} => difference of {offset} frames.");
            Skip(offset);
        }
        logger.Write(LogLevel.Trace,
            $"Queue {id} => after discarding, new back is {Back.Frame} (front:{Front.Frame})."
        );
    }
    public void ResetPrediction(in Frame frame)
    {
        Trace.Assert(firstIncorrectFrame.IsNull || frame <= firstIncorrectFrame);
        logger.Write(LogLevel.Trace, $"Queue {id} => resetting all prediction errors back to frame {frame}.");
        // There's nothing really to do other than reset our prediction
        // state and the incorrect frame counter...
        prediction.ResetFrame();
        firstIncorrectFrame = Frame.Null;
        lastFrameRequested = Frame.Null;
    }
    public bool GetConfirmedInput(in Frame requestedFrame, ref GameInput<TInput> input)
    {
        Trace.Assert(firstIncorrectFrame.IsNull || requestedFrame < firstIncorrectFrame);
        ref var requested = ref AtFrame(requestedFrame);
        if (requested.Frame != requestedFrame)
            return false;
        input = requested;
        return true;
    }
    public bool GetInput(Frame requestedFrame, out GameInput<TInput> input)
    {
        logger.Write(LogLevel.Trace, $"Queue {id} => requesting input frame {requestedFrame}.");
        // No one should ever try to grab any input when we have a prediction
        // error.  Doing so means that we're just going further down the wrong
        // path.  Trace.Assert this to verify that it's true.
        Trace.Assert(firstIncorrectFrame.IsNull);
        // Remember the last requested frame number for later.  We'll need
        // this in AddInput() to drop out of prediction mode.
        lastFrameRequested = requestedFrame;
        Trace.Assert(requestedFrame >= Front.Frame);
        if (prediction.Frame.IsNull)
        {
            // If the frame requested is in our range, fetch it out of the queue and
            // return it.
            var offset = requestedFrame.Number - Front.Frame.Number;
            if (offset < length)
            {
                ref var next = ref NextAfter(offset);
                Trace.Assert(next.Frame == requestedFrame);
                input = next;
                logger.Write(LogLevel.Trace, $"Queue {id} => returning confirmed frame number {input.Frame}.");
                return true;
            }
            // The requested frame isn't in the queue.  Bummer.  This means we need
            // to return a prediction frame.  Predict that the user will do the
            // same thing they did last time.
            if (requestedFrame == 0)
            {
                logger.Write(LogLevel.Trace,
                    $"Queue {id} => basing new prediction frame from nothing, you're client wants frame 0.");
                prediction.Erase();
            }
            else if (lastAddedFrame.IsNull)
            {
                logger.Write(LogLevel.Trace,
                    $"Queue {id} => basing new prediction frame from nothing, since we have no frames yet.");
                prediction.Erase();
            }
            else
            {
                logger.Write(LogLevel.Trace,
                    $"Queue {id} => basing new prediction frame from previously added frame (queue entry:{BackIndex}, frame:{Back.Frame})"
                );
                prediction = Back;
            }
            prediction.IncrementFrame();
        }
        Trace.Assert(prediction.Frame >= 0);
        // If we've made it this far, we must be predicting.  Go ahead and
        // forward the prediction frame contents.  Be sure to return the
        // frame number requested by the client, though.
        input = prediction;
        input.Frame = requestedFrame;
        logger.Write(LogLevel.Trace,
            $"Queue {id} => returning prediction frame number {input.Frame} ({prediction.Frame}).");
        return false;
    }
    public void AddInput(ref GameInput<TInput> input)
    {
        logger.Write(LogLevel.Trace, $"Queue {id} => adding input frame number {input.Frame} to queue.");
        // These next two lines simply verify that inputs are passed in
        // sequentially by the user, regardless of frame delay.
        Trace.Assert(lastUserAddedFrame.IsNull || input.Frame == lastUserAddedFrame.Next());
        lastUserAddedFrame = input.Frame;
        // Move the queue head to the correct point in preparation to
        // input the frame into the queue.
        var newFrame = AdvanceQueueHead(input.Frame);
        if (newFrame.IsNotNull)
            AddDelayedInputToQueue(input, in newFrame);
        // Update the frame number for the input.  This will also set the
        // frame to GameInput.NullFrame for frames that get dropped (by
        // design).
        input.Frame = newFrame;
    }
    void AddDelayedInputToQueue(GameInput<TInput> input, in Frame frameNumber)
    {
        logger.Write(LogLevel.Trace, $"Queue {id} => adding delayed input frame number {frameNumber} to queue.");
        Trace.Assert(lastAddedFrame.IsNull || frameNumber == lastAddedFrame.Next());
        Trace.Assert(frameNumber == 0 || Back.Frame == frameNumber.Previous());
        input.Frame = frameNumber;
        Push(input);
        firstFrame = false;
        lastAddedFrame = frameNumber;
        if (prediction.Frame.IsNotNull)
        {
            Trace.Assert(frameNumber == prediction.Frame);
            // We've been predicting...  See if the inputs we've gotten match
            // what we've been predicting.  If so, don't worry about it.  If not,
            // remember the first input which was incorrect so we can report it
            // in GetFirstIncorrectFrame()
            if (firstIncorrectFrame.IsNull && !EqualityComparer<TInput>.Default.Equals(prediction.Data, input.Data))
            {
                logger.Write(LogLevel.Debug,
                    $"Queue {id} => frame {frameNumber} does not match prediction.  marking error.");
                firstIncorrectFrame = frameNumber;
            }
            // If this input is the same frame as the last one requested, and we
            // still haven't found any mis-predicted inputs, we can dump out
            // of predition mode entirely!  Otherwise, advance the prediction frame
            // count up.
            if (prediction.Frame == lastFrameRequested &&
                firstIncorrectFrame.IsNull)
            {
                logger.Write(LogLevel.Debug,
                    $"Queue {id} => prediction is correct!  dumping out of prediction mode.");
                prediction.ResetFrame();
            }
            else
                prediction.IncrementFrame();
        }
    }
    Frame AdvanceQueueHead(Frame frame)
    {
        logger.Write(LogLevel.Trace, $"advancing queue head to frame {frame}.");
        var expectedFrame = firstFrame ? Frame.Zero : Back.Frame.Next();
        frame += LocalFrameDelay;
        if (expectedFrame > frame)
        {
            // This can occur when the frame delay has dropped since the last
            // time we shoved a frame into the system.  In this case, there's
            // no room on the queue.  Toss it.
            logger.Write(LogLevel.Information,
                $"Queue {id} => Dropping input frame {frame} (expected next frame to be {expectedFrame})");
            return Frame.Null;
        }
        while (expectedFrame < frame)
        {
            // This can occur when the frame delay has been increased since the last
            // time we shoved a frame into the system.  We need to replicate the
            // last frame in the queue several times in order to fill the space
            // left.
            logger.Write(LogLevel.Information,
                $"Queue {id} => Adding padding frame {expectedFrame} to account for change in frame delay");
            ref var lastFrame = ref Back;
            AddDelayedInputToQueue(lastFrame, in expectedFrame);
            expectedFrame++;
        }
        Trace.Assert(frame == 0 || frame == Back.Frame.Next());
        return frame;
    }
}
