using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text;
using System.Threading.Tasks;

namespace TestVideo
{
    interface IVideoCoder : INotifyPropertyChanged
    {
        bool IsBusy { get; set; }
        string VideoCompress(string videoPath);
    }
}
