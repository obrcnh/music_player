using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.ApplicationModel.Core;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Imaging;
using Windows.UI.Xaml.Navigation;
using Windows.Media;
using Windows.Media.Core;
using Windows.Media.Playback;
using Windows.Storage;
using Windows.Storage.Pickers;
using Windows.Storage.Streams;
using Windows.UI.Popups;
using player.List;
using player.Song;
using Windows.ApplicationModel.DataTransfer;
using Windows.UI.Notifications;
using Newtonsoft.Json;
using System.Diagnostics;
using Windows.UI.Xaml.Shapes;
using Windows.Data.Xml.Dom;
using Windows.ApplicationModel;
using Newtonsoft.Json.Linq;
using Windows.UI.ViewManagement;
using Windows.Storage.FileProperties;
using Project;


//“空白页”项模板在 http://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409 上有介绍

namespace player
{
    /// <summary>
    /// 可用于自身或导航至 Frame 内部的空白页。
    /// </summary>
    public sealed partial class MainPage : Page
    {
        //播放列表
        private ListItem list;//歌曲列表，里面装有歌曲
        //查询列表
        private ListItem searchlist;
        int index;
        //用于查询得
        DataStore Store = new DataStore();

        DataTransferManager dataTransferManager = DataTransferManager.GetForCurrentView();
        public static string selectName = "";
        private string sharename = "";
        private string shareartist = "";
        private string sharealbum = "";
        private string shareurl = "";
        private StorageFile shareimag;
        private string shareimagname = "";

        //定义要放到数据库的Item组成
        class myItem
        {
            //
            public string name;
            public string artist;
            public string album;
            public string url;
            public string imagname;
            public ImageSource imag;
            public myItem(string name, string artist, string album, string url, string imagname, ImageSource imag)
            {
                this.name = name;
                this.artist = artist;
                this.album = album;
                this.url = url;
                this.imagname = imagname;
                this.imag = imag;
            }
        }

        public static MainPage Current { get; internal set; }



        public MainPage()
        {
            this.InitializeComponent();
            //设置tilebar的颜色

            var viewTitleBar = Windows.UI.ViewManagement.ApplicationView.GetForCurrentView().TitleBar;
            viewTitleBar.BackgroundColor = Windows.UI.Colors.CornflowerBlue;
            viewTitleBar.ButtonBackgroundColor = Windows.UI.Colors.CornflowerBlue;
            NavigationCacheMode = NavigationCacheMode.Enabled;
            Media.Volume = 0;
            //创建磁贴和分享功能的初始化操作
            TileUpdateManager.CreateTileUpdaterForApplication().EnableNotificationQueue(true);
            DataTransferManager dataTransferManager = DataTransferManager.GetForCurrentView();

            var coreTitleBar = CoreApplication.GetCurrentView().TitleBar;
            coreTitleBar.ExtendViewIntoTitleBar = true;
            Window.Current.SetTitleBar(GridTitleBar);
            Window.Current.Activated += (sender, args) =>
            {
                if (args.WindowActivationState != CoreWindowActivationState.Deactivated)
                {
                    GridTitleBar.Opacity = 1;
                    InputSearchBox.Opacity = 1;
                }
                else
                {
                    GridTitleBar.Opacity = 0.5;
                    InputSearchBox.Opacity = 0.5;
                }
            };//用来修改标题的初始化操作，对歌曲播放无影响

            // Hook up app to system transport controls.
            systemControls = SystemMediaTransportControls.GetForCurrentView();
            systemControls.ButtonPressed += SystemControls_ButtonPressed;

            // Register to handle the following system transpot control buttons.
            systemControls.IsPlayEnabled = true;
            systemControls.IsPauseEnabled = true;

            index = 0;


            list = Common1.list;//歌曲列表，里面装有歌曲
            //查询列表
            searchlist = Common2.list;

            Store.init(list);//初始化数据库
        }




