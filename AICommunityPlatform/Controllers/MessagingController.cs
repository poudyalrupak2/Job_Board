// Controllers/MessagingController.cs
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using AICommunityPlatform.Data;
using AICommunityPlatform.Models;
using AICommunityPlatform.ViewModels;
using System.Security.Claims;

namespace AICommunityPlatform.Controllers
{
    [Authorize]
    public class MessagingController : Controller
    {
        private readonly ApplicationDbContext _context;

        public MessagingController(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            var userId = GetCurrentUserId();

            // Get all conversations for the current user
            var conversations = await _context.Messages
                .Where(m => m.SenderId == userId || m.ReceiverId == userId)
                .GroupBy(m => m.SenderId == userId ? m.ReceiverId : m.SenderId)
                .Select(g => new ConversationViewModel
                {
                    ParticipantId = g.Key,
                    LastMessage = g.OrderByDescending(m => m.SentAt).First(),
                    UnreadCount = g.Count(m => m.ReceiverId == userId && !m.IsRead)
                })
                .ToListAsync();

            // Load participant details
            foreach (var conversation in conversations)
            {
                conversation.Participant = await _context.Users
                    .FirstOrDefaultAsync(u => u.Id == conversation.ParticipantId);
            }

            var viewModel = new MessagingIndexViewModel
            {
                Conversations = conversations.OrderByDescending(c => c.LastMessage.SentAt).ToList()
            };

            return View(viewModel);
        }

        public async Task<IActionResult> Conversation(int userId)
        {
            var currentUserId = GetCurrentUserId();

            // Get the other user
            var otherUser = await _context.Users.FindAsync(userId);
            if (otherUser == null)
            {
                return NotFound();
            }

            // Get messages between current user and the other user
            var messages = await _context.Messages
                .Where(m => (m.SenderId == currentUserId && m.ReceiverId == userId) ||
                           (m.SenderId == userId && m.ReceiverId == currentUserId))
                .OrderBy(m => m.SentAt)
                .ToListAsync();

            // Mark messages as read
            var unreadMessages = messages.Where(m => m.ReceiverId == currentUserId && !m.IsRead);
            foreach (var message in unreadMessages)
            {
                message.IsRead = true;
            }
            await _context.SaveChangesAsync();

            // Load sender details for each message
            foreach (var message in messages)
            {
                message.Sender = await _context.Users.FindAsync(message.SenderId);
                message.Receiver = await _context.Users.FindAsync(message.ReceiverId);
            }

            var viewModel = new ConversationDetailViewModel
            {
                OtherUser = otherUser,
                Messages = messages,
                CurrentUserId = currentUserId
            };

            return View(viewModel);
        }

        [HttpPost]
        public async Task<IActionResult> SendMessage(int receiverId, string content)
        {
            try
            {
                var senderId = GetCurrentUserId();

                if (string.IsNullOrWhiteSpace(content))
                {
                    return Json(new { success = false, message = "Message cannot be empty" });
                }

                // Check if receiver exists
                var receiver = await _context.Users.FindAsync(receiverId);
                if (receiver == null)
                {
                    return Json(new { success = false, message = "Recipient not found" });
                }

                // Check if users are connected (optional business rule)
                var connection = await _context.Connections
                    .FirstOrDefaultAsync(c =>
                        ((c.SenderId == senderId && c.ReceiverId == receiverId) ||
                         (c.SenderId == receiverId && c.ReceiverId == senderId)) &&
                        c.Status == ConnectionStatus.Accepted);

                if (connection == null)
                {
                    return Json(new { success = false, message = "You can only message your connections" });
                }

                var message = new Message
                {
                    SenderId = senderId,
                    ReceiverId = receiverId,
                    Content = content,
                    SentAt = DateTime.Now,
                    IsRead = false
                };

                _context.Messages.Add(message);
                await _context.SaveChangesAsync();

                // Load sender details for response
                message.Sender = await _context.Users.FindAsync(senderId);

                return Json(new
                {
                    success = true,
                    message = new
                    {
                        id = message.Id,
                        content = message.Content,
                        senderName = message.Sender.DisplayName,
                        sentAt = message.SentAt.ToString("HH:mm"),
                        isFromCurrentUser = true
                    }
                });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Error sending message" });
            }
        }

