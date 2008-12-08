using System;
using System.Collections;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Web;
using System.Web.Security;
using System.Web.UI;
using System.Web.UI.HtmlControls;
using System.Web.UI.WebControls;
using System.Web.UI.WebControls.WebParts;
using System.Xml.Linq;
using DataLayer;
using System.Drawing;
using System.Drawing.Imaging;

namespace WebVideoLibraryViewer
{
    public partial class GetThumbnail : System.Web.UI.Page
    {
        protected void Page_Load(object sender, EventArgs e)
        {
            if (!string.IsNullOrEmpty(Request.QueryString["ClipID"]))
            {
                int clipID = -1;
                int.TryParse(Request.QueryString["ClipID"], out clipID);

                Clip clip = Clip.GetSingle(clipID);

                Response.Clear();
                Response.ContentType = "image/jpeg";
                
                ResizeBitmap(clip.Thumbnail, 160, 120).Save(Response.OutputStream, ImageFormat.Jpeg);
            }
        }

        public Bitmap ResizeBitmap(Bitmap b, int nWidth, int nHeight)
        {
            Bitmap result = new Bitmap(nWidth, nHeight);
            
            using (Graphics g = Graphics.FromImage((System.Drawing.Image)result))
            {
                g.DrawImage(b, 0, 0, nWidth, nHeight);
            }

            return result;
        }
    }
}
