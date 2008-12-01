//David Morrison & Christian Cox
//11-25-08
//WebVideoLibrary

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using System.IO;
using openCV;

namespace WebVideoLibrary
{
    public partial class frmMain : Form
    {
        /// <summary>
        /// An enumeration for each clip in Tier 3
        /// </summary>
        private enum Tier3Clip
        {
            Clip1 = 1,
            Clip2 = 2,
            Clip3 = 3,
            Clip4 = 4
        }

        public frmMain()
        {
            InitializeComponent();
        }


        /// <summary>
        /// Fires first thing after the form loads
        /// </summary>
        private void frmMain_Load(object sender, EventArgs e)
        {
            openFileDialog1.Filter = "Video files|*.avi;*.divx;*.mpg;*.wmv;*.mp4|All Files|*.*";
            openFileDialog1.FileName = string.Empty; //initial filename when box pops up = nothing
            txtVideoInputPath.Text = "c:\\temp\\bouncing_ball.divx";//so we dont have to browse everytime we test

            //load the output codec combobox with some predefined FOURCC's
            cboOutputCodec.Items.Add(new ListItem("DivX", cvlib.CvCreateFourCC('D', 'I', 'V', 'X')));
            cboOutputCodec.Items.Add(new ListItem("Uncompressed DIB", cvlib.CvCreateFourCC('D', 'I', 'B', ' ')));
            cboOutputCodec.Items.Add(new ListItem("Motion JPG", cvlib.CvCreateFourCC('M', 'J', 'P', 'G')));
            cboOutputCodec.SelectedIndex = 0;
        }


        /// <summary>
        /// Fires when the user clicks the open file button
        /// </summary>
        private void btnOpenFile_Click(object sender, EventArgs e)
        {
            DialogResult result = openFileDialog1.ShowDialog();

            if (result == DialogResult.OK)
            {
                txtVideoInputPath.Text = openFileDialog1.FileName;
            }
        }


