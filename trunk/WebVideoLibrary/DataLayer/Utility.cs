using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DataLayer
{
    public static class Utility
    {
        public static string GetDatabasePathAndFile()
        {
            return @"c:\database.db3";
        }

        public static string GetConnectionString()
        {
            return @"Data Source=" + GetDatabasePathAndFile();
        }
    }
}
