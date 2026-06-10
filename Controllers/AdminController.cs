using CVAnalyzer.Data;
using CVAnalyzer.Models;
using CVAnalyzer.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Pgvector.EntityFrameworkCore;

namespace CVAnalyzer.Controllers
{
    [Authorize(Roles = "Admin")]
    public class AdminController : Controller
    {
        private readonly AppDbContext _db;
        private readonly IGeminiParsingService _geminiService;

        public AdminController(AppDbContext db, IGeminiParsingService geminiService)
        {
            _db = db;
            _geminiService = geminiService;
        }

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            // Kullanıcıları (sadece "User" rolündekileri) profil ve CV'leri ile çek
            var users = await _db.Users
                .Include(u => u.UserProfile)
                .Include(u => u.CandidateCVs)
                .Where(u => u.Role == "User")
                .ToListAsync();

            var totalCvs = await _db.CandidateCVs.CountAsync();

            var model = new AdminDashboardViewModel
            {
                TotalCandidates = users.Count,
                TotalCVs = totalCvs,
                Candidates = users.Select(u => new CandidateDto
                {
                    UserId = u.Id,
                    FullName = u.FullName,
                    Email = u.Email,
                    Headline = u.UserProfile?.Headline,
                    Location = u.UserProfile?.Location,
                    Skills = u.UserProfile?.Skills ?? new List<string>(),
                    LastCVSync = u.CandidateCVs.OrderByDescending(c => c.CreatedAt).FirstOrDefault()?.CreatedAt
                }).OrderByDescending(c => c.LastCVSync ?? DateTime.MinValue).ToList()
            };

            return View(model);
        }

        [HttpGet]
        public async Task<IActionResult> CandidateProfile(int id)
        {
            var user = await _db.Users
                .Include(u => u.UserProfile)
                .Include(u => u.CandidateCVs)
                .FirstOrDefaultAsync(u => u.Id == id && u.Role == "User");

            if (user == null)
            {
                return NotFound("Aday bulunamadı veya erişim yetkiniz yok.");
            }

            return View(user);
        }

