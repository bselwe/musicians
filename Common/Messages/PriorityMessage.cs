namespace Common.Messages
{
    public class PriorityMessage : Message
    {
        public PriorityStatus Status { get; set; }

        public enum PriorityStatus
        {
            Winner,
            Loser,
            NotLoser
        }
    }
}