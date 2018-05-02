using System;
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

        public int Musicians { get; private set; }
        public int Connected { get; private set; } = 0;

        public bool AllConnected => Connected == Musicians;

        public Conductor(IHubContext<ConductorHub> hubContext)
        {
            hub = hubContext;
            Musicians = MusiciansLoader.GetNumberOfMusicians();
        }

        public Task ConnectMusician(JoinMessage message, string connectionId)
        {
            return LockAsync(async () => 
            {
                if (AllConnected)
                    return;

                Connected++;
                await hub.Groups.AddAsync(connectionId, message.SenderId.ToString());
                Console.WriteLine($"Connected: {message.SenderId} ({connectionId}), status: {Connected}/{Musicians}");

                if (AllConnected)
                {
                    Console.WriteLine("All connected. Starting...");
                    await hub.Clients.All.SendAsync("start");
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