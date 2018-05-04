using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Common;
using Common.Messages;
using Microsoft.AspNetCore.SignalR.Client;
using static Common.Messages.ExchangeMessage;
using static Common.Messages.PriorityMessage;

namespace Musicians
{
    public class Musician
    {
        private readonly HubConnection connection;
        private Dictionary<int, Neighbor> neighbors = new Dictionary<int, Neighbor>();

        public int Id { get; private set; }
        public Position Position { get; private set; }
        public MusicianPriority Priority { get; private set; }
        public bool PrioritySet => Priority != MusicianPriority.Unknown;
        public int PriorityValue { get; private set; }
        public MusicianStatus Status { get; private set; }
        public IReadOnlyDictionary<int, Neighbor> Neighbors => neighbors;
        public IEnumerable<int> NeighborsIds => Neighbors.Keys;

        public int AcceptedExchange { get; private set; }
        public int RejectedExchange { get; private set; }
        public bool ExchangeCompleted => AcceptedExchange + RejectedExchange == Neighbors.Count;
        public bool ExchangeAccepted => AcceptedExchange == Neighbors.Count && RejectedExchange == 0;

        public int NotWinnerMessages { get; private set; }
        public bool NotWinner => NotWinnerMessages == Neighbors.Count;
        public bool NotWinnerSent { get; private set; }

        public Musician(int id, Position position, int priorityValue)
        {
            Id = id;
            Position = position;
            Priority = MusicianPriority.Unknown;
            PriorityValue = priorityValue;
            Status = MusicianStatus.WaitingForStart;
            
            // Configure connection with the conductor
            connection = new HubConnectionBuilder()
                .WithUrl(Configuration.ConductorHubUrl)
                .WithConsoleLogger()
                .Build();
        }

        public void SetNeighbors(Dictionary<int, Neighbor> neighbors)
        {
            this.neighbors = new Dictionary<int, Neighbor>(neighbors);
        }

        public async Task Run()
        {
            connection.Closed += HandleConnectionError;
            await connection.StartAsync();

            connection.On("start", () => OnStartMessage());
            connection.On("exchange", (ExchangeMessage message) => OnExchangeMessage(message));
            connection.On("prioritize", (PriorityMessage message) => OnPriorityMessage(message));
            connection.On("reject", (RejectMessage message) => OnRejectMessage(message));

            await Connect();
        }

        private Task OnStartMessage()
        {
            if (Status == MusicianStatus.WaitingForStart)
            {
                Console.WriteLine($"[{Id}] Starting with priority value {PriorityValue}");
                Status = MusicianStatus.WaitingForExchange;
                return Exchange(PriorityValue, ExchangeStatus.Requested, NeighborsIds);
            }

            return Task.CompletedTask;
        }

        private Task OnExchangeMessage(ExchangeMessage message)
        {
            if (Status == MusicianStatus.WaitingForExchange || PrioritySet)
            {
                Console.WriteLine($"[{Id}] Received exchange message from {message.Sender} with status {message.Status}");

                switch (message.Status)
                {
                    case ExchangeStatus.Requested:
                        return HandleRequestedExchangeMessage(message);
                    case ExchangeStatus.Accepted:
                        return HandleAcceptedExchangeMessage(message);
                    case ExchangeStatus.Rejected:
                        return HandleRejectedExchangeMessage(message);
                }
            }

            return Reject(message, $"Current status: {Status}, expected: {MusicianStatus.WaitingForExchange}");
        }

        private Task HandleRequestedExchangeMessage(ExchangeMessage message)
        {
            if (Priority == MusicianPriority.Loser)
                return Exchange(message.Value, ExchangeStatus.Accepted, new[] { message.Sender });
            else if (Priority == MusicianPriority.Winner)
                return Exchange(Id, ExchangeStatus.Rejected, new[] { message.Sender });

            if (message.Value > PriorityValue)
                return Exchange(message.Value, ExchangeStatus.Accepted, new[] { message.Sender });
            return Exchange(Id, ExchangeStatus.Rejected, new[] { message.Sender });
        }

