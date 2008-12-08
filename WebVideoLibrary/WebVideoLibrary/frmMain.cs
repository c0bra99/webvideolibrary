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
using System.Runtime.InteropServices;

namespace WebVideoLibrary
{
    public partial class frmMain : Form
    {
        private enum Tier1Clip
        {
            OnlyClip = 1
        }

        /// <summary>
        /// An enumeration for each clip in Tier 2
        /// </summary>
        private enum Tier2Clip
        {
            Clip1 = 1,
            Clip2 = 2
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
            //load the output codec combobox with some predefined FOURCC's
            cboOutputCodec.Items.Add(new ListItem("DivX", cvlib.CvCreateFourCC('D', 'I', 'V', 'X')));
            cboOutputCodec.Items.Add(new ListItem("Uncompressed DIB", cvlib.CvCreateFourCC('D', 'I', 'B', ' ')));
            cboOutputCodec.Items.Add(new ListItem("Motion JPG", cvlib.CvCreateFourCC('M', 'J', 'P', 'G')));
            cboOutputCodec.SelectedIndex = 0;

            if (!File.Exists(Utility.DATABASE_FILENAME_AND_PATH))
            {
                //If the database does not exist where we think it should, lets copy it there.
                if (!File.Exists(Utility.DATABASE_FILENAME))
                {
                    MessageBox.Show("Could not find DataBase file: " + Utility.DATABASE_FILENAME);
                    return;
                }
                File.Copy(Utility.DATABASE_FILENAME, Utility.DATABASE_FILENAME_AND_PATH);
            }
        }


        /// <summary>
        /// Fires when the user clicks the open file button
        /// </summary>
        private void btnOpenFile_Click(object sender, EventArgs e)
        {
            //DialogResult result = openFileDialog1.ShowDialog();
            DialogResult result = folderBrowserDialog1.ShowDialog();
            if (result == DialogResult.OK)
            {
                txtVideoInputPath.Text = folderBrowserDialog1.SelectedPath;
            }
        }


