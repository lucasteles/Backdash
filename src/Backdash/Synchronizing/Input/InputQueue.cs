using Backdash.Core;
using Backdash.Data;

namespace Backdash.Synchronizing.Input;

sealed class InputQueue<TInput> where TInput : unmanaged
{
    public readonly int QueueId;
    readonly EqualityComparer<TInput> inputComparer;
    readonly Logger logger;

    bool firstFrame;
    Frame firstIncorrectFrame;
    Frame lastUserAddedFrame, lastAddedFrame, lastFrameRequested;
    GameInput<TInput> prediction;
    public int LocalFrameDelay { get; internal set; }

    readonly CircularBuffer<GameInput<TInput>> inputs;

    public InputQueue(int queueId, int queueSize, Logger logger, EqualityComparer<TInput>? inputComparer = null)
    {
        this.logger = logger;
        this.inputComparer = inputComparer ?? EqualityComparer<TInput>.Default;
        QueueId = queueId;
        LocalFrameDelay = 0;
        firstFrame = true;
        lastUserAddedFrame = Frame.Null;
        firstIncorrectFrame = Frame.Null;
        lastFrameRequested = Frame.Null;
        lastAddedFrame = Frame.Null;
        prediction = new();
        inputs = new(queueSize);
        inputs.Clear();
        inputs.Fill(new(Frame.Zero));
    }

    ref GameInput<TInput> LastInput => ref inputs.Front();
    ref GameInput<TInput> FirstInput => ref inputs.Back();

    public Frame FirstIncorrectFrame => firstIncorrectFrame;

    public void DiscardConfirmedFrames(Frame frame)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(frame.Number);

        if (!lastFrameRequested.IsNull)
            frame = Frame.Min(in frame, in lastFrameRequested);
        logger.Write(LogLevel.Trace,
            $"Queue {QueueId} => discarding confirmed frames up to {frame} (last add:{lastAddedFrame.Number} len:{inputs.Size} front:{FirstInput.Frame.Number} back:{LastInput.Frame.Number})");
        if (frame.Number >= lastAddedFrame.Number)
            inputs.Clear();
        else
        {
            var offset = frame.Number - FirstInput.Frame.Number + 1;
            logger.Write(LogLevel.Trace, $"Queue {QueueId} => difference of {offset} frames.");
            inputs.Discard(offset);
        }