        private Task HandleAcceptedExchangeMessage(ExchangeMessage message)
        {
            AcceptedExchange++;
            return HandleVerifiedExchangeMessage(message);
        }

        private Task HandleRejectedExchangeMessage(ExchangeMessage message)
        {
            RejectedExchange++;
            return HandleVerifiedExchangeMessage(message);
        }

        private Task HandleVerifiedExchangeMessage(ExchangeMessage message)
        {
            if (ExchangeAccepted)
            {
                Console.WriteLine($"[{Id}] Winner");
                Status = MusicianStatus.Performing;
                return Prioritize(PriorityStatus.Winner, NeighborsIds);
            }

            if (ExchangeCompleted)
                Status = MusicianStatus.WaitingForPriority;
            
            return Task.CompletedTask;
        }

        private Task OnPriorityMessage(PriorityMessage message)
        {
            if (Status == MusicianStatus.WaitingForPriority)
            {
                Console.WriteLine($"[{Id}] Received priority message from {message.Sender} with status {message.Status}");
                
                switch (message.Status)
                {
                    case PriorityStatus.Winner:
                        return HandleWinnerPriorityMessage(message);
                    case PriorityStatus.NotWinner:
                        return HandleNotWinnerPriorityMessage(message);
                }
            }

            return Reject(message, $"Current status: {Status}, expected: {MusicianStatus.WaitingForPriority}");
        }

        private async Task HandleWinnerPriorityMessage(PriorityMessage message)
        {
            Neighbors[message.Sender].Priority = MusicianPriority.Winner;
            Priority = MusicianPriority.Loser;

            if (!NotWinnerSent)
            {
                NotWinnerSent = true;
                var neighbors = NeighborsIds.Where(id => Neighbors[id].Priority != MusicianPriority.Winner);
                await Prioritize(PriorityStatus.NotWinner, neighbors);
            }

            // TODO: Implement
            // That means, I'm a loser... So I have to wait for the winner
            // WAIT FOR THE WINNER TO START THE NEXT ROUND HERE
            // + Change status not to receive more priority messages
        }

        private async Task HandleNotWinnerPriorityMessage(PriorityMessage message)
        {
            if (!NotWinnerSent)
            {
                NotWinnerSent = true;
                var neighbors = NeighborsIds.Where(id => id != message.Sender && Neighbors[id].Priority != MusicianPriority.Winner);
                await Prioritize(PriorityStatus.NotWinner, neighbors);
            }

            NotWinnerMessages++;    
            if (NotWinner)
            {
                Console.WriteLine($"[{Id}] Not winner, trying to exchange again...");
                ResetForExchange();
                await Exchange(PriorityValue, ExchangeStatus.Requested, NeighborsIds);
            }
        }

        private void OnRejectMessage(RejectMessage message)
        {
            Console.WriteLine($"[{Id}] Rejected by {message.Sender} with reason: \"{message.Reason}\"");
        }

        private void ResetForExchange()
        {
            AcceptedExchange = RejectedExchange = NotWinnerMessages = 0;
            NotWinnerSent = false;
            Status = MusicianStatus.WaitingForExchange;
        }

        private Task Connect()
        {
            return connection.InvokeAsync("connect", new ConnectMessage() { Sender = Id });
        }

        private Task Exchange(int value, ExchangeStatus status, IEnumerable<int> receivers)
        {
            var message = new ExchangeMessage() 
            { 
                Sender = Id, 
                Receivers = receivers,
                Value = value,
                Status = status
            };

            return connection.InvokeAsync("exchange", message);
        }

        private Task Prioritize(PriorityStatus status, IEnumerable<int> receivers)
        {
            var message = new PriorityMessage() 
            { 
                Sender = Id, 
                Receivers = receivers,
                Status = status
            };

            return connection.InvokeAsync("prioritize", message);
        }

        private Task Reject(Message msg, string reason)
        {
            var message = new RejectMessage() 
            { 
                Sender = Id, 
                Receivers = new[] { msg.Sender },
                Reason = reason
            };

            return connection.InvokeAsync("reject", message);
        }

        private void HandleConnectionError(Exception ex)
        {
            Console.WriteLine($"Connection closed with error: {ex?.Message}");
        }
    }
}