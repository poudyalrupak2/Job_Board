using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using AICommunityPlatform.Data;
using AICommunityPlatform.Models;
using System.Security.Claims;
using AICommunityPlatform.ViewModels;

namespace AICommunityPlatform.Controllers
{
    [Authorize]
    public class CommunityController : Controller
    {
        private readonly ApplicationDbContext _context;

        public CommunityController(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            var userId = GetCurrentUserId();

            // Get posts from connections and public posts
            var posts = await _context.Posts
                .Include(p => p.Author)
                .Include(p => p.Comments)
                .ThenInclude(c => c.User)
                .Include(p => p.Likes)
                .ThenInclude(l => l.User)
                .Include(p => p.Group)
                .Where(p => p.IsPublic ||
                           _context.Connections.Any(c =>
                               ((c.SenderId == userId && c.ReceiverId == p.AuthorId) ||
                                (c.SenderId == p.AuthorId && c.ReceiverId == userId)) &&
                               c.Status == ConnectionStatus.Accepted))
                .OrderByDescending(p => p.CreatedAt)
                .Take(50)
                .ToListAsync();

            return View(posts);
        }

        public async Task<IActionResult> Members(string searchTerm)
        {
            var members = _context.Users
                .Where(u => u.Role == UserRole.CommunityMember);

            if (!string.IsNullOrEmpty(searchTerm))
            {
                members = members.Where(u => u.FirstName.Contains(searchTerm) ||
                                           u.LastName.Contains(searchTerm) ||
                                           (u.Skills != null && u.Skills.Contains(searchTerm)) ||
                                           (u.Bio != null && u.Bio.Contains(searchTerm)));
            }

            var memberList = await members.ToListAsync();
            ViewBag.SearchTerm = searchTerm;

            return View(memberList);
        }

        public async Task<IActionResult> Groups()
        {
            var currentUserId = GetCurrentUserId();

            var groups = await _context.Groups
                .Include(g => g.Moderator)
                .Include(g => g.Members)
                .ThenInclude(gm => gm.User)
                .OrderByDescending(g => g.CreatedAt)
                .ToListAsync();

            // Add membership status for current user
            foreach (var group in groups)
            {
                var membership = group.Members.FirstOrDefault(m => m.UserId == currentUserId);
                ViewBag.UserMembershipStatus = ViewBag.UserMembershipStatus ?? new Dictionary<int, string>();
                ((Dictionary<int, string>)ViewBag.UserMembershipStatus)[group.Id] =
                    membership?.IsApproved == true ? "member" :
                    membership != null ? "pending" : "not_member";
            }

            return View(groups);
        }

        [HttpGet]
        public async Task<IActionResult> Group(int id)
        {
            var group = await _context.Groups
                .Include(g => g.Moderator)
                .Include(g => g.Members)
                .ThenInclude(gm => gm.User)
                .Include(g => g.Posts)
                .ThenInclude(p => p.Author)
                .FirstOrDefaultAsync(g => g.Id == id);

            if (group == null)
            {
                return NotFound();
            }

            var userId = GetCurrentUserId();
            var membership = group.Members.FirstOrDefault(m => m.UserId == userId);

            ViewBag.UserMembership = membership;
            ViewBag.IsMember = membership?.IsApproved == true;
            ViewBag.IsPending = membership != null && !membership.IsApproved;
            ViewBag.IsModerator = group.ModeratorId == userId;

            return View(group);
        }

        [HttpPost]
        public async Task<IActionResult> CreatePost(string content, bool isPublic = true, int? groupId = null)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(content))
                {
                    return Json(new { success = false, message = "Post content cannot be empty" });
                }

                var userId = GetCurrentUserId();

                // If posting to a group, verify membership
                if (groupId.HasValue)
                {
                    var isMember = await _context.GroupMembers
                        .AnyAsync(gm => gm.GroupId == groupId.Value && gm.UserId == userId && gm.IsApproved);

                    if (!isMember)
                    {
                        return Json(new { success = false, message = "You are not a member of this group" });
                    }
                }

                var post = new Post
                {
                    Content = content,
                    AuthorId = userId,
                    GroupId = groupId,
                    CreatedAt = DateTime.Now,
                    IsPublic = isPublic && !groupId.HasValue // Group posts are not public
                };

                _context.Posts.Add(post);
                await _context.SaveChangesAsync();

                // Load author details for response
                post.Author = await _context.Users.FindAsync(userId);

