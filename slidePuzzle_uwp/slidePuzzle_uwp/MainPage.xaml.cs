/// \file MainPage
///
/// \mainpage PROG2120 - Windows Mobile Programming - Final Project: Tile Game (Option 1)
///
/// \section intro Program Introduction
/// - This is the main source file and code behind the MainPage.xaml. This file contains all the main 
///   functions to create and render a Tile/Puzzle game that is made up of either pictures or numbers
///   or both. This program has the ability to utilize the the touch interface, mouse or accelerometer 
///   in order to manipulate the tiles inside the game. The Pictures for the game are scrapped from
///   the entire Picture Folder Library and all sub folders as well as the ability to capture a photo
///   utilizing either of the two cameras and using this photo as the game photo. The UI has both 
///   icon based page changing and touch/swipe page changing. The application can be run in any 
///   orientation and can be resized to multiple sizes dynamically. There is a pause/start timer
///   to calculate the users score and a Leaderboard that can be checked at any time.
///
/// Major <b>MainPage.xaml.cs</b>
/// \section version Current version of this Program
/// <ul>
/// <li>\author         Geun Young Gil & Marcus Rankin</li>
/// <li>\references     Windows Dev Center and Channel 9
/// <li>\version        1.00.00</li>
/// <li>\date           2015.12.18</li>
/// <li>\pre            Nothing
/// <li>\warning        Nothing
/// <li>\copyright      KitchenSoft</li>
/// <ul>

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Windows.Devices.Sensors;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Graphics.Imaging;
using Windows.Media.Capture;
using Windows.Storage;
using Windows.Storage.Streams;
using Windows.UI;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Imaging;
using Windows.UI.Xaml.Navigation;
using Windows.UI.Xaml.Shapes;

// The Blank Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409

