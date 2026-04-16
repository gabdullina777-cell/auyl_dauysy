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

        public IActionResult Map() => View();

        public IActionResult AiHelper() => View();

        [HttpPost]
        public async Task<IActionResult> AiHelper(string problemText, string region, string category)
        {
            ViewBag.Problem = problemText;
            ViewBag.Region = region;
            ViewBag.Category = category;

            if (!User.Identity!.IsAuthenticated)
            {
                ViewBag.Error = "Пайдалану үшін тіркелу қажет.";
                return View();
            }

            if (string.IsNullOrWhiteSpace(problemText))
                return View();

            var apiKey = Environment.GetEnvironmentVariable("OPENROUTER_API_KEY");

            if (string.IsNullOrEmpty(apiKey))
            {
                ViewBag.Error = "API key табылмады.";
                return View();
            }

            var client = new HttpClient();
            client.DefaultRequestHeaders.Add("Authorization", $"Bearer {apiKey}");
            client.DefaultRequestHeaders.Add("HTTP-Referer", "https://auyldauysy-production.up.railway.app");
            client.DefaultRequestHeaders.Add("X-Title", "Auyl Dausy");

            var prompt = $@"Сен Қазақстан ауылдарының мәселелерін ресми петицияға айналдыратын көмекшісің.

Мәселе: {problemText}
Облыс: {region}
Санат: {category}

Осы мәселе негізінде толық, грамотты, ресми қазақша петиция мәтінін жаз.
Петиция мынадай құрылымда болуы керек:

1. Тақырып: (қысқа, нақты)
2. Мәселенің сипаттамасы: (2-3 сөйлем)
3. Себептері мен салдары: (2-3 сөйлем)
4. Талаптар: (нақты не істелуін сұрайды)
5. Қорытынды өтініш: (1 сөйлем)

Тек петиция мәтінін жаз, басқа түсіндірме берме. Қазақша жаз.";

            var body = new
            {
                model = "meta-llama/llama-3.1-8b-instruct:free",
                messages = new[]
                {
                    new { role = "system", content = "Сен қазақ тілінде ресми петиция жазатын көмекшісің." },
                    new { role = "user", content = prompt }
                },
                max_tokens = 1000,
                temperature = 0.7
            };

            var json = System.Text.Json.JsonSerializer.Serialize(body);
            var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");

            try
            {
                var response = await client.PostAsync(
                    "https://openrouter.ai/api/v1/chat/completions", content);
                var responseStr = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                {
                    ViewBag.Error = "ИИ жауап бере алмады. Кейінірек қайта көріңіз.";
                    return View();
                }

                var doc = System.Text.Json.JsonDocument.Parse(responseStr);
                var result = doc.RootElement
                    .GetProperty("choices")[0]
                    .GetProperty("message")
                    .GetProperty("content")
                    .GetString();

                ViewBag.Result = result;
            }
            catch (Exception ex)
            {
                ViewBag.Error = $"Қате шықты: {ex.Message}";
            }

            return View();
        }
    }
}