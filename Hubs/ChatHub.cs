using BizOne.DAL;
using Microsoft.AspNet.SignalR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Web;

namespace BizOne.Hubs
{
    public class ChatHub : Hub
    {
        // Use your existing DAL to handle SQL logic
        private readonly ChatRepository _repo = new ChatRepository();

        public async Task SendMessage(long senderId, string senderName, string message, string roomIdentifier)
        {
            // Get the current time to send to clients
            string sendingDate = DateTime.Now.ToString("dd MMM yyyy hh:mm tt");

            // 1. Save to DB
            _repo.SaveMessage(senderId, roomIdentifier, message);

            // 2. Broadcast with ALL 5 parameters required by your JS listener
            Clients.Group(roomIdentifier).ReceiveMessage(sendingDate, senderId, senderName, message, roomIdentifier);
        }
        public async Task JoinRoom(string roomIdentifier, long userId)
        {
            await Groups.Add(Context.ConnectionId, roomIdentifier);

            // 1. Mark as read in DB
            _repo.MarkAsRead(userId, roomIdentifier);

            // 2. Tell THIS specific user to refresh their top badge
            Clients.Caller.updateGlobalUnreadCount();

            // 3. Optional: Tell others in the room this user has read messages
            Clients.Group(roomIdentifier).UserReadUpdate(userId, roomIdentifier);
        }

    }
}