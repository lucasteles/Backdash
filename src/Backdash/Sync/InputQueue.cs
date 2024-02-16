using System.Diagnostics;
using Backdash.Core;
using Backdash.Data;

namespace Backdash.Sync;

sealed class InputQueue
{
    readonly Logger logger;
    readonly FrameArray<GameInput> inputs;

    int length;
    bool firstFrame;
    Frame head, tail;
    Frame firstIncorrectFrame;
    Frame lastUserAddedFrame, lastAddedFrame, lastFrameRequested;
    GameInput prediction;

    public int FrameDelay { get; set; }

    public InputQueue(int inputSize, int queueSize, Logger logger)
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

        inputs = new(queueSize);
        inputs.Fill(new(inputSize)
        {
            Frame = Frame.Zero,
        });
    }

    Frame PreviousFrame(in Frame offset) => offset == 0 ? new(inputs.Length - 1) : offset.Previous();

    public Frame GetFirstIncorrectFrame() => firstIncorrectFrame;

    public void DiscardConfirmedFrames(Frame frame)
    {
        Trace.Assert(frame >= Frame.Zero);

        if (lastFrameRequested.IsNotNull)
            frame = Frame.Min(in frame, in lastFrameRequested);

        logger.Write(LogLevel.Debug,
            $"discarding confirmed frames up to {frame} (last added:{lastAddedFrame} length:{length} [head:{head} tail:{tail}]).");

        if (frame >= lastAddedFrame)
            tail = head;
        else
        {
            var offset = frame.Number - inputs[in tail].Frame.Number + 1;

            logger.Write(LogLevel.Debug, $"difference of {offset} frames.");
            Trace.Assert(offset >= 0);

            tail = (tail + offset) % inputs.Length;
            length -= offset;
        }

        logger.Write(LogLevel.Debug, $"after discarding, new tail is {tail} (frame:{inputs[in tail].Frame}).");

        Trace.Assert(length >= 0);
    }

    public void ResetPrediction(in Frame frame)
    {
        Trace.Assert(firstIncorrectFrame.IsNull
                     || frame <= firstIncorrectFrame);

        logger.Write(LogLevel.Debug, $"resetting all prediction errors back to frame {frame}.");

        // There's nothing really to do other than reset our prediction
        // state and the incorrect frame counter...
        prediction.ResetFrame();
        firstIncorrectFrame = Frame.Null;
        lastFrameRequested = Frame.Null;
    }

    public bool GetConfirmedInput(in Frame requestedFrame, ref GameInput input)
    {
        Trace.Assert(firstIncorrectFrame.IsNull || requestedFrame < firstIncorrectFrame);
        var offset = requestedFrame % inputs.Length;
        if (inputs[in offset].Frame != requestedFrame)
            return false;

        input = inputs[in offset];
        return true;
    }

    public bool GetInput(Frame requestedFrame, out GameInput input)
    {
        logger.Write(LogLevel.Debug, $"requesting input frame {requestedFrame}.");

        // No one should ever try to grab any input when we have a prediction
        // error.  Doing so means that we're just going further down the wrong
        // path.  Trace.Assert this to verify that it's true.
        Trace.Assert(firstIncorrectFrame.IsNull);

        // Remember the last requested frame number for later.  We'll need
        // this in AddInput() to drop out of prediction mode.
        lastFrameRequested = requestedFrame;

        Trace.Assert(requestedFrame >= inputs[in tail].Frame);

        if (prediction.Frame.IsNull)
        {
            // If the frame requested is in our range, fetch it out of the queue and
            // return it.
            var offset = requestedFrame - inputs[in tail].Frame;

            if (offset < length)
            {
                offset = (offset + tail) % inputs.Length;
                Trace.Assert(inputs[in offset].Frame == requestedFrame);
                input = inputs[in offset];
                logger.Write(LogLevel.Debug, $"returning confirmed frame number {input.Frame}.");
                return true;
            }

            // The requested frame isn't in the queue.  Bummer.  This means we need
            // to return a prediction frame.  Predict that the user will do the
            // same thing they did last time.
            if (requestedFrame == 0)
            {
                logger.Write(LogLevel.Debug, "basing new prediction frame from nothing, you're client wants frame 0.");
                prediction.Erase();
            }
            else if (lastAddedFrame.IsNull)
            {
                logger.Write(LogLevel.Debug, "basing new prediction frame from nothing, since we have no frames yet.");
                prediction.Erase();
            }
            else
            {
                logger.Write(LogLevel.Debug,
                    $"basing new prediction frame from previously added frame (queue entry:{PreviousFrame(in head)}, frame:{inputs[PreviousFrame(in head)].Frame})"
                );

                prediction = inputs[PreviousFrame(in head)];
            }

            prediction.IncrementFrame();
        }

        Trace.Assert(prediction.Frame >= 0);

        // If we've made it this far, we must be predicting.  Go ahead and
        // forward the prediction frame contents.  Be sure to return the
        // frame number requested by the client, though.
        input = prediction;
        input.Frame = requestedFrame;
        logger.Write(LogLevel.Debug, $"returning prediction frame number {input.Frame} ({prediction.Frame}).");

        return false;
    }

    public void AddInput(ref GameInput input)
    {
        logger.Write(LogLevel.Debug, $"adding input frame number {input.Frame} to queue.");

        // These next two lines simply verify that inputs are passed in
        // sequentially by the user, regardless of frame delay.
        Trace.Assert(lastUserAddedFrame.IsNull || input.Frame == lastUserAddedFrame.Next());
        lastUserAddedFrame = input.Frame;

        // Move the queue head to the correct point in preparation to
        // input the frame into the queue.
        var newFrame = AdvanceQueueHead(input.Frame);
        if (newFrame.IsNotNull)
            AddDelayedInputToQueue(ref input, newFrame);

        // Update the frame number for the input.  This will also set the
        // frame to GameInput.NullFrame for frames that get dropped (by
        // design).
        input.Frame = newFrame;
    }

    void AddDelayedInputToQueue(ref GameInput input, Frame frameNumber)
    {
        logger.Write(LogLevel.Debug, $"adding delayed input frame number {frameNumber} to queue.");

        Trace.Assert(input.Size == prediction.Size);
        Trace.Assert(
            lastAddedFrame.IsNull || frameNumber == lastAddedFrame.Next());
        Trace.Assert(frameNumber == 0 || inputs[PreviousFrame(in head)].Frame == frameNumber.Previous());

        // Add the frame to the back of the queue
        inputs[in head] = input;
        inputs[in head].Frame = frameNumber;
        head = (head + 1) % inputs.Length;
        length++;
        firstFrame = false;
        lastAddedFrame = frameNumber;

        if (prediction.Frame.IsNotNull)
        {
            Trace.Assert(frameNumber == prediction.Frame);

            // We've been predicting...  See if the inputs we've gotten match
            // what we've been predicting.  If so, don't worry about it.  If not,
            // remember the first input which was incorrect so we can report it
            // in GetFirstIncorrectFrame()
            if (firstIncorrectFrame.IsNull && !Mem.SpanEqual<byte>(prediction.Buffer, input.Buffer, truncate: true))
            {
                logger.Write(LogLevel.Debug, $"frame {frameNumber} does not match prediction.  marking error.");
                firstIncorrectFrame = frameNumber;
            }

            // If this input is the same frame as the last one requested and we
            // still haven't found any mis-predicted inputs, we can dump out
            // of predition mode entirely!  Otherwise, advance the prediction frame
            // count up.
            if (prediction.Frame == lastFrameRequested &&
                firstIncorrectFrame.IsNull)
            {
                logger.Write(LogLevel.Debug, "prediction is correct!  dumping out of prediction mode.");
                prediction.ResetFrame();
            }
            else
            {
                prediction.IncrementFrame();
            }
        }

        Trace.Assert(length <= inputs.Length);
    }

    Frame AdvanceQueueHead(Frame frame)
    {
        logger.Write(LogLevel.Debug, $"advancing queue head to frame {frame}.");

        var expectedFrame = firstFrame ? Frame.Zero : inputs[PreviousFrame(in head)].Frame.Next();

        frame += FrameDelay;

        if (expectedFrame > frame)
        {
            // This can occur when the frame delay has dropped since the last
            // time we shoved a frame into the system.  In this case, there's
            // no room on the queue.  Toss it.
            logger.Write(LogLevel.Information,
                $"Dropping input frame {frame} (expected next frame to be {expectedFrame}).");
            return Frame.Null;
        }

        while (expectedFrame < frame)
        {
            // This can occur when the frame delay has been increased since the last
            // time we shoved a frame into the system.  We need to replicate the
            // last frame in the queue several times in order to fill the space
            // left.
            logger.Write(LogLevel.Information,
                $"Adding padding frame {expectedFrame} to account for change in frame delay.");
            ref var lastFrame = ref inputs[PreviousFrame(in head)];
            AddDelayedInputToQueue(ref lastFrame, expectedFrame);
            expectedFrame++;
        }

        Trace.Assert(frame == 0 || frame == inputs[PreviousFrame(in head)].Frame.Next());
        return frame;
    }
}
