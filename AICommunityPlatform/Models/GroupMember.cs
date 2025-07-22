﻿namespace AICommunityPlatform.Models
{
  
        public class GroupMember
        {
            public int Id { get; set; }

            public int GroupId { get; set; }
            public virtual Group Group { get; set; }

            public int UserId { get; set; }
            public virtual User User { get; set; }

            public DateTime JoinedAt { get; set; }

            public bool IsApproved { get; set; } = false;
        }
    
}
