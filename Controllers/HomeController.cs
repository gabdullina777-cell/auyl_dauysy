using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ayul_dayusy.Models;
using Microsoft.AspNetCore.Authorization;

namespace ayul_dayusy.Controllers
{
    public class HomeController : Controller
    {
        private readonly AppDbContext _db;

        public HomeController(AppDbContext db)
        {
            _db = db;
        }

        public async Task<IActionResult> Index()
        {
            ViewBag.UserCount = await _db.Users.CountAsync();
            ViewBag.PetitionCount = await _db.Petitions.CountAsync();
            ViewBag.SolvedCount = await _db.Petitions.CountAsync(p => p.IsSolved);
            return View();
        }

        public async Task<IActionResult> Petitions(string? category, string? region)
        {
            var query = _db.Petitions.AsQueryable();

            if (!string.IsNullOrEmpty(category))
                query = query.Where(p => p.Category == category);

            if (!string.IsNullOrEmpty(region))
                query = query.Where(p => p.Region == region);

            var petitions = await query
                .OrderByDescending(p => p.VoteCount)
                .ToListAsync();

            var votedIds = new List<int>();
            if (User.Identity!.IsAuthenticated)
            {
                var user = await _db.Users
                    .FirstOrDefaultAsync(u => u.UserName == User.Identity.Name);
                if (user != null)
                {
                    votedIds = await _db.PetitionVotes
                        .Where(v => v.UserId == user.Id)
                        .Select(v => v.PetitionId)
                        .ToListAsync();
                }
            }

            ViewBag.VotedIds = votedIds;
            ViewBag.Category = category;
            ViewBag.Region = region;
            return View(petitions);
        }

        [Authorize]
        public IActionResult Submit() => View();
        public IActionResult Map() => View();

        [Authorize]
        [HttpPost]
        public async Task<IActionResult> Submit(string title, string description,
      string region, string village, string category, List<IFormFile>? files)
        {
            var user = await _db.Users
                .FirstOrDefaultAsync(u => u.UserName == User.Identity!.Name);

            var petition = new Petition
            {
                Title = title,
                Description = description,
                Region = region,
                Village = village,
                Category = category,
                AuthorId = user?.Id ?? "",
                AuthorName = user?.FullName ?? "Белгісіз"
            };

            _db.Petitions.Add(petition);
            await _db.SaveChangesAsync();
            return RedirectToAction("Petitions");
        }

        [Authorize]
        [HttpPost]
        public async Task<IActionResult> Vote(int id)
        {
            var user = await _db.Users
                .FirstOrDefaultAsync(u => u.UserName == User.Identity!.Name);
            if (user == null) return RedirectToAction("Petitions");

            var alreadyVoted = await _db.PetitionVotes
                .AnyAsync(v => v.PetitionId == id && v.UserId == user.Id);

            if (!alreadyVoted)
            {
                _db.PetitionVotes.Add(new PetitionVote
                {
                    PetitionId = id,
                    UserId = user.Id
                });

                var petition = await _db.Petitions.FindAsync(id);
                if (petition != null)
                    petition.VoteCount++;

                await _db.SaveChangesAsync();
            }

            return RedirectToAction("Petitions");
        }
    }
}