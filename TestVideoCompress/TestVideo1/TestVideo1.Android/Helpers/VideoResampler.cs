using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Acr.UserDialogs;
using Android.App;
using Android.Content;
using Android.Media;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using Java.Lang;
using Java.Nio;
using MediaCodecHelper;

namespace TestVideo1.Droid.Helpers
{

    /**
     * Wraps ResampleVideo, Running it in a new thread. Required because of the way 
     * SurfaceTexture.OnFrameAvailableListener works when the current thread has a Looper Configured.
     */
    internal class VideoEditWrapper
    {
        internal System.Exception exception;
        VideoResampler _videoResampler;
        internal VideoEditWrapper(VideoResampler videoResampler)
        {
            _videoResampler = videoResampler;
        }
        public void Run()
        {
            try
            {
                _videoResampler.ResampleVideo();
            }
            catch (System.Exception th)
            {
                exception = th;
            }
        }
    }
    public class VideoResampler 
    {
        public bool IsLoading { get; set; }
        private const int TIMEOUT_USEC = 10000;

        public const int WIDTH_QCIF = 176;
        public const int HEIGHT_QCIF = 144;
        public const int BITRATE_QCIF = 1000000;

        public const int WIDTH_QVGA = 320;
        public const int HEIGHT_QVGA = 240;
        public const int BITRATE_QVGA = 2000000;

        public const int WIDTH_720P = 1280;
        public const int HEIGHT_720P = 720;
        public const int BITRATE_720P = 6000000;

        private const string TAG = "VideoResampler";
        private const bool WORK_AROUND_BUGS = false; // avoid fatal codec bugs
        private const bool VERBOSE = true; // lots of logging

        // parameters for the encoder
        public const int FPS_30 = 30; // 30fps
        public const int FPS_15 = 15; // 15fps
        public const int IFRAME_INTERVAL_10 = 10; // 10 seconds between I-frames

        // size of a frame, in pixels
        private int mWidth = WIDTH_720P;
        private int mHeight = HEIGHT_720P;

        // bit rate, in bits per second
        private int mBitRate = BITRATE_720P;

        private int mFrameRate = FPS_15;

        private int mIFrameInterval = IFRAME_INTERVAL_10;

        // private Uri mInputUri;
        private Android.Net.Uri mOutputUri;

        InputSurface mInputSurface;

        OutputSurface mOutputSurface;

        MediaCodec mEncoder = null;

        MediaMuxer mMuxer = null;
        int mTrackIndex = -1;
        bool mMuxerStarted = false;

        // MediaExtractor mExtractor = null;

        // MediaFormat mExtractFormat = null;

        // int mExtractIndex = 0;

        List<SamplerClip> mClips = new List<SamplerClip>();

        // int mStartTime = -1;

        // int mEndTime = -1;

        long mLastSampleTime = 0;

        long mEncoderPresentationTimeUs = 0;

        public event PropertyChangedEventHandler PropertyChanged;

        public void OnPropertyChanged(string prop = "")
        {
            if (PropertyChanged != null)
                PropertyChanged(this, new PropertyChangedEventArgs(prop));
        }

        protected void SetPropertyChanged(object value, object property)
        {
            if (property != value)
            {
                property = value;
                OnPropertyChanged(nameof(property));
            }
        }

        public VideoResampler()
        {

        }

        /*
         * public void setInput( Uri intputUri ) { mInputUri = intputUri; }
         */

        public void AddSamplerClip(SamplerClip clip)
        {
            mClips.Add(clip);
        }

        public void SetOutput(Android.Net.Uri outputUri)
        {
            mOutputUri = outputUri;
        }

        public void SetOutputResolution(int width, int height)
        {
            if ((width % 16) != 0 || (height % 16) != 0)
            {
                System.Console.WriteLine($"{TAG} WARNING: width or height not multiple of 16");
            }
            mWidth = width;
            mHeight = height;
        }

        public void SetOutputBitRate(int bitRate)
        {
            mBitRate = bitRate;
        }

