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
        public IActionResult AiHelper() => View();

        [HttpPost]
        public async Task<IActionResult> AiHelper(string problemText, string region, string category)
        {
            ViewBag.Problem = problemText;
            ViewBag.Region = region;
            ViewBag.Category = category;

            if (!string.IsNullOrEmpty(problemText))
            {
                var apiKey = Environment.GetEnvironmentVariable("OPENROUTER_API_KEY");
                var client = new HttpClient();
                client.DefaultRequestHeaders.Add("Authorization", $"Bearer {apiKey}");
                client.DefaultRequestHeaders.Add("HTTP-Referer", "https://auyldauysy-production.up.railway.app");

                var prompt = $@"Сен Қазақстан ауылдарының мәселелерін ресми петицияға айналдыратын көмекшісің.

Мәселе: {problemText}
Облыс: {region}
Санат: {category}

Осы мәселе негізінде толық, грамотты, ресми қазақша петиция мәтінін жаз. 
Петиция мынадай болуы керек:
- Тақырып (1 жол)
- Негізгі мәтін (3-4 абзац): мәселенің сипаттамасы, себептері, салдары, талаптар
- Соңғы сөйлем: ресми өтініш

Тек петиция мәтінін жаз, басқа түсіндірме берме.";

                var body = new
                {
                    model = "mistralai/mistral-7b-instruct:free",
                    messages = new[]
                    {
                new { role = "user", content = prompt }
            }
                };

                var json = System.Text.Json.JsonSerializer.Serialize(body);
                var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");

                try
                {
                    var response = await client.PostAsync("https://openrouter.ai/api/v1/chat/completions", content);
                    var responseStr = await response.Content.ReadAsStringAsync();
                    var doc = System.Text.Json.JsonDocument.Parse(responseStr);
                    var result = doc.RootElement
                        .GetProperty("choices")[0]
                        .GetProperty("message")
                        .GetProperty("content")
                        .GetString();
                    ViewBag.Result = result;
                }
                catch
                {
                    ViewBag.Error = "ИИ жауап бере алмады. Қайта көріңіз.";
                }
            }

            return View();
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