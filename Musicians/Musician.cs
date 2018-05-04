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
        private List<Neighbor> neighbors = new List<Neighbor>();

        public int Id { get; private set; }
        public Position Position { get; private set; }
        public MusicianPriority Priority { get; private set; }
        public int PriorityValue { get; private set; }
        public MusicianStatus Status { get; private set; }
        public IReadOnlyList<Neighbor> Neighbors => neighbors;
        public IEnumerable<int> NeighborsIds => Neighbors.Select(n => n.Id);

        public int AcceptedExchange { get; private set; }
        public int RejectedExchange { get; private set; }
        public bool ExchangeCompleted => AcceptedExchange + RejectedExchange == Neighbors.Count;
        public bool ExchangeAccepted => AcceptedExchange == Neighbors.Count && RejectedExchange == 0;

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

        public void SetNeighbors(IEnumerable<Neighbor> neighbors)
        {
            this.neighbors = new List<Neighbor>(neighbors);
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
            if (Status == MusicianStatus.WaitingForExchange)
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
            if (message.Value > PriorityValue)
                return Exchange(message.Value, ExchangeStatus.Accepted, new[] { message.Sender });

            return Exchange(Id, ExchangeStatus.Rejected, new[] { message.Sender });
        }

        private Task HandleAcceptedExchangeMessage(ExchangeMessage message)
        {
            AcceptedExchange++;

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

        private Task HandleRejectedExchangeMessage(ExchangeMessage message)
        {
            RejectedExchange++;

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
                    case PriorityStatus.Loser:
                        return HandleLoserPriorityMessage(message);
                    case PriorityStatus.NotLoser:
                        return HandleNotLoserPriorityMessage(message);
                }
            }

            return Reject(message, $"Current status: {Status}, expected: {MusicianStatus.WaitingForPriority}");
        }

        private Task HandleWinnerPriorityMessage(PriorityMessage message)
        {
            return Task.CompletedTask;
        }
        
        private Task HandleLoserPriorityMessage(PriorityMessage message)
        {
            return Task.CompletedTask;
        }

        private Task HandleNotLoserPriorityMessage(PriorityMessage message)
        {
            return Task.CompletedTask;
        }

        private void OnRejectMessage(RejectMessage message)
        {
            Console.WriteLine($"[{Id}] Rejected by {message.Sender} with reason: \"{message.Reason}\"");
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