        [HttpPost]
        public async Task<IActionResult> MarkAsRead(int messageId)
        {
            try
            {
                var userId = GetCurrentUserId();
                var message = await _context.Messages
                    .FirstOrDefaultAsync(m => m.Id == messageId && m.ReceiverId == userId);

                if (message != null)
                {
                    message.IsRead = true;
                    await _context.SaveChangesAsync();
                }

                return Json(new { success = true });
            }
            catch (Exception ex)
            {
                return Json(new { success = false });
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetNewMessages(int lastMessageId = 0)
        {
            try
            {
                var userId = GetCurrentUserId();

                var newMessages = await _context.Messages
                    .Include(m => m.Sender)
                    .Where(m => m.ReceiverId == userId && m.Id > lastMessageId)
                    .OrderBy(m => m.SentAt)
                    .ToListAsync();

                var messagesData = newMessages.Select(m => new {
                    id = m.Id,
                    senderId = m.SenderId,
                    senderName = m.Sender.DisplayName,
                    content = m.Content,
                    sentAt = m.SentAt.ToString("HH:mm"),
                    isRead = m.IsRead
                });

                return Json(new { success = true, messages = messagesData });
            }
            catch (Exception ex)
            {
                return Json(new { success = false });
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetUnreadCount()
        {
            try
            {
                var userId = GetCurrentUserId();
                var unreadCount = await _context.Messages
                    .CountAsync(m => m.ReceiverId == userId && !m.IsRead);

                return Json(new { success = true, count = unreadCount });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, count = 0 });
            }
        }

        [HttpPost]
        public async Task<IActionResult> DeleteMessage(int messageId)
        {
            try
            {
                var userId = GetCurrentUserId();
                var message = await _context.Messages
                    .FirstOrDefaultAsync(m => m.Id == messageId &&
                                            (m.SenderId == userId || m.ReceiverId == userId));

                if (message != null)
                {
                    _context.Messages.Remove(message);
                    await _context.SaveChangesAsync();
                    return Json(new { success = true });
                }

                return Json(new { success = false, message = "Message not found" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Error deleting message" });
            }
        }

        [HttpGet]
        public async Task<IActionResult> SearchMessages(string query)
        {
            try
            {
                var userId = GetCurrentUserId();

                if (string.IsNullOrWhiteSpace(query))
                {
                    return Json(new { success = true, results = new List<object>() });
                }

                var messages = await _context.Messages
                    .Include(m => m.Sender)
                    .Include(m => m.Receiver)
                    .Where(m => (m.SenderId == userId || m.ReceiverId == userId) &&
                               m.Content.Contains(query))
                    .OrderByDescending(m => m.SentAt)
                    .Take(20)
                    .ToListAsync();

                var results = messages.Select(m => new {
                    id = m.Id,
                    content = m.Content,
                    senderName = m.Sender.DisplayName,
                    receiverName = m.Receiver.DisplayName,
                    sentAt = m.SentAt.ToString("MMM dd, yyyy HH:mm"),
                    otherUserId = m.SenderId == userId ? m.ReceiverId : m.SenderId
                });

                return Json(new { success = true, results });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, results = new List<object>() });
            }
        }

        // Group Messaging
        [HttpPost]
        public async Task<IActionResult> SendGroupMessage(int groupId, string content)
        {
            try
            {
                var senderId = GetCurrentUserId();

                if (string.IsNullOrWhiteSpace(content))
                {
                    return Json(new { success = false, message = "Message cannot be empty" });
                }

                // Check if user is a member of the group
                var isMember = await _context.GroupMembers
                    .AnyAsync(gm => gm.GroupId == groupId && gm.UserId == senderId && gm.IsApproved);

                if (!isMember)
                {
                    return Json(new { success = false, message = "You are not a member of this group" });
                }

                var message = new Message
                {
                    SenderId = senderId,
                    ReceiverId = 0, // No specific receiver for group messages
                    GroupId = groupId,
                    Content = content,
                    SentAt = DateTime.Now,
                    IsRead = false
                };

                _context.Messages.Add(message);
                await _context.SaveChangesAsync();

                // Load sender details
                message.Sender = await _context.Users.FindAsync(senderId);

                return Json(new
                {
                    success = true,
                    message = new
                    {
                        id = message.Id,
                        content = message.Content,
                        senderName = message.Sender.DisplayName,
                        sentAt = message.SentAt.ToString("HH:mm")
                    }
                });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Error sending message" });
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetGroupMessages(int groupId, int skip = 0, int take = 50)
        {
            try
            {
                var userId = GetCurrentUserId();

                // Check if user is a member of the group
                var isMember = await _context.GroupMembers
                    .AnyAsync(gm => gm.GroupId == groupId && gm.UserId == userId && gm.IsApproved);

                if (!isMember)
                {
                    return Json(new { success = false, message = "Access denied" });
                }

                var messages = await _context.Messages
                    .Include(m => m.Sender)
                    .Where(m => m.GroupId == groupId)
                    .OrderByDescending(m => m.SentAt)
                    .Skip(skip)
                    .Take(take)
                    .ToListAsync();

                var messagesData = messages.Select(m => new {
                    id = m.Id,
                    senderId = m.SenderId,
                    senderName = m.Sender.DisplayName,
                    content = m.Content,
                    sentAt = m.SentAt.ToString("HH:mm"),
                    isFromCurrentUser = m.SenderId == userId
                }).Reverse();

                return Json(new { success = true, messages = messagesData });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, messages = new List<object>() });
            }
        }

        private int GetCurrentUserId()
        {
            return int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
        }
    }
}




