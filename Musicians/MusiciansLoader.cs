using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Common;

namespace Musicians
{
    public static class MusiciansLoader
    {
        public static IEnumerable<Musician> GetMusicians()
        {
            var random = new Random(Guid.NewGuid().GetHashCode());
            var musicians = new List<Musician>();

            var data = File.ReadAllLines(Configuration.MusiciansFile);
            TryParseInt(data[0], out int numberOfMusicians);
            
            if (numberOfMusicians != data.Length - 1)
                throw new ArgumentException("Invalid number of arguments");

            var maxPriorityValue = (int) Math.Pow(numberOfMusicians, 4);

            for (int i = 0; i < numberOfMusicians; i++)
            {
                var positions = data[i + 1].Split();
                if (positions.Length != 2)
                    throw new ArgumentException("Invalid number of arguments");

                TryParseInt(positions[0], out int x);
                TryParseInt(positions[1], out int y);

                var position = new Position(x, y);
                var priorityValue = random.Next(maxPriorityValue);
                var musician = new Musician(i, position, priorityValue);
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