        /// <summary>
        /// Fires when the user clicks the start button.
        /// </summary>
        private void btnStart_Click(object sender, EventArgs e)
        {
            //Check to make sure the file exists.
            if (!Directory.Exists(txtVideoInputPath.Text))
            {
                MessageBox.Show("Directory does not exist!");
                return;
            }
            
            //get the selected output FourCC from the combo box.
            int outputFourCC = (int)((ListItem)cboOutputCodec.SelectedItem).Value;
            
            //we will need this when we actually do every file in a directory instead of just 1 video
            string[] files = Directory.GetFiles(txtVideoInputPath.Text, "*.*", SearchOption.TopDirectoryOnly); //fild all the files in that path with that pattern
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
                        Clip tier1Clip = new Clip();
                        tier1Clip.Description = videoName;
                        tier1Clip.FilePath = file;
                        tier1Clip.Tier = 1;
                        tier1Clip.ClipNumber = 1;
                        tier1Clip.AddAttribute("Frames", numTotalFramesInVideo.ToString());
                        clips.Add(Tier1Clip.OnlyClip, tier1Clip);

                        tier2Clip = new Clip();
                        tier2Clip.Description = videoName;
                        tier2Clip.FilePath = GetOutputPath(2, (int)currentTier2Clip, file);
                        tier2Clip.Tier = 2;
                        tier2Clip.ClipNumber = 1;
                        clips.Add(currentTier2Clip, tier2Clip);

                        tier3Clip = new Clip();
                        tier3Clip.Description = videoName;
                        tier3Clip.FilePath = GetOutputPath(3, (int)currentTier3Clip, file);
                        tier3Clip.Tier = 3;
                        tier3Clip.ClipNumber = 1;
                        clips.Add(currentTier3Clip, tier3Clip);
                    }
                    else
                    {
                        if ((tier2FramesUsed == numFramesPerTier2Clip) && (currentTier2Clip != Tier2Clip.Clip2))
                        {
                            ((Clip)clips[currentTier2Clip]).AddAttribute("Frames", tier2FramesUsed.ToString());

                            AppendLogLine("Frames written to tier 2, clip " + currentTier2Clip + " = " + tier2FramesUsed);
                            currentTier2Clip++;
                            tier2FramesUsed = 0;
                            cvlib.CvReleaseVideoWriter(ref tier2VidWriter);
                            tier2VidWriter = cvlib.CvCreateVideoWriter(GetOutputPath(2, (int)currentTier2Clip, file), outputFourCC, fps, outputVideoSize, 1);
                            tier2Clip = new Clip();
                            tier2Clip.FilePath = GetOutputPath(2, (int)currentTier2Clip, file);
                            tier2Clip.Description = videoName;
                            tier2Clip.Tier = 2;
                            tier2Clip.ClipNumber = (int)currentTier2Clip;
                            clips.Add(currentTier2Clip, tier2Clip);
                        }
                        if (tier3FramesUsed == numFramesPerTier3Clip && (currentTier3Clip != Tier3Clip.Clip4))
                        {
                            AppendLogLine("Frames written to tier 3, clip " + currentTier3Clip + " = " + tier3FramesUsed);
                            ((Clip)clips[currentTier3Clip]).AddAttribute("Frames", tier3FramesUsed.ToString());
                            currentTier3Clip++;
                            tier3FramesUsed = 0;
                            cvlib.CvReleaseVideoWriter(ref tier3VidWriter);
                            tier3VidWriter = cvlib.CvCreateVideoWriter(GetOutputPath(3, (int)currentTier3Clip, file), outputFourCC, fps, outputVideoSize, 1);
                            tier3Clip = new Clip();
                            tier3Clip.FilePath = GetOutputPath(3, (int)currentTier3Clip, file);
                            tier3Clip.Description = videoName;
                            tier3Clip.Tier = 3;
                            tier3Clip.ClipNumber = (int)currentTier3Clip;
                            clips.Add(currentTier3Clip, tier3Clip);
                        }
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

                    //Set our thumbnails = to the frame in the center of each clip
                    if (tier2FramesUsed == numFramesPerTier2Clip / 2)
                    {
                        tier2Clip.Thumbnail = (Bitmap)bmpImage.Clone();
                        GoodFeaturesToTrack(image, tier2Clip);
                    }
                    if (tier3FramesUsed == numFramesPerTier3Clip / 2)
                    {
                        tier3Clip.Thumbnail = (Bitmap)bmpImage.Clone();
                        GoodFeaturesToTrack(image, tier3Clip);
                    }
                    if (totalNumFramesUsed == numTotalFramesInVideo / 2)
                    {
                        Clip tier1clip = ((Clip)clips[Tier1Clip.OnlyClip]);
                        tier1clip.Thumbnail = (Bitmap)bmpImage.Clone();
                        GoodFeaturesToTrack(image, tier1clip);
                    }

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
                ((Clip)clips[currentTier2Clip]).AddAttribute("Frames", tier2FramesUsed.ToString());
                ((Clip)clips[currentTier3Clip]).AddAttribute("Frames", tier3FramesUsed.ToString());

                AddDominantColorAttributesToClips(clips, dominantColorCalculators);
                foreach (Clip clip in clips.Values)
                {
                    clip.Save();
                }
            }

            pictureBox.Image.Dispose();
            MessageBox.Show("Done!");
        }


