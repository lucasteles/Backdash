using System.Diagnostics;
using Backdash.Core;
using Backdash.Data;

namespace Backdash.Synchronizing.Input;

sealed class InputQueue<TInput> where TInput : unmanaged
{
    readonly Logger logger;
    bool firstFrame;
    Frame firstIncorrectFrame;
    Frame lastUserAddedFrame, lastAddedFrame, lastFrameRequested;
    GameInput<TInput> prediction;
    readonly int queueId;
    public int LocalFrameDelay { get; set; }

    readonly CircularBuffer<GameInput<TInput>> buffer;
    readonly EqualityComparer<TInput> inputComparer;

    public InputQueue(int queueId, int queueSize, Logger logger, EqualityComparer<TInput>? inputComparer = null)
    {
        this.queueId = queueId;
        this.logger = logger;
        this.inputComparer = inputComparer ?? EqualityComparer<TInput>.Default;
        LocalFrameDelay = 0;
        firstFrame = true;
        lastUserAddedFrame = Frame.Null;
        firstIncorrectFrame = Frame.Null;
        lastFrameRequested = Frame.Null;
        lastAddedFrame = Frame.Null;
        prediction = new();
        buffer = new(queueSize);
        buffer.Clear();
        buffer.Fill(new(Frame.Zero));
    }

    ref GameInput<TInput> LastInput => ref buffer.Current();
    ref GameInput<TInput> FirstInput => ref buffer.Last();

    public Frame FirstIncorrectFrame => firstIncorrectFrame;

    public void DiscardConfirmedFrames(Frame frame)
    {
        Trace.Assert(frame >= Frame.Zero);
        if (!lastFrameRequested.IsNull)
            frame = Frame.Min(in frame, in lastFrameRequested);
        logger.Write(LogLevel.Trace,
            $"Queue {queueId} => discarding confirmed frames up to {frame} (last add:{lastAddedFrame.Number} len:{buffer.Size} front:{FirstInput.Frame.Number} back:{LastInput.Frame.Number})");
        if (frame >= lastAddedFrame)
            buffer.Clear();
        else
        {
            var offset = frame.Number - FirstInput.Frame.Number + 1;
            logger.Write(LogLevel.Trace, $"Queue {queueId} => difference of {offset} frames.");
            buffer.Discard(offset);
        }

        logger.Write(LogLevel.Trace,
            $"Queue {queueId} => after discarding, new back is {LastInput.Frame.Number} (front:{FirstInput.Frame.Number})."
        );
    }

    public void ResetPrediction(in Frame frame)
    {
        Trace.Assert(firstIncorrectFrame.IsNull || frame.Number <= firstIncorrectFrame.Number);
        logger.Write(LogLevel.Trace,
            $"Queue {queueId} => resetting all prediction errors back to frame {frame.Number}.");
        // There's nothing really to do other than reset our prediction state and the incorrect frame counter...
        prediction.ResetFrame();
        firstIncorrectFrame = Frame.Null;
        lastFrameRequested = Frame.Null;
    }

    public bool GetConfirmedInput(in Frame requestedFrame, ref GameInput<TInput> input)
    {
        Trace.Assert(firstIncorrectFrame.IsNull || requestedFrame.Number < firstIncorrectFrame.Number);

        ref var requested = ref buffer.AtRaw(requestedFrame.Number);

        if (requested.Frame.Number != requestedFrame.Number)
            return false;

        input = requested;
        return true;
    }

    public bool GetInput(Frame requestedFrame, out GameInput<TInput> input)
    {
        logger.Write(LogLevel.Trace, $"Queue {queueId} => requesting input frame {requestedFrame.Number}.");
        // No one should ever try to grab any input when we have a prediction error.
        // Doing so means that we're just going further down the wrong path.
        Trace.Assert(firstIncorrectFrame.IsNull);
        // Remember the last requested frame number for later. We'll need this in AddInput() to drop out of prediction mode.
        lastFrameRequested = requestedFrame;
        Trace.Assert(requestedFrame >= FirstInput.Frame);
        if (prediction.Frame.IsNull)
        {
            // If the frame requested is in our range, fetch it out of the queue and  return it.
            var offset = requestedFrame.Number - FirstInput.Frame.Number;
            if (offset < buffer.Size)
            {
                ref var next = ref buffer.At(offset);
                Trace.Assert(next.Frame == requestedFrame);
                input = next;
                logger.Write(LogLevel.Trace,
                    $"Queue {queueId} => returning confirmed frame number {input.Frame.Number}.");
                return true;
            }

            // The requested frame isn't in the queue.
            // This means we need to return a prediction frame. Predict that the user will do the same thing they did last time.
            if (requestedFrame == 0)
            {
                logger.Write(LogLevel.Trace,
                    $"Queue {queueId} => basing new prediction frame from nothing, you're client wants frame 0.");
                prediction.Erase();
            }
            else if (lastAddedFrame.IsNull)
            {
                logger.Write(LogLevel.Trace,
                    $"Queue {queueId} => basing new prediction frame from nothing, since we have no frames yet.");
                prediction.Erase();
            }
            else
            {
                logger.Write(LogLevel.Trace,
                    $"Queue {queueId} => basing new prediction frame from previously added frame (queue entry:{buffer.CurrentIndex}, frame:{LastInput.Frame.Number})"
                );
                prediction = LastInput;
            }

            prediction.IncrementFrame();
        }

        Trace.Assert(prediction.Frame >= 0);
        // If we've made it this far, we must be predicting. Go ahead and forward the prediction frame contents.
        // Be sure to return the frame number requested by the client, though.
        input = prediction;
        input.Frame = requestedFrame;
        logger.Write(LogLevel.Trace,
            $"Queue {queueId} => returning prediction frame number {input.Frame.Number} ({prediction.Frame.Number}).");
        return false;
    }

