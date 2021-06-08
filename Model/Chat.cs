using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SampleMvcApp.Model
{
    public class Chat
    {
        public int Id { get; set; }
        public string SenderId { get; set; }
        public string RecipientId { get; set; }

    }
}