        private void AddDominantColorAttributesToClips(Hashtable clips, Dictionary<Tier3Clip, DominantColorCalculator> dominantColorCalculators)
        {
            DominantColorCalculator tier2Clip1DomColorCalc = DominantColorCalculator.Add(dominantColorCalculators[Tier3Clip.Clip1], dominantColorCalculators[Tier3Clip.Clip2]);
            DominantColorCalculator tier2Clip2DomColorCalc = DominantColorCalculator.Add(dominantColorCalculators[Tier3Clip.Clip3], dominantColorCalculators[Tier3Clip.Clip4]);
            DominantColorCalculator tier1DomColorCalc = DominantColorCalculator.Add(tier2Clip1DomColorCalc, tier2Clip2DomColorCalc);

            string tier1DomColor = tier1DomColorCalc.GetDominantColor().ToString();
            string tier2Clip1DomColor = tier2Clip1DomColorCalc.GetDominantColor().ToString();
            string tier2Clip2DomColor = tier2Clip2DomColorCalc.GetDominantColor().ToString();
            string tier3Clip1DomColor = dominantColorCalculators[Tier3Clip.Clip1].GetDominantColor().ToString();
            string tier3Clip2DomColor = dominantColorCalculators[Tier3Clip.Clip2].GetDominantColor().ToString();
            string tier3Clip3DomColor = dominantColorCalculators[Tier3Clip.Clip3].GetDominantColor().ToString();
            string tier3Clip4DomColor = dominantColorCalculators[Tier3Clip.Clip4].GetDominantColor().ToString();

            AppendLogLine("Dominant Color for Tier1: " + tier1DomColor);
            AppendLogLine("Dominant Color for Tier2 Clip1: " + tier2Clip1DomColor);
            AppendLogLine("Dominant Color for Tier2 Clip2: " + tier2Clip2DomColor);
            AppendLogLine("Dominant Color for Tier3 Clip1: " + tier3Clip1DomColor);
            AppendLogLine("Dominant Color for Tier3 Clip2: " + tier3Clip2DomColor);
            AppendLogLine("Dominant Color for Tier3 Clip3: " + tier3Clip3DomColor);
            AppendLogLine("Dominant Color for Tier3 Clip4: " + tier3Clip4DomColor);

            ((Clip)clips[Tier1Clip.OnlyClip]).AddAttribute("Dominant Color", tier1DomColor);
            ((Clip)clips[Tier2Clip.Clip1]).AddAttribute("Dominant Color", tier2Clip1DomColor);
            ((Clip)clips[Tier2Clip.Clip2]).AddAttribute("Dominant Color", tier2Clip2DomColor);
            ((Clip)clips[Tier3Clip.Clip1]).AddAttribute("Dominant Color", tier3Clip1DomColor);
            ((Clip)clips[Tier3Clip.Clip2]).AddAttribute("Dominant Color", tier3Clip2DomColor);
            ((Clip)clips[Tier3Clip.Clip3]).AddAttribute("Dominant Color", tier3Clip3DomColor);
            ((Clip)clips[Tier3Clip.Clip4]).AddAttribute("Dominant Color", tier3Clip4DomColor);
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
            string dir = Path.GetDirectoryName(videoFilePath) + "\\output\\";
            if (!Directory.Exists(dir))
            {
                Directory.CreateDirectory(dir);
            }
            
            string file = Path.GetFileNameWithoutExtension(videoFilePath);
            string extension = ".avi"; //always use .avi, if you use .divx, or .mjpg it doesn't write the frames for some reason.

            return dir  + file + "-Tier" + tier + "Clip" + clip + extension;
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


        public void GoodFeaturesToTrack(IplImage image, Clip clip)
        {
            int corner_count = 6;
            CvSize size = new CvSize(image.width, image.height);
            IplImage gray, eig_image, tmp_image;

            if (image.imageData == IntPtr.Zero)
            {
                return;
            }

            // in case of image is not 1 channel
            if (image.nChannels != 1)
            {
                // create gray scale image
                gray = cvlib.CvCreateImage(size, (int)cvlib.IPL_DEPTH_8U, 1);
                // do color conversion
                if (gray.imageData != IntPtr.Zero) cvlib.CvCvtColor(ref image, ref gray, cvlib.CV_BGR2GRAY);
                else return;
            }
            else //or simply make a clone
            {
                gray = cvlib.CvCloneImage(ref image);
            }
            eig_image = cvlib.CvCreateImage(new CvSize(image.width, image.height), (int)cvlib.IPL_DEPTH_32F, 1);
            tmp_image = cvlib.CvCreateImage(new CvSize(image.width, image.height), (int)cvlib.IPL_DEPTH_32F, 1);
            CvPoint2D32f[] pts = new CvPoint2D32f[corner_count];
            GCHandle h;
            cvlib.CvGoodFeaturesToTrack(ref gray, ref eig_image, ref tmp_image, cvtools.Convert1DArrToPtr(pts, out h), ref corner_count, 0.01, 1, IntPtr.Zero, 3, 1, 0.04);


            StringBuilder sb = new StringBuilder("{");
            foreach (CvPoint2D32f p in pts)
            {
                sb.Append("(");
                sb.Append(p.x);
                sb.Append(",");
                sb.Append(p.y);
                sb.Append(")");
            }
            sb.Append('}');

            clip.AddAttribute("Good Feature Points", sb.ToString());

            cvlib.CvReleaseImage(ref eig_image);
            cvlib.CvReleaseImage(ref tmp_image);
            cvlib.CvReleaseImage(ref gray);
            cvtools.ReleaseHandel(h);
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