                return Json(new
                {
                    success = true,
                    message = "Post created successfully!",
                    postId = post.Id
                });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Error creating post" });
            }
        }

        [HttpPost]
        public async Task<IActionResult> LikePost(int postId)
        {
            try
            {
                var userId = GetCurrentUserId();

                var existingLike = await _context.PostLikes
                    .FirstOrDefaultAsync(pl => pl.PostId == postId && pl.UserId == userId);

                if (existingLike != null)
                {
                    // Unlike the post
                    _context.PostLikes.Remove(existingLike);
                    await _context.SaveChangesAsync();

                    var likeCount = await _context.PostLikes.CountAsync(pl => pl.PostId == postId);
                    return Json(new { success = true, liked = false, likeCount = likeCount });
                }
                else
                {
                    // Like the post
                    var like = new PostLike
                    {
                        PostId = postId,
                        UserId = userId,
                        CreatedAt = DateTime.Now
                    };

                    _context.PostLikes.Add(like);
                    await _context.SaveChangesAsync();

                    var likeCount = await _context.PostLikes.CountAsync(pl => pl.PostId == postId);
                    return Json(new { success = true, liked = true, likeCount = likeCount });
                }
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Error processing like" });
            }
        }

        [HttpPost]
        public async Task<IActionResult> CommentOnPost(int postId, string content)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(content))
                {
                    return Json(new { success = false, message = "Comment cannot be empty" });
                }

                var userId = GetCurrentUserId();

                var comment = new PostComment
                {
                    PostId = postId,
                    UserId = userId,
                    Content = content,
                    CreatedAt = DateTime.Now
                };

                _context.PostComments.Add(comment);
                await _context.SaveChangesAsync();

                // Load user details
                comment.User = await _context.Users.FindAsync(userId);

                return Json(new
                {
                    success = true,
                    comment = new
                    {
                        id = comment.Id,
                        content = comment.Content,
                        userName = comment.User.DisplayName,
                        userInitials = comment.User.FirstName.Substring(0, 1) + comment.User.LastName.Substring(0, 1),
                        createdAt = comment.CreatedAt.ToString("MMM dd, yyyy")
                    }
                });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Error adding comment" });
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SendConnectionRequest(int userId)
        {
            try
            {
                // Debug logging
                Console.WriteLine($"SendConnectionRequest called with userId: {userId}");
                Console.WriteLine($"Request.Method: {Request.Method}");
                Console.WriteLine($"Request.ContentType: {Request.ContentType}");

                // Check form data if parameter is 0
                if (userId == 0 && Request.HasFormContentType)
                {
                    foreach (var key in Request.Form.Keys)
                    {
                        Console.WriteLine($"Form key: {key}, value: {Request.Form[key]}");
                    }

                    if (Request.Form.ContainsKey("userId"))
                    {
                        if (int.TryParse(Request.Form["userId"], out var formUserId))
                        {
                            userId = formUserId;
                            Console.WriteLine($"Using userId from form: {userId}");
                        }
                    }
                }

                var currentUserId = GetCurrentUserId();
                Console.WriteLine($"Current user ID: {currentUserId}");

                if (currentUserId <= 0)
                {
                    Console.WriteLine("Invalid current user ID");
                    return Json(new { success = false, message = "User not authenticated" });
                }

                if (userId <= 0)
                {
                    Console.WriteLine($"Invalid target userId: {userId}");
                    return Json(new { success = false, message = "Invalid user ID" });
                }

                if (currentUserId == userId)
                {
                    Console.WriteLine("User trying to connect to themselves");
                    return Json(new { success = false, message = "Cannot connect to yourself" });
                }

                // Check if target user exists
                var targetUser = await _context.Users.FindAsync(userId);
                if (targetUser == null)
                {
                    Console.WriteLine($"Target user not found: {userId}");
                    return Json(new { success = false, message = "User not found" });
                }

                var existingConnection = await _context.Connections
                    .FirstOrDefaultAsync(c =>
                        (c.SenderId == currentUserId && c.ReceiverId == userId) ||
                        (c.SenderId == userId && c.ReceiverId == currentUserId));

                if (existingConnection != null)
                {
                    Console.WriteLine($"Existing connection found with status: {existingConnection.Status}");

                    if (existingConnection.Status == ConnectionStatus.Accepted)
                    {
                        return Json(new { success = false, message = "Already connected" });
                    }
                    else if (existingConnection.Status == ConnectionStatus.Pending)
                    {
                        // Check if there's a mutual request (both users sent requests)
                        var mutualRequest = await _context.Connections
                            .FirstOrDefaultAsync(c =>
                                c.SenderId == userId && c.ReceiverId == currentUserId && c.Status == ConnectionStatus.Pending);

                        if (mutualRequest != null)
                        {
                            // Auto-accept both requests
                            existingConnection.Status = ConnectionStatus.Accepted;
                            existingConnection.AcceptedDate = DateTime.Now;
                            mutualRequest.Status = ConnectionStatus.Accepted;
                            mutualRequest.AcceptedDate = DateTime.Now;

                            await _context.SaveChangesAsync();

                            return Json(new { success = true, message = "Connection established!" });
                        }

                        return Json(new { success = false, message = "Connection request already pending" });
                    }
                    else if (existingConnection.Status == ConnectionStatus.Rejected)
                    {
                        return Json(new { success = false, message = "Connection request was previously rejected" });
                    }
                }

                var connection = new Connection
                {
                    SenderId = currentUserId,
                    ReceiverId = userId,
                    RequestDate = DateTime.Now,
                    Status = ConnectionStatus.Pending
                };

                _context.Connections.Add(connection);
                var result = await _context.SaveChangesAsync();

                Console.WriteLine($"SaveChanges result: {result} rows affected");

                return Json(new { success = true, message = "Connection request sent!" });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Exception in SendConnectionRequest: {ex.Message}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");
                return Json(new { success = false, message = $"Error sending connection request: {ex.Message}" });
            }
        }

        // NEW METHODS FOR MESSAGING AND CONNECTION STATUS

        // Get connection statuses for multiple users
        [HttpPost]
        public async Task<IActionResult> GetConnectionStatuses([FromBody] ConnectionStatusRequest request)
        {
            try
            {
                var currentUserId = GetCurrentUserId();
                var statuses = new Dictionary<string, string>();

                foreach (var userIdStr in request.UserIds)
                {
                    if (int.TryParse(userIdStr, out int userId))
                    {
                        var status = await GetConnectionStatus(currentUserId, userId);
                        statuses[userIdStr] = status;
                    }
                }

                return Json(new { success = true, statuses });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Error loading connection statuses" });
            }
        }

        // Check single connection status
        [HttpPost]
        public async Task<IActionResult> CheckConnectionStatus(int userId)
        {
            try
            {
                var currentUserId = GetCurrentUserId();
                var status = await GetConnectionStatus(currentUserId, userId);

                return Json(new { success = true, status });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Error checking connection status" });
            }
        }

        // Accept connection request (alternative method name)
        [HttpPost]
        public async Task<IActionResult> AcceptConnectionRequest(int userId)
        {
            try
            {
                var currentUserId = GetCurrentUserId();

                // Find the pending connection request
                var connection = await _context.Connections
                    .FirstOrDefaultAsync(c => c.SenderId == userId && c.ReceiverId == currentUserId && c.Status == ConnectionStatus.Pending);

                if (connection == null)
                {
                    return Json(new { success = false, message = "No pending connection request found" });
                }

                // Accept the connection
                connection.Status = ConnectionStatus.Accepted;
                connection.AcceptedDate = DateTime.Now;

                await _context.SaveChangesAsync();

                return Json(new { success = true, message = "Connection request accepted!" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Error accepting connection request" });
            }
        }

        // Helper method to get connection status
        private async Task<string> GetConnectionStatus(int currentUserId, int otherUserId)
        {
            var connection = await _context.Connections
                .FirstOrDefaultAsync(c =>
                    ((c.SenderId == currentUserId && c.ReceiverId == otherUserId) ||
                     (c.SenderId == otherUserId && c.ReceiverId == currentUserId)));

            if (connection == null)
            {
                return "NotConnected";
            }

            if (connection.Status == ConnectionStatus.Accepted)
            {
                return "Connected";
            }
            else if (connection.Status == ConnectionStatus.Pending)
            {
                // If current user sent the request, show "Pending"
                // If other user sent the request, show "PendingApproval"
                return connection.SenderId == currentUserId ? "Pending" : "PendingApproval";
            }
            else if (connection.Status == ConnectionStatus.Rejected)
            {
                return "Declined";
            }

            return "NotConnected";
        }

        [HttpPost]
        public async Task<IActionResult> AcceptConnection(int connectionId)
        {
            try
            {
                var userId = GetCurrentUserId();
                var connection = await _context.Connections
                    .FirstOrDefaultAsync(c => c.Id == connectionId && c.ReceiverId == userId);

                if (connection == null)
                {
                    return Json(new { success = false, message = "Connection request not found" });
                }

                connection.Status = ConnectionStatus.Accepted;
                connection.AcceptedDate = DateTime.Now;

                await _context.SaveChangesAsync();

                return Json(new { success = true, message = "Connection accepted!" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Error accepting connection" });
            }
        }

        [HttpPost]
        public async Task<IActionResult> RejectConnection(int connectionId)
        {
            try
            {
                var userId = GetCurrentUserId();
                var connection = await _context.Connections
                    .FirstOrDefaultAsync(c => c.Id == connectionId && c.ReceiverId == userId);

                if (connection == null)
                {
                    return Json(new { success = false, message = "Connection request not found" });
                }

                connection.Status = ConnectionStatus.Rejected;
                await _context.SaveChangesAsync();

                return Json(new { success = true, message = "Connection request rejected" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Error rejecting connection" });
            }
        }

        [HttpPost]
        public async Task<IActionResult> CreateGroup(string name, string description, bool isPrivate = false)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(name))
                {
                    return Json(new { success = false, message = "Group name is required" });
                }

                var userId = GetCurrentUserId();

                var group = new Group
                {
                    Name = name,
                    Description = description,
                    ModeratorId = userId,
                    CreatedAt = DateTime.Now,
                    IsPrivate = isPrivate
                };

                _context.Groups.Add(group);
                await _context.SaveChangesAsync();

                // Add creator as first member
                var membership = new GroupMember
                {
                    GroupId = group.Id,
                    UserId = userId,
                    JoinedAt = DateTime.Now,
                    IsApproved = true
                };

                _context.GroupMembers.Add(membership);
                await _context.SaveChangesAsync();

                return Json(new { success = true, message = "Group created successfully!", groupId = group.Id });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Error creating group" });
            }
        }

        [HttpPost]
        public async Task<IActionResult> JoinGroup(int groupId)
        {
            try
            {
                // Add validation for groupId
                if (groupId <= 0)
                {
                    return Json(new { success = false, message = "Invalid group ID" });
                }

                var userId = GetCurrentUserId();

                var existingMembership = await _context.GroupMembers
                    .FirstOrDefaultAsync(gm => gm.GroupId == groupId && gm.UserId == userId);

                if (existingMembership != null)
                {
                    var status = existingMembership.IsApproved ? "Already a member" : "Request pending approval";
                    return Json(new { success = false, message = status });
                }

                var group = await _context.Groups.FindAsync(groupId);
                if (group == null)
                {
                    return Json(new { success = false, message = "Group not found" });
                }

                var membership = new GroupMember
                {
                    GroupId = groupId,
                    UserId = userId,
                    JoinedAt = DateTime.Now,
                    IsApproved = !group.IsPrivate // Auto-approve for public groups
                };

                _context.GroupMembers.Add(membership);
                await _context.SaveChangesAsync();

                return Json(new
                {
                    success = true,
                    message = group.IsPrivate ? "Join request sent for approval" : "Successfully joined the group!",
                    requiresApproval = group.IsPrivate
                });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = $"Error joining group: {ex.Message}" });
            }
        }

        [HttpPost]
        public async Task<IActionResult> LeaveGroup(int groupId)
        {
            try
            {
                if (groupId <= 0)
                {
                    return Json(new { success = false, message = "Invalid group ID" });
                }

                var userId = GetCurrentUserId();

                var membership = await _context.GroupMembers
                    .FirstOrDefaultAsync(gm => gm.GroupId == groupId && gm.UserId == userId);

                if (membership == null)
                {
                    return Json(new { success = false, message = "You are not a member of this group" });
                }

                // Check if user is the moderator
                var group = await _context.Groups.FindAsync(groupId);
                if (group?.ModeratorId == userId)
                {
                    return Json(new { success = false, message = "Group moderators cannot leave the group. Transfer ownership first." });
                }

                _context.GroupMembers.Remove(membership);
                await _context.SaveChangesAsync();

                return Json(new { success = true, message = "Successfully left the group" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Error leaving group" });
            }
        }

        [HttpPost]
        public async Task<IActionResult> SharePost(int postId)
        {
            try
            {
                // For now, just return the share URL
                var shareUrl = $"{Request.Scheme}://{Request.Host}/Community/Post/{postId}";
                return Json(new { success = true, shareUrl = shareUrl });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Error sharing post" });
            }
        }

        [HttpGet]
        public async Task<IActionResult> Post(int id)
        {
            var post = await _context.Posts
                .Include(p => p.Author)
                .Include(p => p.Comments)
                .ThenInclude(c => c.User)
                .Include(p => p.Likes)
                .ThenInclude(l => l.User)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (post == null)
            {
                return NotFound();
            }

            return View(post);
        }

        [HttpGet]
        public async Task<IActionResult> Connections()
        {
            var userId = GetCurrentUserId();

            // Get accepted connections
            var connections = await _context.Connections
                .Include(c => c.Sender)
                .Include(c => c.Receiver)
                .Where(c => (c.SenderId == userId || c.ReceiverId == userId) &&
                           c.Status == ConnectionStatus.Accepted)
                .ToListAsync();

            // Get pending received requests
            var pendingRequests = await _context.Connections
                .Include(c => c.Sender)
                .Where(c => c.ReceiverId == userId && c.Status == ConnectionStatus.Pending)
                .ToListAsync();

            var viewModel = new ConnectionsViewModel
            {
                Connections = connections,
                PendingRequests = pendingRequests
            };

            return View(viewModel);
        }

        private int GetCurrentUserId()
        {
            return int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
        }
    }

    // Helper classes
    public class ConnectionStatusRequest
    {
        public List<string> UserIds { get; set; } = new List<string>();
    }
}