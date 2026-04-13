using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ayul_dayusy.Models;

namespace ayul_dayusy.Controllers
{
    public class ChatController : Controller
    {
        private readonly AppDbContext _db;

        public ChatController(AppDbContext db)
        {
            _db = db;
        }

        [HttpGet]
        public async Task<IActionResult> GetMessages(string region)
        {
            var messages = await _db.ChatMessages
                .Where(m => m.Region == region)
                .OrderByDescending(m => m.CreatedAt)
                .Take(50)
                .OrderBy(m => m.CreatedAt)
                .Select(m => new {
                    m.AuthorName,
                    m.Message,
                    time = m.CreatedAt.ToString("HH:mm")
                })
                .ToListAsync();

            return Json(messages);
        }

        [Authorize]
        [HttpPost]
        public async Task<IActionResult> SendMessage(string region, string message)
        {
            if (string.IsNullOrWhiteSpace(message) || message.Length > 500)
                return BadRequest();

            var user = await _db.Users.FirstOrDefaultAsync(u => u.UserName == User.Identity!.Name);

            var msg = new ChatMessage
            {
                Region = region,
                Message = message,
                AuthorId = user?.Id ?? "",
                AuthorName = user?.FullName ?? "Белгісіз"
            };

            _db.ChatMessages.Add(msg);
            await _db.SaveChangesAsync();

            return Ok();
        }
    }
}