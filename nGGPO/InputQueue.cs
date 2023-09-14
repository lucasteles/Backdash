using System;
using System.Diagnostics;

namespace nGGPO;

class InputQueue : IDisposable
{
    readonly int inputSize;
    int head, tail, length;
    bool firstFrame;

    Frame lastUserAddedFrame, lastAddedFrame, firstIncorrectFrame;
    Frame lastFrameRequested;

    public int FrameDelay { get; set; }

    readonly GameInput[] inputs;
    GameInput prediction;

    int PreviousFrame(int offset) => offset == 0 ? inputs.Length - 1 : offset - 1;

    public InputQueue(int queueSize, int inputSize)
    {
        head = tail = length = FrameDelay = 0;
        firstFrame = true;
        this.inputSize = inputSize;
        lastUserAddedFrame = Frame.Null;
        firstIncorrectFrame = Frame.Null;
        lastFrameRequested = Frame.Null;
        lastAddedFrame = Frame.Null;

        prediction = GameInput.Empty;

        // This is safe because we know the GameInput is a proper structure (as in,
        // no virtual methods, no contained classes, etc.).
        inputs = new GameInput[queueSize];
        for (var i = 0; i < inputs.Length; i++)
            inputs[i] = GameInput.Empty;
    }

    public int GetLastConfirmedFrame()
    {
        Logger.Info("returning last confirmed frame {0}.", lastAddedFrame);
        return lastAddedFrame;
    }

    public int GetFirstIncorrectFrame() => firstIncorrectFrame;

    public void DiscardConfirmedFrames(int frame)
    {
        Trace.Assert(frame >= 0);

        if (lastFrameRequested.IsValid)
            frame = Math.Min(frame, lastFrameRequested);

        Logger.Info(
            "discarding confirmed frames up to {0} (last_added:{1} length:{2} [head:{3} tail:{4}]).",
            frame, lastAddedFrame, length, head, tail);

        if (frame >= lastAddedFrame)
            tail = head;
        else
        {
            var offset = frame - inputs[tail].Frame.Next;

            Logger.Info("difference of {} frames.", offset);
            Trace.Assert(offset >= 0);

            tail = (tail + offset) % inputs.Length;
            length -= offset;
        }

        Logger.Info("after discarding, new tail is {} (frame:{}).",
            tail, inputs[tail].Frame);

        Trace.Assert(length >= 0);
    }

    public void ResetPrediction(Frame frame)
    {
        Trace.Assert(firstIncorrectFrame.IsNull
                     || frame <= firstIncorrectFrame);

        Logger.Info("resetting all prediction errors back to frame {}.", frame);

        // There's nothing really to do other than reset our prediction
        // state and the incorrect frame counter...
        prediction.ResetFrame();
        firstIncorrectFrame = Frame.Null;
        lastFrameRequested = Frame.Null;
    }

    public bool GetConfirmedInput(Frame requestedFrame, ref GameInput input)
    {
        Trace.Assert(firstIncorrectFrame.IsNull ||
                     requestedFrame < firstIncorrectFrame);
        var offset = requestedFrame % inputs.Length;
        if (inputs[offset].Frame != requestedFrame) return false;
        input = inputs[offset];
        return true;
    }

