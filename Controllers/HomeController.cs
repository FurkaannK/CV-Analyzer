using CVAnalyzer.Data;
using CVAnalyzer.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CVAnalyzer.Controllers
{
    [Authorize]
    public class HomeController : Controller
    {
        private readonly AppDbContext _db;

        public HomeController(AppDbContext db)
        {
            _db = db;
        }

        // GET: /
        public async Task<IActionResult> Index()
        {

            ViewBag.TotalAnalyses  = await _db.CandidateCVs.CountAsync();
            return View();
        }


        // GET: /Privacy
        public IActionResult Privacy() => View();
    }
}
