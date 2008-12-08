using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.Data;

namespace DataLayer
{
    public class ClipAttribute
    {
        public int ID { get; set; }
        public int ClipID { get; set; }
        public string Name { get; set; }
        public string Value { get; set; }

        public ClipAttribute()
        {
            ID = -1;
        }

        public ClipAttribute(int id, int clipID, string name, string value)
        {
            ID = id;
            ClipID = clipID;
            Name = name;
            Value = value;
        }

        /// <summary>
        /// Saves this ClipAttribute to the DB
        /// </summary>
        public void Save()
        {
            using (SQLiteConnection cnn = new SQLiteConnection(Utility.CONNECTION_STRING))
            {
                using (SQLiteCommand cmd = cnn.CreateCommand())
                {
                    cnn.Open();
                    cmd.CommandText = "INSERT INTO ClipAttributes(ID, ClipID, Name, Value) VALUES(?, ?, ?, ?)";
                    cmd.Parameters.Add("ID", DbType.Int32);
                    if (ID != -1)
                    {
                        cmd.Parameters["ID"].Value = ID;
                    }
                    cmd.Parameters.Add("ClipID", DbType.Int32);
                    cmd.Parameters["ClipID"].Value = ClipID;
                    cmd.Parameters.Add("Name", DbType.String);
                    cmd.Parameters["Name"].Value = Name;
                    cmd.Parameters.Add("Value", DbType.String);
                    cmd.Parameters["Value"].Value = Value;

                    int rowsUpdated = cmd.ExecuteNonQuery();
                    if (rowsUpdated != 1)
                    {
                        throw new SQLiteException("Too many or too little rows updated! Expected 1, Updated: " + rowsUpdated);
                    }
                }
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
                    }
                }
            }
        }


        /// <summary>
        /// Saves a list of ClipAttributes to the DB
        /// </summary>
        public static void Save(List<ClipAttribute> attributes)
        {
            if (attributes != null)
            {
                foreach (ClipAttribute attribute in attributes)
                {
                    attribute.Save();
                }
            }
        }


        /// <summary>
        /// Gets a single clip from the database using the ClipID passed in
        /// </summary>
        public static List<ClipAttribute> GetAttributesForClip(int clipID)
        {
            List<ClipAttribute> attributes = new List<ClipAttribute>();

            using (SQLiteConnection cnn = new SQLiteConnection(Utility.CONNECTION_STRING))
            {
                using (SQLiteCommand cmd = cnn.CreateCommand())
                {
                    cnn.Open();
                    cmd.CommandText = "SELECT * FROM ClipAttributes WHERE ClipID = " + clipID;

                    using (SQLiteDataReader reader = cmd.ExecuteReader())
                    {
                        if (reader.HasRows)
                        {
                            while (reader.Read())
                            {
                                attributes.Add(GetClipAttributeFromDataReader(reader));
                            }
                        }
                    }
                }
            }

            return attributes;
        }


        private static ClipAttribute GetClipAttributeFromDataReader(SQLiteDataReader reader)
        {
            ClipAttribute attribute = new ClipAttribute();
            
            attribute.ID = (int)(long)reader["ID"];
            attribute.Name = (string)reader["Name"];
            attribute.Value = (string)reader["Value"];
            attribute.ClipID = (int)(long)reader["ClipID"];

            return attribute;
        }
    }
}