    public bool GetInput(Frame requestedFrame, out GameInput input)
    {
        Logger.Info("requesting input frame {}.", requestedFrame);

        // No one should ever try to grab any input when we have a prediction
        // error.  Doing so means that we're just going further down the wrong
        // path.  Trace.Assert this to verify that it's true.
        Trace.Assert(firstIncorrectFrame.IsNull);

        // Remember the last requested frame number for later.  We'll need
        // this in AddInput() to drop out of prediction mode.
        lastFrameRequested = requestedFrame;

        Trace.Assert(requestedFrame >= inputs[tail].Frame);

        if (prediction.Frame.IsNull)
        {
            // If the frame requested is in our range, fetch it out of the queue and
            // return it.
            var offset = requestedFrame - inputs[tail].Frame;

            if (offset < length)
            {
                offset = (offset + tail) % inputs.Length;
                Trace.Assert(inputs[offset].Frame == requestedFrame);
                input = inputs[offset];
                Logger.Info("returning confirmed frame number {}.", input.Frame);
                return true;
            }

            // The requested frame isn't in the queue.  Bummer.  This means we need
            // to return a prediction frame.  Predict that the user will do the
            // same thing they did last time.
            if (requestedFrame == 0)
            {
                Logger.Debug(
                    "basing new prediction frame from nothing, you're client wants frame 0.");
                prediction.Clear();
            }
            else if (lastAddedFrame.IsNull)
            {
                Logger.Debug(
                    "basing new prediction frame from nothing, since we have no frames yet.");
                prediction.Clear();
            }
            else
            {
                Logger.Info(
                    "basing new prediction frame from previously added frame (queue entry:{}, frame:{}).",
                    PreviousFrame(head), inputs[PreviousFrame(head)].Frame);

                prediction.Dispose();
                prediction = inputs[PreviousFrame(head)];
            }

            prediction.IncrementFrame();
        }

        Trace.Assert(prediction.Frame >= 0);

        // If we've made it this far, we must be predicting.  Go ahead and
        // forward the prediction frame contents.  Be sure to return the
        // frame number requested by the client, though.
        input = prediction;
        input.SetFrame(requestedFrame);
        Logger.Info("returning prediction frame number {} ({}).", input.Frame,
            prediction.Frame);

        return false;
    }

    public void AddInput(ref GameInput input)
    {
        Logger.Info("adding input frame number {} to queue.", input.Frame);

        // These next two lines simply verify that inputs are passed in
        // sequentially by the user, regardless of frame delay.
        Trace.Assert(
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
        input.SetFrame(newFrame);
    }

    void AddDelayedInputToQueue(ref GameInput input, Frame frameNumber)
    {
        Logger.Info("adding delayed input frame number {} to queue.", frameNumber);

        Trace.Assert(input.Size == prediction.Size);
        Trace.Assert(
            lastAddedFrame.IsNull || frameNumber == lastAddedFrame.Next);
        Trace.Assert(frameNumber == 0 || inputs[PreviousFrame(head)].Frame == frameNumber - 1);

        // Add the frame to the back of the queue
        inputs[head] = input;
        inputs[head].SetFrame(frameNumber);
        head = (head + 1) % inputs.Length;
        length++;
        firstFrame = false;
        lastAddedFrame = frameNumber;

        if (prediction.Frame.IsValid)
        {
            Trace.Assert(frameNumber == prediction.Frame);

            // We've been predicting...  See if the inputs we've gotten match
            // what we've been predicting.  If so, don't worry about it.  If not,
            // remember the first input which was incorrect so we can report it
            // in GetFirstIncorrectFrame()
            if (firstIncorrectFrame.IsNull && !prediction.Equals(input, true))
            {
                Logger.Info("frame {} does not match prediction.  marking error.", frameNumber);
                firstIncorrectFrame = frameNumber;
            }

            // If this input is the same frame as the last one requested and we
            // still haven't found any mis-predicted inputs, we can dump out
            // of predition mode entirely!  Otherwise, advance the prediction frame
            // count up.
            if (prediction.Frame == lastFrameRequested &&
                firstIncorrectFrame.IsNull)
            {
                Logger.Debug("prediction is correct!  dumping out of prediction mode.");
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
        Logger.Info("advancing queue head to frame {}.", frame);

        var expectedFrame = firstFrame ? Frame.Zero : inputs[PreviousFrame(head)].Frame.Next;

        frame += FrameDelay;

        if (expectedFrame > frame)
        {
            // This can occur when the frame delay has dropped since the last
            // time we shoved a frame into the system.  In this case, there's
            // no room on the queue.  Toss it.
            Logger.Info("Dropping input frame {} (expected next frame to be {}).",
                frame, expectedFrame);
            return Frame.Null;
        }

        while (expectedFrame < frame)
        {
            // This can occur when the frame delay has been increased since the last
            // time we shoved a frame into the system.  We need to replicate the
            // last frame in the queue several times in order to fill the space
            // left.
            Logger.Info("Adding padding frame {} to account for change in frame delay.",
                expectedFrame);
            ref var lastFrame = ref inputs[PreviousFrame(head)];
            AddDelayedInputToQueue(ref lastFrame, expectedFrame);
            expectedFrame++;
        }

        Trace.Assert(frame == 0 || frame == inputs[PreviousFrame(head)].Frame + 1);
        return frame;
    }

    public void Dispose()
    {
        prediction.Dispose();
        foreach (var input in inputs)
            input.Dispose();
    }
}