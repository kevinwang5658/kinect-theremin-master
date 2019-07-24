using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using NAudio;
using NAudio.Wave;
using NAudio.CoreAudioApi;
using NAudio.Midi;
using Microsoft.Kinect;
using System.Threading.Tasks;

namespace kinect_theremin
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        // Object for playing the sine wave
        private SineWavePlayer _player;

        // Kinect helper
        private KinectHelper _helper;

        // Player handedness
        private JointType _freqHand;
        private JointType _ampHand;

        // Intervals (for drawing guides/determining frequency);
        private double _freqInterval;
        //double _ampInterval;

        // Boolean to enable/disable frequency guides
        private bool _enableGuides = true;

        private MidiOut _midiOut;

        private Dictionary<int, int> keyMapping = new Dictionary<int, int>()
        {
            {-1, 76},
            {0, 74},
            {1, 72},
            {2, 71},
            {3, 69},
            {4, 67},
            {5, 65},
            {6, 64},
            {7, 62},
            {8, 60},
            {9, 59},
            {10, 57},
            {11, 55},
            {12, 53},
            {13, 52},
            {14, 50},
            {15, 48}
        };

        // MainWindow Constructor 
        public MainWindow()
        {
            InitializeComponent();
            // Instantiate the wave player
            _player = new SineWavePlayer();

            for (int device = 0; device < MidiOut.NumberOfDevices; device++)
            {
                midiSelectorBox.Items.Add(MidiOut.DeviceInfo(device).ProductName);
            }
            if (midiSelectorBox.Items.Count > 0)
            {
                midiSelectorBox.SelectedIndex = 0;
            }
            
            _player.Frequency = _player.MinFreq;
            // Set the starting handedness
            _freqHand = JointType.HandRight;
            _ampHand = JointType.HandLeft;
            // Draw the guides
            _freqInterval = guideCanvas.Width / 10;
            //_ampInterval = guideCanvas.Height / 4;
            //DrawGuides();
            DrawGuidesThreshold();
            // Instantiate and initialize the KinectHelper
            _helper = new KinectHelper(true, false, true);
            _helper.ToggleSeatedMode(true);
            _helper.SkeletonDataChanged += new KinectHelper.SkeletonDataChangedEvent(SkeletonDataChange);
            skeletonImage.Source = _helper.skeletonBitmap;
            rgbImage.Source = _helper.colorBitmap;
        }

        private void midiOption_Changed(object sender, SelectionChangedEventArgs e)
        {
            Console.WriteLine("aad");

            if (_midiOut != null)
            {
                _midiOut.Dispose();
                _midiOut = null;
            }

            Console.WriteLine(midiSelectorBox.SelectedIndex);
            _midiOut = new MidiOut(midiSelectorBox.SelectedIndex);
        }

        // Event handler for the leftHandCheckbox's Checked/Unchecked events
        // Used to toggle the user's handedness
        private void leftHandCheckbox_Checked(object sender, RoutedEventArgs e)
        {
            ToggleHandedness();
        }

        int counter = 0;

        // Event handler for KinectHelper.SkeletonDataChanged event
        // Used to get the positions of the user's hands and control the theremin

        private Point freqHandPosPrev = new Point(0, 0);
        private Point ampHandPosPrev = new Point(0, 0);
        private long prevTime = DateTime.Now.Millisecond;

        private void SkeletonDataChange(object o, SkeletonDataChangeEventArgs e)
        {
            // Get the primary skeleton (the first one being tracked)
            Skeleton skel = null;
            for (int i = 0; i < e.skeletons.Length; i++)
            {
                if (e.skeletons[i].TrackingState == SkeletonTrackingState.Tracked)
                {
                    skel = e.skeletons[i];
                    break;
                }
            }
            // If no skeletons found, no need to continue
            if (skel == null)
                return;

            // Get the left and right hand positions from the skeleton
            Point freqHandPos = _helper.SkeletonPointToScreen(skel.Joints[_freqHand].Position);
            Point ampHandPos = _helper.SkeletonPointToScreen(skel.Joints[_ampHand].Position);

            // Determine the frequency based on the position of the right hand
            double freqValue = 1 - freqHandPos.X / skeletonImage.Width;
            float customFreq;
            // If guides are enabled, determine the exact chromatic note to play 

            Console.WriteLine(freqHandPos.Y / skeletonImage.Height);
            //if (freqHandPos.Y / skeletonImage.Height < 0.45)

            double vyFreq = (freqHandPos.Y - freqHandPosPrev.Y) / (DateTime.Now.Millisecond - prevTime);
            double vyAmp = (ampHandPos.Y - ampHandPosPrev.Y) / (DateTime.Now.Millisecond - prevTime);

            Console.WriteLine(vyFreq + "  " + vyAmp);

            if (freqHandPos.Y / skeletonImage.Height < 0.45)
                NoteToSound(freqValue, freqHandPos.X / skeletonImage.Width);
            else
                note = null;

            double freqValue1 = 1 - ampHandPos.X / skeletonImage.Width;
           if (ampHandPos.Y / skeletonImage.Height < 0.45)
            //if (vyAmp > 0.5)
                GetChromaticNoteFrequency1(freqValue1, ampHandPos.X / skeletonImage.Width);
            else
                note1 = null;
            // If not, determine the frequency based on the exact position  

            freqHandPosPrev = freqHandPos;
            ampHandPosPrev = ampHandPos;


        }

        // Draw frequency "guides" for differentiating each note
        private void DrawGuides()
        {
            
            // Draw the frequency guides
            for (int i = 0; i < 10; i++)
            {
                Rectangle guide = new Rectangle();
                guide.Width = 5;
                guide.Height = guideCanvas.Height;
                guide.Fill = new SolidColorBrush(Colors.Gray);
                //guide.Stroke = new SolidColorBrush(Colors.Black);
                guide.StrokeThickness = 1;
                guideCanvas.Children.Add(guide);
                Canvas.SetLeft(guide,_freqInterval * (i + 1));
                Canvas.SetTop(guide, 0);
            }

        }

        private void DrawGuidesThreshold()
        {
            Console.WriteLine("abc");
            
            double locationY = guideCanvas.Height * 0.45;
            Rectangle guide = new Rectangle();
            guide.Width = guideCanvas.Width;
            guide.Height = 5;
            guide.Fill = new SolidColorBrush(Colors.LightSkyBlue);
            guide.StrokeThickness = 1;
            guideCanvas.Children.Add(guide);
            Canvas.SetLeft(guide, 0);
            Canvas.SetTop(guide, locationY);
        }

        // Clear frequency guides
        private void ClearGuides()
        {
            guideCanvas.Children.Clear();
        }

        Note note = null;

        async private void NoteToSound(double x, double y)
        {
            if (note == null) {
                note = new Note();
            }

            int chromaticNote = (int) Math.Ceiling(x * 10);
            double chromaticValue = (double) chromaticNote / 12;
            if (note.note != (int)chromaticNote && Math.Abs(note.position - x) > (1.0/12))
            {
                note.position = x;
                note.note = (int)chromaticNote;
                int channel = 1;
                var noteOnEvent = new NoteOnEvent(1000, channel, keyMapping[note.note], 100, 1000);
                _midiOut.Send(noteOnEvent.GetAsShortMessage());
                Console.WriteLine(chromaticNote + " --- " + chromaticValue);

                

                //await Task.Delay(2500);

                //_midiOut.Reset();

                //_midiOut.Send(new NoteEvent(1000, channel, MidiCommandCode.NoteOff, keyMapping[note], 100).GetAsShortMessage());
            }
        }

        Note note1 = null;

        async private void GetChromaticNoteFrequency1(double x, double y)
        {

            if (note1 == null)
            {
                note1 = new Note();
            }

            int chromaticNote = (int)Math.Ceiling(x * 10);
            double chromaticValue = (double)chromaticNote / 12;
            if (note1.note != (int)chromaticNote && Math.Abs(note1.position - x) > (1.0/12))
            {
                note1.note = (int)chromaticNote;
                note1.position = x;
                int channel = 1;
                var noteOnEvent = new NoteOnEvent(1000, channel, keyMapping[note1.note], 100, 1000);
                _midiOut.Send(noteOnEvent.GetAsShortMessage());
                Console.WriteLine(chromaticNote + " --- " + chromaticValue);

                await Task.Delay(800);

                //_midiOut.Send(new NoteEvent(1000, channel, MidiCommandCode.NoteOff, keyMapping[note1], 100).GetAsShortMessage());
            }
        }

        private void sendOn(int note)
        {
        }

        // Event handler for the useGuidesCheckbox's Checked and Unchecked events
        // Used to toggle _enableGuides, play discrete notes or just the current frequency.
        private void useGuidesCheckbox_Checked(object sender, RoutedEventArgs e)
        {

            // If guides are currently enabled, disable them and clear them from the canvas
            if (_enableGuides)
            {
                _enableGuides = false;
                ClearGuides();
            }
            // If guides are currently disabled, enable them and draw them on the canvas
            else
            {
                _enableGuides = true;
                DrawGuides();
            }

        }

        class Note {
            public int note = 0;
            public double position = -1000;
        }
    }
}
