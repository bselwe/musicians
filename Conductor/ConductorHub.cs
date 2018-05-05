using System.Threading.Tasks;
using Common.Messages;
using Microsoft.AspNetCore.SignalR;

namespace Conductor
{
    public class ConductorHub : Hub
    {
        private readonly Conductor conductor;

        public ConductorHub(Conductor conductor)
        {
            this.conductor = conductor;
        }

        public Task Connect(ConnectMessage message)
        {
            return conductor.ConnectMusician(message, Context.ConnectionId);
        }

        public Task Exchange(ExchangeMessage message)
        {
            return conductor.SendMessage("exchange", message);
        }

        public Task Prioritize(PriorityMessage message)
        {
            return conductor.SendMessage("prioritize", message);
        }

        public Task Perform(PriorityMessage message)
        {
            return conductor.SendMessage("perform", message);
        }

        public Task Reject(RejectMessage message)
        {
            return conductor.SendMessage("reject", message);
        }
    }
}