        [HttpPost]
        public async Task<IActionResult> SmartSearch(string query)
        {
            if (string.IsNullOrWhiteSpace(query))
                return BadRequest("Lütfen bir arama kriteri girin.");

            try
            {
                // 1. NLP Parsing
                var filters = await _geminiService.ExtractSearchFiltersAsync(query);
                
                // 2. Query Vectors (General + Skills)
                var queryEmbeddingArray = await _geminiService.GenerateEmbeddingAsync(query);
                var queryVector = new Pgvector.Vector(queryEmbeddingArray);
                
                Pgvector.Vector? skillsQueryVector = null;
                if (filters != null && filters.Skills != null && filters.Skills.Any())
                {
                    var skillsText = string.Join(", ", filters.Skills);
                    var skillsArray = await _geminiService.GenerateEmbeddingAsync(skillsText);
                    skillsQueryVector = new Pgvector.Vector(skillsArray);
                }

                // 3. Veritabanı Araması (Eğer skillsQueryVector varsa ayrı sorgu, yoksa ayrı sorgu)
                var candidatesList = new List<dynamic>();

                if (skillsQueryVector != null)
                {
                    // PostgreSQL'de <=> (uzaklık) operatörü ile + operatörünün öncelik çakışmasını
                    // (operator does not exist: double precision <=> vector) önlemek için toplamayı C#'ta yapıyoruz.
                    var queryList = await _db.CandidateCVs
                        .Where(c => c.Embedding != null && c.SkillsEmbedding != null)
                        .Select(c => new 
                        {
                            c.Id,
                            MainDist = c.Embedding!.CosineDistance(queryVector),
                            SkillDist = c.SkillsEmbedding!.CosineDistance(skillsQueryVector)
                        })
                        .OrderBy(c => c.MainDist)
                        .Take(50)
                        .ToListAsync();

                    candidatesList = queryList
                        .Select(c => new { Id = c.Id, Distance = (c.MainDist + c.SkillDist) / 2.0 })
                        .OrderBy(c => c.Distance)
                        .ToList<dynamic>();
                }
                else
                {
                    var queryList = await _db.CandidateCVs
                        .Where(c => c.Embedding != null)
                        .Select(c => new 
                        {
                            c.Id,
                            MainDist = c.Embedding!.CosineDistance(queryVector)
                        })
                        .OrderBy(c => c.MainDist)
                        .Take(50)
                        .ToListAsync();

                    candidatesList = queryList.Select(c => new { Id = c.Id, Distance = c.MainDist }).ToList<dynamic>();
                }

                if (!candidatesList.Any())
                    return PartialView("_SmartSearchResults", new List<SmartSearchResultViewModel>());

                var topCandidateIds = candidatesList.Select(x => (int)x.Id).ToList();

                var topCandidatesUnordered = await _db.CandidateCVs
                    .Include(c => c.User)
                    .Include(c => c.User.UserProfile)
                    .Where(c => topCandidateIds.Contains(c.Id))
                    .ToListAsync();

                // 4. Yetenek Bazlı Deneyim ve Hard Filtreleme
                
                // Kelime sınırı (Word Boundary) sorunlarını ve C#, C++ gibi özel karakterleri
                // güvenli şekilde eşleştirmek için kelime normalizasyon fonksiyonu:
                string NormalizeForMatch(string input) 
                {
                    if (string.IsNullOrWhiteSpace(input)) return " ";
                    // Noktalama işaretlerini (., - vb) boşluğa çevir, ama +, #, ve harf/rakamlar kalsın.
                    var cleaned = System.Text.RegularExpressions.Regex.Replace(input.ToLowerInvariant(), @"[^\w\+#]", " ");
                    // Fazla boşlukları teke indir
                    cleaned = System.Text.RegularExpressions.Regex.Replace(cleaned, @"\s+", " ");
                    return $" {cleaned.Trim()} ";
                }

                var finalResults = candidatesList.Select(item => {
                    var c = topCandidatesUnordered.First(cand => cand.Id == item.Id);
                    
                    bool isLocationMatched = false;
                    bool isExperienceMatched = false;
                    List<string> matchedSkills = new();

                    if (filters != null && c.ParsedData != null)
                    {
                        // Lokasyon
                        if (!string.IsNullOrWhiteSpace(filters.Location))
                        {
                            var candLocation = c.ParsedData.Personal?.Location ?? "";
                            var cleanCandLoc = candLocation.Replace("İ", "I").Replace("ı", "i").ToLowerInvariant();
                            var cleanFilterLoc = filters.Location.Replace("İ", "I").Replace("ı", "i").ToLowerInvariant();
                            
                            if (cleanCandLoc.Contains(cleanFilterLoc)) isLocationMatched = true;
                        }

                        // Yetenek Eşleşmesi (Gelişmiş Normalizasyonlu - V4)
                        if (filters.Skills != null && filters.Skills.Any())
                        {
                            foreach (var requiredSkill in filters.Skills)
                            {
                                var reqNorm = NormalizeForMatch(requiredSkill);
                                
                                var candSkill = c.ParsedData.Skills?.FirstOrDefault(s => {
                                    var skillNorm = NormalizeForMatch(s.Name);
                                    // Adayın yeteneği arananı tam bir kelime olarak kapsıyorsa veya aranan, adayınkini kapsıyorsa
                                    return skillNorm.Contains(reqNorm) || reqNorm.Contains(skillNorm);
                                });
                                
                                if (candSkill != null)
                                {
                                    matchedSkills.Add(requiredSkill);
                                    
                                    if (filters.MinExperienceYears > 0 && candSkill.YearsOfExperience >= filters.MinExperienceYears)
                                    {
                                        isExperienceMatched = true;
                                    }
                                }
                                else 
                                {
                                    var embedNorm = NormalizeForMatch(c.ParsedData.EmbeddingText ?? "");
                                    var skillsEmbedNorm = NormalizeForMatch(c.ParsedData.SkillsEmbeddingText ?? "");
                                    
                                    if (embedNorm.Contains(reqNorm) || skillsEmbedNorm.Contains(reqNorm))
                                    {
                                        matchedSkills.Add(requiredSkill);
                                    }
                                }
                            }
                        }
                        
                        // Eğer aranan özel bir yetenek yoksa ama sadece "3 yıllık deneyim" dendiyse fallback (eski sistem)
                        if (filters.MinExperienceYears > 0 && !isExperienceMatched && (filters.Skills == null || !filters.Skills.Any()))
                        {
                            int totalYears = 0;
                            foreach (var exp in c.ParsedData.Experience ?? new List<ATSExperience>())
                            {
                                var startMatch = System.Text.RegularExpressions.Regex.Match(exp.StartDate ?? "", @"\d{4}");
                                var endMatch = System.Text.RegularExpressions.Regex.Match(exp.EndDate ?? "", @"\d{4}");
                                int startYear = startMatch.Success ? int.Parse(startMatch.Value) : DateTime.Now.Year;
                                int endYear = endMatch.Success ? int.Parse(endMatch.Value) : DateTime.Now.Year;
                                if (endYear >= startYear) totalYears += (endYear - startYear);
                            }
                            if (totalYears >= filters.MinExperienceYears) isExperienceMatched = true;
                        }
                    }

                    // -- Puanlama (Gerçekçi Dağılım) --
                    // Skor enflasyonunu (100% barajına çok hızlı ulaşılmasını) engellemek için
                    // Vektör skoruna maksimum 60, Hard Filtrelere maksimum 40 puan veriyoruz.
                    double maxAcceptedDistance = 0.55;
                    double vectorScore = 0;
                    
                    if (filters != null && (filters.MinExperienceYears > 0 || !string.IsNullOrWhiteSpace(filters.Location) || (filters.Skills != null && filters.Skills.Any())))
                    {
                        // Filtreli Arama
                        if (item.Distance <= maxAcceptedDistance)
                            vectorScore = (1 - (item.Distance / maxAcceptedDistance)) * 60; // Max 60 puan vektörden

                        double filterScore = 0;
                        double maxFilterScore = 40; 
                        double currentMax = 0;
                        double earned = 0;
                        
                        if (!string.IsNullOrWhiteSpace(filters.Location)) currentMax += 10;
                        if (filters.MinExperienceYears > 0) currentMax += 15;
                        if (filters.Skills != null && filters.Skills.Any()) currentMax += 15;

                        if (!string.IsNullOrWhiteSpace(filters.Location) && isLocationMatched) earned += 10;
                        if (filters.MinExperienceYears > 0 && isExperienceMatched) earned += 15;
                        
                        if (filters.Skills != null && filters.Skills.Any() && matchedSkills.Any())
                        {
                            double ratio = (double)matchedSkills.Count / filters.Skills.Count;
                            earned += (15 * ratio);
                        }
                        
                        filterScore = (earned / currentMax) * maxFilterScore;
                        vectorScore += filterScore;
                    }
                    else
                    {
                        // Filtresiz salt vektör araması
                        if (item.Distance <= maxAcceptedDistance)
                            vectorScore = (1 - (item.Distance / maxAcceptedDistance)) * 100;
                    }

                    int finalScore = (int)Math.Max(0, Math.Min(100, Math.Round(vectorScore)));

                    // Açıklama metni oluştur
                    var reasons = new List<string>();
                    if (matchedSkills.Any()) reasons.Add($"Yetenek: {string.Join(", ", matchedSkills)}");
                    if (filters != null && filters.MinExperienceYears > 0 && isExperienceMatched) reasons.Add($"{filters.MinExperienceYears}+ Yıl Özel Deneyim");
                    if (filters != null && !string.IsNullOrWhiteSpace(filters.Location) && isLocationMatched) reasons.Add($"Lokasyon: {filters.Location}");
                    
                    string finalReason = reasons.Any() ? $"Nokta Atışı: {string.Join(" | ", reasons)}" : (finalScore > 0 ? $"Anlamsal Uyum (Uzaklık: {item.Distance:F2})" : "Kriterleri karşılamıyor.");

                    return new SmartSearchResultViewModel
                    {
                        Candidate = c,
                        MatchScore = finalScore,
                        Reason = finalReason,
                        VectorDistance = item.Distance,
                        MatchedSkills = matchedSkills,
                        IsLocationMatched = isLocationMatched,
                        IsExperienceMatched = isExperienceMatched
                    };
                })
                .Where(r => r.MatchScore > 0)
                .OrderByDescending(r => r.MatchScore)
                .Take(10)
                .ToList();

                return PartialView("_SmartSearchResults", finalResults);
            }
            catch (Exception ex)
            {
                return BadRequest("Arama sırasında bir hata oluştu: " + ex.Message);
            }
        }
    }
}
