using System.Collections.Generic;
using System.Linq;
using static Common.Musician;

namespace Common
{
    public class Musician
    {
        public int Id { get; private set; }
        public Position Position { get; private set; }
        public MusicianStatus Status { get; private set; }
        
        protected List<Neighbor> neighbors = new List<Neighbor>();
        public IReadOnlyList<Neighbor> Neighbors => neighbors;

        public Musician(int id, Position position)
        {
            Id = id;
            Position = position;
            Status = MusicianStatus.Unknown;
        }

        public void SetNeighbors(IEnumerable<Neighbor> neighbors)
        {
            this.neighbors = new List<Neighbor>(neighbors);
        }

        public enum MusicianStatus
        {
            Unknown,
            Winner,
            Loser,
            NotLoser
        }
    }

    public class Neighbor
    {
        public int Id { get; private set; }
        public MusicianStatus Status { get; set; }

        public Neighbor(int id)
        {   
            Id = id;
        }
    }
}