        /// <summary>
        /// Fires when the user clicks the start button.
        /// </summary>
        private void btnStart_Click(object sender, EventArgs e)
        {
            //Check to make sure the file exists.
            if (!File.Exists(txtVideoInputPath.Text))
            {
                MessageBox.Show("Files does not exist!");
                return;
            }

            //we will need this when we actually do every file in a directory instead of just 1 video
            string[] files = Directory.GetFiles("c:\\temp", "*.*", SearchOption.TopDirectoryOnly); //fild all the files in that path with that pattern

            //open the file input
            CvCapture capture = cvlib.CvCreateFileCapture(txtVideoInputPath.Text);

            //check to make sure it opened ok
            if (capture.ptr == IntPtr.Zero)
            {
                MessageBox.Show("Creation of File Capture failed. Check to make sure the correct codec is installed.");
                return;
            }

            int numTotalFramesInVideo = (int)cvlib.cvGetCaptureProperty(capture, cvlib.CV_CAP_PROP_FRAME_COUNT);
            double fps = cvlib.cvGetCaptureProperty(capture, cvlib.CV_CAP_PROP_FPS);
            int fourcc = (int)cvlib.cvGetCaptureProperty(capture, cvlib.CV_CAP_PROP_FOURCC);
            double vidLengthInSeconds = numTotalFramesInVideo / fps;

            AppendLogLine("Input Video Codec FourCC: " + FromFourCC(fourcc));
            AppendLogLine("Input Video FPS: " + fps);
            AppendLogLine("Input Video total # frames in video: " + numTotalFramesInVideo);
            AppendLogLine("Input Video length in seconds: " + Math.Round(vidLengthInSeconds, 2, MidpointRounding.AwayFromZero));


            //We need to grab the first frame before being able to get the width and height
            IplImage image = cvlib.CvQueryFrame(ref capture);
            int vidWidth = (int)cvlib.cvGetCaptureProperty(capture, cvlib.CV_CAP_PROP_FRAME_WIDTH);
            int vidHeight = (int)cvlib.cvGetCaptureProperty(capture, cvlib.CV_CAP_PROP_FRAME_HEIGHT);
            Bitmap bmpImage = cvlib.ToBitmap(image, false);

            Dictionary<Tier3Clip, DominantColorCalculator> dominantColorCalculators = new Dictionary<Tier3Clip, DominantColorCalculator>();
            dominantColorCalculators.Add(Tier3Clip.Clip1, new DominantColorCalculator(vidHeight, vidWidth));
            dominantColorCalculators.Add(Tier3Clip.Clip2, new DominantColorCalculator(vidHeight, vidWidth));
            dominantColorCalculators.Add(Tier3Clip.Clip3, new DominantColorCalculator(vidHeight, vidWidth));
            dominantColorCalculators.Add(Tier3Clip.Clip4, new DominantColorCalculator(vidHeight, vidWidth));


            int numFramesPerTier2Clip = numTotalFramesInVideo / 2;
            AppendLogLine("Clips in tier 2 should have: " + numFramesPerTier2Clip + " frames");
            if (numFramesPerTier2Clip * 2 != numTotalFramesInVideo)
            {
                int difference = numTotalFramesInVideo - (numFramesPerTier2Clip * 2);
                AppendLogLine("Last clip in tier 2 should have: " + (numFramesPerTier2Clip + difference) + " frames");
                //add this difference # of frames to the last clip to make up for integer division
            }
            int numFramesPerTier3Clip = numFramesPerTier2Clip / 2;
            AppendLogLine("Clips in tier 3 should have: " + numFramesPerTier3Clip + " frames");
            if (numFramesPerTier3Clip * 4 != numTotalFramesInVideo)
            {
                int difference = numTotalFramesInVideo - (numFramesPerTier3Clip * 4);
                AppendLogLine("Last clip in tier 3 should have: " + (numFramesPerTier3Clip + difference) + " frames");
                //add this difference # of frames to the last clip to make up for integer division
            }

            int tier2Clip = 1; //Start out on tier 2 clip 1, this will go from 1 -> 2
            int tier3Clip = 1; //Start out on tier 3 clip 1, this will go from 1 -> 4

            //get the selected output FourCC from the combo box.
            int outputFourCC = (int)((ListItem)cboOutputCodec.SelectedItem).Value;
            CvSize outputVideoSize = new CvSize(vidWidth, vidHeight);
            CvVideoWriter tier2VidWriter = cvlib.CvCreateVideoWriter(GetOutputPath(2, tier2Clip), outputFourCC, fps, outputVideoSize, 1);
            CvVideoWriter tier3VidWriter = cvlib.CvCreateVideoWriter(GetOutputPath(3, tier3Clip), outputFourCC, fps, outputVideoSize, 1);

            cvlib.CvWriteFrame(tier2VidWriter, ref image);
            cvlib.CvWriteFrame(tier3VidWriter, ref image);

            //increment the number of frames shown counters
            int totalNumFramesUsed = 1;
            int tier2FramesUsed = 1;
            int tier3FramesUsed = 1;

            //flip the input image so it will display on screen properly
            cvlib.CvFlip(ref image, ref image, 0);

            //show the first frame before the while loop
            ShowImage(bmpImage);

            while (totalNumFramesUsed < numTotalFramesInVideo)
            {
                if ((tier2FramesUsed == numFramesPerTier2Clip) && (tier2Clip != 2))
                {
                    AppendLogLine("Frames written to tier 2, clip " + tier2Clip + " = " + tier2FramesUsed);
                    tier2Clip++;
                    tier2FramesUsed = 0;
                    cvlib.CvReleaseVideoWriter(ref tier2VidWriter);
                    tier2VidWriter = cvlib.CvCreateVideoWriter(GetOutputPath(2, tier2Clip), outputFourCC, fps, outputVideoSize, 1);
                }
                if (tier3FramesUsed == numFramesPerTier3Clip && (tier3Clip != 4))
                {
                    AppendLogLine("Frames written to tier 3, clip " + tier3Clip + " = " + tier3FramesUsed);
                    tier3Clip++;
                    tier3FramesUsed = 0;
                    cvlib.CvReleaseVideoWriter(ref tier3VidWriter);
                    tier3VidWriter = cvlib.CvCreateVideoWriter(GetOutputPath(3, tier3Clip), outputFourCC, fps, outputVideoSize, 1);
                }

                //We need to grab the next frame to show
                image = cvlib.CvQueryFrame(ref capture);

                cvlib.CvWriteFrame(tier2VidWriter, ref image);
                cvlib.CvWriteFrame(tier3VidWriter, ref image);

                //increment the number of frames shown counters
                totalNumFramesUsed++;
                tier2FramesUsed++;
                tier3FramesUsed++;

                cvlib.CvFlip(ref image, ref image, 0);

                Bitmap btmpImage = cvlib.ToBitmap(image, false);
                ShowImage(btmpImage);

                Application.DoEvents();
            }

            AppendLogLine("Frames written to tier 2, clip " + tier2Clip + " = " + tier2FramesUsed);
            AppendLogLine("Frames written to tier 3, clip " + tier3Clip + " = " + tier3FramesUsed);
            cvlib.CvReleaseVideoWriter(ref tier3VidWriter);
            cvlib.CvReleaseVideoWriter(ref tier2VidWriter);
            cvlib.CvReleaseCapture(ref capture);
        }

   
        /// <summary>
        /// Appends the text passed in with a new line character and places it in the log textbox.
        /// </summary>
        private void AppendLogLine(string text)
        {
            txtLog.AppendText(text + Environment.NewLine);
        }


        /// <summary>
        /// Gets the output path for a video file, per tier, per clip
        /// </summary>
        private string GetOutputPath(int tier, int clip)
        {
            string dir = Path.GetDirectoryName(txtVideoInputPath.Text);
            string file = Path.GetFileNameWithoutExtension(txtVideoInputPath.Text);
            string extension = ".avi"; //always use .avi, if you use .divx, or .mjpg it doesn't write the frames for some reason.

            return dir + "\\" + file + "-Tier" + tier + "Clip" + clip + extension;
        }

        /// <summary>
        /// Returns the FourCC human readable characters
        /// </summary>
        private static string FromFourCC(int fourCC)
        {
            char[] chars = new char[4];
            chars[0] = (char)(fourCC & 0xFF);
            chars[1] = (char)((fourCC >> 8) & 0xFF);
            chars[2] = (char)((fourCC >> 16) & 0xFF);
            chars[3] = (char)((fourCC >> 24) & 0xFF);

            return new string(chars);
        }


        /// <summary>
        /// Shows the image in the picturebox on the form
        /// </summary>
        /// <param name="image">The image to show in the picturebox.</param>
        private void ShowImage(Bitmap image)
        {
            if (pictureBox.Image != null)
            {
                pictureBox.Image.Dispose();
            }
            pictureBox.Image = image;


            //make sure the form is big enough to show the complete image.
            if (pictureBox.Height < image.Height)
            {
                this.Height += (image.Height - pictureBox.Height);
            }

            if (pictureBox.Width < image.Width)
            {
                this.Width += (image.Width - pictureBox.Width);
            }
        }
    }
}