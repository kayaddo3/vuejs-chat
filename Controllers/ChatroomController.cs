using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using Newtonsoft.Json;
using SampleMvcApp.Model;

namespace SampleMvcApp.Controllers
{
    [Authorize]
    public class ChatroomController : Controller
    {
        private readonly IDistributedCache _distributedCache;
        public ChatAppDbContext _chatAppDbContext;

        public ChatroomController(IDistributedCache distributedCache, ChatAppDbContext chatAppDbContext)
        {
            _distributedCache = distributedCache;
            _chatAppDbContext = chatAppDbContext;
        }

        public IActionResult Index()
        {
            return View();
        }

        public async Task<JsonResult> GetChatHistory([FromQuery]string receipient)
        {
            var currentChat = await _chatAppDbContext.Chat.FirstOrDefaultAsync(a => (a.SenderId == User.Identity.Name && a.RecipientId == receipient) || (a.SenderId == receipient && a.RecipientId == User.Identity.Name));

            var cacheKey = "chatHistory";
            string serializedChatHistory;
            var chatHistory = new List<Message>();
            byte[] redisChatHistory = await _distributedCache.GetAsync(cacheKey);

            if (redisChatHistory != null && redisChatHistory.Length>0)
            {
                serializedChatHistory = Encoding.UTF8.GetString(redisChatHistory);
                chatHistory = JsonConvert.DeserializeObject<List<Message>>(serializedChatHistory);
            }
            else
            {
                
                chatHistory = await _chatAppDbContext.Messages
                    .Where(a => currentChat != null && a.ChatId == currentChat.Id).ToListAsync();
                
                serializedChatHistory = JsonConvert.SerializeObject(chatHistory);
                redisChatHistory = Encoding.UTF8.GetBytes(serializedChatHistory);
                var options = new DistributedCacheEntryOptions()
                    .SetAbsoluteExpiration(TimeSpan.FromSeconds(30))
                    .SetSlidingExpiration(TimeSpan.FromSeconds(10));
                await _distributedCache.SetAsync(cacheKey, redisChatHistory, options);
            }
            return Json(chatHistory);
        }
    }
}
