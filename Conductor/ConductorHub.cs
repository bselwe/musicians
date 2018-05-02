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

        public Task Join(ConnectMessage message)
        {
            return conductor.JoinMusician(message);
        }
    }
}