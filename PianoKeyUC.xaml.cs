using NAudio.Midi;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace PianoApp
{
    /// <summary>
    /// PianoKeyUC.xaml 的交互逻辑
    /// </summary>
    public partial class PianoKeyUC : UserControl
    {
        MidiIn midiIn = null;
        int midiInIndex = -1;
        int midiOutIndex = -1;
        MidiOut midiOut = null;

        //音符频率
        private int mOctave = 4;    // 默认八度（八度范围为1到7）
        private static int MIN_OCTAVE = 1;
        private static int MAX_OCTAVE = 7;


        //默认频率，假设这是第四个八度
        private int CNOTE = 60;
        /// <summary>
        /// 升C
        /// </summary>
        private int CSHARPNOTE = 61;
        private int DNOTE = 62;
        /// <summary>
        /// 升D
        /// </summary>
        private int DSHARPNOTE = 63;
        private int ENOTE = 64;
        private int FNOTE = 65;
        /// <summary>
        /// 升F
        /// </summary>
        private int FSHARPNOTE = 66;
        private int GNOTE = 67;
        /// <summary>
        /// 升G
        /// </summary>
        private int GSHARPNOTE = 68;
        private int ANOTE = 69;
        /// <summary>
        /// 升A
        /// </summary>
        private int ASHARPNOTE = 70;
        private int BNOTE = 71;

        // 音符名称
        private const string CKEY = "c";
        private const string DKEY = "d";
        private const string EKEY = "e";
        private const string FKEY = "f";
        private const string GKEY = "g";
        private const string AKEY = "a";
        private const string BKEY = "b";

        private const string CSHARPKEY = "csharp";
        private const string DSHARPKEY = "dsharp";
        private const string FSHARPKEY = "fsharp";
        private const string GSHARPKEY = "gsharp";
        private const string ASHARPKEY = "asharp";


        //黑键的宽度和长度作为百分比
        private const double BLACK_KEY_WIDTH_PERCENT = 60;
        private const double BLACK_KEY_HEIGHT_PERCENT = 60;

        private List<Border> whiteBorder = new List<Border>();

        private List<Border> blackBorder = new List<Border>();

        private Double mBlackHeight = 0.0;
        private Double mBlackWidth = 0.0;

        private int mNumOctaves = 1;       // 默认显示1个八度
        private int mColumns = 6;            // 默认每个八度有7列
        private int mStartOctave = 3;      // 默认起始和结束八度
        private int mStopOctave = 5;

        private bool mLoaded = false;

        public PianoKeyUC()
        {
            InitializeComponent();
            try
            {
                midiOut = new MidiOut(0);
            }
            catch (Exception ex)
            {
            }
        }

        #region 音频处理部分 

        /// <summary>
        /// 设置默认音符频率
        /// </summary>
        private void SetNotes()
        {
            int C4 = 60;
            int CSHARP4 = 61;
            int D4 = 62;
            int DSHARP4 = 63;
            int E4 = 64;
            int F4 = 65;
            int FSHARP4 = 66;
            int G4 = 67;
            int GSHARP4 = 68;
            int A4 = 69;
            int ASHARP4 = 70;
            int B4 = 71;

            int change = 0;


            CNOTE = C4 + change;
            CSHARPNOTE = CSHARP4 + change;
            DNOTE = D4 + change;
            DSHARPNOTE = DSHARP4 + change;
            ENOTE = E4 + change;
            FNOTE = F4 + change;
            FSHARPNOTE = FSHARP4 + change;
            GNOTE = G4 + change;
            GSHARPNOTE = GSHARP4 + change;
            ANOTE = A4 + change;
            ASHARPNOTE = ASHARP4 + change;
            BNOTE = B4 + change;

        }

        /// <summary>
        /// 根据当前的八度改变音符频率
        /// </summary>
        /// <param name="note"></param>
        /// <param name="octave"></param>
        /// <returns></returns>
        private int SetNoteAsPerOctave(int note, int octave)
        {
            int change = 0;//octave == 3
            if (octave == 3)
                change = -12;
            else if (octave == 2)
                change = -24;
            else if (octave == 1)
                change = -36;
            else if (octave == 5)
                change = 12;
            else if (octave == 6)
                change = 24;
            else if (octave == 7)
                change = 36;
            return note + change;
        }

        /// <summary>
        /// 设置当前八度。这用于设置单个八度（起始 = 结束）
        /// </summary>
        /// <param name="o"></param>
        public void SetOctave(int o)
        {
            if (o < MIN_OCTAVE)
                o = MIN_OCTAVE;
            if (o > MAX_OCTAVE)
                o = MAX_OCTAVE;
            mOctave = o;
            mStartOctave = o;
            mStopOctave = o;
            mNumOctaves = 1;
            InitUI();
            SetNotes();
        }

        /// <summary>
        /// 设置多个八度，其中起始 < 结束
        /// </summary>
        /// <param name="start"></param>
        /// <param name="stop"></param>
        public void SetMultipleOctaves(int start, int stop)
        {

            if (start < MIN_OCTAVE)
                start = MIN_OCTAVE;
            if (start > MAX_OCTAVE)
                start = MAX_OCTAVE;

            if (stop < MIN_OCTAVE)
                stop = MIN_OCTAVE;
            if (stop > MAX_OCTAVE)
                stop = MAX_OCTAVE;
            if (stop < start)
                stop = start;
            if (stop == start)
            {
                SetOctave(start);
                return;
            }
            mOctave = start;
            mStartOctave = start;
            mStopOctave = stop;
            mNumOctaves = stop - start + 1;
            InitUI();
            SetNotes();

        }

        public int GetOctave() { return mOctave; }

        /// <summary>
        /// 获取MIDI输入和输出设备的列表
        /// </summary>
        void ListDevices()
        {
            Console.WriteLine("Midi in devices");
            for (int i = 0; i < MidiIn.NumberOfDevices; i++)
            {
                midiInIndex = 0;
                Console.WriteLine(MidiIn.DeviceInfo(i).ProductName);
            }
            Console.WriteLine("Midi Out devices");
            for (int i = 0; i < MidiOut.NumberOfDevices; i++)
            {
                midiOutIndex = i;
                Console.WriteLine(MidiOut.DeviceInfo(i).ProductName);
            }

        }

        /// <summary>
        /// 发送midi命令播放音符频率固定持续时间
        /// </summary>
        /// <param name="note"></param>
        void PlayNote(int note)
        {
            try
            {
                if (midiOutIndex == -1)
                    return;
                //midiOut.Reset();
                midiOut.Send(MidiMessage.StartNote(note, 127, 1).RawData);
                Console.WriteLine("play note" + note);
            }
            catch (Exception ex)
            {
            }
        }

        /// <summary>
        /// 发送midi命令停止播放一个音符
        /// </summary>
        /// <param name="note"></param>
        void StopNote(int note)
        {
            if (midiOutIndex == -1)
                return;
            midiOut.Send(MidiMessage.StopNote(note, 127, 1).RawData);
        }

        /// <summary>
        /// 将按键名称映射到音调
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        int NoteNameToSound(string name)
        {
            int retVal = 0;

            if (name == CKEY)
                retVal = CNOTE;
            else if (name == DKEY)
                retVal = DNOTE;
            else if (name == EKEY)
                retVal = ENOTE;
            else if (name == FKEY)
                retVal = FNOTE;
            else if (name == GKEY)
                retVal = GNOTE;
            else if (name == AKEY)
                retVal = ANOTE;
            else if (name == BKEY)
                retVal = BNOTE;

            else if (name == CSHARPKEY)
                retVal = CSHARPNOTE;
            else if (name == DSHARPKEY)
                retVal = DSHARPNOTE;
            else if (name == FSHARPKEY)
                retVal = FSHARPNOTE;
            else if (name == GSHARPKEY)
                retVal = GSHARPNOTE;
            else if (name == ASHARPKEY)
                retVal = ASHARPNOTE;

            return retVal;
        }

        #endregion ****************************************************************************************

        #region UI处理部分 

        /// <summary>
        ///初始化键盘，矩形，和Grid约束
        /// </summary>
        private void InitUI()
        {
            grd.Children.Clear();
            whiteBorder.Clear();
            blackBorder.Clear();
            mLoaded = false;
            grd.Width = this.ActualWidth - 30;
            grd.Height = this.ActualHeight;
            grd.HorizontalAlignment = HorizontalAlignment.Left;
            grd.VerticalAlignment = VerticalAlignment.Top;
            grd.ShowGridLines = false;
            grd.Background = new SolidColorBrush(Colors.White);

            grd.RowDefinitions.Clear();
            grd.ColumnDefinitions.Clear();

            for (int i = 1; i <= mNumOctaves; i++)
            {
                for (int j = 0; j <= mColumns; j++)
                {
                    ColumnDefinition col = new ColumnDefinition();
                    col.Width = new GridLength(1, GridUnitType.Star);
                    grd.ColumnDefinitions.Add(col);
                }
            }
            RowDefinition row = new RowDefinition();
            row.Height = new GridLength(1, GridUnitType.Star);
            grd.RowDefinitions.Add(row);

            mBlackHeight = this.Height * (BLACK_KEY_HEIGHT_PERCENT / 100);
            mBlackWidth = 100.0 * (BLACK_KEY_WIDTH_PERCENT / 100); // 实际上这个宽度将是白键宽度的百分比
                                                                   // 这将在ResizeUI()中设置

            int gridCol = 0;
            int gridColumns = grd.ColumnDefinitions.Count;
            // 白键和黑键交错
            for (int octaves = mStartOctave; octaves <= mStopOctave; octaves++)
            {
                for (int i = 0; i <= mColumns; i++)
                {
                    Border border = new Border();
                    if (gridColumns == 21)
                    {
                        if (gridCol == 7)
                        {
                            border.CornerRadius = new CornerRadius(2);
                            border.Background = new SolidColorBrush(Colors.White);
                            border.Height = Double.NaN;
                            border.Margin = new Thickness(0, 0, 0, 0);
                            border.BorderThickness = new Thickness(2);
                            border.BorderBrush = new SolidColorBrush(Colors.Black);
                            TextBlock textBlock = new TextBlock();
                            textBlock.Text = "C";
                            if (SystemParameters.WorkArea.Width >= 1280)
                            {
                                textBlock.FontSize = 25;
                            }
                            else
                            {
                                textBlock.FontSize = 16;
                            }
                            textBlock.FontWeight = FontWeights.Bold;
                            textBlock.Foreground = new SolidColorBrush(Colors.Black);
                            textBlock.VerticalAlignment = VerticalAlignment.Bottom;
                            textBlock.HorizontalAlignment = HorizontalAlignment.Center;
                            textBlock.Margin = new Thickness(5, 0, 5, 10);
                            textBlock.IsHitTestVisible = false;
                            border.Child = textBlock;
                        }
                        else if (gridCol == 8)
                        {
                            border.CornerRadius = new CornerRadius(2);
                            border.Background = new SolidColorBrush(Colors.White);
                            border.Height = Double.NaN;
                            border.Margin = new Thickness(0, 0, 0, 0);
                            border.BorderThickness = new Thickness(2);
                            border.BorderBrush = new SolidColorBrush(Colors.Black);
                            TextBlock textBlock = new TextBlock();
                            textBlock.Text = "D";
                            if (SystemParameters.WorkArea.Width >= 1280)
                            {
                                textBlock.FontSize = 25;
                            }
                            else
                            {
                                textBlock.FontSize = 16;
                            }
                            textBlock.FontWeight = FontWeights.Bold;
                            textBlock.Foreground = new SolidColorBrush(Colors.Black);
                            textBlock.VerticalAlignment = VerticalAlignment.Bottom;
                            textBlock.HorizontalAlignment = HorizontalAlignment.Center;
                            textBlock.Margin = new Thickness(5, 0, 5, 10);
                            textBlock.IsHitTestVisible = false;
                            border.Child = textBlock;
                        }
                        else if (gridCol == 9)
                        {
                            border.CornerRadius = new CornerRadius(2);
                            border.Background = new SolidColorBrush(Colors.White);
                            border.Height = Double.NaN;
                            border.Margin = new Thickness(0, 0, 0, 0);
                            border.BorderThickness = new Thickness(2);
                            border.BorderBrush = new SolidColorBrush(Colors.Black);
                            TextBlock textBlock = new TextBlock();
                            textBlock.Text = "E";
                            if (SystemParameters.WorkArea.Width >= 1280)
                            {
                                textBlock.FontSize = 25;
                            }
                            else
                            {
                                textBlock.FontSize = 16;
                            }
                            textBlock.FontWeight = FontWeights.Bold;
                            textBlock.Foreground = new SolidColorBrush(Colors.Black);
                            textBlock.VerticalAlignment = VerticalAlignment.Bottom;
                            textBlock.HorizontalAlignment = HorizontalAlignment.Center;
                            textBlock.Margin = new Thickness(5, 0, 5, 10);
                            textBlock.IsHitTestVisible = false;
                            border.Child = textBlock;
                        }
                        else if (gridCol == 10)
                        {
                            border.CornerRadius = new CornerRadius(2);
                            border.Background = new SolidColorBrush(Colors.White);
                            border.Height = Double.NaN;
                            border.Margin = new Thickness(0, 0, 0, 0);
                            border.BorderThickness = new Thickness(2);
                            border.BorderBrush = new SolidColorBrush(Colors.Black);
                            TextBlock textBlock = new TextBlock();
                            textBlock.Text = "F";
                            if (SystemParameters.WorkArea.Width >= 1280)
                            {
                                textBlock.FontSize = 25;
                            }
                            else
                            {
                                textBlock.FontSize = 16;
                            }
                            textBlock.FontWeight = FontWeights.Bold;
                            textBlock.Foreground = new SolidColorBrush(Colors.Black);
                            textBlock.VerticalAlignment = VerticalAlignment.Bottom;
                            textBlock.HorizontalAlignment = HorizontalAlignment.Center;
                            textBlock.Margin = new Thickness(5, 0, 5, 10);
                            textBlock.IsHitTestVisible = false;
                            border.Child = textBlock;
                        }
                        else if (gridCol == 11)
                        {
                            border.CornerRadius = new CornerRadius(2);
                            border.Background = new SolidColorBrush(Colors.White);
                            border.Height = Double.NaN;
                            border.Margin = new Thickness(0, 0, 0, 0);
                            border.BorderThickness = new Thickness(2);
                            border.BorderBrush = new SolidColorBrush(Colors.Black);
                            TextBlock textBlock = new TextBlock();
                            textBlock.Text = "G";
                            if (SystemParameters.WorkArea.Width >= 1280)
                            {
                                textBlock.FontSize = 25;
                            }
                            else
                            {
                                textBlock.FontSize = 16;
                            }
                            textBlock.FontWeight = FontWeights.Bold;
                            textBlock.Foreground = new SolidColorBrush(Colors.Black);
                            textBlock.VerticalAlignment = VerticalAlignment.Bottom;
                            textBlock.HorizontalAlignment = HorizontalAlignment.Center;
                            textBlock.Margin = new Thickness(5, 0, 5, 10);
                            textBlock.IsHitTestVisible = false;
                            border.Child = textBlock;
                        }
                        else if (gridCol == 12)
                        {
                            border.CornerRadius = new CornerRadius(2);
                            border.Background = new SolidColorBrush(Colors.White);
                            border.Height = Double.NaN;
                            border.Margin = new Thickness(0, 0, 0, 0);
                            border.BorderThickness = new Thickness(2);
                            border.BorderBrush = new SolidColorBrush(Colors.Black);
                            TextBlock textBlock = new TextBlock();
                            textBlock.Text = "A";
                            if (SystemParameters.WorkArea.Width >= 1280)
                            {
                                textBlock.FontSize = 25;
                            }
                            else
                            {
                                textBlock.FontSize = 16;
                            }
                            textBlock.FontWeight = FontWeights.Bold;
                            textBlock.Foreground = new SolidColorBrush(Colors.Black);
                            textBlock.VerticalAlignment = VerticalAlignment.Bottom;
                            textBlock.HorizontalAlignment = HorizontalAlignment.Center;
                            textBlock.Margin = new Thickness(5, 0, 5, 10);
                            textBlock.IsHitTestVisible = false;
                            border.Child = textBlock;
                        }
                        else if (gridCol == 13)
                        {
                            border.CornerRadius = new CornerRadius(2);
                            border.Background = new SolidColorBrush(Colors.White);
                            border.Height = Double.NaN;
                            border.Margin = new Thickness(0, 0, 0, 0);
                            border.BorderThickness = new Thickness(2);
                            border.BorderBrush = new SolidColorBrush(Colors.Black);
                            if (octaves == mStartOctave && i == 0)
                            {
                                border.SizeChanged += border_SizeChanged;
                            }
                            TextBlock textBlock = new TextBlock();
                            textBlock.Text = "B";
                            if (SystemParameters.WorkArea.Width >= 1280)
                            {
                                textBlock.FontSize = 25;
                            }
                            else
                            {
                                textBlock.FontSize = 16;
                            }
                            textBlock.FontWeight = FontWeights.Bold;
                            textBlock.Foreground = new SolidColorBrush(Colors.Black);
                            textBlock.VerticalAlignment = VerticalAlignment.Bottom;
                            textBlock.HorizontalAlignment = HorizontalAlignment.Center;
                            textBlock.Margin = new Thickness(5, 0, 5, 10);
                            textBlock.IsHitTestVisible = false;
                            border.Child = textBlock;
                        }
                        else
                        {
                            border.CornerRadius = new CornerRadius(2);
                            border.Background = new SolidColorBrush(Colors.White);
                            border.Height = Double.NaN;
                            border.Margin = new Thickness(0, 0, 0, 0);
                            border.BorderThickness = new Thickness(2);
                            border.BorderBrush = new SolidColorBrush(Colors.Black);
                        }
                    }
                    else
                    {
                        border.CornerRadius = new CornerRadius(2);
                        border.Background = new SolidColorBrush(Colors.White);
                        border.Height = Double.NaN;
                        border.Margin = new Thickness(0, 0, 0, 0);
                        border.BorderThickness = new Thickness(2);
                        border.BorderBrush = new SolidColorBrush(Colors.Black);
                    }
                    border.Tag = octaves;
                    // 设置钢琴白键名
                    switch (i)
                    {
                        case 0:
                            border.Name = CKEY;
                            break;
                        case 1:
                            border.Name = DKEY;
                            break;
                        case 2:
                            border.Name = EKEY;
                            break;
                        case 3:
                            border.Name = FKEY;
                            break;
                        case 4:
                            border.Name = GKEY;
                            break;
                        case 5:
                            border.Name = AKEY;
                            break;
                        case 6:
                            border.Name = BKEY;
                            break;
                    }
                    border.MouseLeftButtonDown += border_MouseLeftButtonDown; ;
                    border.MouseLeftButtonUp += border_MouseLeftButtonUp;
                    border.MouseEnter += border_MouseEnter;
                    border.MouseLeave += border_MouseLeave;
                    if (octaves == mStartOctave && i == 0)
                    {
                        border.SizeChanged += border_SizeChanged;
                    }
                    Grid.SetRow(border, 0);
                    Grid.SetColumn(border, gridCol);
                    grd.Children.Add(border);
                    whiteBorder.Add(border);
                    if (i == 0)
                    {
                        //csharp
                        border = new Border();
                        border.CornerRadius = new CornerRadius(2);
                        border.Background = new SolidColorBrush(Colors.Black);
                        border.Height = mBlackHeight;
                        border.Width = mBlackWidth;
                        border.Margin = new Thickness(0, 0, 0, 0);
                        border.BorderThickness = new Thickness(2);
                        border.BorderBrush = new SolidColorBrush(Colors.Black);
                        border.VerticalAlignment = VerticalAlignment.Top;
                        border.HorizontalAlignment = HorizontalAlignment.Center;
                        border.MouseLeftButtonDown += border_MouseLeftButtonDown; ;
                        border.MouseLeftButtonUp += border_MouseLeftButtonUp;
                        border.MouseEnter += border_MouseEnter;
                        border.MouseLeave += border_MouseLeave;
                        border.Name = CSHARPKEY;
                        border.Tag = octaves;
                        if (gridColumns == 21)
                        {
                            if (gridCol == 7)
                            {
                                StackPanel stackPanelUD = new StackPanel();
                                stackPanelUD.Orientation = Orientation.Vertical;
                                stackPanelUD.VerticalAlignment = VerticalAlignment.Center;
                                stackPanelUD.HorizontalAlignment = HorizontalAlignment.Center;

                                StackPanel stackPanel = new StackPanel();
                                stackPanel.Margin = new Thickness(0, 0, 0, 10);
                                stackPanel.Orientation = Orientation.Horizontal;
                                stackPanel.HorizontalAlignment = HorizontalAlignment.Center;
                                stackPanel.IsHitTestVisible = false;
                                TextBlock textBlocka = new TextBlock();
                                textBlocka.Text = "D";
                                if (SystemParameters.WorkArea.Width >= 1280)
                                {
                                    textBlocka.FontSize = 22;
                                    stackPanel.Height = 30;
                                }
                                else
                                {
                                    textBlocka.FontSize = 16;
                                    stackPanel.Height = 19;
                                }
                                textBlocka.FontWeight = FontWeights.Bold;
                                textBlocka.Foreground = new SolidColorBrush(Colors.White);
                                textBlocka.VerticalAlignment = VerticalAlignment.Bottom;
                                stackPanel.Children.Add(textBlocka);
                                TextBlock textBlockb = new TextBlock();
                                textBlockb.Text = "b";
                                if (SystemParameters.WorkArea.Width >= 1280)
                                {
                                    textBlockb.FontSize = 13;
                                }
                                else
                                {
                                    textBlockb.FontSize = 5;
                                }
                                textBlockb.FontWeight = FontWeights.Bold;
                                textBlockb.Foreground = new SolidColorBrush(Colors.White);
                                textBlockb.VerticalAlignment = VerticalAlignment.Top;
                                stackPanel.Children.Add(textBlockb);
                                stackPanelUD.Children.Add(stackPanel);

                                StackPanel stackPanels = new StackPanel();
                                stackPanels.Margin = new Thickness(0, 0, 0, 10);
                                stackPanels.Orientation = Orientation.Horizontal;
                                stackPanels.HorizontalAlignment = HorizontalAlignment.Center;
                                stackPanels.IsHitTestVisible = false;
                                TextBlock textBlockc = new TextBlock();
                                textBlockc.Text = "C";
                                if (SystemParameters.WorkArea.Width >= 1280)
                                {
                                    textBlockc.FontSize = 22;
                                    stackPanels.Height = 30;
                                }
                                else
                                {
                                    textBlockc.FontSize = 16;
                                    stackPanels.Height = 19;
                                }
                                textBlockc.FontWeight = FontWeights.Bold;
                                textBlockc.Foreground = new SolidColorBrush(Colors.White);
                                textBlockc.VerticalAlignment = VerticalAlignment.Bottom;
                                stackPanels.Children.Add(textBlockc);
                                TextBlock textBlockd = new TextBlock();
                                textBlockd.Text = "#";
                                if (SystemParameters.WorkArea.Width >= 1280)
                                {
                                    textBlockd.FontSize = 13;
                                }
                                else
                                {
                                    textBlockd.FontSize = 5;
                                }
                                textBlockd.FontWeight = FontWeights.Bold;
                                textBlockd.Foreground = new SolidColorBrush(Colors.White);
                                textBlockd.VerticalAlignment = VerticalAlignment.Top;
                                stackPanels.Children.Add(textBlockd);

                                stackPanelUD.Children.Add(stackPanels);
                                border.Child = stackPanelUD;
                            }
                        }
                        Grid.SetRow(border, 0);
                        Grid.SetColumn(border, gridCol);
                        Grid.SetColumnSpan(border, 2);
                        Grid.SetZIndex(border, 1);
                        grd.Children.Add(border);
                        blackBorder.Add(border);
                    }

                    else if (i == 1)
                    {
                        //dsharp
                        border = new Border();
                        border.CornerRadius = new CornerRadius(2);
                        border.Background = new SolidColorBrush(Colors.Black);
                        border.Height = mBlackHeight;
                        border.Width = mBlackWidth;
                        border.Margin = new Thickness(0, 0, 0, 0);
                        border.BorderThickness = new Thickness(2);
                        border.BorderBrush = new SolidColorBrush(Colors.Black);
                        border.VerticalAlignment = VerticalAlignment.Top;
                        border.HorizontalAlignment = HorizontalAlignment.Center;
                        border.MouseLeftButtonDown += border_MouseLeftButtonDown; ;
                        border.MouseLeftButtonUp += border_MouseLeftButtonUp;
                        border.MouseEnter += border_MouseEnter;
                        border.MouseLeave += border_MouseLeave;
                        border.Name = DSHARPKEY;
                        border.Tag = octaves;
                        if (gridColumns == 21)
                        {
                            if (gridCol == 8)
                            {
                                StackPanel stackPanelUD = new StackPanel();
                                stackPanelUD.Orientation = Orientation.Vertical;
                                stackPanelUD.VerticalAlignment = VerticalAlignment.Center;
                                stackPanelUD.HorizontalAlignment = HorizontalAlignment.Center;

                                StackPanel stackPanel = new StackPanel();
                                stackPanel.Margin = new Thickness(0, 0, 0, 10);
                                stackPanel.Orientation = Orientation.Horizontal;
                                stackPanel.HorizontalAlignment = HorizontalAlignment.Center;
                                stackPanel.IsHitTestVisible = false;
                                TextBlock textBlocka = new TextBlock();
                                textBlocka.Text = "E";
                                if (SystemParameters.WorkArea.Width >= 1280)
                                {
                                    textBlocka.FontSize = 22;
                                    stackPanel.Height = 30;
                                }
                                else
                                {
                                    textBlocka.FontSize = 16;
                                    stackPanel.Height = 19;
                                }
                                textBlocka.FontWeight = FontWeights.Bold;
                                textBlocka.Foreground = new SolidColorBrush(Colors.White);
                                textBlocka.VerticalAlignment = VerticalAlignment.Bottom;
                                stackPanel.Children.Add(textBlocka);
                                TextBlock textBlockb = new TextBlock();
                                textBlockb.Text = "b";
                                if (SystemParameters.WorkArea.Width >= 1280)
                                {
                                    textBlockb.FontSize = 13;
                                }
                                else
                                {
                                    textBlockb.FontSize = 5;
                                }
                                textBlockb.FontWeight = FontWeights.Bold;
                                textBlockb.Foreground = new SolidColorBrush(Colors.White);
                                textBlockb.VerticalAlignment = VerticalAlignment.Top;
                                stackPanel.Children.Add(textBlockb);
                                stackPanelUD.Children.Add(stackPanel);


                                StackPanel stackPanels = new StackPanel();
                                stackPanels.Margin = new Thickness(0, 0, 0, 10);
                                stackPanels.Orientation = Orientation.Horizontal;
                                stackPanels.HorizontalAlignment = HorizontalAlignment.Center;
                                stackPanels.IsHitTestVisible = false;
                                TextBlock textBlockc = new TextBlock();
                                textBlockc.Text = "D";
                                if (SystemParameters.WorkArea.Width >= 1280)
                                {
                                    textBlockc.FontSize = 22;
                                    stackPanels.Height = 30;
                                }
                                else
                                {
                                    textBlockc.FontSize = 16;
                                    stackPanels.Height = 19;
                                }
                                textBlockc.FontWeight = FontWeights.Bold;
                                textBlockc.Foreground = new SolidColorBrush(Colors.White);
                                textBlockc.VerticalAlignment = VerticalAlignment.Bottom;
                                stackPanels.Children.Add(textBlockc);
                                TextBlock textBlockd = new TextBlock();
                                textBlockd.Text = "#";
                                if (SystemParameters.WorkArea.Width >= 1280)
                                {
                                    textBlockd.FontSize = 13;
                                }
                                else
                                {
                                    textBlockd.FontSize = 5;
                                }
                                textBlockd.FontWeight = FontWeights.Bold;
                                textBlockd.Foreground = new SolidColorBrush(Colors.White);
                                textBlockd.VerticalAlignment = VerticalAlignment.Top;
                                stackPanels.Children.Add(textBlockd);

                                stackPanelUD.Children.Add(stackPanels);
                                border.Child = stackPanelUD;
                            }
                        }
                        Grid.SetRow(border, 0);
                        Grid.SetColumn(border, gridCol);
                        Grid.SetColumnSpan(border, 2);
                        Grid.SetZIndex(border, 1);
                        grd.Children.Add(border);
                        blackBorder.Add(border);
                    }

                    else if (i == 2)//不做操作
                    {
                    }

                    else if (i == 3)
                    {
                        //fsharp
                        border = new Border();
                        border.CornerRadius = new CornerRadius(2);
                        border.Background = new SolidColorBrush(Colors.Black);
                        border.Height = mBlackHeight;
                        border.Width = mBlackWidth;
                        border.Margin = new Thickness(0, 0, 0, 0);
                        border.BorderThickness = new Thickness(2);
                        border.BorderBrush = new SolidColorBrush(Colors.Black);
                        border.VerticalAlignment = VerticalAlignment.Top;
                        border.HorizontalAlignment = HorizontalAlignment.Center;
                        border.MouseLeftButtonDown += border_MouseLeftButtonDown; ;
                        border.MouseLeftButtonUp += border_MouseLeftButtonUp;
                        border.MouseEnter += border_MouseEnter;
                        border.MouseLeave += border_MouseLeave;
                        border.Name = FSHARPKEY;
                        border.Tag = octaves;
                        if (gridColumns == 21)
                        {
                            if (gridCol == 10)
                            {
                                StackPanel stackPanelUD = new StackPanel();
                                stackPanelUD.Orientation = Orientation.Vertical;
                                stackPanelUD.VerticalAlignment = VerticalAlignment.Center;
                                stackPanelUD.HorizontalAlignment = HorizontalAlignment.Center;

                                StackPanel stackPanel = new StackPanel();
                                stackPanel.Margin = new Thickness(0, 0, 0, 10);
                                stackPanel.Orientation = Orientation.Horizontal;
                                stackPanel.HorizontalAlignment = HorizontalAlignment.Center;
                                stackPanel.IsHitTestVisible = false;
                                TextBlock textBlocka = new TextBlock();
                                textBlocka.Text = "G";
                                if (SystemParameters.WorkArea.Width >= 1280)
                                {
                                    textBlocka.FontSize = 22;
                                    stackPanel.Height = 30;
                                }
                                else
                                {
                                    textBlocka.FontSize = 16;
                                    stackPanel.Height = 19;
                                }
                                textBlocka.FontWeight = FontWeights.Bold;
                                textBlocka.Foreground = new SolidColorBrush(Colors.White);
                                textBlocka.VerticalAlignment = VerticalAlignment.Bottom;
                                stackPanel.Children.Add(textBlocka);
                                TextBlock textBlockb = new TextBlock();
                                textBlockb.Text = "b";
                                if (SystemParameters.WorkArea.Width >= 1280)
                                {
                                    textBlockb.FontSize = 13;
                                }
                                else
                                {
                                    textBlockb.FontSize = 5;
                                }
                                textBlockb.FontWeight = FontWeights.Bold;
                                textBlockb.Foreground = new SolidColorBrush(Colors.White);
                                textBlockb.VerticalAlignment = VerticalAlignment.Top;
                                stackPanel.Children.Add(textBlockb);
                                stackPanelUD.Children.Add(stackPanel);


                                StackPanel stackPanels = new StackPanel();
                                stackPanels.Margin = new Thickness(0, 0, 0, 10);
                                stackPanels.Orientation = Orientation.Horizontal;
                                stackPanels.HorizontalAlignment = HorizontalAlignment.Center;
                                stackPanels.IsHitTestVisible = false;
                                TextBlock textBlockc = new TextBlock();
                                textBlockc.Text = "F";
                                if (SystemParameters.WorkArea.Width >= 1280)
                                {
                                    textBlockc.FontSize = 22;
                                    stackPanels.Height = 30;
                                }
                                else
                                {
                                    textBlockc.FontSize = 16;
                                    stackPanels.Height = 19;
                                }
                                textBlockc.FontWeight = FontWeights.Bold;
                                textBlockc.Foreground = new SolidColorBrush(Colors.White);
                                textBlockc.VerticalAlignment = VerticalAlignment.Bottom;
                                stackPanels.Children.Add(textBlockc);
                                TextBlock textBlockd = new TextBlock();
                                textBlockd.Text = "#";
                                if (SystemParameters.WorkArea.Width >= 1280)
                                {
                                    textBlockd.FontSize = 13;
                                }
                                else
                                {
                                    textBlockd.FontSize = 5;
                                }
                                textBlockd.FontWeight = FontWeights.Bold;
                                textBlockd.Foreground = new SolidColorBrush(Colors.White);
                                textBlockd.VerticalAlignment = VerticalAlignment.Top;
                                stackPanels.Children.Add(textBlockd);

                                stackPanelUD.Children.Add(stackPanels);
                                border.Child = stackPanelUD;
                            }
                        }
                        Grid.SetRow(border, 0);
                        Grid.SetColumn(border, gridCol);
                        Grid.SetColumnSpan(border, 2);
                        Grid.SetZIndex(border, 1);
                        grd.Children.Add(border);
                        blackBorder.Add(border);
                    }
                    else if (i == 4)
                    {
                        //gsharp
                        border = new Border();
                        border.CornerRadius = new CornerRadius(2);
                        border.Background = new SolidColorBrush(Colors.Black);
                        border.Height = mBlackHeight;
                        border.Width = mBlackWidth;
                        border.Margin = new Thickness(0, 0, 0, 0);
                        border.BorderThickness = new Thickness(2);
                        border.BorderBrush = new SolidColorBrush(Colors.Black);
                        border.VerticalAlignment = VerticalAlignment.Top;
                        border.HorizontalAlignment = HorizontalAlignment.Center;
                        border.MouseLeftButtonDown += border_MouseLeftButtonDown; ;
                        border.MouseLeftButtonUp += border_MouseLeftButtonUp;
                        border.MouseEnter += border_MouseEnter;
                        border.MouseLeave += border_MouseLeave;
                        border.Name = GSHARPKEY;
                        border.Tag = octaves;
                        if (gridColumns == 21)
                        {
                            if (gridCol == 11)
                            {
                                StackPanel stackPanelUD = new StackPanel();
                                stackPanelUD.Orientation = Orientation.Vertical;
                                stackPanelUD.VerticalAlignment = VerticalAlignment.Center;
                                stackPanelUD.HorizontalAlignment = HorizontalAlignment.Center;

                                StackPanel stackPanel = new StackPanel();
                                stackPanel.Margin = new Thickness(0, 0, 0, 10);
                                stackPanel.Orientation = Orientation.Horizontal;
                                stackPanel.HorizontalAlignment = HorizontalAlignment.Center;
                                stackPanel.IsHitTestVisible = false;
                                TextBlock textBlocka = new TextBlock();
                                textBlocka.Text = "A";
                                if (SystemParameters.WorkArea.Width >= 1280)
                                {
                                    textBlocka.FontSize = 22;
                                    stackPanel.Height = 30;
                                }
                                else
                                {
                                    textBlocka.FontSize = 16;
                                    stackPanel.Height = 19;
                                }
                                textBlocka.FontWeight = FontWeights.Bold;
                                textBlocka.Foreground = new SolidColorBrush(Colors.White);
                                textBlocka.VerticalAlignment = VerticalAlignment.Bottom;
                                stackPanel.Children.Add(textBlocka);
                                TextBlock textBlockb = new TextBlock();
                                textBlockb.Text = "b";
                                if (SystemParameters.WorkArea.Width >= 1280)
                                {
                                    textBlockb.FontSize = 13;
                                }
                                else
                                {
                                    textBlockb.FontSize = 5;
                                }
                                textBlockb.FontWeight = FontWeights.Bold;
                                textBlockb.Foreground = new SolidColorBrush(Colors.White);
                                textBlockb.VerticalAlignment = VerticalAlignment.Top;
                                stackPanel.Children.Add(textBlockb);
                                stackPanelUD.Children.Add(stackPanel);


                                StackPanel stackPanels = new StackPanel();
                                stackPanels.Margin = new Thickness(0, 0, 0, 10);
                                stackPanels.Orientation = Orientation.Horizontal;
                                stackPanels.HorizontalAlignment = HorizontalAlignment.Center;
                                stackPanels.IsHitTestVisible = false;
                                TextBlock textBlockc = new TextBlock();
                                textBlockc.Text = "G";
                                if (SystemParameters.WorkArea.Width >= 1280)
                                {
                                    textBlockc.FontSize = 22;
                                    stackPanels.Height = 30;
                                }
                                else
                                {
                                    textBlockc.FontSize = 16;
                                    stackPanels.Height = 19;
                                }
                                textBlockc.FontWeight = FontWeights.Bold;
                                textBlockc.Foreground = new SolidColorBrush(Colors.White);
                                textBlockc.VerticalAlignment = VerticalAlignment.Bottom;
                                stackPanels.Children.Add(textBlockc);
                                TextBlock textBlockd = new TextBlock();
                                textBlockd.Text = "#";
                                if (SystemParameters.WorkArea.Width >= 1280)
                                {
                                    textBlockd.FontSize = 13;
                                }
                                else
                                {
                                    textBlockd.FontSize = 5;
                                }
                                textBlockd.FontWeight = FontWeights.Bold;
                                textBlockd.Foreground = new SolidColorBrush(Colors.White);
                                textBlockd.VerticalAlignment = VerticalAlignment.Top;
                                stackPanels.Children.Add(textBlockd);

                                stackPanelUD.Children.Add(stackPanels);
                                border.Child = stackPanelUD;
                            }
                        }
                        Grid.SetRow(border, 0);
                        Grid.SetColumn(border, gridCol);
                        Grid.SetColumnSpan(border, 2);
                        Grid.SetZIndex(border, 1);
                        grd.Children.Add(border);
                        blackBorder.Add(border);
                    }
                    else if (i == 5)
                    {
                        //asharp
                        border = new Border();
                        border.CornerRadius = new CornerRadius(2);
                        border.Background = new SolidColorBrush(Colors.Black);
                        border.Height = mBlackHeight;
                        border.Width = mBlackWidth;
                        border.Margin = new Thickness(0, 0, 0, 0);
                        border.BorderThickness = new Thickness(2);
                        border.BorderBrush = new SolidColorBrush(Colors.Black);
                        border.VerticalAlignment = VerticalAlignment.Top;
                        border.HorizontalAlignment = HorizontalAlignment.Center;
                        border.MouseLeftButtonDown += border_MouseLeftButtonDown; ;
                        border.MouseLeftButtonUp += border_MouseLeftButtonUp;
                        border.MouseEnter += border_MouseEnter;
                        border.MouseLeave += border_MouseLeave;
                        border.Name = ASHARPKEY;
                        border.Tag = octaves;
                        if (gridColumns == 21)
                        {
                            if (gridCol == 12)
                            {
                                StackPanel stackPanelUD = new StackPanel();
                                stackPanelUD.Orientation = Orientation.Vertical;
                                stackPanelUD.VerticalAlignment = VerticalAlignment.Center;
                                stackPanelUD.HorizontalAlignment = HorizontalAlignment.Center;

                                StackPanel stackPanel = new StackPanel();
                                stackPanel.Margin = new Thickness(0, 0, 0, 10);
                                stackPanel.Orientation = Orientation.Horizontal;
                                stackPanel.HorizontalAlignment = HorizontalAlignment.Center;
                                stackPanel.IsHitTestVisible = false;
                                TextBlock textBlocka = new TextBlock();
                                textBlocka.Text = "B";
                                if (SystemParameters.WorkArea.Width >= 1280)
                                {
                                    textBlocka.FontSize = 22;
                                    stackPanel.Height = 30;
                                }
                                else
                                {
                                    textBlocka.FontSize = 16;
                                    stackPanel.Height = 19;
                                }
                                textBlocka.FontWeight = FontWeights.Bold;
                                textBlocka.Foreground = new SolidColorBrush(Colors.White);
                                textBlocka.VerticalAlignment = VerticalAlignment.Bottom;
                                stackPanel.Children.Add(textBlocka);
                                TextBlock textBlockb = new TextBlock();
                                textBlockb.Text = "b";
                                if (SystemParameters.WorkArea.Width >= 1280)
                                {
                                    textBlockb.FontSize = 13;
                                }
                                else
                                {
                                    textBlockb.FontSize = 5;
                                }
                                textBlockb.FontWeight = FontWeights.Bold;
                                textBlockb.Foreground = new SolidColorBrush(Colors.White);
                                textBlockb.VerticalAlignment = VerticalAlignment.Top;
                                stackPanel.Children.Add(textBlockb);
                                stackPanelUD.Children.Add(stackPanel);


                                StackPanel stackPanels = new StackPanel();
                                stackPanels.Margin = new Thickness(0, 0, 0, 10);
                                stackPanels.Orientation = Orientation.Horizontal;
                                stackPanels.HorizontalAlignment = HorizontalAlignment.Center;
                                stackPanels.IsHitTestVisible = false;
                                TextBlock textBlockc = new TextBlock();
                                textBlockc.Text = "A";
                                if (SystemParameters.WorkArea.Width >= 1280)
                                {
                                    textBlockc.FontSize = 22;
                                    stackPanels.Height = 30;
                                }
                                else
                                {
                                    textBlockc.FontSize = 16;
                                    stackPanels.Height = 19;
                                }
                                textBlockc.FontWeight = FontWeights.Bold;
                                textBlockc.Foreground = new SolidColorBrush(Colors.White);
                                textBlockc.VerticalAlignment = VerticalAlignment.Bottom;
                                stackPanels.Children.Add(textBlockc);
                                TextBlock textBlockd = new TextBlock();
                                textBlockd.Text = "#";
                                if (SystemParameters.WorkArea.Width >= 1280)
                                {
                                    textBlockd.FontSize = 13;
                                }
                                else
                                {
                                    textBlockd.FontSize = 5;
                                }
                                textBlockd.FontWeight = FontWeights.Bold;
                                textBlockd.Foreground = new SolidColorBrush(Colors.White);
                                textBlockd.VerticalAlignment = VerticalAlignment.Top;
                                stackPanels.Children.Add(textBlockd);

                                stackPanelUD.Children.Add(stackPanels);
                                border.Child = stackPanelUD;
                            }
                        }
                        Grid.SetRow(border, 0);
                        Grid.SetColumn(border, gridCol);
                        Grid.SetColumnSpan(border, 2);
                        Grid.SetZIndex(border, 1);
                        grd.Children.Add(border);
                        blackBorder.Add(border);
                    }

                    else if (i == 6)//不做操作
                    {
                    }
                    gridCol++;
                }
            }

            #region 添加最后一个键
            if (mStopOctave < 7)
            {
                ColumnDefinition cols = new ColumnDefinition();
                cols.Width = new GridLength(1, GridUnitType.Star);
                grd.ColumnDefinitions.Add(cols);
                Border border = new Border();
                border.CornerRadius = new CornerRadius(2);
                border.Background = new SolidColorBrush(Colors.White);
                border.Height = Double.NaN;
                border.Margin = new Thickness(0, 0, 0, 0);
                border.BorderThickness = new Thickness(2);
                border.BorderBrush = new SolidColorBrush(Colors.Black);
                border.MouseLeftButtonDown += border_MouseLeftButtonDown; ;
                border.MouseLeftButtonUp += border_MouseLeftButtonUp;
                border.MouseEnter += border_MouseEnter;
                border.MouseLeave += border_MouseLeave;
                border.Tag = mStopOctave + 1;
                // 设置键名
                border.Name = CKEY;
                Grid.SetRow(border, 0);
                Grid.SetColumn(border, gridCol + 1);
                grd.Children.Add(border);
                whiteBorder.Add(border);
            }
            #endregion

            mLoaded = true;
        }



        /// <summary>
        /// 检查矩形是否为黑键
        /// </summary>
        /// <param name="r"></param>
        /// <returns></returns>
        private bool IsBlackBorder(Border r)
        {
            bool borderVal = false;
            blackBorder.ForEach((elem) =>
            {
                if (elem == r)
                {
                    borderVal = true;
                }
            });
            return borderVal;
        }
        /// <summary>
        /// 检查矩形是否为黑键Border
        /// </summary>
        /// <param name="r"></param>
        /// <returns></returns>
        private bool isBlackRectBorder(Border r)
        {
            bool retVal = false;
            blackBorder.ForEach((elem) =>
            {
                if (elem == r)
                {
                    retVal = true;
                }
            });
            return retVal;
        }

        /// <summary>
        /// 通过名称和八度获得黑色矩形(键)的名称
        /// </summary>
        /// <param name="name">名字</param>
        /// <param name="octave">八度</param>
        /// <returns></returns>
        private Border GetBlackKeyByName(string name, int octave)
        {
            foreach (Border border in blackBorder)
            {
                if (border.Name == name && (int)border.Tag == octave)
                {
                    return border;
                }
            }
            return null;
        }


        /// <summary>
        /// 当容器调整大小时，强制重新绘制按键
        /// </summary>
        public void ResizeUI()
        {
            if (!mLoaded)
                return;
            grd.Width = this.ActualWidth;
            grd.Height = this.ActualHeight;

            Border border = whiteBorder[0];
            mBlackHeight = this.ActualHeight * (BLACK_KEY_HEIGHT_PERCENT / 100);
            mBlackWidth = border.ActualWidth * (BLACK_KEY_WIDTH_PERCENT / 100);

            blackBorder.ForEach((elem) =>
            {
                elem.Width = mBlackWidth;
                elem.Height = mBlackHeight;
            });
        }

        /// <summary>
        /// 高亮按下的键
        /// </summary>
        /// <param name="rect"></param>
        /// <param name="isBlack"></param>
        public void HighlightKey(Border border, bool isBlack)
        {
            if (!isBlack)
                border.Background = new SolidColorBrush(Colors.Yellow);
            else
            {
                border.Background = new SolidColorBrush(Colors.DarkGray);
                border.BorderBrush = new SolidColorBrush(Colors.DarkGray);
                Border borderPair = null;
                if (border.Name == CSHARPKEY)
                    borderPair = GetBlackKeyByName(CSHARPKEY, (int)border.Tag);

                if (border.Name == DSHARPKEY)
                    borderPair = GetBlackKeyByName(DSHARPKEY, (int)border.Tag);

                if (border.Name == FSHARPKEY)
                    borderPair = GetBlackKeyByName(FSHARPKEY, (int)border.Tag);

                if (border.Name == GSHARPKEY)
                    borderPair = GetBlackKeyByName(GSHARPKEY, (int)border.Tag);

                if (border.Name == ASHARPKEY)
                    borderPair = GetBlackKeyByName(ASHARPKEY, (int)border.Tag);

                if (borderPair != null)
                {
                    borderPair.Background = new SolidColorBrush(Colors.DarkGray);
                }
            }
        }
        /// <summary>
        /// 取消按键的高亮效果
        /// </summary>
        /// <param name="rect"></param>
        /// <param name="isBlack"></param>
        public void UnHighlightKey(Border border, bool isBlack)
        {
            if (!isBlack)
                border.Background = new SolidColorBrush(Colors.Ivory);
            else
            {
                border.Background = new SolidColorBrush(Colors.Black);
                border.BorderBrush = new SolidColorBrush(Colors.Black);
                Border borderPair = null;
                if (border.Name == CSHARPKEY)
                    borderPair = GetBlackKeyByName(CSHARPKEY, (int)border.Tag);

                if (border.Name == DSHARPKEY)
                    borderPair = GetBlackKeyByName(DSHARPKEY, (int)border.Tag);

                if (border.Name == FSHARPKEY)
                    borderPair = GetBlackKeyByName(FSHARPKEY, (int)border.Tag);

                if (border.Name == GSHARPKEY)
                    borderPair = GetBlackKeyByName(GSHARPKEY, (int)border.Tag);

                if (border.Name == ASHARPKEY)
                    borderPair = GetBlackKeyByName(ASHARPKEY, (int)border.Tag);

                if (borderPair != null)
                    borderPair.Background = new SolidColorBrush(Colors.Black);
            }
        }

        /// <summary>
        /// 处理鼠标左键按下事件
        /// </summary>
        /// <param name="r"></param>
        private void BorderLeftButtonDown(Border border)
        {
            Console.WriteLine("left button down");
            HighlightKey(border, IsBlackBorder(border));
            PlayNote(SetNoteAsPerOctave(NoteNameToSound(border.Name), (int)border.Tag));
        }

        /// <summary>
        /// 处理鼠标左键松开事件
        /// </summary>
        /// <param name="r"></param>
        private void BorderLeftButtonUp(Border border)
        {
            Console.WriteLine("left button up");
            UnHighlightKey(border, IsBlackBorder(border));
            StopNote(SetNoteAsPerOctave(NoteNameToSound(border.Name), (int)border.Tag));
        }

        /// <summary>
        /// 处理鼠标离开事件
        /// </summary>
        /// <param name="border"></param>
        /// <param name="e"></param>
        private void BorderMouseLeave(Border border, MouseEventArgs e)
        {
            Console.WriteLine("mouse leave");
            if (e.LeftButton == MouseButtonState.Pressed)
            {
                UnHighlightKey(border, IsBlackBorder(border));
                StopNote(SetNoteAsPerOctave(NoteNameToSound(border.Name), (int)border.Tag));
            }
        }

        /// <summary>
        /// 处理鼠标进入事件
        /// </summary>
        /// <param name="r"></param>
        /// <param name="e"></param>
        private void BorderMouseEnter(Border border, MouseEventArgs e)
        {
            Console.WriteLine("mouse enter");
            if (e.LeftButton == MouseButtonState.Pressed)
            {
                HighlightKey(border, IsBlackBorder(border));
                PlayNote(SetNoteAsPerOctave(NoteNameToSound(border.Name), (int)border.Tag));
            }

        }

        #endregion ****************************************************************************************


        #region 钢琴按键Border事件处理***************************************************************************

        /// <summary>
        /// 鼠标左键点击矩形事件
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void border_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            BorderLeftButtonDown((Border)sender);
        }

        /// <summary>
        /// 鼠标左键抬起矩形事件
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void border_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            BorderLeftButtonUp((Border)sender);
        }

        /// <summary>
        ///  鼠标移出矩形事件
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void border_MouseLeave(object sender, MouseEventArgs e)
        {
            BorderMouseLeave((Border)sender, e);
        }

        /// <summary>
        /// 鼠标进入矩形事件
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void border_MouseEnter(object sender, MouseEventArgs e)
        {
            BorderMouseEnter((Border)sender, e);
        }

        /// <summary>
        /// 矩形大小变化事件
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void border_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            ResizeUI();
        }

        #endregion ********************************************************

        /// <summary>
        /// 网格加载事件
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void grd_Loaded(object sender, RoutedEventArgs e)
        {

        }

        /// <summary>
        /// 网格大小改变事件
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void grd_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            ResizeUI();
        }

        /// <summary>
        /// 界面加载事件
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void UserControl_Loaded(object sender, RoutedEventArgs e)
        {
            ListDevices();
            SetNotes();
            //if (3 == 5)
            //{
            //    SetOctave(3);
            //}
            //else
            //{
            //    SetMultipleOctaves(3, 5);
            //}
        }

        /// <summary>
        /// 界面大小改变事件
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void UserControl_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            ResizeUI();
        }

    }
}
