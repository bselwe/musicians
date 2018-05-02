using System;
using System.Threading.Tasks;
using Common;
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

        public Task Join(JoinMessage message)
        {
            return conductor.ConnectMusician(message, Context.ConnectionId);
        }

        public Task Neighbors(NeighborsMessage message)
        {
            return conductor.SendNeighborsMessage(message);
        }
    }
}