using System.Collections.ObjectModel;
using Windows.UI.Xaml.Media;
using player.Song;

namespace player.List
{
    class ListItem
    {
        private ObservableCollection<Song.SongItem> items;
        private SongItem selectedItem;

        public SongItem SelectedItem
        {
            get { return selectedItem; }
            set { this.selectedItem = value; }
        }

        public ListItem()
        {
            selectedItem = null;
            items = new ObservableCollection<Song.SongItem>();
        }

        public ObservableCollection<Song.SongItem> getItems
        {
            get { return this.items; }
        }

        public void AddItem(string name, string artist, string album, ImageSource imag, string imagname, string source, int sign)
        {
            SongItem tem = new Song.SongItem(name, artist, album, imag, imagname, source, sign);
            selectedItem = tem;
            items.Add(tem);
        }
        public void AddItem(string name, string artist, string album, string source, bool like, int flag)
        {
            items.Add(new Song.SongItem(name, artist, album, source, like, flag));
        }

        public SongItem AddItem(SongItem item)
        {
            items.Add(item);
            selectedItem = item;
            return item;
        }


        public void removeItem()
        {
            items.Remove(selectedItem);
            selectedItem = null;
        }

        public void removeItem(SongItem item)
        {
            items.Remove(item);
        }

        public void clearItems()
        {
            items.Clear();
            selectedItem = null;
        }
    }
}