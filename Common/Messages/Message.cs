using System.Collections.Generic;

namespace Common.Messages
{
    public abstract class Message
    {
        public int Sender { get; set; }
        public IEnumerable<int> Receivers { get; set; }
    }
}