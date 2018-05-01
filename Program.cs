using System;
using System.IO;
using Alchemy;

namespace Musicians
{
    class Program
    {
        static int numberOfMusicians;
        static Musician[] musicians;
        
        static void Main(string[] args)
        {
            LoadMusicians();
        }

        static void LoadMusicians()
        {
            var data = File.ReadAllLines(Configuration.PositionsFile);
            TryParseInt(data[0], out numberOfMusicians);

            if (numberOfMusicians != data.Length - 1)
                throw new ArgumentException("Invalid number of arguments");

            musicians = new Musician[numberOfMusicians];
            for (int i = 0; i < numberOfMusicians; i++)
            {
                var position = data[i + 1].Split();
                int x, y;

                if (position.Length != 2)
                    throw new ArgumentException("Invalid number of arguments");

                TryParseInt(position[0], out x);
                TryParseInt(position[1], out y);

                musicians[i] = new Musician(i, new Position(x, y));
            }
        }

        static void TryParseInt(string s, out int val)
        {
            if (!Int32.TryParse(s, out val))
                throw new ArgumentException("Invalid format of arguments");
        }
    }
}
