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
        public int Tier { get; set; }
        public int ClipNumber { get; set; }
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
        /// Provides a good string representation of what this Clip contains.
        /// </summary>
        public override string ToString()
        {
            return Description + " Tier " + Tier + ", Clip " + ClipNumber;
        }


        public string GetClipAttributesHTML()
        {
            if (Attributes == null)
            {
                Attributes = ClipAttribute.GetAttributesForClip(ID);
            }
            StringBuilder sb = new StringBuilder();
            
            foreach (ClipAttribute attribute in Attributes)
            {
                sb.Append(attribute.Name);
                sb.Append(": ");
                sb.Append(attribute.Value);
                sb.Append("<br />");
            }

            return sb.ToString();
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
                    cmd.CommandText = "INSERT INTO Clips(ID, Description, FilePath, Thumbnail, Tier, ClipNumber) VALUES(?, ?, ?, ?, ?, ?)";
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
                        using (MemoryStream ms = new MemoryStream())
                        {
                            Thumbnail.Save(ms, System.Drawing.Imaging.ImageFormat.Jpeg); //format the thumbnail as a jpg image
                            cmd.Parameters["Thumbnail"].Value = ms.ToArray();
                        }
                    }
                    cmd.Parameters.Add("Tier", DbType.Int32);
                    cmd.Parameters["Tier"].Value = Tier;
                    cmd.Parameters.Add("ClipNumber", DbType.Int32);
                    cmd.Parameters["ClipNumber"].Value = ClipNumber;

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
                                Clip clip = GetClipFromDataReader(reader);
                                clips.Add(clip);
                            }
                        }
                    }
                }
            }

            return clips;
        }


        /// <summary>
        /// Gets a single clip from the database using the ClipID passed in
        /// </summary>
        public static Clip GetSingle(int clipID)
        {
            Clip clip = null;

            using (SQLiteConnection cnn = new SQLiteConnection(Utility.CONNECTION_STRING))
            {
                using (SQLiteCommand cmd = cnn.CreateCommand())
                {
                    cnn.Open();
                    cmd.CommandText = "SELECT * FROM Clips WHERE ID = " + clipID;

                    using (SQLiteDataReader reader = cmd.ExecuteReader())
                    {
                        if (reader.HasRows)
                        {
                            reader.Read();
                            clip = GetClipFromDataReader(reader);
                        }
                    }
                }
            }

            return clip;
        }


        private static Clip GetClipFromDataReader(SQLiteDataReader reader)
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
            clip.Tier = (int)(long)reader["Tier"];
            clip.ClipNumber = (int)(long)reader["ClipNumber"];

            return clip;
        }

        /// <summary>
        /// Compares the 2 clips passed in based on Description, Tier, then ClipNumber
        /// </summary>
        public static int CompareClips(Clip x, Clip y)
        {
            if (x == null && y == null)
            {
                return 0;
            }
            else if (x == null && y != null)
            {
                return -1; //y is greater
            }
            else if (x != null && y == null)
            {
                return 1; //x is greater
            }
            else
            {
                //x is not null, and y is not null
                //first compare the descriptions
                int descriptionCompare = x.Description.CompareTo(y.Description);
                if (descriptionCompare != 0)
                {
                    //if the descriptions are not the same, sort based on description
                    return descriptionCompare;
                }
                else
                {
                    //if the description is the same, sort based on tier, then on clip number
                    int tierCompare = x.Tier.CompareTo(y.Tier);
                    if (tierCompare != 0)
                    {
                        //if the tiers are not the same, sort on tier.
                        return tierCompare;
                    }
                    else
                    {
                        //if the tiers are equal, just compare based on clip numbers.
                        return x.ClipNumber.CompareTo(y.ClipNumber);
                    }
                }
            }
        }
    }
}
