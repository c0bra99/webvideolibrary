using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Drawing;
using System.Data;
using System.Data.SQLite;
using System.IO;

namespace DataLayer
{
    public class Clip
    {
        public int ID { get; set; }
        public string Description { get; set; }
        public string FilePath { get; set; }
        public Bitmap Thumbnail { get; set; }
        public List<ClipAttribute> Attributes { get; set; }

        public Clip()
        {
            ID = -1;
        }

        public Clip(int id, string description, string filepath, Bitmap thumbnail)
        {
            ID = id;
            Description = description;
            FilePath = filepath;
            Thumbnail = thumbnail;
        }

        public void AddAttribute(string name, string value)
        {
            if (Attributes == null)
            {
                Attributes = new List<ClipAttribute>();
            }
            Attributes.Add(new ClipAttribute(-1, this.ID, name, value));
        }

        /// <summary>
        /// Saves this Clip to the DB
        /// </summary>
        public void Save()
        {
            //save this Clip to the DB
            using (SQLiteConnection cnn = new SQLiteConnection(Utility.CONNECTION_STRING))
            {
                using (SQLiteCommand cmd = cnn.CreateCommand())
                {
                    cnn.Open();
                    cmd.CommandText = "INSERT INTO Clips(ID, Description, FilePath, Thumbnail) VALUES(?, ?, ?, ?)";
                    cmd.Parameters.Add("ID", DbType.Int32);
                    if (ID != -1)
                    {
                        cmd.Parameters["ID"].Value = ID;
                    }
                    cmd.Parameters.Add("Description", DbType.String);
                    cmd.Parameters["Description"].Value = Description;
                    cmd.Parameters.Add("FilePath", DbType.String);
                    cmd.Parameters["FilePath"].Value = FilePath;
                    cmd.Parameters.Add("Thumbnail", DbType.Object);
                    if (Thumbnail != null)
                    {
                        MemoryStream ms = new MemoryStream();
                        Thumbnail.Save(ms, System.Drawing.Imaging.ImageFormat.Jpeg); //format the thumbnail as a jpg image
                        cmd.Parameters["Thumbnail"].Value = ms.ToArray();
                    }
                    int rowsUpdated = cmd.ExecuteNonQuery();
                    if (rowsUpdated != 1)
                    {
                        throw new SQLiteException("Too many or too little rows updated! Expected 1, Updated: " + rowsUpdated);
                    }
   
                }
                //If it is a new Clip, it wont have an ID, lets update it so we can
                //put the correct ID on each ClipAttribute
                if (ID == -1)
                {
                    using (SQLiteCommand cmd = cnn.CreateCommand())
                    {
                        if (cnn.State != ConnectionState.Open)
                        {
                            cnn.Open();
                        }
                        cmd.CommandText = "SELECT last_insert_rowid();";
                        ID = (int)(long)cmd.ExecuteScalar();

                        if (Attributes != null)
                        {
                            foreach (ClipAttribute attribute in Attributes)
                            {
                                attribute.ClipID = ID;
                            }
                        }
                    }
                }
            }

            //save each ClipAttribute to the DB
            ClipAttribute.Save(Attributes);
        }


        public static List<Clip> GetAll()
        {
            List<Clip> clips = new List<Clip>();

            using (SQLiteConnection cnn = new SQLiteConnection(Utility.CONNECTION_STRING))
            {
                using (SQLiteCommand cmd = cnn.CreateCommand())
                {
                    cnn.Open();
                    cmd.CommandText = "SELECT * FROM Clips";
                    using (SQLiteDataReader reader = cmd.ExecuteReader())
                    {
                        if (reader.HasRows)
                        {
                            while (reader.Read())
                            {
                                Clip clip = new Clip();

                                clip.ID = (int)(long)reader["ID"];
                                clip.Description = (string)reader["Description"];
                                clip.FilePath = (string)reader["FilePath"];
                                if (!(reader["Thumbnail"] is DBNull))
                                {
                                    byte[] imagesBytes = (byte[])reader["Thumbnail"];
                                    MemoryStream ms = new MemoryStream(imagesBytes);
                                    clip.Thumbnail = new Bitmap(ms);
                                }

                                clips.Add(clip);
                            }
                        }
                    }
                }
            }

            return clips;
        }
    }
}
