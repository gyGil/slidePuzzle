/// \section intro Program Introduction
/// - 
/// Major <b>TileMaker.cs</b>
/// \details <b>Details</b>
/// -This is the tile maker class for creating and populating the game tiles
///
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
using Windows.UI;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Imaging;
using Windows.UI.Xaml.Shapes;

namespace slidePuzzle_uwp
{

    public static class TileMaker
    {
        /// \brief  GetImageTiles()
        ///
        /// \details <b>Details</b>
        /// Make the list including tiles for the puzzle game and Methods controlling the tiles 
        ///
        /// \param sizeOfGrid - <b>int</b> - Size of game grid
        /// \param hasImage - <b>bool</b> - Tile image check
        /// \param imgList - <b>List<Rectangle></b> - Rectangle image list
        /// 
        /// \return void <b>N/A</b> -N/A
        public static List<Grid> GetImageTiles(int sizeOfGrid, bool hasImage, List<Rectangle> imgList)
        {
            int totalNumTiles = sizeOfGrid * sizeOfGrid;
            var tiles = new List<Grid>();

            for (int i = 0; i < totalNumTiles; ++i)
            {
                Grid g = new Grid();
                g.Background = new SolidColorBrush(Colors.White);
                Rectangle rec= new Rectangle();
                TextBlock tb = new TextBlock();

                // give the number to cell
                if (i < totalNumTiles - 1)
                {
                    tb.FontSize = 24;
                    tb.Opacity = 50.0;
                    tb.Text = (i+1).ToString() ;
                    tb.HorizontalAlignment = HorizontalAlignment.Center;
                    tb.VerticalAlignment = VerticalAlignment.Center;
                    
                    g.Name = "cell" + i.ToString();
                }
                else // blank cell
                {
                    g.Name = "cell" + i.ToString();
                    g.Visibility = Visibility.Collapsed;         
                }

                // set the image on cells
                if(hasImage)    // if it has image
                {
                    rec.Fill = imgList[i].Fill;
                }
                else
                {
                    rec.Fill = new SolidColorBrush(Colors.White);
                }

                g.Children.Add(rec);
                g.Children.Add(tb);
                g.Margin = new Thickness(1, 1, 1, 1);

                tiles.Add(g);
            }

            tiles.Shuffle();

            return tiles;
        }

        /// <summary>
        /// get various colour tiles
        /// </summary>
        /// <param name="sizeOfGrid"></param>
        /// <param name="colour"></param>
        /// <returns></returns>
        public static List<Grid> GetColourTiles(int sizeOfGrid, Color colour, string word)
        {
            int totalNumTiles = sizeOfGrid * sizeOfGrid;
            var tiles = new List<Grid>();
            int middleAreaindex = totalNumTiles / 2 - 9;

            int j = 0;
            for (int i = 0; i < totalNumTiles; ++i)
            {
                Grid grid = new Grid();
                Rectangle rec = new Rectangle();
                rec.Fill = new SolidColorBrush(colour);
                TextBlock tb = new TextBlock();
                tb.FontSize = 24;
                tb.HorizontalAlignment = HorizontalAlignment.Center;
                tb.VerticalAlignment = VerticalAlignment.Center;

                if((word != null) && (j < word.Length) && (i > middleAreaindex))
                {
                    tb.Text = word[j].ToString();
                    ++j;
                }

                grid.Children.Add(rec);
                grid.Children.Add(tb);

                tiles.Add(grid);
            }           

            return tiles;
        }


        /// <summary>
        /// shuffle the list
        /// reference from http://stackoverflow.com/questions/273313/randomize-a-listt-in-c-sharp
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="list"></param>
        public static void Shuffle<T>(this List<T> list)
        {
            Random rnd = new Random();
            int n = list.Count - 1;
            while (n > 0)
            {
                --n;
                int k = rnd.Next(0, n);
                T value = list[k];
                list[k] = list[n];
                list[n] = value;
            }
        }

        /// <summary>
        /// change row column value to indexed value (0 ~ (sizeOfGrid * sizeOfGrid - 1) )
        /// </summary>
        /// <param name="row"></param>
        /// <param name="col"></param>
        /// <param name="sizeOfGrid"></param>
        /// <returns></returns>
        public static int RowColToIndex(int row, int col, int sizeOfGrid)
        {
            return (row * sizeOfGrid) + col;
        }

        /// <summary>
        /// chage indexed value to row and column
        /// </summary>
        /// <param name="index"></param>
        /// <param name="sizeOfGrid"></param>
        /// <returns></returns>
        public static Tuple<int, int> IndexToRowCol(int index, int sizeOfGrid)
        {
            return Tuple.Create(index / sizeOfGrid, index % sizeOfGrid); // row and col
        }

    }
}
