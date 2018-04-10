using System.Collections.Generic;
using System.Linq;
using System.Text;

using Android.App;
using Android.Content;
using Android.Graphics;
using Android.Media;
using Android.Net;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;

namespace TestVideo1.Droid.Helpers
{
    public class MediaHelper
    {

        public static string MIME_TYPE_AVC = "video/avc";

        public static Bitmap GetThumbnailFromVideo(Uri uri, long timeMs)
        {
            MediaMetadataRetriever retriever = new MediaMetadataRetriever();
            retriever.SetDataSource(uri.ToString());
            return retriever.GetFrameAtTime(timeMs * 1000);
        }

        public static int GetDuration(Android.Net.Uri uri)
        {
            return GetMediaMetadataRetrieverPropertyInteger(uri, (int)MetadataKey.Duration, 0);
        }

        public static int GetWidth(Uri uri)
        {
            return GetMediaMetadataRetrieverPropertyInteger(uri, (int)MetadataKey.VideoWidth, 0);
        }

        public static int GetHeight(Uri uri)
        {
            return GetMediaMetadataRetrieverPropertyInteger(uri, (int)MetadataKey.VideoHeight, 0);
        }

        public static int GetBitRate(Uri uri)
        {
            return GetMediaMetadataRetrieverPropertyInteger(uri, (int)MetadataKey.Bitrate, 0);
        }

        public static int GetRotation(Uri uri)
        {
            return GetMediaMetadataRetrieverPropertyInteger(uri, (int)MetadataKey.VideoRotation, 0);
        }

        public static int GetMediaMetadataRetrieverPropertyInteger(Android.Net.Uri uri, int key, int defaultValue)
        {
            MediaMetadataRetriever retriever = new MediaMetadataRetriever();
            retriever.SetDataSource(uri.ToString());
            string value = retriever.ExtractMetadata(key);

            if (value == null)
            {
                return defaultValue;
            }
            return int.Parse(value);
        }
        public static int GetIFrameInterval(Uri uri)
        {

            return GetMediaFormatPropertyInteger(uri,  MediaFormat.KeyIFrameInterval, -1);
        }

        public static int GetFrameRate(Uri uri)
        {

            return GetMediaFormatPropertyInteger(uri, MediaFormat.KeyFrameRate, -1);
        }

        public static int GetMediaFormatPropertyInteger(Uri uri, string key, int defaultValue)
        {
            int value = defaultValue;

            MediaExtractor extractor = new MediaExtractor();
            try
            {
                extractor.SetDataSource(uri.ToString());
            }
            catch (System.Exception e)
            {
                return value;
            }

            MediaFormat format = GetTrackFormat(extractor, MIME_TYPE_AVC);
            extractor.Release();

            if (format.ContainsKey(key))
            {
                value = format.GetInteger(key);
            }

            return value;
        }

        public static MediaFormat GetTrackFormat(MediaExtractor extractor, string mimeType)
        {
            for (int i = 0; i < extractor.TrackCount; i++)
            {
                MediaFormat format = extractor.GetTrackFormat(i);
                string trackMimeType = format.GetString(MediaFormat.KeyMime);
                if (mimeType.Equals(trackMimeType))
                {
                    return format;
                }
            }

            return null;
        }
    }
}