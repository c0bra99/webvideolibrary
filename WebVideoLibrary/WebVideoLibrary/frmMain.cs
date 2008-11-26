using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
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
            txtVideo.Text = "c:\\temp\\bouncing_ball.divx";
        }


        /// <summary>
        /// Fires when the user clicks the open file button
        /// </summary>
        private void btnOpenFile_Click(object sender, EventArgs e)
        {
            DialogResult result = openFileDialog1.ShowDialog();
            if (result == DialogResult.OK)
            {
                txtVideo.Text = openFileDialog1.FileName;
            }
        }


        private void btnStart_Click(object sender, EventArgs e)
        {
            CvCapture capture = cvlib.CvCreateFileCapture(txtVideo.Text);

            //check to make sure it opened ok
            if (capture.ptr == IntPtr.Zero)
            {
                MessageBox.Show("Creation of File Capture failed.");
                return;
            }

            int numFrames = (int)cvlib.cvGetCaptureProperty(capture, cvlib.CV_CAP_PROP_FRAME_COUNT);
            int fps = (int)cvlib.cvGetCaptureProperty(capture, cvlib.CV_CAP_PROP_FPS);
            int numFramesShown = 0;

            //We need to grab the first frame before being able to get the width and height
            IplImage image = cvlib.CvQueryFrame(ref capture);
            int w = (int)cvlib.cvGetCaptureProperty(capture, cvlib.CV_CAP_PROP_FRAME_WIDTH);
            int h = (int)cvlib.cvGetCaptureProperty(capture, cvlib.CV_CAP_PROP_FRAME_HEIGHT);

            //flip the input image
            cvlib.CvFlip(ref image, ref image, 0);

            //show the first frame before the while loop
            ShowImage(image);

            //increment the number of frames shown counter
            numFramesShown++;
            
            while (numFramesShown < numFrames)
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

    }
}