using System;
using System.Collections.Generic;

namespace Musicians
{
    public class Musician
    {
        public int Id { get; private set; }
        public Position Position { get; private set; }

        private List<int> neighbors = new List<int>();
        public IReadOnlyList<int> Neighbors => neighbors;

        public Musician(int id, Position position)
        {
            Id = id;
            Position = position;
        }

        public void AddNeighbors(List<int> neighbors)
        {
            this.neighbors.AddRange(neighbors);
        }
    }

    public struct Position
    {
        public int X { get; private set; }
        public int Y { get; private set; }

        public Position(int x, int y)
        {
            X = x;
            Y = y;
        }
    }
}