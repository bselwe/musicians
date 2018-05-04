namespace Musicians
{
    public class Neighbor
    {
        public int Id { get; private set; }
        public MusicianPriority Priority { get; set; }

        public Neighbor(int id)
        {   
            Id = id;
            Priority = MusicianPriority.Unknown;
        }
    }
}