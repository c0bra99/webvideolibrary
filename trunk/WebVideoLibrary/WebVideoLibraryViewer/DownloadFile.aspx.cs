using System;
using System.IO;
using System.Data;
using System.Configuration;
using System.Collections;
using System.Web;
using System.Web.Security;
using System.Web.UI;
using System.Web.UI.WebControls;
using System.Web.UI.WebControls.WebParts;
using System.Web.UI.HtmlControls;
using DataLayer;

namespace WebVideoLibraryViewer
{
    public partial class DownloadFile : System.Web.UI.Page
    {
        /// <summary>
        /// Event that fires when the page loads.
        /// </summary>
        protected void Page_Load(object sender, EventArgs e)
        {
            string filename = "C:\\test.bmp";
            
            //Get the filename to show the user from the querystring
            string filenametoshow = Request.QueryString["filename"].ToString();
            if (filenametoshow == string.Empty)
            {
                filenametoshow = "NoFileName.txt";
            }

            //Set the content type
            this.Response.ContentType = "application/octet-stream";
            
            //Set the filename to show the user
            this.Response.AddHeader("Content-Disposition", "attachment; filename=" + filenametoshow.Replace(" ", "_"));

            try
            {
                //Set the file size so the save as box shows a size and the remaining time is shown while downloading
                FileInfo fileinfo = new FileInfo(filename);
                this.Response.AddHeader("Content-Length", fileinfo.Length.ToString());

                Response.TransmitFile(filename);
                Response.Flush();
            }
            catch
            {
                Response.Clear();
                Response.ClearContent();
                Response.ClearHeaders();

                Response.ContentType = "text/html";
                Response.Write("There was an error transmitting the file, please try again.");
            }
            finally
            {
                Response.End();
            }
        }
    }
}
