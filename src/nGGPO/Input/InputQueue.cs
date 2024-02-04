using nGGPO.Core;
using nGGPO.Data;

namespace nGGPO.Input;

sealed class InputQueue
{
    readonly ILogger logger;
    int length;
    bool firstFrame;
    Frame head, tail;

    Frame lastUserAddedFrame, lastAddedFrame, firstIncorrectFrame;
    Frame lastFrameRequested;

    public int FrameDelay { get; set; }

    readonly FrameArray<GameInput> inputs;
    GameInput prediction;

    Frame PreviousFrame(in Frame offset) => offset == 0 ? new(inputs.Length - 1) : offset.Previous;

    public InputQueue(int inputSize, int queueSize, ILogger logger)
    {
        this.logger = logger;
        length = FrameDelay = 0;
        head = tail = Frame.Zero;
        firstFrame = true;
        lastUserAddedFrame = Frame.Null;
        firstIncorrectFrame = Frame.Null;
        lastFrameRequested = Frame.Null;
        lastAddedFrame = Frame.Null;

        prediction = new(inputSize);

        // This is safe because we know the GameInput is a proper structure (as in,
        // no virtual methods, no contained classes, etc.).
        inputs = new(queueSize);
        inputs.Fill(new(inputSize));
    }

    public Frame GetLastConfirmedFrame()
    {
        logger.Info($"returning last confirmed frame {lastAddedFrame}.");
        return lastAddedFrame;
    }

    public Frame GetFirstIncorrectFrame() => firstIncorrectFrame;

    public void DiscardConfirmedFrames(Frame frame)
    {
        Tracer.Assert(frame >= Frame.Zero);

        if (lastFrameRequested.IsValid)
            frame = Frame.Min(in frame, in lastFrameRequested);

        logger.Info(
            $"discarding confirmed frames up to {frame} (last_added:{lastAddedFrame} length:{length} [head:{head} tail:{tail}]).");

        if (frame >= lastAddedFrame)
            tail = head;
        else
        {
            var offset = frame - inputs[in tail].Frame.Next;

            logger.Info($"difference of {offset} frames.");
            Tracer.Assert(offset >= Frame.Zero);

            tail = (tail + offset) % inputs.Length;
            length -= offset.Number;
        }

        logger.Info($"after discarding, new tail is {tail} (frame:{inputs[in tail].Frame}).");

        Tracer.Assert(length >= 0);
    }

    public void ResetPrediction(Frame frame)
    {
        Tracer.Assert(firstIncorrectFrame.IsNull
                      || frame <= firstIncorrectFrame);

        logger.Info($"resetting all prediction errors back to frame {frame}.");

        // There's nothing really to do other than reset our prediction
        // state and the incorrect frame counter...
        prediction.ResetFrame();
        firstIncorrectFrame = Frame.Null;
        lastFrameRequested = Frame.Null;
    }

    public bool GetConfirmedInput(Frame requestedFrame, ref GameInput input)
    {
        Tracer.Assert(firstIncorrectFrame.IsNull ||
                      requestedFrame < firstIncorrectFrame);
        var offset = requestedFrame % inputs.Length;
        if (inputs[in offset].Frame != requestedFrame) return false;
        input = inputs[in offset];
        return true;
    }

    public bool GetInput(Frame requestedFrame, out GameInput input)
    {
        logger.Info($"requesting input frame {requestedFrame}.");

        // No one should ever try to grab any input when we have a prediction
        // error.  Doing so means that we're just going further down the wrong
        // path.  Tracer.Assert this to verify that it's true.
        Tracer.Assert(firstIncorrectFrame.IsNull);

        // Remember the last requested frame number for later.  We'll need
        // this in AddInput() to drop out of prediction mode.
        lastFrameRequested = requestedFrame;

        Tracer.Assert(requestedFrame >= inputs[in tail].Frame);

        if (prediction.Frame.IsNull)
        {
            // If the frame requested is in our range, fetch it out of the queue and
            // return it.
            var offset = requestedFrame - inputs[in tail].Frame;

            if (offset < length)
            {
                offset = (offset + tail) % inputs.Length;
                Tracer.Assert(inputs[in offset].Frame == requestedFrame);
                input = inputs[in offset];
                logger.Info($"returning confirmed frame number {input.Frame}.");
                return true;
            }

            // The requested frame isn't in the queue.  Bummer.  This means we need
            // to return a prediction frame.  Predict that the user will do the
            // same thing they did last time.
            if (requestedFrame == 0)
            {
                logger.Trace($"basing new prediction frame from nothing, you're client wants frame 0.");
                prediction.Clear();
            }
            else if (lastAddedFrame.IsNull)
            {
                logger.Trace($"basing new prediction frame from nothing, since we have no frames yet.");
                prediction.Clear();
            }
            else
            {
                logger.Info(
                    $"basing new prediction frame from previously added frame (queue entry:{PreviousFrame(in head)}, frame:{inputs[PreviousFrame(in head)].Frame})"
                );

                prediction = inputs[PreviousFrame(in head)];
            }

            prediction.IncrementFrame();
        }

        Tracer.Assert(prediction.Frame >= 0);

        // If we've made it this far, we must be predicting.  Go ahead and
        // forward the prediction frame contents.  Be sure to return the
        // frame number requested by the client, though.
        input = prediction;
        input.Frame = requestedFrame;
        logger.Info($"returning prediction frame number {input.Frame} ({prediction.Frame}).");

        return false;
    }

