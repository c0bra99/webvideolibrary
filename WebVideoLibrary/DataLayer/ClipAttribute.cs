﻿using System;
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
        { }

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
            using (SQLiteConnection cnn = new SQLiteConnection(Utility.GetConnectionString()))
            {
                using (SQLiteCommand cmd = cnn.CreateCommand())
                {
                    cnn.Open();
                    cmd.CommandText = "INSERT INTO ClipAttributes(ID, ClipID, Name, Value) VALUES(?, ?, ?, ?)";
                    cmd.Parameters.Add("ID", DbType.Int32);
                    cmd.Parameters["ID"].Value = ID;
                    cmd.Parameters.Add("ClipID", DbType.Int32);
                    cmd.Parameters["ClipID"].Value = ClipID;
                    cmd.Parameters.Add("Name", DbType.String);
                    cmd.Parameters["Name"].Value = Name;
                    cmd.Parameters.Add("Value", DbType.String);
                    cmd.Parameters["Value"].Value = Name;

                    int rowsUpdated = cmd.ExecuteNonQuery();
                    if (rowsUpdated != 1)
                    {
                        throw new SQLiteException("Too many or too little rows updated! Expected 1, Updated: " + rowsUpdated);
                    }
                }
            }
        }


        /// <summary>
        /// Saves a list of ClipAttributes to the DB
        /// </summary>
        public static void Save(List<ClipAttribute> attributes)
        {
            foreach (ClipAttribute attribute in attributes)
            {
                attribute.Save();
            }
        }
    }
}