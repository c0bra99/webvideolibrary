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
        public frmMain()
        {
            InitializeComponent();
        }

        private void frmMain_Load(object sender, EventArgs e)
        {
            openFileDialog1.Filter = "Video files|*.avi;*.divx;*.mpg";
            openFileDialog1.FileName = "";
            txtVideoInputPath.Text = "c:\\temp\\bouncing_ball.divx";
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

            //open the file input
            CvCapture capture = cvlib.CvCreateFileCapture(txtVideoInputPath.Text);

            //check to make sure it opened ok
            if (capture.ptr == IntPtr.Zero)
            {
                MessageBox.Show("Creation of File Capture failed.");
                return;
            }

            int numTotalFramesInVideo = (int)cvlib.cvGetCaptureProperty(capture, cvlib.CV_CAP_PROP_FRAME_COUNT);
            int fps = (int)cvlib.cvGetCaptureProperty(capture, cvlib.CV_CAP_PROP_FPS);
            int fourcc = (int)cvlib.cvGetCaptureProperty(capture, cvlib.CV_CAP_PROP_FOURCC);
            int vidLengthInSeconds = numTotalFramesInVideo / fps;
            int numFramesPerTier2Clip = numTotalFramesInVideo / 4;
            if (numFramesPerTier2Clip * 4 != numTotalFramesInVideo)
            {
                int difference = numTotalFramesInVideo - (numFramesPerTier2Clip * 4);
                //add this difference # of frames to the last clip to make up for integer division
            }
            int numFramesPerTier3Clip = numFramesPerTier2Clip / 2;

            int numFramesShown = 0;

            //We need to grab the first frame before being able to get the width and height
            IplImage image = cvlib.CvQueryFrame(ref capture);
            int w = (int)cvlib.cvGetCaptureProperty(capture, cvlib.CV_CAP_PROP_FRAME_WIDTH);
            int h = (int)cvlib.cvGetCaptureProperty(capture, cvlib.CV_CAP_PROP_FRAME_HEIGHT);


            string dir = Path.GetDirectoryName(txtVideoInputPath.Text);
            string file = Path.GetFileNameWithoutExtension(txtVideoInputPath.Text);
            string extension = Path.GetExtension(txtVideoInputPath.Text);

            string outputVideoPath = dir + file + "-Tier2Clip1" + extension;
            CvVideoWriter vidWriter = cvlib.CvCreateVideoWriter(outputVideoPath, -1, fps, new CvSize(w, h), 1);
            cvlib.CvWriteFrame(vidWriter, ref image);
            //cvlib.cvWriteFrame(ref vidWriter, ref image);

            cvlib.CvReleaseVideoWriter(ref vidWriter);

            //flip the input image
            cvlib.CvFlip(ref image, ref image, 0);

            //show the first frame before the while loop
            ShowImage(image);

            //increment the number of frames shown counter
            numFramesShown++;
            
            while (numFramesShown < numTotalFramesInVideo)
            {
                //We need to grab the next frame to show
                image = cvlib.CvQueryFrame(ref capture);

                cvlib.CvFlip(ref image, ref image, 0);

                ShowImage(image);
                numFramesShown++;

                Application.DoEvents();
            }
            
            cvlib.CvReleaseCapture(ref capture);
        }


        /// <summary>
        /// Shows the image in the picturebox on the form
        /// </summary>
        /// <param name="image">The image to show in the picturebox.</param>
        private void ShowImage(IplImage image)
        {
            //make sure we have an image to show
            if (image.imageData == IntPtr.Zero)
            {
                return;
            }

            Bitmap bmpImage = cvlib.ToBitmap(image, false);
            if (pictureBox.Image != null)
            {
                pictureBox.Image.Dispose();
            }
            pictureBox.Image = bmpImage;

            if (this.Height < bmpImage.Height)
            {
                this.Height = bmpImage.Height + 100;
            }
            if (this.Width < bmpImage.Width)
            {
                this.Width = bmpImage.Width + 100;
            }
        }


        private void WriteFrameToVideo(IplImage frame)
        {

        }
    }
}