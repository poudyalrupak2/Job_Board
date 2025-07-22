using AICommunityPlatform.Models;

namespace AICommunityPlatform.ViewModels
{
    public class MessagingIndexViewModel
    {
        public List<ConversationViewModel> Conversations { get; set; } = new();
        public int TotalUnreadCount => Conversations.Sum(c => c.UnreadCount);
    }

    public class ConversationViewModel
    {
        public int ParticipantId { get; set; }
        public User Participant { get; set; }
        public Message LastMessage { get; set; }
        public int UnreadCount { get; set; }
    }

    public class ConversationDetailViewModel
    {
        public User OtherUser { get; set; }
        public List<Message> Messages { get; set; } = new();
        public int CurrentUserId { get; set; }
    }

    public class SendMessageViewModel
    {
        public int ReceiverId { get; set; }
        public string Content { get; set; }
        public int? GroupId { get; set; }
    }
}