namespace slidePuzzle_uwp
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page
    {
        private const int MAX_ACCELEROMETER_TIME_SPAN = 50; ///< 50 milliseconds
        private const int MAX_ACCELEROMETER_MOVE_TIME = 8; ///< 50 * 10 milliseconds

        //===============================    PUBLIC MEMBERS  ===============================//
        public int SizeOfGrid { get; set; } ///< size of grid (4 => 4 x 4)
        public bool _enabledAccel { get; set; } ///< accelerometer is enabled or not
        public bool _isPaused { get; set; } ///< game is paused or not
        public bool _isGameFinished { get; set; }   ///< game is finished or not

        //===============================    PRIVATE MEMBERS  ===============================//
        private Accelerometer accelerometer;
        private DispatcherTimer dtimer;     ///< timer https://msdn.microsoft.com/en-us/library/system.windows.threading.dispatchertimer(v=vs.110).aspx
        private Stopwatch playtimeTimer;    ///< stopwatch to measure play time  https://msdn.microsoft.com/en-us/library/system.diagnostics.stopwatch(v=vs.110).aspx
        private Stopwatch accelWatch;   ///< stopwatch for accelerometer
        private double accelDeltaX; ///< delta X for accelerometer
        private double accelDeltaY; ///< delta Y for accelerometer
        private int accelMoveTime;
        private Grid blankCell;
        private Grid targetCell;
        Point targetCellOrigin; ///< 
        List<Player> leaderBoard;   ///< leader board to store in file
        ObservableCollection<Player> ocLeaderBoard; ///< collection binding with leader board (automatically updated)
        List<Rectangle> puzzleImgList;
        private StorageFolder pictureFolder;

        private ObservableCollection<Picture> Images;
        private ObservableCollection<StorageFile> allPictures;

        //===============================    METHODS  ===============================//
        public MainPage()
        {
            this._isPaused = true;
            this._isGameFinished = true;
            this.InitializeComponent();
            this.SizeOfGrid = 4; // 4 x 4 grid
            this.accelDeltaX = 0.0;
            this.accelDeltaY = 0.0;
            this.accelMoveTime = MainPage.MAX_ACCELEROMETER_MOVE_TIME;
            this.dtimer = new DispatcherTimer();
            this.dtimer.Interval = new TimeSpan(0, 0, 0, 0, 1);
            this.dtimer.Tick += Dtimer_Tick;
            this.accelWatch = new Stopwatch();
            this.playtimeTimer = new Stopwatch();
            this.leaderBoard = PlayerList.GetPlayersEx().OrderBy(e => e.PlayTime).ToList();
            ocLeaderBoard = new ObservableCollection<Player>();
            this.CopyBoard();
            this._enabledAccel = false;
            this.SetupAccelerometer();

            Images = new ObservableCollection<Picture>();
            puzzleImgList = new List<Rectangle>();
            DataAccess.list.Clear();
        }

        /// \brief  CopyBoard()
        ///
        /// \details <b>Details</b>
        /// - Populate Leaderboard
        ///
        /// \param N/A - <b>N/A</b> - N/A
        /// 
        /// \return void <b>N/A</b> -N/A
        public void CopyBoard()
        {
            List<Player> tmp = this.leaderBoard.OrderBy(e => e.PlayTime).ToList();
            this.leaderBoard = tmp;
            this.ocLeaderBoard.Clear();
            int i = 0;
            foreach (Player p in this.leaderBoard)
            {
                this.ocLeaderBoard.Add(new Player(++i,p.PlayTime, p.Name));
            }
        }

        /// \brief CreatePlayBoard()
        ///
        /// \details <b>Details</b>
        /// - Create the grid playing tile surface
        ///
        /// \param N/A - <b>N/A</b> - N/A
        /// 
        /// \return void <b>N/A</b> -N/A
        private void CreatePlayBoard()
        {
            this.gridGame.Children.Clear();
            List<Grid> gridList;
            if (this.puzzleImgList.Count > 0)
            {
                gridList = TileMaker.GetImageTiles(this.SizeOfGrid, true, this.puzzleImgList);    // get tiles for new board
            }          
            else
            {
                gridList = TileMaker.GetImageTiles(this.SizeOfGrid, false, null);    // get tiles for new board
            }         
            
            this.CreateBoard(gridList, this.SizeOfGrid);

            // add event handlers to cells
            foreach (Grid grid in this.gridGame.Children)
            {
                if (grid.Name != "cell" + (this.SizeOfGrid * SizeOfGrid - 1).ToString())    // except blank cell
                {
                    grid.PointerPressed += Cell_PointerPressed;
                    grid.PointerMoved += Cell_PointerMoved;
                    grid.PointerReleased += Cell_PointerReleased;
                }
            }

            SetBoardVisibilities();
        }

        /// \brief  btnPlay_Click()
        ///
        /// \details <b>Details</b>
        /// - Populate Leaderboard
        ///
        /// \param sender - <b>object</b> - Object interacted with
        /// \param e - <b>RoutedEventArgs</b> - Event Argument
        /// 
        /// \return void <b>N/A</b> -N/A
        private void btnPlay_Click(object sender, RoutedEventArgs e)
        {
            ImageBrush ib = new ImageBrush();

            if (!this._isPaused && !this._isGameFinished)  // make paused
            {
                if (this.playtimeTimer.IsRunning)
                    this.playtimeTimer.Stop();
                ib.ImageSource = new BitmapImage(new Uri("ms-appx:/Assets/play.png"));
                this.btnPlay.Background = ib;
                this._isPaused = !this._isPaused;
                this.targetCell = null;
                this.rootPivot.IsLocked = false;
            }
            else if (this._isPaused) // make playing
            {
                if (this._isGameFinished) // start game
                {
                    this.playtimeTimer.Reset();
                    this.gridGame.Children.Clear();
                    this.CreatePlayBoard();
                    this.blankCell = this.FindName("cell" + (this.SizeOfGrid * SizeOfGrid - 1).ToString()) as Grid;    // find blank cell
                    this._isGameFinished = false;

                    //**************************** (psudo code) ****************************// 
                    // 1. check acceleromer is enabled or not 
                }
                this.SetBoardVisibilities();
                this.dtimer.Start();
                this.rootPivot.IsLocked = true;
                this.playtimeTimer.Start();
               

                ib.ImageSource = new BitmapImage(new Uri("ms-appx:/Assets/pause.png")); // change the status to play
                this.btnPlay.Background = ib;
                this._isPaused = !this._isPaused;
            }
            else   // game is not starting.
            {
                ib.ImageSource = new BitmapImage(new Uri("ms-appx:/Assets/play.png"));
                this.btnPlay.Background = ib;
                this.targetCell = null;
                this.rootPivot.IsLocked = false;
            }
        }

        /// \brief  SetupAccelerometer()
        ///
        /// \details <b>Details</b>
        /// - Start to monitor the accelerometer
        ///
        /// \return void <b>N/A</b> -N/A
        private void SetupAccelerometer()
        {
            accelerometer = Windows.Devices.Sensors.Accelerometer.GetDefault();
            if (accelerometer != null)
            {
                accelWatch.Start();
            }
        }


        /// \brief  Dtimer_Tick()
        ///
        /// \details <b>Details</b>
        /// - Timer tick event to check time and accelerometer changes
        ///
        /// \param sender - <b>object</b> - Object interacted with
        /// \param e - <b>object</b> - Object event
        /// 
        /// \return void <b>N/A</b> -N/A
        private void Dtimer_Tick(object sender, object e)
        {
            if (this.playtimeTimer.IsRunning)
            {
                TimeSpan ts = this.playtimeTimer.Elapsed;
                this.elapsedTime.Text = String.Format("{0:00}:{1:00}:{2:00}", ts.Minutes, ts.Seconds, ts.Milliseconds / 10);
            }

            // retreive accelerometer values regularily
            if (this.accelWatch.IsRunning && this._enabledAccel && !this._isPaused && !this._isGameFinished)
                this.AccelerometerController();
        }

        /// \brief  AcceleromterController()
        ///
        /// \details <b>Details</b>
        /// - Monitor accelerometer to create the correct game tile movements
        ///
        /// \return void <b>N/A</b> -N/A
        private void AccelerometerController()
        {
            TimeSpan ts = this.accelWatch.Elapsed;
            if (ts.Milliseconds > MainPage.MAX_ACCELEROMETER_TIME_SPAN)
            {
                // get the values from accelerometer                   
                double x = accelerometer.GetCurrentReading().AccelerationX;
                double y = accelerometer.GetCurrentReading().AccelerationY;

                // determine accelerometer is moving or not
                if (x > 0.10 || x < -0.10 ||
                    y > 0.10 || y < -0.10)
                {
                    ++this.accelMoveTime;
                    double amplifiedX = x * 30;
                    double amplifiedY = y * -30;
                    this.accelDeltaX += amplifiedX;
                    this.accelDeltaY += amplifiedY;

                    if (this.accelMoveTime == 2) // determin what cell should move
                    {
                        double absAccelDeltaX = Math.Abs(this.accelDeltaX); //absolute value
                        double absAccelDeltaY = Math.Abs(this.accelDeltaY);
                        Grid tmpTargtetTile = null;

                        // determine which direction try to go
                        if (absAccelDeltaX > absAccelDeltaY)    // move x-axis
                        {
                            if (this.accelDeltaX > 0) //right
                            {
                                tmpTargtetTile = this.GetTileOnSide('L', this.blankCell);
                            }
                            else  //left
                            {
                                tmpTargtetTile = this.GetTileOnSide('R', this.blankCell);
                            }
                        }
                        else  // move y-axis
                        {
                            if (this.accelDeltaY > 0) //Down
                            {
                                tmpTargtetTile = this.GetTileOnSide('U', this.blankCell);
                            }
                            else  //Up
                            {
                                tmpTargtetTile = this.GetTileOnSide('D', this.blankCell);
                            }
                        }

                        if (tmpTargtetTile != null) // if target cell is picked
                        {
                            if(this.targetCell != null) // previous target cell is render to original place
                            {
                                this.RenderMoving(0, 0, false);
                            }
                            this.targetCell = tmpTargtetTile;
                            this.RenderMoving(this.accelDeltaX, this.accelDeltaY, false);
                        }
                        else  // if not, all are initialized
                        {
                            accelMoveTime = 0;
                            this.accelDeltaX = 0.0;
                            this.accelDeltaY = 0.0;
                        }
                    }
                    else if(this.accelMoveTime > 2) // if it has the consiquently the moves toward same direction
                    {
                        if(this.targetCell != null) // check to exist target cell
                        {
                            if (this.accelMoveTime >= MainPage.MAX_ACCELEROMETER_MOVE_TIME)
                                this.RenderMoving(this.accelDeltaX, this.accelDeltaY, true);
                            else
                            {
                                this.RenderMoving(this.accelDeltaX, this.accelDeltaY, false);
                            }                         
                        }
                        else // if target cell doesn't exist, initialize
                        {
                            accelMoveTime = 0;
                            this.accelDeltaX = 0.0;
                            this.accelDeltaY = 0.0;
                        }
                    }
                }
                else    // it didn't detect any moves
                {
                    if(targetCell != null)
                        this.RenderMoving(0, 0, false);
                    this.targetCell = null;
                    accelMoveTime = 0;
                    this.accelDeltaX = 0.0;
                    this.accelDeltaY = 0.0;
                }

                this.accelWatch.Reset();    // reset accelerometer timer
                this.accelWatch.Start();
            }
        }

        /// <summary>
        /// Get the tile on specific side based on baseGrid
        /// </summary>
        /// <param name="direction"></param>
        /// <param name="baseGrid"></param>
        /// <returns></returns>
        private Grid GetTileOnSide(char direction,Grid baseGrid)
        {
            Grid retTile = null;

            int baseRow = Grid.GetRow(baseGrid);    // get base row and col
            int baseCol = Grid.GetColumn(baseGrid);

            if (direction == 'U')
            {
                if (baseRow > 0)
                    retTile = this.gridGame.Children.Cast<Grid>().First(e => Grid.GetRow(e) == baseRow - 1 && Grid.GetColumn(e) == baseCol) as Grid;
            }           
            else if (direction == 'D')
            {
                if (baseRow < this.SizeOfGrid - 1)
                    retTile = this.gridGame.Children.Cast<Grid>().First(e => Grid.GetRow(e) == baseRow + 1 && Grid.GetColumn(e) == baseCol) as Grid;
            }              
            else if (direction == 'R')
            {
                if (baseCol < this.SizeOfGrid - 1)
                    retTile = this.gridGame.Children.Cast<Grid>().First(e => Grid.GetRow(e) == baseRow && Grid.GetColumn(e) == baseCol + 1) as Grid;
            }                
            else if (direction == 'L')
            {
                if (baseCol > 0)
                    retTile = this.gridGame.Children.Cast<Grid>().First(e => Grid.GetRow(e) == baseRow && Grid.GetColumn(e) == baseCol - 1) as Grid;
            }
                

            return retTile;
        }

        /// <summary>
        /// get the direction which the cell can move toward empty place. (D: Down, U - Up, R - Right, L - Left, X - Nowhere)
        /// </summary>
        /// <param name="cell"></param>
        /// <returns></returns>
        private char WhichDirectionIsEmpty(Grid cell)
        {
            if (this.blankCell != null)
            {
                int cellIndex = TileMaker.RowColToIndex(Grid.GetRow(cell), Grid.GetColumn(cell), this.SizeOfGrid);
                int blankCellIndex = TileMaker.RowColToIndex(Grid.GetRow(blankCell), Grid.GetColumn(blankCell), this.SizeOfGrid);

                if (blankCellIndex - SizeOfGrid == cellIndex) return 'D';    // compare the upside cell of blank cell

                if (blankCellIndex + SizeOfGrid == cellIndex) return 'U';   // compare the downside cell of blank cell

                if ((blankCellIndex - 1 == cellIndex) &&
                    (blankCellIndex % SizeOfGrid != 0))
                    return 'R'; // compare the leftside cell of blank cell

                if ((blankCellIndex + 1 == cellIndex) &&
                    ((blankCellIndex + 1) % SizeOfGrid != 0))
                    return 'L';  // compare the Rightside cell of blank cell
            }

            return 'X';
        }

        /// <summary>
        /// determine indicated direction is available or not
        /// if it is availeable return the cell, if not return null
        /// </summary>
        /// <param name="cell"></param>
        /// <param name="Direction"></param>
        /// <returns></returns>
        private Grid IsThisDirectionCellAvail(char direction)
        {
            int row = Grid.GetRow(this.blankCell);
            int col = Grid.GetColumn(this.blankCell);
            Grid g = null;

            if ((direction == 'U') && (row > 0))
            {
                g = this.gridGame.Children.Cast<Grid>().First(e => Grid.GetRow(e) == row + 1 && Grid.GetColumn(e) == col) as Grid;
            }
            else if ((direction == 'D') && (row < SizeOfGrid - 1))
            {
                g = this.gridGame.Children.Cast<Grid>().First(e => Grid.GetRow(e) == row - 1 && Grid.GetColumn(e) == col) as Grid;
            }
            else if ((direction == 'R') && (col < SizeOfGrid - 1))
            {
                g = this.gridGame.Children.Cast<Grid>().First(e => Grid.GetRow(e) == row && Grid.GetColumn(e) == col + 1) as Grid;
            }
            else if ((direction == 'L') && (col > 0))
            {
                g = this.gridGame.Children.Cast<Grid>().First(e => Grid.GetRow(e) == row && Grid.GetColumn(e) == col - 1) as Grid;
            }

            return g;
        }

        /// <summary>
        /// if the pointer-pressed cell move, set that cell as target cell to get refereced to Cell_PointerMoved() and Cell_PointerReleased() 
        /// event handlers
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Cell_PointerPressed(object sender, PointerRoutedEventArgs e)
        {
            if ((this.targetCell == null) && !this._isGameFinished && !this._isPaused)
            {
                char direction = this.WhichDirectionIsEmpty(sender as Grid);    // determine which this cell can move

                if (direction != 'X') // if the pressed cell can move, set as target cell
                {
                    this.targetCell = sender as Grid;   // set as targeted cell
                    this.targetCellOrigin = e.GetCurrentPoint(this.gridGame).Position;  // get the current mouse position
                }
                else
                {
                    this.targetCell = null;
                }
            }
        }

        /// <summary>
        /// pointer is moving, render the move
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Cell_PointerMoved(object sender, PointerRoutedEventArgs e)
        {

            if ((this.targetCell != null) && !this._isGameFinished && !this._isPaused)
            {
                Point curPos = e.GetCurrentPoint(this.gridGame).Position;   // get the current postion
                double deltaX = curPos.X - this.targetCellOrigin.X; // get the delta value of moving
                double deltaY = curPos.Y - this.targetCellOrigin.Y;

                this.RenderMoving(deltaX, deltaY, false);
            }
        }

        /// <summary>
        /// pointer is released, determine change the cell positon and check whether answer is got or not
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Cell_PointerReleased(object sender, PointerRoutedEventArgs e)
        {
            if ((this.targetCell != null) && !this._isGameFinished && !this._isPaused)
            {
                Point curPos = e.GetCurrentPoint(this.gridGame).Position;   // get the current postion
                double deltaX = curPos.X - this.targetCellOrigin.X; // get the delta value of moving
                double deltaY = curPos.Y - this.targetCellOrigin.Y;

                this.RenderMoving(deltaX, deltaY, true);
            }

            this.targetCell = null;
        }

        /// \brief  gridGame_PointerReleased()
        ///
        /// \details <b>Details</b>
        /// - Mouse button released check
        ///
        /// \param sender - <b>object</b> - Object interacted with
        /// \param e - <b>RoutedEventArgs</b> - Pointer Event Argument
        /// 
        /// \return void <b>N/A</b> -N/A
        private void gridGame_PointerReleased(object sender, PointerRoutedEventArgs e)
        {
            this.Cell_PointerReleased(sender, e);
        }

        /// <summary>
        /// render UIelement into right direction and length
        /// </summary>
        /// <param name="deltaX"></param>
        /// <param name="deltaY"></param>
        /// <param name="isReleased"></param>
        private void RenderMoving(double deltaX, double deltaY, bool isReleased)
        {
            bool pointingUp = false;
            bool pointingDown = false;
            bool pointingRight = false;
            bool pointingLeft = false;
            double maxDeltaX = this.targetCell.ActualWidth;
            double maxDeltaY = this.targetCell.ActualHeight;

            // determine user's direction after determining empty places
            if (deltaY < 0.00) pointingUp = true;
            else pointingDown = true;
            if (deltaX < 0.00) pointingLeft = true;
            else pointingRight = true;

            double absDeltaX = Math.Abs(deltaX); //absolute value
            double absDeltaY = Math.Abs(deltaY);

            // set as maxDeltaX if moved position go over max length
            if (absDeltaX > maxDeltaX)
            {
                if (deltaX < 0) deltaX = -1 * maxDeltaX;
                else deltaX = maxDeltaX;
            }

            if (absDeltaY > maxDeltaY)
            {
                if (deltaY < 0) deltaY = -1 * maxDeltaY;
                else deltaY = maxDeltaY;
            }

            TranslateTransform tt = new TranslateTransform();

            char direction = this.WhichDirectionIsEmpty(this.targetCell);
            // make moving motion
            if (pointingUp && (direction == 'U') && (absDeltaY > absDeltaX))
            {
                tt.X = 0.00;
                tt.Y = deltaY;
            }
            else if (pointingRight && (direction == 'R') && (absDeltaX > absDeltaY))
            {
                tt.X = deltaX;
                tt.Y = 0.00;
            }
            else if (pointingDown && (direction == 'D') && (absDeltaX < absDeltaY))
            {
                tt.X = 0.00;
                tt.Y = deltaY;
            }
            else if (pointingLeft && (direction == 'L') && (absDeltaX > absDeltaY))
            {
                tt.X = deltaX;
                tt.Y = 0.00;
            }
            else
            {
                tt.X = 0.00;
                tt.Y = 0.00;
            }

            if (!isReleased)
            {
                this.targetCell.RenderTransform = tt;    // transform
            }
            else // Finished to move and exchange tile
            {
                TranslateTransform tmptt = new TranslateTransform();
                tmptt.X = 0.00;
                tmptt.Y = 0.00;
                this.targetCell.RenderTransform = tmptt;    // transform

                if ((tt.X > maxDeltaX * 0.6) ||
                    (tt.X < maxDeltaX * -0.6) ||
                    (tt.Y > maxDeltaY * 0.6) ||
                    (tt.Y < maxDeltaY * -0.6))
                {
                    this.MoveTile();
                }

                if (this.GetAnswer())
                {
                    this.CreateFlickingScreen(" Finish!", true);
                    this.CloseGame();
                }

                this.targetCell = null;
            }
        }

        /// <summary>
        /// make flasing screen
        /// </summary>
        /// <param name="word"></param>
        /// <param name="isForRecord"></param>
        private async void CreateFlickingScreen(string word, bool isForRecord)
        {
            for (int i = 0; i < 10; ++i)
            {
                if (i % 3 == 0)
                    this.CreateBoard(TileMaker.GetColourTiles(8, Colors.Orange, word), 8);
                else if (i % 3 == 1)
                    this.CreateBoard(TileMaker.GetColourTiles(8, Colors.Aqua, word), 8);
                else
                    this.CreateBoard(TileMaker.GetColourTiles(8, Colors.Ivory, word), 8);

                await Task.Delay(200);
            }

            if (isForRecord)
            {
                this.tbGetName.Text = "";
                this.Second.IsPaneOpen = true;
                this.gridGetName.Visibility = Visibility.Visible;  // make visiable to put player name
            }
        }

        /// \brief  GreetingFlickingScreen()
        ///
        /// \details <b>Details</b>
        /// - Game screen to display each letter passed in graphically
        ///
        /// \param word - <b>string</b> - word to display
        /// 
        /// \return void <b>N/A</b> -N/A
        private async void GreetingFlickingScreen(string word)
        {
            for (int i = 0; i < word.Length; ++i)
            {
                string tmp = word.Substring(0, i + 1);
                if (i % 7 == 0)
                    this.CreateBoard(TileMaker.GetColourTiles(8, Colors.White, tmp), 8);
                else if (i % 7 == 1)
                    this.CreateBoard(TileMaker.GetColourTiles(8, Colors.GreenYellow, tmp), 8);
                else if (i % 7 == 2)
                    this.CreateBoard(TileMaker.GetColourTiles(8, Colors.Gray, tmp), 8);
                else if (i % 7 == 3)
                    this.CreateBoard(TileMaker.GetColourTiles(8, Colors.LimeGreen, tmp), 8);
                else if (i % 7 == 4)
                    this.CreateBoard(TileMaker.GetColourTiles(8, Colors.Gold, tmp), 8);
                else if (i % 7 == 5)
                    this.CreateBoard(TileMaker.GetColourTiles(8, Colors.Aqua, tmp), 8);
                else
                    this.CreateBoard(TileMaker.GetColourTiles(8, Colors.HotPink, tmp), 8);

                await Task.Delay(400);
            }

            this.btnPlay.Visibility = Visibility.Visible; // make play button enable
        }

        /// <summary>
        /// Close game 
        /// </summary>
        private void CloseGame()
        {
            // close the game
            this._isPaused = true;
            this._isGameFinished = true;

            if (this.playtimeTimer.IsRunning)
                playtimeTimer.Stop();

            ImageBrush ib = new ImageBrush();
            ib.ImageSource = new BitmapImage(new Uri("ms-appx:/Assets/play.png"));
            this.btnPlay.Background = ib;
            this.rootPivot.IsLocked = false;

            // enabledAceel = false
        }

        /// \brief  MoveTile()
        ///
        /// \details <b>Details</b>
        /// - Tile movement
        ///
        /// \return void <b>N/A</b> -N/A
        private void MoveTile()
        {
            if ((targetCell != null) && (blankCell != null))
            {
                int targetRow = Grid.GetRow(this.targetCell); // get target cell's row and column
                int targetCol = Grid.GetColumn(this.targetCell);
                int blnakcellRow = Grid.GetRow(this.blankCell); // get target cell's row and column
                int blankcellCol = Grid.GetColumn(this.blankCell);

                // remove cells
                this.gridGame.Children.Remove(this.targetCell);
                this.gridGame.Children.Remove(this.blankCell);

                Grid.SetRow(this.targetCell, blnakcellRow);
                Grid.SetColumn(this.targetCell, blankcellCol);
                Grid.SetRow(this.blankCell, targetRow);
                Grid.SetColumn(this.blankCell, targetCol);

                this.gridGame.Children.Add(this.blankCell);
                this.gridGame.Children.Add(this.targetCell);
            }
        }

        /// <summary>
        /// if all cell is matched retrun true
        /// </summary>
        /// <returns></returns>
        private bool GetAnswer()
        {
            for (int i = 0; i < this.SizeOfGrid; ++i)
            {
                for (int j = 0; j < this.SizeOfGrid; ++j)
                {
                    int index = TileMaker.RowColToIndex(i, j, this.SizeOfGrid);
                    Grid grid = this.FindName("cell" + index.ToString()) as Grid;    // find blank cell

                    int row = Grid.GetRow(grid);
                    int col = Grid.GetColumn(grid);

                    if ((row != i) ||
                        (col != j))
                    {
                        return false;
                    }
                }
            }
            return true;
        }


        /// <summary>
        /// made specific row and col grid
        /// </summary>
        /// <param name="rowNum"></param>
        /// <param name="colNum"></param>
        /// <param name="grid"></param>
        private void SetRowColGridStar(int rowNum, int colNum, Grid grid)
        {
            grid.ColumnDefinitions.Clear(); // clear row and col
            grid.RowDefinitions.Clear();

            // reset row and column definition
            for (int i = 0; i < rowNum; ++i)
            {
                ColumnDefinition c = new ColumnDefinition();
                c.Width = new GridLength(1, GridUnitType.Star);
                grid.ColumnDefinitions.Add(c);
            }

            for (int i = 0; i < colNum; ++i)
            {
                RowDefinition r = new RowDefinition();
                r.Height = new GridLength(1, GridUnitType.Star);
                grid.RowDefinitions.Add(r);
            }
        }

        /// <summary>
        /// set all tiles to grid
        /// </summary>
        /// <param name="list"></param>
        /// <param name="sizeOfGrid"></param>
        private void CreateBoard(List<Grid> list, int sizeOfGrid)
        {
            this.gridGame.Children.Clear();
            this.SetRowColGridStar(sizeOfGrid, sizeOfGrid, this.gridGame);
            int index = 0;
            for (int i = 0; i < sizeOfGrid; ++i)
            {
                for (int j = 0; j < sizeOfGrid; ++j)
                {
                    Grid.SetRow(list[index], i);
                    Grid.SetColumn(list[index], j);
                    this.gridGame.Children.Add(list[index]);
                    ++index;
                }
            }
        }

        /// <summary>
        /// Auto complete game
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private async void First1_Click(object sender, RoutedEventArgs e)
        {
            if (!this._isGameFinished)
            {
                for (int i = 0; i < this.SizeOfGrid; ++i)
                {
                    for (int j = 0; j < this.SizeOfGrid; ++j)
                    {
                        Grid g = this.gridGame.Children.Cast<Grid>().First(k => Grid.GetRow(k) == i && Grid.GetColumn(k) == j) as Grid;
                        string rightName = "cell" + TileMaker.RowColToIndex(i, j, this.SizeOfGrid).ToString();
                        if (g.Name != rightName)    // if it is not the right place
                        {
                            Grid answerCell = this.FindName(rightName) as Grid;    // find blank cell

                            int iRow = Grid.GetRow(g); // get target cell's row and column
                            int iCol = Grid.GetColumn(g);
                            int answerCellRow = Grid.GetRow(answerCell); // get target cell's row and column
                            int answerCellCol = Grid.GetColumn(answerCell);

                            // remove cells
                            this.gridGame.Children.Remove(g);
                            this.gridGame.Children.Remove(answerCell);

                            Grid.SetRow(g, answerCellRow);
                            Grid.SetColumn(g, answerCellCol);
                            Grid.SetRow(answerCell, iRow);
                            Grid.SetColumn(answerCell, iCol);

                            this.gridGame.Children.Add(g);
                            this.gridGame.Children.Add(answerCell);
                            await Task.Delay(200);
                        }
                    }
                }
            }
        }

        /// <summary>
        /// when borad button clicked update board record
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btnBoard_Click(object sender, RoutedEventArgs e)
        {
            this.Second.IsPaneOpen = !this.Second.IsPaneOpen;
        }

        /// <summary>
        /// after enter player name for record is add to list.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btnGetName_Click(object sender, RoutedEventArgs e)
        {
            Player p = new Player(this.playtimeTimer.Elapsed,
                                   this.tbGetName.Text);
            this.leaderBoard.Add(p);
            this.CopyBoard();

            DataAccess.WriteLocalFile(this.leaderBoard);
            this.gridGetName.Visibility = Visibility.Collapsed;
        }

        /// <summary>
        /// when page loaded, it show greeting splash and disable the play button
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>          
        private void Page_Loaded_1(object sender, RoutedEventArgs e)
        {
            this.btnPlay.Visibility = Visibility.Collapsed;
            this.GreetingFlickingScreen("Welcome.");
        }

        /// \brief  ImageGridView_ItemClick()
        ///
        /// \details <b>Details</b>
        /// - Handle image chosen for game by saving and cutting image for either a small
        ///   3x3 (9 tile) game or large 4x4 (16 tile) game.
        ///
        /// \param sender - <b>object</b> - Object interacted with
        /// \param e - <b>ItemClickEventArgs</b> - Item Click Event Argument
        /// 
        /// \return void <b>N/A</b> -N/A
        private async void ImageGridView_ItemClick(object sender, ItemClickEventArgs e)
        {
            // Get image
            Picture pic = new Picture();
            pic = (Picture)e.ClickedItem;
            this.puzzleImgList.Clear();

            // Get file path
            StorageFile imageFile = await StorageFile.GetFileFromPathAsync(pic.FilePath);

            if (imageFile == null)
            {
                return;
            }

            /* Create a IRandomAccessStream and stream the image into the object */
            var imageStream = await imageFile.OpenAsync(Windows.Storage.FileAccessMode.Read);

            // Create tile pieces images from image chosen for either small or large game
            for (uint i = 0; i < this.SizeOfGrid; i++)
            {
                for (uint j = 0; j < this.SizeOfGrid; j++)
                {
                    await Task.Delay(10);
                    /* Decode the file stream */
                    BitmapDecoder bitmapDecoder = await BitmapDecoder.CreateAsync(imageStream);

                    /* Encoder */
                    InMemoryRandomAccessStream inMemRAS = new InMemoryRandomAccessStream();

                    BitmapEncoder bitmapEncoder = await BitmapEncoder.CreateForTranscodingAsync(inMemRAS, bitmapDecoder);

                    /* Transform bitmap to a (square) symetrical shape in case the image is not square */
                    bitmapEncoder.BitmapTransform.ScaledHeight = 2000;
                    bitmapEncoder.BitmapTransform.ScaledWidth = 2000;

                    /* Set the size of each individual game tile and location of tile to be created */
                    BitmapBounds bitmapBounds = new BitmapBounds();

                    // Set width and height
                    bitmapBounds.Height = bitmapEncoder.BitmapTransform.ScaledHeight/ (uint)SizeOfGrid;
                    bitmapBounds.Width = bitmapBounds.Height;

                    // Set coordinates of beginning of image crop
                    bitmapBounds.X += bitmapBounds.Width * j;
                    bitmapBounds.Y += bitmapBounds.Height * i;

                    bitmapEncoder.BitmapTransform.Bounds = bitmapBounds;

                    /* Commit and flush all image data */
                    try
                    {
                        await bitmapEncoder.FlushAsync();
                    }
                    catch (Exception ex)
                    {
                        string s = ex.ToString();
                    }

                    /* Create a BitmapImage and set its source to the stream */
                    BitmapImage bitmapTile = new BitmapImage();
                    bitmapTile.SetSource(inMemRAS);

                    ImageBrush ib = new ImageBrush();
                    ib.ImageSource = bitmapTile;

                    Rectangle rec = new Rectangle();
                    rec.Fill = ib;
                    this.puzzleImgList.Add(rec);
                }
            }
        }

        /// \brief  SetupImageList()
        ///
        /// \details <b>Details</b>
        /// - Populate Leaderboard
        ///
        /// \return allPictures <b>Task<ObservableCollection<StorageFile>></b> -Storage file collection
        private async Task<ObservableCollection<StorageFile>> SetupImageList()
        {
            // Get access to picture library
            pictureFolder = KnownFolders.PicturesLibrary;
            // Create storage file
            ObservableCollection<StorageFile> allPictures = new ObservableCollection<StorageFile>();
            // Get all pictures in all folders within the SavedPictures folder
            await RetrieveFilesInFolders(allPictures, pictureFolder);

            return allPictures;
        }

        /// \brief  RetrieveFilesInFolders()
        ///
        /// \details <b>Details</b>
        /// - Populate Leaderboard
        ///
        /// \param pictureList - <b>ObservableCollection<StorageFile></b> - Picture list
        /// \param pictureFolder - <b>StorageFolder</b> - Folder where pictures are
        /// 
        /// \return void <b>N/A</b> -N/A
        /// \references
        // UWP-064 @ channel9.msdn.com/Series/Windows-10-development-for-absolute-beginners/
        // https://channel9.msdn.com/Series/Windows-10-development-for-absolute-beginners/UWP-064-Album-Cover-Match-Game-Setup-and-Working-with-Files-and-System-Folders?ocid=EntriesInArea
        // Create an asyncronous threading task which retrieves all files in all folders within the SavedPictures folder
        private async Task RetrieveFilesInFolders(ObservableCollection<StorageFile> pictureList, StorageFolder pictureFolder)
        {
            // Get each jpeg file inside current folder and add to a list
            foreach (StorageFile item in await pictureFolder.GetFilesAsync())
            {
                if ((item.FileType == ".jpg") || (item.FileType == ".jpeg") || (item.FileType == ".png"))
                {
                    pictureList.Add(item);
                }
            }

            // Get each jpeg file in each folder (call current function we're already in)
            foreach (StorageFolder item in await pictureFolder.GetFoldersAsync())
            {
                await RetrieveFilesInFolders(pictureList, item);
            }
        }

        /// \brief  PrepareNewGame()
        ///
        /// \details <b>Details</b>
        /// - Clear and re-populate the image list
        ///
        /// \return void <b>N/A</b> -N/A
        private async Task PrepareNewGame()
        {
            Images.Clear();

            // Choose random pictures
            List<StorageFile> gameImageList = await GetImageList(allPictures);

            // Get metadata from pictures
            await PopulatePictureList(gameImageList);
        }

        /// \brief  GetImageList()
        ///
        /// \details <b>Details</b>
        /// - Create the game image list into a List from the Observable collection
        ///
        /// \param allPictures - <b>ObservableCollection<StorageFile></b> - Observable collection of Storage files
        /// 
        /// \return gameImageList <b>List<StorageFile></b> - populated game image list
        private async Task<List<StorageFile>> GetImageList(ObservableCollection<StorageFile> allPictures)
        {
            List<StorageFile> gameImageList = new List<StorageFile>();

            foreach (StorageFile pic in allPictures)
            {
                gameImageList.Add(pic);
            }

            return gameImageList;
        }

        /// \brief  PopulatePictureList()
        ///
        /// \details <b>Details</b>
        /// - Populate the Picture (class) list with the Bitmap image and file path
        ///
        /// \param gameImageList - <b>List<StorageFile></b> - List of StorageFiles
        /// \return void <b>N/A</b> -N/A
        private async Task PopulatePictureList(List<StorageFile> gameImageList)
        {
            foreach (StorageFile file in gameImageList)
            {
                IRandomAccessStreamWithContentType fileStream = await file.OpenReadAsync();

                BitmapImage gameImage = new BitmapImage();
                gameImage.SetSource(fileStream);

                Picture picture = new Picture();
                picture.GameImage = gameImage;
                picture.FilePath = file.Path;

                Images.Add(picture);
            }
        }

        /// \brief  TakePicture_Click()
        ///
        /// \details <b>Details</b>
        /// - Capturing, saving and populating the game image list with the user
        ///   taken camera picture
        ///
        /// \param sender - <b>object</b> - Object interacted with
        /// \param e - <b>RoutedEventArgs</b> - Event Argument
        /// 
        /// \return void <b>N/A</b> -N/A
        private async void TakePicture_Click(object sender, RoutedEventArgs e)
        {
            CameraCaptureUI captureUI = new CameraCaptureUI();
            captureUI.PhotoSettings.Format = CameraCaptureUIPhotoFormat.Jpeg;
            captureUI.PhotoSettings.CroppedSizeInPixels = new Size(500, 500);

            StorageFile photo = await captureUI.CaptureFileAsync(CameraCaptureUIMode.Photo);

            if (photo == null)
            {
                // User cancelled photo capture
                return;
            }
            await photo.CopyAsync(pictureFolder);

            IRandomAccessStreamWithContentType fileStream = await photo.OpenReadAsync();
            BitmapImage gameImage = new BitmapImage();
            gameImage.SetSource(fileStream);
            Picture picture = new Picture();
            picture.GameImage = gameImage;
            picture.FilePath = photo.Path;
            Images.Add(picture);
        }

        /// \brief  ImageMainGrid_Loaded()
        ///
        /// \details <b>Details</b>
        /// - Setup of picture grid apon user selection/opened
        ///
        /// \param sender - <b>object</b> - Object interacted with
        /// \param e - <b>RoutedEventArgs</b> - Event Argument
        /// 
        /// \return void <b>N/A</b> -N/A
        private async void ImageMainGrid_Loaded(object sender, RoutedEventArgs e)
        {
            StartupProgressRing.IsActive = true;
            allPictures = await SetupImageList();
            await PrepareNewGame();
            StartupProgressRing.IsActive = false;
        }

        /// \brief  Number_Unchecked()
        ///
        /// \details <b>Details</b>
        /// - Check if numbers are chosen for display in the game
        ///
        /// \param sender - <b>object</b> - Object interacted with
        /// \param e - <b>RoutedEventArgs</b> - Event Argument
        /// 
        /// \return void <b>N/A</b> -N/A
        private void Number_Unchecked(object sender, RoutedEventArgs e)
        {
            if (!(bool)cbImageBoard.IsChecked)
                cbImageBoard.IsChecked = true;
        }

        /// \brief  Image_Unchecked()
        ///
        /// \details <b>Details</b>
        /// - Check if images are chosen for display in the game
        ///
        /// \param sender - <b>object</b> - Object interacted with
        /// \param e - <b>RoutedEventArgs</b> - Event Argument
        /// 
        /// \return void <b>N/A</b> -N/A
        private void Image_Unchecked(object sender, RoutedEventArgs e)
        {
            if (!(bool)cbNumberBoard.IsChecked)
                cbNumberBoard.IsChecked = true;
        }

        /// \brief  SetBoardVisibilities()
        ///
        /// \details <b>Details</b>
        /// - Set the overall board display setup/visibility options
        ///
        /// \return void <b>N/A</b> -N/A
        private void SetBoardVisibilities()
        {
            int max = this.SizeOfGrid * this.SizeOfGrid;
            for(int i=0;i< max; ++i)
            {
                Grid cell = this.FindName("cell" + i.ToString()) as Grid;    // find blank cell
                Rectangle rec = cell.Children.OfType<Rectangle>().FirstOrDefault();
                TextBlock tb = cell.Children.OfType<TextBlock>().FirstOrDefault();
                if (cbImageBoard.IsChecked == true)
                    rec.Visibility = Visibility.Visible;
                else
                {
                    rec.Visibility = Visibility.Collapsed;
                }
                    

                if (cbNumberBoard.IsChecked == true)
                    tb.Visibility = Visibility.Visible;
                else
                    tb.Visibility = Visibility.Collapsed;
            }
        }

        /// \brief  ToggleSwitch_Toggled()
        ///
        /// \details <b>Details</b>
        /// - Check if acceclerometer is chosen for tile movement game play
        ///
        /// \param sender - <b>object</b> - Object interacted with
        /// \param e - <b>RoutedEventArgs</b> - Event Argument
        /// 
        /// \return void <b>N/A</b> -N/A
        private void ToggleSwitch_Toggled(object sender, RoutedEventArgs e)
        {
            if (this.tsAceel.IsOn)
                this._enabledAccel = true;
            else
                this._enabledAccel = false;
        }
    }
}
