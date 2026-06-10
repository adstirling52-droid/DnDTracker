using System;
using System.IO;
using System.Windows;
using System.Windows.Media.Imaging;

namespace DnDTracker
{
    public partial class ItemImageWindow : Window
    {
        public ItemImageWindow(string imagePath, string itemName)
        {
            InitializeComponent();

            Title = itemName;

            if (!string.IsNullOrWhiteSpace(imagePath) && File.Exists(imagePath))
            {
                LargeItemImage.Source = new BitmapImage(new Uri(imagePath, UriKind.Absolute));
            }
        }
    }
}
