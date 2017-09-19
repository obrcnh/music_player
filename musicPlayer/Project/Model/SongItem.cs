using System;
using System.ComponentModel;
using Windows.Storage;
using Windows.Storage.Streams;
using Windows.UI.Popups;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Imaging;

namespace player.Song
{
    public class SongItem : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;
        private string name;   // 歌名mp3
        private string artist;   // 歌手mp3
        private string album;   // 专辑名mp3

        private bool like = false; //用于标识是否喜爱这首歌曲

        private ImageSource imag;   // 图片
        private string imagname;   // 图片名

        private string url;    //歌曲的文件路径
        private int sign;      //区分本地文件和网络文件(0: local, 1: url)
        public SongItem(string name, string artist, string album, ImageSource imag, string imagname, string url, int sign)
        {
            this.name = name + "-" + artist;
            this.artist = artist;
            this.album = album;
            this.imag = imag;
            this.imagname = imagname;
            this.url = url;
            this.sign = sign;
        }
        public SongItem(string name, string artist, string album, string url, bool ifLike, int ifSign)
        {

            this.name = name;
            this.artist = artist;
            this.album = album;
            this.url = url;
            this.like = ifLike;
            this.sign = ifSign;
        }

        private void NotifyPropertyChanged(string property_name)
        {
            if (PropertyChanged != null)
                PropertyChanged(this, new PropertyChangedEventArgs(property_name));
        }

        public string Name
        {
            set
            {
                name = value;
                NotifyPropertyChanged("Name");
            }
            get
            {
                return name;
            }
        }

        public string Artist
        {
            set
            {
                artist = value;
                NotifyPropertyChanged("Artist");
            }
            get
            {
                return artist;
            }
        }

        public string Album
        {
            set
            {
                album = value;
                NotifyPropertyChanged("Album");
            }
            get
            {
                return album;
            }
        }

        public bool Like
        {
            set
            {
                like = value;
                NotifyPropertyChanged("Like");
            }
            get
            {
                return like;
            }
        }

        public ImageSource Imag
        {
            set
            {
                imag = value;
                NotifyPropertyChanged("Imag");
            }
            get
            {
                return imag;
            }
        }

        public string Imagname
        {
            set
            {
                imagname = value;
                NotifyPropertyChanged("Imagname");
                setImg();
            }
            get
            {
                return imagname;
            }
        }
        public async void setImg()
        {
            if (imagname == "")
            {
                this.imag = new BitmapImage(new Uri("ms-appx:///Assets/a.png"));
            }
            else
            {
                var file = await ApplicationData.Current.LocalFolder.GetFileAsync(imagname);
                //根据图片路径获取图片
                IRandomAccessStream fileStream = await file.OpenAsync(FileAccessMode.Read);
                BitmapImage bitmapimage = new BitmapImage();
                await bitmapimage.SetSourceAsync(fileStream);
                this.imag = bitmapimage;
            }
        }
        public string Url
        {
            set
            {
                url = value;
                NotifyPropertyChanged("Url");
            }
            get
            {
                return url;
            }
        }
        public int Sign
        {
            set
            {
                sign = value;
                NotifyPropertyChanged("Sign");
            }
            get
            {
                return sign;
            }
        }
    }
}