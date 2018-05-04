namespace Common.Messages
{
    public class ExchangeMessage : Message
    {
        public int Value { get; set; }
        public ExchangeStatus Status { get; set; }

        public enum ExchangeStatus
        {
            Requested,
            Rejected,
            Accepted
        }
    }
}