        private async void MediaPlaybackList_CurrentItemChanged(MediaPlaybackList sender, CurrentMediaPlaybackItemChangedEventArgs args)
        {
            if (args.NewItem.Source.CustomProperties["Title"] != null)
            {
                await Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
                {
                    /*列表歌曲名，名字暂定*/
                    SongName.Text = args.NewItem.Source.CustomProperties["Title"] as string;
                    //修改播放界面里面的歌曲
                });
            }
        }

        private void DataRequested(DataTransferManager sender, DataRequestedEventArgs e)
        {
            DataRequest request = e.Request;
            DataPackage requestData = request.Data;
            requestData.Properties.Title = sharename;
            requestData.SetText("歌手:" + shareartist + "\n" + "专辑:" + sharealbum + "\n" + "链接:" + shareurl);

            DataRequestDeferral deferral = request.GetDeferral();
            try
            {
                requestData.SetBitmap(RandomAccessStreamReference.CreateFromFile(shareimag));
            }
            finally
            {
                deferral.Complete();
            }
        }

        protected override void OnNavigatedFrom(NavigationEventArgs e)
        {
            dataTransferManager.DataRequested -= DataRequested;
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            SystemNavigationManager.GetForCurrentView().AppViewBackButtonVisibility = AppViewBackButtonVisibility.Collapsed;
            dataTransferManager.DataRequested += DataRequested;
        }

        //创建动态磁贴
        void updatetile()
        {
            if (list.SelectedItem == null)
            {
                var i = new MessageDialog("please select a song!");
                return;
            }
            try
            {
                var Items = list.SelectedItem;
                var updater = TileUpdateManager.CreateTileUpdaterForApplication();
                updater.Clear();
                XmlDocument tile = new XmlDocument();
                tile.LoadXml(File.ReadAllText("Tile.xml"));
                XmlNodeList tileText = tile.GetElementsByTagName("text");
                XmlNodeList tileImage = tile.GetElementsByTagName("image");
                for (int i = 0; i < tileText.Count;)
                {
                    ((XmlElement)tileText[i]).InnerText = Items.Name;
                    i++;
                    ((XmlElement)tileText[i]).InnerText = Items.Artist;
                    i++;
                }
                TileNotification notification = new TileNotification(tile);
                updater.Update(notification);
            }
            catch (Exception err)
            {
                throw err;
            }
        }

        //分享事件
        private async void share_Click(object sender, RoutedEventArgs e)
        {
            var item = list.SelectedItem;
            if (item == null)
            {
                return;
            }
            sharename = item.Name;
            shareartist = item.Artist;
            sharealbum = item.Album;
            shareurl = item.Url;
            shareimag = await Package.Current.InstalledLocation.GetFileAsync("Assets\\allstar.png");
            DataTransferManager.ShowShareUI();
        }

        // about 点击事件
        private void aboutClick(object sender, RoutedEventArgs e)
        {
            var i = new MessageDialog("万维hr播放器1.0" + "\n\n" + "开发人员：" + "\n" + "高欣锐 冯梓维 陈南宏 邓旺").ShowAsync();
        }

        // 背景颜色设置

