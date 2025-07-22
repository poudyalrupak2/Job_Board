namespace AICommunityPlatform.Models
{
    public class Message
    {
        public int Id { get; set; }

        public int SenderId { get; set; }
        public virtual User Sender { get; set; }

        public int ReceiverId { get; set; }
        public virtual User Receiver { get; set; }

        public string Content { get; set; }

        public DateTime SentAt { get; set; }

        public bool IsRead { get; set; } = false;

        public int? GroupId { get; set; }
        public virtual Group Group { get; set; }

        // Message types
        public MessageType Type { get; set; } = MessageType.Text;

        // For file attachments
        public string? AttachmentPath { get; set; }
        public string? AttachmentName { get; set; }
    }

    public enum MessageType
    {
        Text,
        Image,
        File,
        System
    }

}
