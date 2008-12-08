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
using System.Collections.Generic;
using System.IO;

namespace WebVideoLibraryViewer
{
    public partial class _Default : System.Web.UI.Page
    {
        protected void Page_Load(object sender, EventArgs e)
        {
            if (!File.Exists(Utility.DATABASE_FILENAME_AND_PATH))
            {
                ProgramNotRanYet();
                return;
            }

            if (!IsPostBack)
            {
                TreeNode root = new TreeNode("All Videos");
                treeView.Nodes.Add(root);
                Dictionary<string, TreeNode> treeNodes = new Dictionary<string, TreeNode>();
                List<Clip> clips = Clip.GetAll();

                if (clips.Count == 0)
                {
                    ProgramNotRanYet();
                    return;
                }
                //sort the clips based on description, tier, clip number
                clips.Sort(new Comparison<Clip>(Clip.CompareClips));

                foreach (Clip clip in clips)
                {
                    string thumbnailURL = "~/GetThumbnail.aspx?ClipID=" + clip.ID;
                    string videoURL = "~/DownloadFile.aspx?FileName=" + Server.UrlEncode(clip.FilePath);
                    TreeNode node = new TreeNode(clip.ToString() + "<br />" + clip.GetClipAttributesHTML(), clip.ID.ToString(), thumbnailURL, videoURL, "");
                    if (clip.Tier == 1)
                    {
                        treeNodes.Add(clip.Description, node);
                        root.ChildNodes.Add(node);
                    }
                    else if (clip.Tier == 2)
                    {
                        treeNodes[clip.Description].ChildNodes.Add(node);
                        treeNodes.Add(clip.Description + clip.Tier.ToString() + "_" + clip.ClipNumber.ToString(), node);
                    }
                    else if (clip.Tier == 3)
                    {
                        if (clip.ClipNumber == 1 || clip.ClipNumber == 2)
                        {
                            treeNodes[clip.Description + "2_1"].ChildNodes.Add(node);
                        }
                        else
                        {
                            treeNodes[clip.Description + "2_2"].ChildNodes.Add(node);
                        }
                    }
                }
            }
        }

        private void ProgramNotRanYet()
        {
            Response.Clear();
            Response.Write("Database does not exist yet, please refresh this page after you have ran the program to generate the database and video clips.");
            Response.End();
            return;
        }
    }
}
