using System;
using System.Collections.Generic;

namespace OAuth2.Line.Core.Database
{
    public partial class Message
    {
        public Message()
        {
            MessageStatuses = new HashSet<MessageStatus>();
        }

        public int Id { get; set; }
        public string MessageText { get; set; } = null!;
        public DateTime CreatedAt { get; set; }

        public virtual ICollection<MessageStatus> MessageStatuses { get; set; }
    }
}
