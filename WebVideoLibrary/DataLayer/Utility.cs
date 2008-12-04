using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DataLayer
{
    public static class Utility
    {
        public const string DATABASE_FILENAME = @"database.s3db";
        public const string DATABASE_FILENAME_AND_PATH = @"c:\" + DATABASE_FILENAME;
        public const string CONNECTION_STRING = @"Data Source=" + DATABASE_FILENAME_AND_PATH;
    }
}
