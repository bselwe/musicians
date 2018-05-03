using System;
using System.IO;
using System.Linq;
using Common;

namespace Conductor
{
    public static class MusiciansLoader
    {
        public static int GetNumberOfMusicians()
        {
            var data = File.ReadLines(Configuration.MusiciansFile).First();
            TryParseInt(data, out int numberOfMusicians);
            return numberOfMusicians;
        }

        static void TryParseInt(string s, out int val)
        {
            if (!Int32.TryParse(s, out val))
                throw new ArgumentException("Invalid format of arguments");
        }
    }
}