        public void SetOutputFrameRate(int frameRate)
        {
            mFrameRate = frameRate;
        }

        public void SetOutputIFrameInterval(int IFrameInterval)
        {
            mIFrameInterval = IFrameInterval;
        }

        /*
         * public void setStartTime( int startTime ) { mStartTime = startTime; }
         * 
         * public void setEndTime( int endTime ) { mEndTime = endTime; }
         */
        public void Start(TestVideo.Droid.VideoCoder videoCoder) {
            VideoEditWrapper wrapper = new VideoEditWrapper(this);
            Task.Run(() =>
            {
                videoCoder.IsBusy = true;
                wrapper.Run();
                videoCoder.IsBusy = false;
            });           

            if (wrapper.exception != null)
            {
                throw wrapper.exception;
            }                                 
        }

        private void SetupEncoder()
        {

            MediaFormat outputFormat = MediaFormat.CreateVideoFormat(MediaHelper.MIME_TYPE_AVC, mWidth, mHeight);
            outputFormat.SetInteger(MediaFormat.KeyColorFormat, (int)MediaCodecCapabilities.Formatsurface);
            outputFormat.SetInteger(MediaFormat.KeyBitRate, mBitRate);

            outputFormat.SetInteger(MediaFormat.KeyFrameRate, mFrameRate);
            outputFormat.SetInteger(MediaFormat.KeyIFrameInterval, mIFrameInterval);

            mEncoder = MediaCodec.CreateEncoderByType(MediaHelper.MIME_TYPE_AVC);
            mEncoder.Configure(outputFormat, null, null, MediaCodecConfigFlags.Encode);
            mInputSurface = new InputSurface(mEncoder.CreateInputSurface());
            mInputSurface.MakeCurrent();
            mEncoder.Start();
        }

        private void SetupMuxer()
        {

            try
            {
                mMuxer = new MediaMuxer(mOutputUri.ToString(), MuxerOutputType.Mpeg4);
            }
            catch (System.Exception ioe)
            {
                throw new System.Exception("MediaMuxer creation failed", ioe);
            }
        }

        internal void ResampleVideo()
        {

            SetupEncoder();
            SetupMuxer();

            foreach(SamplerClip clip in mClips)
            {
                FeedClipToEncoder(clip);
            }

            mEncoder.SignalEndOfInputStream();

            ReleaseOutputResources();
        }

        private void FeedClipToEncoder(SamplerClip clip)
        {

            mLastSampleTime = 0;

            MediaCodec decoder = null;

            MediaExtractor extractor = SetupExtractorForClip(clip);

            if (extractor == null)
            {
                return;
            }

            int trackIndex = GetVideoTrackIndex(extractor);
            extractor.SelectTrack(trackIndex);

            MediaFormat clipFormat = extractor.GetTrackFormat(trackIndex);

            if (clip.getStartTime() != -1)
            {
                extractor.SeekTo(clip.getStartTime() * 1000, MediaExtractorSeekTo.PreviousSync);
                clip.setStartTime(extractor.SampleTime / 1000);
            }

            try
            {
                decoder = MediaCodec.CreateDecoderByType(MediaHelper.MIME_TYPE_AVC);
                mOutputSurface = new OutputSurface();

                decoder.Configure(clipFormat, mOutputSurface.Surface, null, 0);
                decoder.Start();

                ResampleVideo(extractor, decoder, clip);

            }
            catch(System.Exception e)
            {

            }
            finally
            {

                if (mOutputSurface != null)
                {
                    mOutputSurface.Release();
                }
                if (decoder != null)
                {
                    decoder.Stop();
                    decoder.Release();
                }

                if (extractor != null)
                {
                    extractor.Release();
                    extractor = null;
                }
            }
        }

        private MediaExtractor SetupExtractorForClip(SamplerClip clip)
        {


            MediaExtractor extractor = new MediaExtractor();
            try
            {
                extractor.SetDataSource(clip.getUri().ToString());
            }
            catch (System.Exception e)
            {
                return null;
            }

            return extractor;
        }

