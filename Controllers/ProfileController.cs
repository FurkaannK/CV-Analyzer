using CVAnalyzer.Data;
using CVAnalyzer.Models;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using BCrypt.Net;

namespace CVAnalyzer.Controllers
{
    [Authorize]
    public class ProfileController : Controller
    {
        private readonly AppDbContext _db;

        public ProfileController(AppDbContext db)
        {
            _db = db;
        }

        // GET: /Profile
        public async Task<IActionResult> Index()
        {
            var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!int.TryParse(userIdStr, out var userId))
                return RedirectToAction("Login", "Auth");

            var user = await _db.Users
                .Include(u => u.UserProfile)
                .Include(u => u.CandidateCVs)
                .FirstOrDefaultAsync(u => u.Id == userId);

            if (user == null)
                return NotFound();

            var model = new ProfileViewModel
            {
                FullName = user.FullName,
                Email = user.Email,
                BirthDate = user.UserProfile?.BirthDate,
                BirthPlace = user.UserProfile?.BirthPlace,
                Gender = user.UserProfile?.Gender,
                MaritalStatus = user.UserProfile?.MaritalStatus,
                Biography = user.UserProfile?.Biography,
                Headline = user.UserProfile?.Headline,
                Phone = user.UserProfile?.Phone,
                Location = user.UserProfile?.Location,
                LinkedinUrl = user.UserProfile?.LinkedinUrl,
                Skills = user.UserProfile?.Skills ?? new List<string>(),
                SkillsText = user.UserProfile?.Skills != null ? string.Join(", ", user.UserProfile.Skills) : "",
                CandidateCVs = user.CandidateCVs.OrderByDescending(c => c.CreatedAt).ToList()
            };

            return View(model);
        }

        // POST: /Profile/UpdateProfile
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateProfile(ProfileViewModel model)
        {
            var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!int.TryParse(userIdStr, out var userId))
                return RedirectToAction("Login", "Auth");

            var user = await _db.Users
                .Include(u => u.UserProfile)
                .FirstOrDefaultAsync(u => u.Id == userId);

            if (user == null)
                return NotFound();

            // Sadece form doğrulama kısmındaki isim hatasını kontrol ediyoruz.
            // Şifre kısımları boş gelebilir çünkü iki farklı işlem yapacağız.
            if (string.IsNullOrWhiteSpace(model.FullName))
            {
                ModelState.AddModelError("FullName", "Ad Soyad boş bırakılamaz.");
                model.Email = user.Email;
                return View("Index", model);
            }

            user.FullName = model.FullName;
            
            if (user.UserProfile == null)
            {
                user.UserProfile = new UserProfile { UserId = user.Id };
                _db.UserProfiles.Add(user.UserProfile);
            }

            // Unutmamak gerekir, Postgresql UTC istiyor datetime icin, kullanicidan gelen date e ToUniversalTime yapmaliyiz 
            if (model.BirthDate.HasValue && model.BirthDate.Value.Kind == DateTimeKind.Unspecified)
            {
                user.UserProfile.BirthDate = DateTime.SpecifyKind(model.BirthDate.Value, DateTimeKind.Utc);
            }
            else
            {
                user.UserProfile.BirthDate = model.BirthDate?.ToUniversalTime();
            }
            
            user.UserProfile.BirthPlace = model.BirthPlace;
            user.UserProfile.Gender = model.Gender;
            user.UserProfile.MaritalStatus = model.MaritalStatus;
            user.UserProfile.Biography = model.Biography;
            user.UserProfile.Headline = model.Headline;
            user.UserProfile.Phone = model.Phone;
            user.UserProfile.Location = model.Location;
            user.UserProfile.LinkedinUrl = model.LinkedinUrl;
            
