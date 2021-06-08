using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SampleMvcApp.Model
{
    public class Message
    {
        public int Id { get; set; }
        public int ChatId { get; set; }
        public string SenderId { get; set; }
        public string RecipientId { get; set; }
        public string MsgContent { get; set; }
        public DateTime CreatedAt{ get; set; }
    }
}
