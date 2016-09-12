
/// \section intro Program Introduction
/// - This is the picture class for storing the Bitmap image representation and file path of the image
///
/// Major <b>Picture.cs</b>
/// \details <b>Details</b>
/// - Picture holder class
///
/// <li>\author         Geun Young Gil & Marcus Rankin</li>
/// <li>\version        1.00.00</li>
/// <li>\date           2015.12.18</li>
/// <li>\pre            Nothing
/// <li>\warning        Nothing
/// <li>\copyright      KitchenSoft</li>
/// <ul>

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Storage;
using Windows.UI.Xaml.Media.Imaging;

namespace slidePuzzle_uwp
{
    public class Picture
    {
        public BitmapImage GameImage;
        public string FilePath { get; set; }
    }
}
