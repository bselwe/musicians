using static Musicians.Musician;

namespace Musicians
{
    public class Neighbor
    {
        public int Id { get; private set; }
        public PriorityResult Priority { get; set; }
        public ExchangeResult Exchange { get; set; }

        public Neighbor(int id)
        {   
            Id = id;
            Priority = PriorityResult.Unknown;
            Exchange = ExchangeResult.Unknown;
        }

        public enum PriorityResult
        {
            Unknown,
            Winner,
            NotWinner
        }

        public enum ExchangeResult
        {
            Unknown,
            Rejected,
            Accepted
        }
    }
}