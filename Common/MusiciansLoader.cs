using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;

namespace Common
{
    public static class MusiciansLoader
    {
        public static int GetNumberOfMusicians()
        {
            var data = File.ReadLines(Configuration.PositionsFile).First();
            TryParseInt(data, out int musicians);
            return musicians;
        }

        public static IEnumerable<Position> GetMusiciansPositions()
        {
            var data = File.ReadAllLines(Configuration.PositionsFile);
            TryParseInt(data[0], out int musicians);

            if (musicians != data.Length - 1)
                throw new ArgumentException("Invalid number of arguments");

            for (int i = 0; i < musicians; i++)
            {
                var position = data[i + 1].Split();
                if (position.Length != 2)
                    throw new ArgumentException("Invalid number of arguments");

                TryParseInt(position[0], out int x);
                TryParseInt(position[1], out int y);

                yield return new Position(x, y);
            }
        }

        static void TryParseInt(string s, out int val)
        {
            if (!Int32.TryParse(s, out val))
                throw new ArgumentException("Invalid format of arguments");
        }
    }
}