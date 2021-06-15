using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using Newtonsoft.Json;
using SampleMvcApp.Model;

namespace SampleMvcApp.Hubs
{
    public class ChatHub:Hub
    {
        public ChatAppDbContext _chatAppDbContext;
        private readonly IDistributedCache _distributedCache;

        public ChatHub(ChatAppDbContext chatAppDbContext, IDistributedCache distributedCache)
        {
            _chatAppDbContext = chatAppDbContext;
            _distributedCache = distributedCache;
        }
        public async Task AddToChat(string groupName, string user)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, groupName);

            await Clients.Group(groupName).SendAsync("Entered", user);
        }

        public async Task BlockUser(string userid)
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, userid);

            //db operation to remove user from userlist.
             
        }

        public override async Task OnConnectedAsync()
        {

            var cacheKey = "onlineUsers";
            string serializedCachedOnlineUsers;
            var onlineUsers = new List<UserOnline>();
            byte[] redisCacheOnlineUsers = await _distributedCache.GetAsync(cacheKey);

            if (redisCacheOnlineUsers != null && redisCacheOnlineUsers.Length > 0)
            {
                serializedCachedOnlineUsers = Encoding.UTF8.GetString(redisCacheOnlineUsers);
                onlineUsers = JsonConvert.DeserializeObject<List<UserOnline>>(serializedCachedOnlineUsers);

                var currentUser= onlineUsers.FirstOrDefault(x => x.Username == Context.User.Identity.Name);
                if (currentUser != null)
                {
                    onlineUsers.Remove(currentUser);

                }

                onlineUsers.Add(new UserOnline { ConnectionId = Context.ConnectionId, Username = Context.User.Identity.Name, UserIdentifier = Context.UserIdentifier });

                await Clients.All.SendAsync("UpdateOnlineUsers", onlineUsers.Distinct());

               

            }
            else
            {
                onlineUsers.Add(new UserOnline { ConnectionId = Context.ConnectionId, Username = Context.User.Identity.Name, UserIdentifier = Context.UserIdentifier });

                await Clients.All.SendAsync("UpdateOnlineUsers", onlineUsers.Distinct());

            }

            serializedCachedOnlineUsers = JsonConvert.SerializeObject(onlineUsers);
            redisCacheOnlineUsers = Encoding.UTF8.GetBytes(serializedCachedOnlineUsers);

            var options = new DistributedCacheEntryOptions()
                .SetAbsoluteExpiration(TimeSpan.FromHours(24))
                .SetSlidingExpiration(TimeSpan.FromHours(1));
            await _distributedCache.SetAsync(cacheKey, redisCacheOnlineUsers, options);

            //var datta = ConnectedUser.UsersOnline.FirstOrDefault(x=>x.Username== Context.User.Identity.Name);
            //if (datta!=null)
            //{
            //    ConnectedUser.UsersOnline.Remove(datta);

            //}
            //ConnectedUser.UsersOnline.Add(new UserOnline { ConnectionId = Context.ConnectionId, Username = Context.User.Identity.Name, UserIdentifier = Context.UserIdentifier });

            //await Clients.All.SendAsync("UpdateOnlineUsers", ConnectedUser.UsersOnline.Distinct());


            await base.OnConnectedAsync();
        }

        public override async Task OnDisconnectedAsync(Exception exception)
        {
            var cacheKey = "onlineUsers";
            string serializedCachedOnlineUsers;
            var onlineUsers = new List<UserOnline>();
            byte[] redisCacheOnlineUsers = await _distributedCache.GetAsync(cacheKey);
            if (redisCacheOnlineUsers != null && redisCacheOnlineUsers.Length > 0)
            {
                serializedCachedOnlineUsers = Encoding.UTF8.GetString(redisCacheOnlineUsers);
                onlineUsers = JsonConvert.DeserializeObject<List<UserOnline>>(serializedCachedOnlineUsers);

                var currentUser = onlineUsers.FirstOrDefault(x => x.Username == Context.User.Identity.Name);
                if (currentUser != null)
                {
                    onlineUsers.Remove(currentUser);

                }

                serializedCachedOnlineUsers = JsonConvert.SerializeObject(onlineUsers);
                redisCacheOnlineUsers = Encoding.UTF8.GetBytes(serializedCachedOnlineUsers);

                var options = new DistributedCacheEntryOptions()
                    .SetAbsoluteExpiration(TimeSpan.FromHours(24))
                    .SetSlidingExpiration(TimeSpan.FromHours(1));
                await _distributedCache.SetAsync(cacheKey, redisCacheOnlineUsers, options);

                await Clients.All.SendAsync("UpdateOnlineUsers", onlineUsers.Distinct());

            }
             


            ConnectedUser.UsersOnline.Remove(new UserOnline{ ConnectionId = Context.ConnectionId,Username = Context.User.Identity.Name, UserIdentifier = Context.UserIdentifier});
            await base.OnDisconnectedAsync(exception);
        }
        public async Task SendMessage(string username, string recipientid, string message)
        {
           // var recipient = ConnectedUser.UsersOnline.FirstOrDefault(x => x.Username == recipientid);

            var cacheKey = "onlineUsers";
            string serializedCachedOnlineUsers;
            var onlineUsers = new List<UserOnline>();
            byte[] redisCacheOnlineUsers = await _distributedCache.GetAsync(cacheKey);


            if (redisCacheOnlineUsers != null && redisCacheOnlineUsers.Length > 0)
            {
                serializedCachedOnlineUsers = Encoding.UTF8.GetString(redisCacheOnlineUsers);
                onlineUsers = JsonConvert.DeserializeObject<List<UserOnline>>(serializedCachedOnlineUsers);

                var recipient = onlineUsers.FirstOrDefault(x => x.Username == recipientid);
                if (recipient?.UserIdentifier != null)
                {
                    await Clients.User(recipient.UserIdentifier).SendAsync("ReceiveMessage", username, message);

                }
            }

            //await Clients.All.SendAsync("ReceiveMessage", username, message);
            //save to db...

            var currentChat = await _chatAppDbContext.Chat.FirstOrDefaultAsync(a=>(a.SenderId== username && a.RecipientId==recipientid) || (a.SenderId==recipientid && a.RecipientId==username) );
            if (currentChat!=null)
            {
                var msg = new Message
                {
                    SenderId = username,
                    MsgContent = message,
                    RecipientId = recipientid,
                    CreatedAt = DateTime.UtcNow,
                    ChatId = currentChat.Id
                };

                await _chatAppDbContext.Messages.AddAsync(msg);
                await _chatAppDbContext.SaveChangesAsync();
            }
            else
            {
                var chat = new Chat
                {
                    SenderId = username,
                    RecipientId = recipientid
                };

                await _chatAppDbContext.Chat.AddAsync(chat);
                await _chatAppDbContext.SaveChangesAsync();

                var msg = new Message
                {
                    SenderId = username,
                    MsgContent = message,
                    RecipientId = recipientid,
                    CreatedAt = DateTime.UtcNow,
                    ChatId = chat.Id
                };

                await _chatAppDbContext.Messages.AddAsync(msg);
                await _chatAppDbContext.SaveChangesAsync();
            }


            
        }
    }

    public static class ConnectedUser
    {
        public static List<UserOnline> UsersOnline = new List<UserOnline>();
    }

    public class UserOnline
    {
        public string Username { get; set; }
        public string ConnectionId { get; set; }
        public string UserIdentifier { get; set; }
    }
}