        logger.Write(LogLevel.Trace,
            $"Queue {QueueId} => after discarding, new back is {LastInput.Frame.Number} (front:{FirstInput.Frame.Number})."
        );
    }

    public void ResetPrediction(in Frame frame)
    {
        ThrowIf.Assert(firstIncorrectFrame.IsNull || frame.Number <= firstIncorrectFrame.Number);
        logger.Write(LogLevel.Trace,
            $"Queue {QueueId} => resetting all prediction errors back to frame {frame.Number}.");
        // There's nothing really to do other than reset our prediction state and the incorrect frame counter...
        prediction.ResetFrame();
        firstIncorrectFrame = Frame.Null;
        lastFrameRequested = Frame.Null;
    }

    public void DiscardInputsAfter(in Frame frame)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(frame.Number);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(frame.Number, lastUserAddedFrame.Number);

        var offset = lastUserAddedFrame.Number - frame.Number;
        if (offset < 0) return;
        logger.Write(LogLevel.Debug, $"Queue {QueueId} => dropping last {offset} frames.");
        inputs.Advance(-offset);
        lastUserAddedFrame = frame;
        lastFrameRequested = frame;
        lastAddedFrame = frame + LocalFrameDelay;
    }

    public bool GetConfirmedInput(in Frame requestedFrame, ref GameInput<TInput> input)
    {
        ThrowIf.Assert(firstIncorrectFrame.IsNull || requestedFrame.Number < firstIncorrectFrame.Number);

        ref var requested = ref inputs.AtRaw(requestedFrame.Number);

        if (requested.Frame.Number != requestedFrame.Number)
            return false;

        input = requested;
        return true;
    }

    public bool GetInput(in Frame requestedFrame, out GameInput<TInput> input)
    {
        logger.Write(LogLevel.Trace, $"Queue {QueueId} => requesting input frame {requestedFrame.Number}.");
        // No one should ever try to grab any input when we have a prediction error.
        // Doing so means that we're just going further down the wrong path.
        ThrowIf.Assert(firstIncorrectFrame.IsNull);
        // Remember the last requested frame number for later. We'll need this in AddInput() to drop out of prediction mode.
        lastFrameRequested = requestedFrame;
        ThrowIf.Assert(requestedFrame.Number >= FirstInput.Frame.Number);
        if (prediction.Frame.IsNull)
        {
            // If the frame requested is in our range, fetch it out of the queue and  return it.
            var offset = requestedFrame.Number - FirstInput.Frame.Number;
            if (offset < inputs.Size)
            {
                ref var next = ref inputs.At(offset);
                ThrowIf.Assert(next.Frame == requestedFrame);
                input = next;
                logger.Write(LogLevel.Trace,
                    $"Queue {QueueId} => returning confirmed frame number {input.Frame.Number}.");
                return true;
            }

            // The requested frame isn't in the queue.
            // This means we need to return a prediction frame. Predict that the user will do the same thing they did last time.
            if (requestedFrame == 0)
            {
                logger.Write(LogLevel.Trace,
                    $"Queue {QueueId} => basing new prediction frame from nothing, you're client wants frame 0.");
                prediction.Erase();
            }
            else if (lastAddedFrame.IsNull)
            {
                logger.Write(LogLevel.Trace,
                    $"Queue {QueueId} => basing new prediction frame from nothing, since we have no frames yet.");
                prediction.Erase();
            }
            else
            {
                logger.Write(LogLevel.Trace,
                    $"Queue {QueueId} => basing new prediction frame from previously added frame (queue entry:{inputs.CurrentIndex}, frame:{LastInput.Frame.Number})"
                );
                prediction = LastInput;
            }

            prediction.IncrementFrame();
        }

        ThrowIf.Assert(prediction.Frame.Number >= 0);
        // If we've made it this far, we must be predicting. Go ahead and forward the prediction frame contents.
        // Be sure to return the frame number requested by the client, though.
        input = prediction;
        input.Frame = requestedFrame;
        logger.Write(LogLevel.Trace,
            $"Queue {QueueId} => returning prediction frame number {input.Frame.Number} ({prediction.Frame.Number}).");
        return false;
    }

    public void AddInput(ref GameInput<TInput> input)
    {
        logger.Write(LogLevel.Trace, $"Queue {QueueId} => adding input frame number {input.Frame.Number} to queue.");
        // These next two lines simply verify that inputs are passed in sequentially by the user, regardless of frame delay.
        ThrowIf.Assert(lastUserAddedFrame.IsNull || input.Frame.Number == lastUserAddedFrame.Next().Number);
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
            $"Queue {QueueId} => adding delayed input frame number {inputFrame.Number} to queue.");
        ThrowIf.Assert(lastAddedFrame.IsNull || inputFrame == lastAddedFrame.Next());
        ThrowIf.Assert(inputFrame.Number is 0 || LastInput.Frame.Number == inputFrame.Previous().Number);

        input.Frame = inputFrame;
        inputs.Add(in input);
        firstFrame = false;
        lastAddedFrame = inputFrame;

        if (prediction.Frame.IsNull) return;

        ThrowIf.Assert(inputFrame == prediction.Frame);
        // We've been predicting...  See if the inputs we've gotten match what we've been predicting.  If so, don't worry about it.
        // If not, remember the first input which was incorrect so we can report it in GetFirstIncorrectFrame()
        if (firstIncorrectFrame.IsNull && !inputComparer.Equals(prediction.Data, input.Data))
        {
            logger.Write(LogLevel.Debug,
                $"Queue {QueueId} => frame {inputFrame} does not match prediction.  marking error.");
            firstIncorrectFrame = inputFrame;
        }

        // If this input is the same frame as the last one requested, and we still haven't found any mis-predicted inputs,
        // we can dump out of prediction mode entirely. Otherwise, advance the prediction frame count up.
        if (prediction.Frame.Number == lastFrameRequested.Number && firstIncorrectFrame.IsNull)
        {
            logger.Write(LogLevel.Debug,
                $"Queue {QueueId} => prediction is correct!  dumping out of prediction mode.");
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
                $"Queue {QueueId} => Dropping input frame {frame.Number} (expected next frame to be {expectedFrame.Number})");
            return Frame.Null;
        }

        while (expectedFrame < frame)
        {
            // This can occur when the frame delay has been increased since the last time we shoved a frame into the system.
            // We need to replicate the last frame in the queue several times in order to fill the space left.
            logger.Write(LogLevel.Information,
                $"Queue {QueueId} => Adding padding frame {expectedFrame.Number} to account for change in frame delay");

            AddDelayedInputToQueue(LastInput, in expectedFrame);
            expectedFrame++;
        }

        ThrowIf.Assert(frame == 0 || frame == LastInput.Frame.Next());
        return frame;
    }
}