    public void AddInput(ref GameInput input)
    {
        logger.Info($"adding input frame number {input.Frame} to queue.");

        // These next two lines simply verify that inputs are passed in
        // sequentially by the user, regardless of frame delay.
        Tracer.Assert(
            lastUserAddedFrame.IsNull
            || input.Frame == lastUserAddedFrame.Next
        );
        lastUserAddedFrame = input.Frame;

        // Move the queue head to the correct point in preparation to
        // input the frame into the queue.
        var newFrame = AdvanceQueueHead(input.Frame);
        if (newFrame.IsNull)
            AddDelayedInputToQueue(ref input, newFrame);

        // Update the frame number for the input.  This will also set the
        // frame to GameInput.NullFrame for frames that get dropped (by
        // design).
        input.Frame = newFrame;
    }

    void AddDelayedInputToQueue(ref GameInput input, Frame frameNumber)
    {
        logger.Info($"adding delayed input frame number {frameNumber} to queue.");

        Tracer.Assert(input.Size == prediction.Size);
        Tracer.Assert(
            lastAddedFrame.IsNull || frameNumber == lastAddedFrame.Next);
        Tracer.Assert(frameNumber == 0 || inputs[PreviousFrame(in head)].Frame == frameNumber - 1);

        // Add the frame to the back of the queue
        inputs[in head] = input;
        inputs[in head].Frame = frameNumber;
        head = (head + 1) % inputs.Length;
        length++;
        firstFrame = false;
        lastAddedFrame = frameNumber;

        if (prediction.Frame.IsValid)
        {
            Tracer.Assert(frameNumber == prediction.Frame);

            // We've been predicting...  See if the inputs we've gotten match
            // what we've been predicting.  If so, don't worry about it.  If not,
            // remember the first input which was incorrect so we can report it
            // in GetFirstIncorrectFrame()
            if (firstIncorrectFrame.IsNull && !prediction.Equals(input, true, logger))
            {
                logger.Info($"frame {frameNumber} does not match prediction.  marking error.");
                firstIncorrectFrame = frameNumber;
            }

            // If this input is the same frame as the last one requested and we
            // still haven't found any mis-predicted inputs, we can dump out
            // of predition mode entirely!  Otherwise, advance the prediction frame
            // count up.
            if (prediction.Frame == lastFrameRequested &&
                firstIncorrectFrame.IsNull)
            {
                logger.Trace($"prediction is correct!  dumping out of prediction mode.");
                prediction.ResetFrame();
            }
            else
            {
                prediction.IncrementFrame();
            }
        }

        Tracer.Assert(length <= inputs.Length);
    }

    Frame AdvanceQueueHead(Frame frame)
    {
        logger.Info($"advancing queue head to frame {frame}.");

        var expectedFrame = firstFrame ? Frame.Zero : inputs[PreviousFrame(in head)].Frame.Next;

        frame += FrameDelay;

        if (expectedFrame > frame)
        {
            // This can occur when the frame delay has dropped since the last
            // time we shoved a frame into the system.  In this case, there's
            // no room on the queue.  Toss it.
            logger.Info($"Dropping input frame {frame} (expected next frame to be {expectedFrame}).");
            return Frame.Null;
        }

        while (expectedFrame < frame)
        {
            // This can occur when the frame delay has been increased since the last
            // time we shoved a frame into the system.  We need to replicate the
            // last frame in the queue several times in order to fill the space
            // left.
            logger.Info($"Adding padding frame {expectedFrame} to account for change in frame delay.");
            ref var lastFrame = ref inputs[PreviousFrame(in head)];
            AddDelayedInputToQueue(ref lastFrame, expectedFrame);
            expectedFrame++;
        }

        Tracer.Assert(frame == 0 || frame == inputs[PreviousFrame(in head)].Frame.Next);
        return frame;
    }
}
