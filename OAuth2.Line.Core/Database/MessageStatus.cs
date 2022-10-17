using System;
using System.Collections.Generic;

namespace OAuth2.Line.Core.Database
{
    public partial class MessageStatus
    {
        public int MessageId { get; set; }
        public string Sub { get; set; } = null!;
        public int Status { get; set; }
        public string? ErrorMessage { get; set; }
        public DateTime? CreatedAt { get; set; }

        public virtual Message Message { get; set; } = null!;
        public virtual LineNotifyBinding SubNavigation { get; set; } = null!;
    }
}
