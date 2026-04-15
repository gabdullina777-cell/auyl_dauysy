using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ayul_dayusy.Models;

namespace ayul_dayusy.Controllers
{
    [Authorize(Roles = "Admin")]
    public class AdminController : Controller
    {
        private readonly AppDbContext _db;
        private readonly UserManager<AppUser> _userManager;

        public AdminController(AppDbContext db, UserManager<AppUser> userManager)
        {
            _db = db;
            _userManager = userManager;
        }

        public async Task<IActionResult> Index()
        {
            ViewBag.UserCount = await _db.Users.CountAsync();
            ViewBag.PetitionCount = await _db.Petitions.CountAsync();
            ViewBag.SolvedCount = await _db.Petitions.CountAsync(p => p.IsSolved);
            ViewBag.PendingCount = await _db.Petitions.CountAsync(p => !p.IsSolved);
            return View();
        }

        public async Task<IActionResult> Petitions()
        {
            var petitions = await _db.Petitions
                .OrderByDescending(p => p.CreatedAt)
                .ToListAsync();
            return View(petitions);
        }
        [HttpPost]
        public async Task<IActionResult> AddComment(int id, string comment)
        {
            var petition = await _db.Petitions.FindAsync(id);
            if (petition != null)
            {
                petition.AdminComment = comment;
                await _db.SaveChangesAsync();
            }
            return RedirectToAction("Petitions");
        }
        public async Task<IActionResult> Users()
        {
            var users = await _db.Users
                .OrderByDescending(u => u.CreatedAt)
                .ToListAsync();
            return View(users);
        }

        [HttpPost]
        public async Task<IActionResult> SolvePetition(int id)
        {
            var petition = await _db.Petitions.FindAsync(id);
            if (petition != null)
            {
                petition.IsSolved = !petition.IsSolved;
                await _db.SaveChangesAsync();
            }
            return RedirectToAction("Petitions");
        }

        [HttpPost]
        public async Task<IActionResult> DeletePetition(int id)
        {
            var petition = await _db.Petitions.FindAsync(id);
            if (petition != null)
            {
                _db.Petitions.Remove(petition);
                await _db.SaveChangesAsync();
            }
            return RedirectToAction("Petitions");
        }

        [HttpPost]
        public async Task<IActionResult> DeleteUser(string id)
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user != null)
                await _userManager.DeleteAsync(user);
            return RedirectToAction("Users");
        }
    }
}