            if (!string.IsNullOrWhiteSpace(model.SkillsText))
            {
                user.UserProfile.Skills = model.SkillsText
                    .Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries)
                    .Select(s => s.Trim())
                    .Where(s => !string.IsNullOrEmpty(s))
                    .Distinct()
                    .ToList();
            }
            else
            {
                user.UserProfile.Skills = new List<string>();
            }

            user.UserProfile.UpdatedAt = DateTime.UtcNow;

            _db.Users.Update(user);
            await _db.SaveChangesAsync();

            // Oturumu yeniliyoruz (Navbar'daki isim anında değişsin diye)
            await RefreshSignInAsync(user);

            TempData["SuccessMessage"] = "Profil bilgileriniz başarıyla güncellendi.";
            return RedirectToAction("Index");
        }

        // POST: /Profile/UpdatePassword
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdatePassword(ProfileViewModel model)
        {
            var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!int.TryParse(userIdStr, out var userId))
                return RedirectToAction("Login", "Auth");

            var user = await _db.Users.FindAsync(userId);
            if (user == null)
                return NotFound();

            model.FullName = user.FullName;
            model.Email = user.Email;

            if (string.IsNullOrWhiteSpace(model.CurrentPassword) || string.IsNullOrWhiteSpace(model.NewPassword))
            {
                ModelState.AddModelError("", "Şifre alanlarını boş bırakamazsınız.");
                return View("Index", model);
            }

            if (model.NewPassword != model.ConfirmNewPassword)
            {
                ModelState.AddModelError("ConfirmNewPassword", "Yeni şifreler eşleşmiyor.");
                return View("Index", model);
            }

            if (!BCrypt.Net.BCrypt.Verify(model.CurrentPassword, user.PasswordHash))
            {
                ModelState.AddModelError("CurrentPassword", "Mevcut şifreniz yanlış.");
                return View("Index", model);
            }

            user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(model.NewPassword);
            _db.Users.Update(user);
            await _db.SaveChangesAsync();

            TempData["SuccessMessage"] = "Şifreniz başarıyla değiştirildi.";
            return RedirectToAction("Index");
        }

        // POST: /Profile/ImportFromCV
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ImportFromCV(int cvId)
        {
            var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!int.TryParse(userIdStr, out var userId))
                return RedirectToAction("Login", "Auth");

            var cv = await _db.CandidateCVs.FirstOrDefaultAsync(c => c.Id == cvId && c.UserId == userId);
            if (cv == null) return NotFound();

            var user = await _db.Users.Include(u => u.UserProfile).FirstOrDefaultAsync(u => u.Id == userId);
            if (user == null) return NotFound();

            if (user.UserProfile == null)
            {
                user.UserProfile = new UserProfile { UserId = user.Id };
                _db.UserProfiles.Add(user.UserProfile);
            }

            var parsed = cv.ParsedData;

            // İsim boşsa güncelle
            if (string.IsNullOrWhiteSpace(user.FullName) && !string.IsNullOrWhiteSpace(parsed.Personal.FullName))
            {
                user.FullName = parsed.Personal.FullName;
            }

            // Biyografi (Summary)
            if (string.IsNullOrWhiteSpace(user.UserProfile.Biography) && !string.IsNullOrWhiteSpace(parsed.Summary))
            {
                user.UserProfile.Biography = parsed.Summary;
            }

            // Headline
            if (string.IsNullOrWhiteSpace(user.UserProfile.Headline) && !string.IsNullOrWhiteSpace(parsed.Headline))
            {
                user.UserProfile.Headline = parsed.Headline;
            }

            // Telefon
            if (string.IsNullOrWhiteSpace(user.UserProfile.Phone) && !string.IsNullOrWhiteSpace(parsed.Personal.Phone))
            {
                user.UserProfile.Phone = parsed.Personal.Phone;
            }

            // Lokasyon
            if (string.IsNullOrWhiteSpace(user.UserProfile.Location) && !string.IsNullOrWhiteSpace(parsed.Personal.Location))
            {
                user.UserProfile.Location = parsed.Personal.Location;
            }

            // LinkedIn
            if (string.IsNullOrWhiteSpace(user.UserProfile.LinkedinUrl) && !string.IsNullOrWhiteSpace(parsed.Personal.LinkedinUrl))
            {
                user.UserProfile.LinkedinUrl = parsed.Personal.LinkedinUrl;
            }

            // Yetenekler
            if (parsed.NormalizedSkills != null && parsed.NormalizedSkills.Any())
            {
                var currentSkills = user.UserProfile.Skills?.ToList() ?? new List<string>();
                var currentSkillsLower = currentSkills.Select(s => s.ToLowerInvariant()).ToHashSet();
                
                foreach (var skill in parsed.NormalizedSkills)
                {
                    if (!string.IsNullOrWhiteSpace(skill) && !currentSkillsLower.Contains(skill.ToLowerInvariant()))
                    {
                        currentSkills.Add(skill);
                        currentSkillsLower.Add(skill.ToLowerInvariant());
                    }
                }
                
                // EF Core'un array değişikliğini algılayabilmesi için listeyi yeniden atıyoruz
                user.UserProfile.Skills = currentSkills;
            }

            user.UserProfile.UpdatedAt = DateTime.UtcNow;

            _db.Users.Update(user);
            await _db.SaveChangesAsync();

            // Session yenile
            await RefreshSignInAsync(user);

            TempData["SuccessMessage"] = "Profiliniz CV'nizdeki verilerle başarıyla güncellendi! Sadece boş alanlar dolduruldu.";
            return RedirectToAction("Index");
        }

        // POST: /Profile/DeleteCV
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteCV(int id)
        {
            var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!int.TryParse(userIdStr, out var userId))
                return RedirectToAction("Login", "Auth");

            var cv = await _db.CandidateCVs.FirstOrDefaultAsync(c => c.Id == id && c.UserId == userId);
            if (cv != null)
            {
                _db.CandidateCVs.Remove(cv);
                await _db.SaveChangesAsync();
                TempData["SuccessMessage"] = "Özgeçmişiniz başarıyla silindi.";
            }

            return RedirectToAction("Index");
        }
        
        private async Task RefreshSignInAsync(User user)
        {
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim(ClaimTypes.Name, user.FullName),
                new Claim(ClaimTypes.Email, user.Email),
                new Claim(ClaimTypes.Role, user.Role)
            };

            var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            var principal = new ClaimsPrincipal(identity);

            // Mevcut session üzerine yeni bilgileri yazıyoruz
            await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal);
        }
    }
}
