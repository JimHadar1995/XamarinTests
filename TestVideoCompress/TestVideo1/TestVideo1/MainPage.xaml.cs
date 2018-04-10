using Acr.UserDialogs;
using Plugin.FilePicker;
using Plugin.FilePicker.Abstractions;
using Plugin.MediaManager;
using Plugin.MediaManager.Abstractions.Enums;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TestVideo;
using Xamarin.Forms;

namespace TestVideo1
{
	public partial class MainPage : ContentPage
	{
        IVideoCoder videoCoder;
        public MainPage()
        {
            videoCoder = DependencyService.Get<IVideoCoder>();
            InitializeComponent();
            this.BindingContext = videoCoder;
        }

        private async void Button_Clicked (object sender, EventArgs e) {
            string path = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
            try
            {
                FileData fileData = await CrossFilePicker.Current.PickFile();
                string fileName = fileData.FileName;
                string filePath = fileData.FilePath;
                
                //await CrossMediaManager.Current.Play(Path.Combine(fileData.FilePath), MediaFileType.Video, ResourceAvailability.Local);
                string s = videoCoder.VideoCompress(filePath);               
            }
            catch (Exception ex)
            {
                System.Console.WriteLine("Exception choosing file: " + ex.ToString());
            }
        }
        
    }
}
