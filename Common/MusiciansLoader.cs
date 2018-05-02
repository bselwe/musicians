using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;

namespace Common
{
    public static class MusiciansLoader
    {
        public static IEnumerable<T> GetMusicians<T>(Func<int, Position, T> createMusician) where T : Musician
        {
            var musicians = new List<T>();
            var data = File.ReadAllLines(Configuration.PositionsFile);
            TryParseInt(data[0], out int numberOfMusicians);

            if (numberOfMusicians != data.Length - 1)
                throw new ArgumentException("Invalid number of arguments");

            for (int i = 0; i < numberOfMusicians; i++)
            {
                var position = data[i + 1].Split();
                if (position.Length != 2)
                    throw new ArgumentException("Invalid number of arguments");

                TryParseInt(position[0], out int x);
                TryParseInt(position[1], out int y);

                var musician = createMusician(i, new Position(x, y));
                musicians.Add(musician);
            }

            foreach (var musician in musicians)
            {
                var neighbors = musicians.Where(m => m != musician && m.Position.DistanceTo(musician.Position) <= Configuration.NeighborMaximumDistance);
                var neighborsIds = neighbors.Select(n => new Neighbor(n.Id));
                musician.SetNeighbors(neighborsIds);
            }

            return musicians;
        }

        static void TryParseInt(string s, out int val)
        {
            if (!Int32.TryParse(s, out val))
                throw new ArgumentException("Invalid format of arguments");
        }
    }
}