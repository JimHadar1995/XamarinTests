using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;

namespace TestVideo1.Droid.Helpers
{
    public class Resolution
    {

        public static  Resolution RESOLUTION_1080P = new Resolution( 1920, 1080 );
        public static  Resolution RESOLUTION_720P = new Resolution( 1280, 720 );
        public static  Resolution RESOLUTION_480P = new Resolution( 740, 480 );
        public static  Resolution RESOLUTION_360P = new Resolution( 640, 360 );
        public static  Resolution RESOLUTION_QVGA = new Resolution( 320, 240 );
        public static  Resolution RESOLUTION_QCIF = new Resolution( 176, 144 );

        public int mWidth { get; private set; }
        public int mHeight { get; private set; }

        public Resolution(int width, int height)
        {
            mWidth = width;
            mHeight = height;
        }

        public Resolution rotate()
        {
            return new Resolution(mHeight, mWidth);
        }

    }
}