    public void AddInput(ref GameInput<TInput> input)
    {
        logger.Write(LogLevel.Trace, $"Queue {queueId} => adding input frame number {input.Frame.Number} to queue.");
        // These next two lines simply verify that inputs are passed in sequentially by the user, regardless of frame delay.
        Trace.Assert(lastUserAddedFrame.IsNull || input.Frame == lastUserAddedFrame.Next());
        lastUserAddedFrame = input.Frame;
        // Move the queue head to the correct point in preparation to input the frame into the queue.
        var newFrame = AdvanceQueueHead(input.Frame);
        if (!newFrame.IsNull)
            AddDelayedInputToQueue(input, in newFrame);
        // Update the frame number for the input.  This will also set the frame to GameInput.NullFrame for frames that get dropped (by design).
        input.Frame = newFrame;
    }

    void AddDelayedInputToQueue(GameInput<TInput> input, in Frame inputFrame)
    {
        logger.Write(LogLevel.Trace,
            $"Queue {queueId} => adding delayed input frame number {inputFrame.Number} to queue.");
        Trace.Assert(lastAddedFrame.IsNull || inputFrame == lastAddedFrame.Next());
        Trace.Assert(inputFrame.Number is 0 || LastInput.Frame.Number == inputFrame.Previous().Number);

        input.Frame = inputFrame;
        buffer.Add(in input);
        firstFrame = false;
        lastAddedFrame = inputFrame;

        if (prediction.Frame.IsNull) return;

        Trace.Assert(inputFrame == prediction.Frame);
        // We've been predicting...  See if the inputs we've gotten match what we've been predicting.  If so, don't worry about it.
        // If not, remember the first input which was incorrect so we can report it in GetFirstIncorrectFrame()
        if (firstIncorrectFrame.IsNull && !inputComparer.Equals(prediction.Data, input.Data))
        {
            logger.Write(LogLevel.Debug,
                $"Queue {queueId} => frame {inputFrame} does not match prediction.  marking error.");
            firstIncorrectFrame = inputFrame;
        }

        // If this input is the same frame as the last one requested, and we still haven't found any mis-predicted inputs,
        // we can dump out of prediction mode entirely. Otherwise, advance the prediction frame count up.
        if (prediction.Frame.Number == lastFrameRequested.Number && firstIncorrectFrame.IsNull)
        {
            logger.Write(LogLevel.Debug,
                $"Queue {queueId} => prediction is correct!  dumping out of prediction mode.");
            prediction.ResetFrame();
        }
        else
            prediction.IncrementFrame();
    }

    Frame AdvanceQueueHead(Frame frame)
    {
        logger.Write(LogLevel.Trace, $"advancing queue head to frame {frame.Number}.");
        var expectedFrame = firstFrame ? Frame.Zero : LastInput.Frame.Next();
        frame += LocalFrameDelay;
        if (expectedFrame > frame)
        {
            // This can occur when the frame delay has dropped since the last time we shoved a frame into the system.
            // In this case, there's no room on the queue. Discard it.
            logger.Write(LogLevel.Information,
                $"Queue {queueId} => Dropping input frame {frame.Number} (expected next frame to be {expectedFrame.Number})");
            return Frame.Null;
        }

        while (expectedFrame < frame)
        {
            // This can occur when the frame delay has been increased since the last time we shoved a frame into the system.
            // We need to replicate the last frame in the queue several times in order to fill the space left.
            logger.Write(LogLevel.Information,
                $"Queue {queueId} => Adding padding frame {expectedFrame.Number} to account for change in frame delay");

            AddDelayedInputToQueue(LastInput, in expectedFrame);
            expectedFrame++;
        }

        Trace.Assert(frame == 0 || frame == LastInput.Frame.Next());
        return frame;
    }
}
