using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Common;
using Common.Messages;
using Microsoft.AspNetCore.SignalR;

namespace Conductor
{
    public class Conductor
    {
        private readonly SemaphoreSlim conductorSem = new SemaphoreSlim(1, 1);
        private readonly IHubContext<ConductorHub> hub;

        public Dictionary<int, Musician> Musicians { get; private set; }
        public int Connected { get; private set; } = 0;

        public bool AllConnected => Connected == Musicians.Count;

        public Conductor(IHubContext<ConductorHub> hubContext)
        {
            hub = hubContext;
            Musicians = MusiciansLoader
                .GetMusicians((id, pos) => new Musician(id, pos))
                .ToDictionary(m => m.Id);
        }

        public Task ConnectMusician(JoinMessage message, string connectionId)
        {
            return LockAsync(async () => 
            {
                if (AllConnected)
                    return;

                Connected++;
                await hub.Groups.AddAsync(connectionId, message.SenderId.ToString());
                Console.WriteLine($"Connected: {message.SenderId} ({connectionId}), status: {Connected}/{Musicians.Count}");

                if (AllConnected)
                {
                    Console.WriteLine("All connected. Starting...");
                    await hub.Clients.All.SendAsync("start");
                }
            });
        }

        public Task SendNeighborsMessage(NeighborsMessage message)
        {
            return LockAsync(async () =>
            {
                foreach (var neighbor in Musicians[message.SenderId].Neighbors)
                {
                    await hub.Clients.Group(neighbor.Id.ToString()).SendAsync("neighbors", message.SenderId);
                }
            });
        }

        private async Task LockAsync(Func<Task> action)
        {
            await conductorSem.WaitAsync();

            try
            {
                await action();
            }
            finally
            {
                conductorSem.Release();
            }
        }
    }
}