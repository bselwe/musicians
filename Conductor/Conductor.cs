using System;
using System.Threading.Tasks;
using Common;
using Common.Messages;
using Microsoft.AspNetCore.SignalR;

namespace Conductor
{
    public class Conductor
    {
        private readonly object lockObject = new object();
        private readonly IHubContext<ConductorHub> hub;

        public int Musicians { get; private set; } = 6;
        public int Connected { get; private set; } = 0;

        public bool AllConnected => Connected == Musicians;

        public Conductor(IHubContext<ConductorHub> hubContext)
        {
            hub = hubContext;
            Musicians = MusiciansLoader.GetNumberOfMusicians();
        }

        public Task JoinMusician(ConnectMessage message)
        {
            lock (lockObject)
            {
                if (AllConnected)
                    return Task.CompletedTask;

                Connected++;
                Console.WriteLine($"Connected: {message.ConnectedId}, status: {Connected}/{Musicians}");

                if (AllConnected)
                    return hub.Clients.All.SendAsync("start");
                
                return Task.CompletedTask;
            }
        }
    }
}