        private int GetVideoTrackIndex(MediaExtractor extractor)
        {

            for (int trackIndex = 0; trackIndex < extractor.TrackCount; trackIndex++)
            {
                MediaFormat format = extractor.GetTrackFormat(trackIndex);

                string mime = format.GetString(MediaFormat.KeyMime);
                if (mime != null)
                {
                    if (mime.Equals("video/avc"))
                    {
                        return trackIndex;
                    }
                }
            }

            return -1;
        }

        private void ReleaseOutputResources()
        {

            if (mInputSurface != null)
            {
                mInputSurface.Release();
            }

            if (mEncoder != null)
            {
                mEncoder.Stop();
                mEncoder.Release();
            }

            if (mMuxer != null)
            {
                mMuxer.Stop();
                mMuxer.Release();
                mMuxer = null;
            }
        }

        private void ResampleVideo(MediaExtractor extractor, MediaCodec decoder, SamplerClip clip)
        {
            ByteBuffer[] decoderInputBuffers = decoder.GetInputBuffers();
            ByteBuffer[] encoderOutputBuffers = mEncoder.GetOutputBuffers();
            MediaCodec.BufferInfo info = new MediaCodec.BufferInfo();
            int inputChunk = 0;
            int outputCount = 0;

            long endTime = clip.getEndTime();

            if (endTime == -1)
            {
                endTime = clip.getVideoDuration();
            }

            bool outputDoneNextTimeWeCheck = false;

            bool outputDone = false;
            bool inputDone = false;
            bool decoderDone = false;

            while (!outputDone)
            {
                // Feed more data to the decoder.
                if (!inputDone)
                {
                    int inputBufIndex = decoder.DequeueInputBuffer(TIMEOUT_USEC);
                    if (inputBufIndex >= 0)
                    {
                        if (extractor.SampleTime / 1000 >= endTime)
                        {
                            // End of stream -- send empty frame with EOS flag set.
                            decoder.QueueInputBuffer(inputBufIndex, 0, 0, 0L, MediaCodecBufferFlags.EndOfStream);
                            inputDone = true;
                        }
                        else
                        {
                            // Copy a chunk of input to the decoder. The first chunk should have
                            // the BUFFER_FLAG_CODEC_CONFIG flag set.
                            ByteBuffer inputBuf = decoderInputBuffers[inputBufIndex];
                            inputBuf.Clear();

                            int sampleSize = extractor.ReadSampleData(inputBuf, 0);
                            if (sampleSize < 0)
                            {
                                decoder.QueueInputBuffer(inputBufIndex, 0, 0, 0, MediaCodecBufferFlags.EndOfStream);
                            }
                            else
                            {
                                decoder.QueueInputBuffer(inputBufIndex, 0, sampleSize, extractor.SampleTime, 0);
                                extractor.Advance();
                            }

                            inputChunk++;
                        }
                    }
                }

                // Assume output is available. Loop until both assumptions are false.
                bool decoderOutputAvailable = !decoderDone;
                bool encoderOutputAvailable = true;
                while (decoderOutputAvailable || encoderOutputAvailable)
                {
                    // Start by draining any pending output from the encoder. It's important to
                    // do this before we try to stuff any more data in.
                    int encoderStatus = mEncoder.DequeueOutputBuffer(info, TIMEOUT_USEC);
                    if (encoderStatus ==  (int)MediaCodecInfoState.TryAgainLater)
                    {
                        encoderOutputAvailable = false;
                    }
                    else if (encoderStatus == (int)MediaCodecInfoState.OutputBuffersChanged)
                    {
                        encoderOutputBuffers = mEncoder.GetOutputBuffers();
                    }
                    else if (encoderStatus == (int)MediaCodecInfoState.OutputFormatChanged)
                    {

                        MediaFormat newFormat = mEncoder.OutputFormat;

                        mTrackIndex = mMuxer.AddTrack(newFormat);
                        mMuxer.Start();
                        mMuxerStarted = true;
                    }
                    else if (encoderStatus < 0)
                    {
                        // fail( "unexpected result from encoder.dequeueOutputBuffer: " + encoderStatus );
                    }
                    else
                    { // encoderStatus >= 0
                        ByteBuffer encodedData = encoderOutputBuffers[encoderStatus];
                        if (encodedData == null)
                        {
                            // fail( "encoderOutputBuffer " + encoderStatus + " was null" );
                        }
                        // Write the data to the output "file".
                        if (info.Size != 0)
                        {
                            encodedData.Position(info.Offset);
                            encodedData.Limit(info.Offset + info.Size);
                            outputCount++;

                            mMuxer.WriteSampleData(mTrackIndex, encodedData, info);
                        }
                        outputDone = (info.Flags & MediaCodecBufferFlags.EndOfStream) != 0;

                        mEncoder.ReleaseOutputBuffer(encoderStatus, false);
                    }

                    if (outputDoneNextTimeWeCheck)
                    {
                        outputDone = true;
                    }

                    if (encoderStatus != (int)MediaCodecInfoState.TryAgainLater)
                    {
                        // Continue attempts to drain output.
                        continue;
                    }
                    // Encoder is drained, check to see if we've got a new frame of output from
                    // the decoder. (The output is going to a Surface, rather than a ByteBuffer,
                    // but we still get information through BufferInfo.)
                    if (!decoderDone)
                    {
                        int decoderStatus = decoder.DequeueOutputBuffer(info, TIMEOUT_USEC);
                        if (decoderStatus == (int)MediaCodecInfoState.TryAgainLater)
                        {
                            decoderOutputAvailable = false;
                        }
                        else if (decoderStatus == (int)MediaCodecInfoState.OutputBuffersChanged)
                        {
                            // decoderOutputBuffers = decoder.GetOutputBuffers();
                        }
                        else if (decoderStatus == (int)MediaCodecInfoState.OutputFormatChanged)
                        {
                            // expected before first buffer of data
                            MediaFormat newFormat = decoder.OutputFormat;
                        }
                        else if (decoderStatus < 0)
                        {
                            // fail( "unexpected result from decoder.dequeueOutputBuffer: " + decoderStatus );
                        }
                        else
                        { // decoderStatus >= 0

                            // The ByteBuffers are null references, but we still get a nonzero
                            // size for the decoded data.
                            bool doRender = (info.Size != 0);
                            // As soon as we call ReleaseOutputBuffer, the buffer will be forwarded
                            // to SurfaceTexture to convert to a texture. The API doesn't
                            // guarantee that the texture will be available before the call
                            // returns, so we need to wait for the onFrameAvailable callback to
                            // fire. If we don't wait, we risk rendering from the previous frame.
                            decoder.ReleaseOutputBuffer(decoderStatus, doRender);
                            if (doRender)
                            {
                                mOutputSurface.AwaitNewImage(true);
                                mOutputSurface.DrawImage();
                                // Send it to the encoder.

                                long nSecs = info.PresentationTimeUs * 1000;

                                if (clip.getStartTime() != -1)
                                {
                                    nSecs = (info.PresentationTimeUs - (clip.getStartTime() * 1000)) * 1000;
                                }

                                nSecs = Java.Lang.Math.Max(0, nSecs);

                                mEncoderPresentationTimeUs += (nSecs - mLastSampleTime);

                                mLastSampleTime = nSecs;

                                mInputSurface.SetPresentationTime(mEncoderPresentationTimeUs);
                                mInputSurface.SwapBuffers();
                            }
                            if ((info.Flags & MediaCodecBufferFlags.EndOfStream) != 0)
                            {
                                // mEncoder.signalEndOfInputStream();
                                outputDoneNextTimeWeCheck = true;
                            }
                        }
                    }
                }
            }
            if (inputChunk != outputCount)
            {
                // throw new RuntimeException( "frame lost: " + inputChunk + " in, " + outputCount + " out" );
            }
        }

    }
}