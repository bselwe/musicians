using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Common;
using Common.Messages;
using Microsoft.AspNetCore.SignalR.Client;
using static Common.Messages.ExchangeMessage;

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

        public int AcceptedMessages { get; private set; }
        public int RejectedMessages { get; private set; }
        public int ReceivedMessages => AcceptedMessages + RejectedMessages;
        public bool AllMessagesReceived => ReceivedMessages == Neighbors.Count;

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
            connection.On("reject", (RejectMessage message) => OnRejectMessage(message));

            await SendConnectMessage();
        }

        private Task OnStartMessage()
        {
            if (Status == MusicianStatus.WaitingForStart)
            {
                Console.WriteLine($"[{Id}] Starting with priority value {PriorityValue}");
                Status = MusicianStatus.WaitingForExchange;
                return SendExchangeMessage(PriorityValue, ExchangeStatus.Requested, Neighbors.Select(n => n.Id));
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
                return SendExchangeMessage(message.Value, ExchangeStatus.Accepted, new[] { message.Sender });

            return SendExchangeMessage(Id, ExchangeStatus.Rejected, new[] { message.Sender });
        }

        private Task HandleAcceptedExchangeMessage(ExchangeMessage message)
        {
            AcceptedMessages++;
            return Task.CompletedTask;
        }

        private Task HandleRejectedExchangeMessage(ExchangeMessage message)
        {
            RejectedMessages++;
            return Task.CompletedTask;
        }

        private void OnRejectMessage(RejectMessage message)
        {
            Console.WriteLine($"[{Id}] Messege rejected by {message.Sender} with reason: \"{message.Reason}\"");
        }

        private Task SendConnectMessage()
        {
            return connection.InvokeAsync("connect", new ConnectMessage() { Sender = Id });
        }

        private Task SendExchangeMessage(int value, ExchangeStatus status, IEnumerable<int> receivers)
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
            Console.WriteLine($"Connection closed with error: {ex}");
        }
    }
}