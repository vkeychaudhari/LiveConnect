using Microsoft.AspNetCore.SignalR;
using System.Collections.Concurrent;

namespace SignalRWeb.SignalRHub
{
    public class ClientHub : Hub
    {
        // A thread-safe collection to hold client connection IDs and their statuses
        private static ConcurrentDictionary<string, ClientInfo> _connectedClients = new ConcurrentDictionary<string, ClientInfo>();

        public override Task OnConnectedAsync()
        {
            var clientName = Context.ConnectionId; // You can set this to any client-specific value
            var clientInfo = new ClientInfo
            {
                ConnectionId = Context.ConnectionId,
                Name = clientName // You might want to use a more user-friendly name
            };

            _connectedClients[Context.ConnectionId] = clientInfo;

            // Notify all clients about the updated list
            Clients.All.SendAsync("UpdateClientList", GetConnectedClients());

            // Add client to the connected clients list
            // _connectedClients.TryAdd(Context.ConnectionId, new ClientInfo { ConnectionId = Context.ConnectionId });
            return base.OnConnectedAsync();
        }

        public override Task OnDisconnectedAsync(Exception exception)
        {
            // Remove client from the connected clients list
            _connectedClients.TryRemove(Context.ConnectionId, out _);
            Clients.All.SendAsync("UpdateClientList", GetConnectedClients());
            return base.OnDisconnectedAsync(exception);
        }

        // Handle incoming messages from clients
        public async Task BroadcastMessage(string clientName, string message)
        {
            Console.WriteLine($"Message received from {clientName}: {message}");

            // Retrieve the client information if needed and update its status
            if (_connectedClients.TryGetValue(Context.ConnectionId, out var client))
            {
                client.IsSendingMessage = true;
                await Clients.All.SendAsync("UpdateClientStatus", client);
            }

            // Broadcast the message to all connected clients
            await Clients.All.SendAsync("ReceiveMessage", clientName, message);
        }

        public async Task BroadcastScreen(string screenDataUrl)
        {
            try
            {
                // Validate screen data
                if (string.IsNullOrEmpty(screenDataUrl))
                    throw new ArgumentException("Screen data URL cannot be null or empty.");

                await Clients.All.SendAsync("ReceiveScreen", screenDataUrl);
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error in BroadcastScreen: " + ex.Message);
                throw;
            }
        }

        public async Task BroadcastAudio(string base64Audio)
        {
            await Clients.All.SendAsync("ReceiveAudio", base64Audio);
        }

        // Optional: Reset client status after an activity
        public async Task ResetClientStatus()
        {
            if (_connectedClients.TryGetValue(Context.ConnectionId, out var client))
            {
                client.IsSendingMessage = false;
                client.IsSharingScreen = false;
                await Clients.All.SendAsync("UpdateClientStatus", client);
            }
        }

        // Get the list of connected clients
        public static List<ClientInfo> GetConnectedClients()
        {
            return _connectedClients.Values.ToList();
        }
    }

    // Helper class to store client information
    public class ClientInfo
    {
        public string ConnectionId { get; set; }
        public bool IsSendingMessage { get; set; }
        public bool IsSharingScreen { get; set; }
        public string Name { get; set; }
    }
}
