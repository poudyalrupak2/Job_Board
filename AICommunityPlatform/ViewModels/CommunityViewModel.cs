using AICommunityPlatform.Models;

namespace AICommunityPlatform.ViewModels
{
    public class ConnectionsViewModel
    {
        public List<Connection> Connections { get; set; } = new();
        public List<Connection> PendingRequests { get; set; } = new();
    }
}