        private void yellowClick(object sender, RoutedEventArgs e)
        {
            bottombar.Background = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 238, 238, 0));
            topbar.Background = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 238, 238, 0));
            var view = ApplicationView.GetForCurrentView();
            view.TitleBar.ButtonBackgroundColor = Windows.UI.Color.FromArgb(255, 238, 238, 0);
        }

        private void blueClick(object sender, RoutedEventArgs e)
        {
            bottombar.Background = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 0, 255, 255));
            topbar.Background = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 0, 255, 255));
            var view = ApplicationView.GetForCurrentView();
            view.TitleBar.ButtonBackgroundColor = Windows.UI.Color.FromArgb(255, 0, 255, 255);
        }

        private void greenClick(object sender, RoutedEventArgs e)
        {
            bottombar.Background = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 0, 255, 127));
            topbar.Background = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 0, 255, 127));
            var view = ApplicationView.GetForCurrentView();
            view.TitleBar.ButtonBackgroundColor = Windows.UI.Color.FromArgb(255, 0, 255, 127);
        }

        private void pinkClick(object sender, RoutedEventArgs e)
        {
            bottombar.Background = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 255, 105, 180));
            topbar.Background = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 255, 105, 180));
            var view = ApplicationView.GetForCurrentView();
            view.TitleBar.ButtonBackgroundColor = Windows.UI.Color.FromArgb(255, 255, 105, 180);
        }

        private void redClick(object sender, RoutedEventArgs e)
        {
            bottombar.Background = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 255, 69, 0));
            topbar.Background = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 255, 69, 0));
            var view = ApplicationView.GetForCurrentView();
            view.TitleBar.ButtonBackgroundColor = Windows.UI.Color.FromArgb(255, 255, 69, 0);
        }
        //
        private async void searchOnline(AutoSuggestBox sender, AutoSuggestBoxQuerySubmittedEventArgs args)
        {
            if (InputSearchBox.Text == "")
            {
                return;
            }
            Uri requestUri = new Uri(@"http://s.music.163.com/search/get/?type=1&limit=25&s=" + InputSearchBox.Text.ToString());
            try
            {
                Windows.Web.Http.HttpClient httpclient = new Windows.Web.Http.HttpClient();
                var httpResponse = await httpclient.GetAsync(requestUri);
                httpResponse.EnsureSuccessStatusCode();
                var httpResponseBody = await httpResponse.Content.ReadAsStringAsync();
                int start = httpResponseBody.IndexOf('['), end = httpResponseBody.LastIndexOf(']');
                httpResponseBody = httpResponseBody.Substring(start, end - start + 1);

                JArray jarr = JArray.Parse(httpResponseBody.ToString());

                string name, artist, album, imagname, url;
                ImageSource imag;
                //清空上一次的查询列表
                searchlist.clearItems();

                for (int i = 0; i < jarr.Count; i++)
                {
                    name = jarr[i]["name"].ToString();
                    artist = jarr[i]["artists"][0]["name"].ToString();
                    album = jarr[i]["album"]["name"].ToString();
                    url = jarr[i]["audio"].ToString();
                    imagname = jarr[i]["album"]["picUrl"].ToString();
                    imag = new BitmapImage(new Uri(imagname));
                    searchlist.AddItem(name, artist, album, imag, imagname, url, 1);
                }
                PlayList.Visibility = Visibility.Collapsed;
                SearchOnlineList.Visibility = Visibility.Visible;
            }
            catch (Exception e)
            {
                var i = new MessageDialog("can't find the song");

                return;
            }
        }
        //列表跳转

        private void ToRearchList_Click(object sender, RoutedEventArgs e)
        {
            PlayList.Visibility = Visibility.Collapsed;
            SearchOnlineList.Visibility = Visibility.Visible;
        }

        private void ToPlayList_Click(object sender, RoutedEventArgs e)
        {
            PlayList.Visibility = Visibility.Visible;
            SearchOnlineList.Visibility = Visibility.Collapsed;
        }

        private void OrderPrevPlay()
        {
            if (index == 0)
            {
                index = list.getItems.Count - 1;
            }
            else
            {
                index--;
            }
            PlayListListView.SelectedItem = list.getItems[index];
            list.SelectedItem = list.getItems[index];
            TimeLine.Value = 0;
            TimeLine.Maximum = Media.NaturalDuration.TimeSpan.TotalSeconds;
        }

        private void OrderNextPlay()
        {
            if (index == list.getItems.Count - 1)
            {
                index = 0;
            }
            else
            {
                index++;
            }
            PlayListListView.SelectedItem = list.getItems[index];
           // album.ImageSource = list.getItems[index].Imag;
            list.SelectedItem = list.getItems[index];
            TimeLine.Value = 0;
            TimeLine.Maximum = Media.NaturalDuration.TimeSpan.TotalSeconds;
        }

        private void ShufflePlay()
        {
            Random ran = new Random();
            index = ran.Next(0, list.getItems.Count);
            PlayListListView.SelectedItem = list.getItems[index];
            // album.ImageSource = list.getItems[index].Imag;
            list.SelectedItem = list.getItems[index];
            TimeLine.Value = 0;
            TimeLine.Maximum = Media.NaturalDuration.TimeSpan.TotalSeconds;
        }

        /*播放上一首*/
        private async void Prev_Click(object sender, RoutedEventArgs e)
        {
            if (list.SelectedItem == null)
            {
                return;
            }
            if (playModeFlag == 0)
            {
                OrderPrevPlay();
            }
            else if (playModeFlag == 1)
            {
                ShufflePlay();
            }
            else
            {
                
            }
            //初始化喜欢的图片
            if (list.SelectedItem.Like == true)
            {
                var bitmap = new BitmapImage(new Uri("ms-appx:///Assets/like.png"));
                LikeButtonImage.Source = bitmap;
            }
            else
            {
                var bitmap = new BitmapImage(new Uri("ms-appx:///Assets/unlike.png"));
                LikeButtonImage.Source = bitmap;
            } 
            if (list.SelectedItem.Sign == 0)
            {
                Uri uri = new Uri("ms-appdata:///local/" + list.SelectedItem.Url);
                StorageFile file = await StorageFile.GetFileFromApplicationUriAsync(uri);
                RandomAccessStreamReference imageStreamRef = RandomAccessStreamReference.CreateFromFile(file);
                MusicProperties songProperties = await file.Properties.GetMusicPropertiesAsync();
                StorageItemThumbnail currentThumb = await file.GetThumbnailAsync(ThumbnailMode.MusicView, 200, ThumbnailOptions.UseCurrentScale);
                var albumCover = new BitmapImage();
                albumCover.SetSource(currentThumb);
                album.ImageSource = albumCover;
                Media.Source = new Uri("ms-appdata:///local/" + list.getItems[index].Url);
            }
            else if(list.SelectedItem.Sign == 1)
            {
                Uri pathUri = new Uri(list.SelectedItem.Url);
                Media.Source = pathUri;
                album.ImageSource = list.SelectedItem.Imag;
            }

            updatetile();
            SmallStoryboard.Begin();
            BigStoryboard.Begin();
            PoleStoryboard.Begin();
            PlayPauseButton.Icon = new SymbolIcon(Symbol.Pause);
            PlayPauseButton.Label = "Pause";
        }

        /*播放下一首*/
        private async void Next_Click(object sender, RoutedEventArgs e)
        {
            if (list.SelectedItem == null)
            {
                return;
            }
            if (playModeFlag == 0)
            {
                OrderNextPlay();
            }
            else if (playModeFlag == 1)
            {
                ShufflePlay();
            }
            else
            {
            }
            //初始化喜欢的图片
            if (list.SelectedItem.Like == true)
            {
                var bitmap = new BitmapImage(new Uri("ms-appx:///Assets/like.png"));
                LikeButtonImage.Source = bitmap;
            }
            else
            {
                var bitmap = new BitmapImage(new Uri("ms-appx:///Assets/unlike.png"));
                LikeButtonImage.Source = bitmap;
            }
            if(list.SelectedItem.Sign == 0)
            {
                Uri uri = new Uri("ms-appdata:///local/" + list.SelectedItem.Url);
                StorageFile file = await StorageFile.GetFileFromApplicationUriAsync(uri);
                RandomAccessStreamReference imageStreamRef = RandomAccessStreamReference.CreateFromFile(file);
                MusicProperties songProperties = await file.Properties.GetMusicPropertiesAsync();
                StorageItemThumbnail currentThumb = await file.GetThumbnailAsync(ThumbnailMode.MusicView, 200, ThumbnailOptions.UseCurrentScale);
                var albumCover = new BitmapImage();
                albumCover.SetSource(currentThumb);
                album.ImageSource = albumCover;
                Media.Source = new Uri("ms-appdata:///local/" + list.getItems[index].Url);
            }
            else if(list.SelectedItem.Sign == 1)
            {
                Uri pathUri = new Uri(list.SelectedItem.Url);
                Media.Source = pathUri;
                album.ImageSource = list.SelectedItem.Imag;
            }

            updatetile();
            SmallStoryboard.Begin();
            BigStoryboard.Begin();
            PoleStoryboard.Begin();
            PlayPauseButton.Icon = new SymbolIcon(Symbol.Pause);
            PlayPauseButton.Label = "Pause";
        }

        /*选择本地音乐*/
        MediaSource mediaSource;
        private async void OpenFile_Click(object sender, RoutedEventArgs e)
        {
            FileOpenPicker picker = new FileOpenPicker();
            picker.ViewMode = PickerViewMode.List;
            picker.SuggestedStartLocation = PickerLocationId.PicturesLibrary;
            picker.FileTypeFilter.Add(".mp3");
            try
            {
                StorageFile file = await picker.PickSingleFileAsync();
                mediaSource = MediaSource.CreateFromStorageFile(file);
                if (file != null)
                {
                    using (IRandomAccessStream stream = await file.OpenAsync(FileAccessMode.Read))
                    {
                        MusicProperties songProperties = await file.Properties.GetMusicPropertiesAsync();
                        StorageItemThumbnail currentThumb = await file.GetThumbnailAsync(ThumbnailMode.MusicView, 200, ThumbnailOptions.UseCurrentScale);
                        var albumCover = new BitmapImage();
                        albumCover.SetSource(currentThumb);
                        //修改此处将所得歌曲文件添加到歌曲列表（list）中

                        Windows.Storage.StorageFolder localFolder = Windows.Storage.ApplicationData.Current.LocalFolder;
                        string tempFilename = Guid.NewGuid().ToString();
                        string musicSource = tempFilename + ".mp3";
                        StorageFile musicfile = await localFolder.CreateFileAsync(musicSource, CreationCollisionOption.ReplaceExisting);
                        var buffer = await FileIO.ReadBufferAsync(file);
                        await FileIO.WriteBufferAsync(musicfile, buffer);

                        list.AddItem(songProperties.Title, songProperties.Artist, songProperties.Album, albumCover, songProperties.Title, musicSource, 0);
                        Store.insert(songProperties.Title + " - " + songProperties.Artist, songProperties.Artist, songProperties.Album, musicSource, false);
                        int count = list.getItems.Count;
                        index = count - 1;
                    }

                }
                updatetile();
            }
            catch
            {
                var i = new MessageDialog("Open file error!");
            }
        }

        /*单曲播放初始化时间轴*/
        private void Element_MediaOpened(object sender, RoutedEventArgs e)
        {
            TimeLine.Value = 0;
            TimeLine.Maximum = Media.NaturalDuration.TimeSpan.TotalSeconds;
        }

        /*单曲播放结束继续单曲循环播放*/
        private async void Element_MediaEnded(object sender, RoutedEventArgs e)
        {
            if (playModeFlag == 0)
            {
                OrderNextPlay();
                PlayListListView.SelectedItem = list.getItems[index];
                TimeLine.Value = 0;
                TimeLine.Maximum = Media.NaturalDuration.TimeSpan.TotalSeconds;
                //初始化喜欢的图片
                if (list.SelectedItem.Like == true)
                {
                    var bitmap = new BitmapImage(new Uri("ms-appx:///Assets/like.png"));
                    LikeButtonImage.Source = bitmap;

                }
                else
                {
                    var bitmap = new BitmapImage(new Uri("ms-appx:///Assets/unlike.png"));
                    LikeButtonImage.Source = bitmap;
                }

                if (list.SelectedItem.Sign == 0)
                {
                    Uri uri = new Uri("ms-appdata:///local/" + list.SelectedItem.Url);
                    StorageFile file = await StorageFile.GetFileFromApplicationUriAsync(uri);
                    RandomAccessStreamReference imageStreamRef = RandomAccessStreamReference.CreateFromFile(file);
                    MusicProperties songProperties = await file.Properties.GetMusicPropertiesAsync();
                    StorageItemThumbnail currentThumb = await file.GetThumbnailAsync(ThumbnailMode.MusicView, 200, ThumbnailOptions.UseCurrentScale);
                    var albumCover = new BitmapImage();
                    albumCover.SetSource(currentThumb);
                    album.ImageSource = albumCover;
                }
                else if (list.SelectedItem.Sign == 1)
                {
                    Uri pathUri = new Uri(list.SelectedItem.Url);
                    Media.Source = pathUri;
                    album.ImageSource = list.SelectedItem.Imag;
                }

                


                Media.Source = new Uri("ms-appdata:///local/" + list.getItems[index].Url);
                updatetile();
            }
            else if (playModeFlag == 1)
            {
                ShufflePlay();
                PlayListListView.SelectedItem = list.getItems[index];
                if (list.SelectedItem.Sign == 0)
                {
                    Uri uri = new Uri("ms-appdata:///local/" + list.SelectedItem.Url);
                    StorageFile file = await StorageFile.GetFileFromApplicationUriAsync(uri);
                    RandomAccessStreamReference imageStreamRef = RandomAccessStreamReference.CreateFromFile(file);
                    MusicProperties songProperties = await file.Properties.GetMusicPropertiesAsync();
                    StorageItemThumbnail currentThumb = await file.GetThumbnailAsync(ThumbnailMode.MusicView, 200, ThumbnailOptions.UseCurrentScale);
                    var albumCover = new BitmapImage();
                    albumCover.SetSource(currentThumb);
                    album.ImageSource = albumCover;
                }
                else if (list.SelectedItem.Sign == 1)
                {
                    Uri pathUri = new Uri(list.SelectedItem.Url);
                    Media.Source = pathUri;
                    album.ImageSource = list.SelectedItem.Imag;
                }
                TimeLine.Value = 0;
                TimeLine.Maximum = Media.NaturalDuration.TimeSpan.TotalSeconds;
                //初始化喜欢的图片
                if (list.SelectedItem.Like == true)
                {
                    var bitmap = new BitmapImage(new Uri("ms-appx:///Assets/like.png"));
                    LikeButtonImage.Source = bitmap;
                }
                else
                {
                    var bitmap = new BitmapImage(new Uri("ms-appx:///Assets/unlike.png"));
                    LikeButtonImage.Source = bitmap;
                }

                Media.Source = new Uri("ms-appdata:///local/" + list.getItems[index].Url);
                updatetile();
            }
            else if (playModeFlag == 2)
            {
                Media.Source = new Uri("ms-appdata:///local/" + list.getItems[index].Url);
            }
            SmallStoryboard.Begin();
            BigStoryboard.Begin();
            PoleStoryboard.Begin();
            PlayPauseButton.Icon = new SymbolIcon(Symbol.Pause);
            PlayPauseButton.Label = "Pause";
        }


        /*最小化后台播放音频处理
         侦听 MediaElement.CurrentStateChanged 事件以确定媒体状态何时发生变化，以便它可以通知 SystemMediaTransportControls
         处理 SystemMediaTransportControls.ButtonPressed 事件
         创建两个帮助程序方法来播放和暂停 MediaElement*/
        SystemMediaTransportControls systemControls;

        void SystemControls_ButtonPressed(SystemMediaTransportControls sender,
        SystemMediaTransportControlsButtonPressedEventArgs args)
        {
            switch (args.Button)
            {
                case SystemMediaTransportControlsButton.Play:
                    PlayMedia();
                    break;
                case SystemMediaTransportControlsButton.Pause:
                    PauseMedia();
                    break;
                default:
                    break;
            }
        }

        async void PlayMedia()
        {
            await Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
            {
                Media.Play();
            });
            updatetile();
        }

        async void PauseMedia()
        {
            await Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
            {
                Media.Pause();
            });
        }

        void MusicPlayer_CurrentStateChanged(object sender, RoutedEventArgs e)
        {
            switch (Media.CurrentState)
            {
                case MediaElementState.Playing:
                    systemControls.PlaybackStatus = MediaPlaybackStatus.Playing;
                    break;
                case MediaElementState.Paused:
                    systemControls.PlaybackStatus = MediaPlaybackStatus.Paused;
                    break;
                case MediaElementState.Stopped:
                    systemControls.PlaybackStatus = MediaPlaybackStatus.Stopped;
                    break;
                case MediaElementState.Closed:
                    systemControls.PlaybackStatus = MediaPlaybackStatus.Closed;
                    break;
                default:
                    break;
            }
        }

        //音乐播放方式
        int playModeFlag = 0;
        private void Playmode_Click(object sender, RoutedEventArgs e)
        {
            if (playModeFlag == 0)
            {
                playModeFlag = 1;
                Playmode.Icon = new SymbolIcon(Symbol.Shuffle);
                Playmode.Label = "Shuffle";
                //shuffleButton_Click(sender, e);
            }
            else if (playModeFlag == 1)
            {
                playModeFlag = 2;
                Playmode.Icon = new SymbolIcon(Symbol.Sync);
                Playmode.Label = "Repeat";
                // autoRepeatButton_Click(sender, e);
            }
            else if (playModeFlag == 2)
            {
                playModeFlag = 0;
                Playmode.Icon = new SymbolIcon(Symbol.List);
                Playmode.Label = "Order";
            }
          
        }

        // 播放列表中item的点击事件
        private async void PlayListListView_ItemClick(object sender, ItemClickEventArgs e)
        {
            list.SelectedItem = (Song.SongItem)(e.ClickedItem);
            updatetile();


            if (Media.AutoPlay == true)
            {
                return;//歌曲正在播放得时候无法选择其他事件
            }

            if (list.SelectedItem.Sign == 0)
            {
                Media.Source = new Uri("ms-appdata:///local/" + list.SelectedItem.Url);
                Uri uri = new Uri("ms-appdata:///local/" + list.SelectedItem.Url);
                StorageFile file = await StorageFile.GetFileFromApplicationUriAsync(uri);
                RandomAccessStreamReference imageStreamRef = RandomAccessStreamReference.CreateFromFile(file);
                MusicProperties songProperties = await file.Properties.GetMusicPropertiesAsync();
                StorageItemThumbnail currentThumb = await file.GetThumbnailAsync(ThumbnailMode.MusicView, 200, ThumbnailOptions.UseCurrentScale);
                var albumCover = new BitmapImage();
                albumCover.SetSource(currentThumb);

                album.ImageSource = albumCover;
            }
            else if (list.SelectedItem.Sign == 1)
            {
                Uri pathUri = new Uri(list.SelectedItem.Url);
                Media.Source = pathUri;
                album.ImageSource = list.SelectedItem.Imag;
            }

            TimeLine.Value = 0;
            TimeLine.Maximum = Media.NaturalDuration.TimeSpan.TotalSeconds;
            //初始化喜欢的图片
            if (list.SelectedItem.Like == true)
            {
                var bitmap = new BitmapImage(new Uri("ms-appx:///Assets/like.png"));
                LikeButtonImage.Source = bitmap;
            }
            else
            {
                var bitmap = new BitmapImage(new Uri("ms-appx:///Assets/unlike.png"));
                LikeButtonImage.Source = bitmap;
            }
            //PlayPauseButton.Icon = new SymbolIcon(Symbol.Play);
            PlayPauseButtonFlag = 0;
        }

        // 查询列表中item的点击事件
        private void SearchListListView_ItemClick(object sender, ItemClickEventArgs e)
        {
            list.SelectedItem = (Song.SongItem)(e.ClickedItem);
            list.getItems.Add(list.SelectedItem);

            PlayList.Visibility = Visibility.Visible;
            SearchOnlineList.Visibility = Visibility.Collapsed;
            updatetile();
        }

        int PlayPauseButtonFlag = 0;
        private void PlayPauseButton_Click(object sender, RoutedEventArgs e)
        {
            if (list.SelectedItem == null)
            {
                return;
            }
            Media.Play();
            Media.AutoPlay = true;


            if (PlayPauseButtonFlag == 0)
            {
                PlayMedia();
                PlayPauseButton.Icon = new SymbolIcon(Symbol.Pause);
                PlayPauseButtonFlag = 1;
                PlayPauseButton.Label = "pause";
                SmallStoryboard.Begin();
                BigStoryboard.Begin();
                PoleStoryboard.Begin();
            }
            else
            {
                PauseMedia();
                PlayPauseButton.Icon = new SymbolIcon(Symbol.Play);
                PlayPauseButtonFlag = 0;
                PlayPauseButton.Label = "play";
                SmallStoryboard.Pause();
                BigStoryboard.Pause();
                PoleBackStoryboard.Begin();
            }
        }

        private void LikeSong_Click(object sender, RoutedEventArgs e)
        {
            if (list.SelectedItem == null)
            {
                var i = new MessageDialog("please select a song to play");
                return;
            }

            if (list.SelectedItem.Like == true)
            {

                var bitmap = new BitmapImage(new Uri("ms-appx:///Assets/unlike.png"));
                LikeButtonImage.Source = bitmap;

                LikeButton.Content = "Unlike";
                list.SelectedItem.Like = false;

                Store.update(list.SelectedItem.Name, list.SelectedItem.Artist, list.SelectedItem.Album, list.SelectedItem.Url, false);

            }
            else
            {
                var bitmap = new BitmapImage(new Uri("ms-appx:///Assets/like.png"));
                LikeButtonImage.Source = bitmap;

                PlayListListView.SelectedItem = bitmap;
                LikeButton.Content = "like";
                list.SelectedItem.Like = true;

                Store.update(list.SelectedItem.Name, list.SelectedItem.Artist, list.SelectedItem.Album, list.SelectedItem.Url, true);

            }
        }

        private void DeleteSong_Click(object sender, RoutedEventArgs e)
        {

            if (list.SelectedItem == null)
            {
                var i = new MessageDialog("please select a song");
                return;
            }
            list.removeItem();

            Store.delete(list.SelectedItem.Name);
        }

        private void ChangeMediaVolume(object sender, RoutedEventArgs e)
        {
            Media.Volume = (double)VolumeLine.Value / 100.0;
        }

        private async void selectPlayButton_Click(object sender, RoutedEventArgs e)
        {
            if(list.SelectedItem == null)
            {
                return;
            }

            if (list.SelectedItem.Sign == 0)
            {
                Media.Source = new Uri("ms-appdata:///local/" + list.SelectedItem.Url);
                Uri uri = new Uri("ms-appdata:///local/" + list.SelectedItem.Url);
                StorageFile file = await StorageFile.GetFileFromApplicationUriAsync(uri);
                RandomAccessStreamReference imageStreamRef = RandomAccessStreamReference.CreateFromFile(file);
                MusicProperties songProperties = await file.Properties.GetMusicPropertiesAsync();
                StorageItemThumbnail currentThumb = await file.GetThumbnailAsync(ThumbnailMode.MusicView, 200, ThumbnailOptions.UseCurrentScale);
                var albumCover = new BitmapImage();
                albumCover.SetSource(currentThumb);
                album.ImageSource = albumCover;
            }
            else if (list.SelectedItem.Sign == 1)
            {
                Uri pathUri = new Uri(list.SelectedItem.Url);
                Media.Source = pathUri;
                album.ImageSource = list.SelectedItem.Imag;
            }

            //album.ImageSource = list.SelectedItem.Imag;
            if (list.SelectedItem.Sign == 0)
            {
                Media.Source = new Uri("ms-appdata:///local/" + list.SelectedItem.Url);
            }
            else if (list.SelectedItem.Sign == 1)
            {
                Uri pathUri = new Uri(list.SelectedItem.Url);
                Media.Source = pathUri;
            }
            SmallStoryboard.Begin();
            BigStoryboard.Begin();
            PoleStoryboard.Begin();

            TimeLine.Maximum = Media.NaturalDuration.TimeSpan.TotalSeconds;
            //初始化喜欢的图片
            if (list.SelectedItem.Like == true)
            {
                var bitmap = new BitmapImage(new Uri("ms-appx:///Assets/like.png"));
                LikeButtonImage.Source = bitmap;
            }
            else
            {
                var bitmap = new BitmapImage(new Uri("ms-appx:///Assets/unlike.png"));
                LikeButtonImage.Source = bitmap;
            }

            PlayPauseButton.Icon = new SymbolIcon(Symbol.Pause);
            PlayPauseButton.Label = "Pause";
            PlayPauseButtonFlag = 1;
        }
    }

}
