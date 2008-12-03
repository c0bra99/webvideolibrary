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
using System.Collections;
using DataLayer;

namespace WebVideoLibrary
{
    public partial class frmMain : Form
    {
        /// <summary>
        /// An enumeration for each clip in Tier 2
        /// </summary>
        private enum Tier2Clip
        {
            Clip1 = 1,
            Clip2 = 2,
        }


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

            //get the selected output FourCC from the combo box.
            int outputFourCC = (int)((ListItem)cboOutputCodec.SelectedItem).Value;
            
            //we will need this when we actually do every file in a directory instead of just 1 video
            string[] files = Directory.GetFiles("c:\\temp", "*.*", SearchOption.TopDirectoryOnly); //fild all the files in that path with that pattern
            foreach (string file in files)
	        {
                string videoName = Path.GetFileNameWithoutExtension(file);

                //open the file input
                CvCapture capture = cvlib.CvCreateFileCapture(file);

                //check to make sure it opened ok
                if (capture.ptr == IntPtr.Zero)
                {
                    MessageBox.Show("Creation of File Capture failed. Check to make sure the correct codec is installed.");
                    return;
                }

                lblCurrFrame.Visible = true;
                lblTotalFrames.Visible = true;

                //Get some stats about the video
                int numTotalFramesInVideo = (int)cvlib.cvGetCaptureProperty(capture, cvlib.CV_CAP_PROP_FRAME_COUNT);
                lblTotalFrames.Text = "/ " + numTotalFramesInVideo;
                progressBar1.Maximum = numTotalFramesInVideo;
                progressBar1.Value = 0; //start out @ position 0

                double fps = cvlib.cvGetCaptureProperty(capture, cvlib.CV_CAP_PROP_FPS);
                int fourcc = (int)cvlib.cvGetCaptureProperty(capture, cvlib.CV_CAP_PROP_FOURCC);
                double vidLengthInSeconds = numTotalFramesInVideo / fps;

                AppendLogLine("Input Video Codec FourCC: " + FromFourCC(fourcc));
                AppendLogLine("Input Video FPS: " + fps);
                AppendLogLine("Input Video total # frames in video: " + numTotalFramesInVideo);
                AppendLogLine("Input Video length in seconds: " + Math.Round(vidLengthInSeconds, 2, MidpointRounding.AwayFromZero));

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

                Dictionary<Tier3Clip, DominantColorCalculator> dominantColorCalculators = new Dictionary<Tier3Clip, DominantColorCalculator>(4);

                Tier2Clip currentTier2Clip = Tier2Clip.Clip1; //Start out on tier 2 clip 1, this will go from 1 -> 2
                Tier3Clip currentTier3Clip = Tier3Clip.Clip1; //Start out on tier 3 clip 1, this will go from 1 -> 4

                //increment the number of frames shown counters
                int totalNumFramesUsed = 0;
                int tier2FramesUsed = 0;
                int tier3FramesUsed = 0;

                CvVideoWriter tier2VidWriter = new CvVideoWriter();
                CvVideoWriter tier3VidWriter = new CvVideoWriter();
                CvSize outputVideoSize = new CvSize();

                Hashtable clips = new Hashtable();
                Clip tier2Clip = null;
                Clip tier3Clip = null;

                //This will loop through every frame in the video
                while (totalNumFramesUsed < numTotalFramesInVideo)
                {
                    //We need to grab the next frame to show
                    IplImage image = cvlib.CvQueryFrame(ref capture);

                    if (totalNumFramesUsed == 0)
                    {
                        //the first frame we are working on
                        //We need to grab the first frame before being able to get the width and height
                        int vidWidth = (int)cvlib.cvGetCaptureProperty(capture, cvlib.CV_CAP_PROP_FRAME_WIDTH);
                        int vidHeight = (int)cvlib.cvGetCaptureProperty(capture, cvlib.CV_CAP_PROP_FRAME_HEIGHT);
                        outputVideoSize = new CvSize(vidWidth, vidHeight);

                        tier2VidWriter = cvlib.CvCreateVideoWriter(GetOutputPath(2, (int)currentTier2Clip, file), outputFourCC, fps, outputVideoSize, 1);
                        tier3VidWriter = cvlib.CvCreateVideoWriter(GetOutputPath(3, (int)currentTier3Clip, file), outputFourCC, fps, outputVideoSize, 1);
                        
                        //Get 4 different DominantColorCalculators, 1 for each clip in Tier 3
                        dominantColorCalculators.Add(Tier3Clip.Clip1, new DominantColorCalculator(vidHeight, vidWidth));
                        dominantColorCalculators.Add(Tier3Clip.Clip2, new DominantColorCalculator(vidHeight, vidWidth));
                        dominantColorCalculators.Add(Tier3Clip.Clip3, new DominantColorCalculator(vidHeight, vidWidth));
                        dominantColorCalculators.Add(Tier3Clip.Clip4, new DominantColorCalculator(vidHeight, vidWidth));

                        tier2Clip = new Clip();
                        tier2Clip.FilePath = GetOutputPath(2, (int)currentTier2Clip, file);
                        tier2Clip.Description = GetVideoDescription(videoName, 2, (int)currentTier2Clip);
                        tier3Clip = new Clip();
                        tier3Clip.FilePath = GetOutputPath(3, (int)currentTier3Clip, file);
                        tier3Clip.Description = GetVideoDescription(videoName, 3, (int)currentTier3Clip);

                        clips.Add(currentTier2Clip, tier2Clip);
                        clips.Add(currentTier3Clip, tier3Clip);
                    }

                    if ((tier2FramesUsed == numFramesPerTier2Clip) && (currentTier2Clip != Tier2Clip.Clip2))
                    {
                        AppendLogLine("Frames written to tier 2, clip " + currentTier2Clip + " = " + tier2FramesUsed);
                        currentTier2Clip++;
                        tier2FramesUsed = 0;
                        cvlib.CvReleaseVideoWriter(ref tier2VidWriter);
                        tier2VidWriter = cvlib.CvCreateVideoWriter(GetOutputPath(2, (int)currentTier2Clip, file), outputFourCC, fps, outputVideoSize, 1);
                    }
                    if (tier3FramesUsed == numFramesPerTier3Clip && (currentTier3Clip != Tier3Clip.Clip4))
                    {
                        AppendLogLine("Frames written to tier 3, clip " + currentTier3Clip + " = " + tier3FramesUsed);
                        currentTier3Clip++;
                        tier3FramesUsed = 0;
                        cvlib.CvReleaseVideoWriter(ref tier3VidWriter);
                        tier3VidWriter = cvlib.CvCreateVideoWriter(GetOutputPath(3, (int)currentTier3Clip, file), outputFourCC, fps, outputVideoSize, 1);
                    }
                    
                    cvlib.CvWriteFrame(tier2VidWriter, ref image);
                    cvlib.CvWriteFrame(tier3VidWriter, ref image);

                    //increment the number of frames shown counters
                    totalNumFramesUsed++;
                    tier2FramesUsed++;
                    tier3FramesUsed++;

                    //the image must be flipped to show it on the screen correctly
                    cvlib.CvFlip(ref image, ref image, 0);
                    Bitmap bmpImage = cvlib.ToBitmap(image, false);

                    //Add this frame to our dominant color calculations
                    dominantColorCalculators[currentTier3Clip].AddFrame(bmpImage);
                    ShowImage(bmpImage);

                    lblCurrFrame.Text = totalNumFramesUsed.ToString();
                    progressBar1.PerformStep();
                    Application.DoEvents();
                }

                cvlib.CvReleaseVideoWriter(ref tier3VidWriter);
                cvlib.CvReleaseVideoWriter(ref tier2VidWriter);
                cvlib.CvReleaseCapture(ref capture);

                AppendLogLine("Frames written to tier 2, clip " + (int)currentTier2Clip + " = " + tier2FramesUsed);
                AppendLogLine("Frames written to tier 3, clip " + (int)currentTier3Clip + " = " + tier3FramesUsed);

                AppendLogLine("Dominant Color for Tier3 Clip1: " + dominantColorCalculators[Tier3Clip.Clip1].GetDominantColor().ToString());
                AppendLogLine("Dominant Color for Tier3 Clip2: " + dominantColorCalculators[Tier3Clip.Clip2].GetDominantColor().ToString());
                AppendLogLine("Dominant Color for Tier3 Clip3: " + dominantColorCalculators[Tier3Clip.Clip3].GetDominantColor().ToString());
                AppendLogLine("Dominant Color for Tier3 Clip4: " + dominantColorCalculators[Tier3Clip.Clip4].GetDominantColor().ToString());

                DominantColorCalculator tier2Clip1DomColorCalc = DominantColorCalculator.Add(dominantColorCalculators[Tier3Clip.Clip1], dominantColorCalculators[Tier3Clip.Clip2]);
                DominantColorCalculator tier2Clip2DomColorCalc = DominantColorCalculator.Add(dominantColorCalculators[Tier3Clip.Clip3], dominantColorCalculators[Tier3Clip.Clip4]);
                DominantColorCalculator tier1DomColorCalc = DominantColorCalculator.Add(tier2Clip1DomColorCalc, tier2Clip2DomColorCalc);
                AppendLogLine("Dominant Color for Tier2 Clip1: " + tier2Clip1DomColorCalc.GetDominantColor().ToString());
                AppendLogLine("Dominant Color for Tier2 Clip2: " + tier2Clip2DomColorCalc.GetDominantColor().ToString());
                AppendLogLine("Dominant Color for Tier1: " + tier1DomColorCalc.GetDominantColor().ToString());
            }
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
        private string GetOutputPath(int tier, int clip, string videoFilePath)
        {
            string dir = Path.GetDirectoryName(videoFilePath);
            string file = Path.GetFileNameWithoutExtension(videoFilePath);
            string extension = ".avi"; //always use .avi, if you use .divx, or .mjpg it doesn't write the frames for some reason.

            return dir + "\\output\\" + file + "-Tier" + tier + "Clip" + clip + extension;
        }


        private string GetVideoDescription(string videoName, int tier, int clip)
        {
            return videoName + " Tier " + tier + ", Clip " + clip;
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