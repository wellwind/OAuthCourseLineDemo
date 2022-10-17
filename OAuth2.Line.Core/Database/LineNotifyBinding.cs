using System;
using System.Collections.Generic;

namespace OAuth2.Line.Core.Database
{
    public partial class LineNotifyBinding
    {
        public LineNotifyBinding()
        {
            MessageStatuses = new HashSet<MessageStatus>();
        }

        public string Sub { get; set; } = null!;
        public string Name { get; set; } = null!;
        public string Picture { get; set; } = null!;
        public string LineLoginIdToken { get; set; } = null!;
        public string LineLoginAccessToken { get; set; } = null!;
        public string LineLoginRefreshToken { get; set; } = null!;
        public string? LineNotifyAccessToken { get; set; }

        public virtual ICollection<MessageStatus> MessageStatuses { get; set; }
    }
}
