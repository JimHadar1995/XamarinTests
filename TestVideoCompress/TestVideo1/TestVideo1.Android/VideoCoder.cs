using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Android.App;
using Android.Content;
using Android.Graphics;
using Android.Media;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using Java.IO;
using Java.Nio;
using MediaCodecHelper;
using TestVideo1.Droid.Helpers;
using Xamarin.Forms;
using static Android.Media.MediaCodec;

[assembly: Dependency(typeof(TestVideo.Droid.VideoCoder))]
namespace TestVideo.Droid
{
    public class VideoCoder : IVideoCoder
    {
        private bool _isBusy;
        public bool IsBusy { get { return _isBusy; }
            set {
                if(value != _isBusy)
                {
                    _isBusy = value;
                    
                    PropertyChanged(this, new PropertyChangedEventArgs(nameof(IsBusy)));
                }
            } }
        Android.Net.Uri mInputUri, 
                        mOutputUri;
        MediaCodec mDecoder;
        MediaCodec mEncoder;

        MediaExtractor mExtractor;

        MediaMuxer mMuxer;

        ByteBuffer[] mInputBuffers;
        ByteBuffer[] mOutputBuffers;

        BufferInfo mDecoderBufferInfo;
        BufferInfo mEncoderBufferInfo;

        int mInputWidth;
        int mInputHeight;

        int mBitRate = 2000000;

        List<string> mResolutions = new List<string>() { "1080P", "720P", "480P", "360P", "QVGA", "QCIF" };

        List<string> mBitRates = new List<string>() { "2Mbps", "1Mbps", "500Kbps", "56Kbps" };

        List<string> mFrameRates = new List<string>() { "30fps", "15fps" };

        List<string> mIFrameIntervals = new List<string>() { "1", "5", "10"};

        Resolution mOutputResolution;
        int mOutputBitRate = 512000;
        int mOutputFrameRate = 15;
        int mOutputIFrameInterval = 10;

        public VideoCoder()
        {
            IsBusy = false;   
        }        

        public event PropertyChangedEventHandler PropertyChanged;

        public string VideoCompress(string videoPath)
        {
            mOutputResolution = Resolution.RESOLUTION_480P;
            mOutputBitRate = 1048576;
            mOutputFrameRate = 30;
            mOutputIFrameInterval = 10;
            this.mInputUri = Android.Net.Uri.Parse(videoPath);
            mOutputUri = GetOutputUri(this.mInputUri.Path);
            VideoResampler resampler = new VideoResampler();
            resampler.AddSamplerClip(new SamplerClip(mInputUri));
            resampler.SetOutput(mOutputUri);
            resampler.SetOutputResolution(mOutputResolution.mWidth, mOutputResolution.mHeight);
            resampler.SetOutputBitRate(mOutputBitRate);
            resampler.SetOutputFrameRate(mOutputFrameRate);
            resampler.SetOutputIFrameInterval(mOutputIFrameInterval);

            try
            {
                this.IsBusy = true;
                resampler.Start(this);                                      
            }
            catch(Exception e)
            {

            }
            return mOutputUri.ToString();
        }

        private Android.Net.Uri GetOutputUri(string inputUri)
        {
            string filePath = inputUri.Substring(0, inputUri.LastIndexOf(Java.IO.File.Separator));
            string[] splitByDot = inputUri.Split('.');
            string ext = "";
            if (splitByDot != null && splitByDot.Length > 1)
                ext = splitByDot[splitByDot.Length - 1];
            string fileName = inputUri.Substring(inputUri.LastIndexOf(Java.IO.File.Separator) + 1,
                            inputUri.Length - inputUri.LastIndexOf(Java.IO.File.Separator) - 1);
            if (ext.Length > 0)
                fileName = fileName.Replace("." + ext, "_out." + ext);
            else
                fileName = string.Concat(fileName, "_out");

            File outFile = new File(filePath, fileName);
            if (!outFile.Exists())
                outFile.CreateNewFile();
            return Android.Net.Uri.Parse(outFile.AbsolutePath);
        }
    }

}