using CVAnalyzer.Data;
using CVAnalyzer.Models;
using CVAnalyzer.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using UglyToad.PdfPig;
using UglyToad.PdfPig.DocumentLayoutAnalysis.TextExtractor;

namespace CVAnalyzer.Controllers
{
    [Authorize]
    public class CVController : Controller
    {
        private readonly AppDbContext _db;
        private readonly IGeminiParsingService _geminiService;
        private readonly IWebHostEnvironment _env;

        public CVController(AppDbContext db, IGeminiParsingService geminiService, IWebHostEnvironment env)
        {
            _db = db;
            _geminiService = geminiService;
            _env = env;
        }

        // GET: /CV/Analyze
        // GET: /CV/Analyze
        public IActionResult Analyze()
        {
            return View();
        }

        // POST: /CV/Analyze
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Analyze(IFormFile cvFile)
        {
            if (cvFile == null || cvFile.Length == 0)
            {
                ModelState.AddModelError("", "Lütfen geçerli bir PDF dosyası yükleyin.");
                return View();
            }

            if (Path.GetExtension(cvFile.FileName).ToLower() != ".pdf")
            {
                ModelState.AddModelError("", "Sadece PDF dosyaları desteklenmektedir.");
                return View();
            }

            string extractedText = "";
            try
            {
                using (var stream = cvFile.OpenReadStream())
                using (var pdf = PdfDocument.Open(stream))
                {
                    foreach (var page in pdf.GetPages())
                    {
                        extractedText += ContentOrderTextExtractor.GetText(page) + "\n";
                    }
                }
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", "PDF okunamadı: " + ex.Message);
                return View();
            }

            if (string.IsNullOrWhiteSpace(extractedText))
            {
                ModelState.AddModelError("", "PDF içerisinden metin çıkarılamadı (belki resim formatında olabilir).");
                return View();
            }

            try
            {
                // Call Gemini to parse the CV
                var parsedData = await _geminiService.ParseCVAsync(extractedText);

                if (parsedData == null)
                {
                    ModelState.AddModelError("", "Yapay zeka CV'yi ayrıştıramadı.");
                    return View();
                }

                var candidateCv = new CandidateCV
                {
                    RawText = extractedText,
                    ParsedData = parsedData
                };

                // Generate Embeddings
                try
                {
                    if (!string.IsNullOrWhiteSpace(parsedData.EmbeddingText))
                    {
                        var embeddingArray = await _geminiService.GenerateEmbeddingAsync(parsedData.EmbeddingText);
                        candidateCv.Embedding = new Pgvector.Vector(embeddingArray);
                    }

                    if (!string.IsNullOrWhiteSpace(parsedData.SkillsEmbeddingText))
                    {
                        var skillsEmbeddingArray = await _geminiService.GenerateEmbeddingAsync(parsedData.SkillsEmbeddingText);
                        candidateCv.SkillsEmbedding = new Pgvector.Vector(skillsEmbeddingArray);
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Embedding generation failed: " + ex.Message);
                }

                // Orijinal PDF dosyasını sunucuya kaydet
                try
                {
                    var uploadsFolder = Path.Combine(_env.WebRootPath, "uploads", "cvs");
                    if (!Directory.Exists(uploadsFolder))
                    {
                        Directory.CreateDirectory(uploadsFolder);
                    }
                    var uniqueFileName = Guid.NewGuid().ToString() + "_" + Path.GetFileName(cvFile.FileName);
                    var filePath = Path.Combine(uploadsFolder, uniqueFileName);
                    using (var fileStream = new FileStream(filePath, FileMode.Create))
                    {
                        await cvFile.CopyToAsync(fileStream);
                    }
                    candidateCv.PdfFilePath = "/uploads/cvs/" + uniqueFileName;
                }
                catch (Exception ex)
                {
                    // Dosya kaydedilemezse bile (izin hatası vs) işleme devam edebiliriz.
                    Console.WriteLine("PDF kaydedilirken hata: " + ex.Message);
                }

                if (User.Identity!.IsAuthenticated)
                {
                    var userIdStr = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
                    if (int.TryParse(userIdStr, out var userId))
                    {
                        candidateCv.UserId = userId;
                    }
                }

                _db.CandidateCVs.Add(candidateCv);
                await _db.SaveChangesAsync();

                return RedirectToAction(nameof(Result), new { id = candidateCv.Id });
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", "Yapay Zeka Analizi sırasında hata oluştu: " + ex.Message);
                return View();
            }
        }

        // GET: /CV/Result/{id}  — kaydedilmiş sonucu görüntüle
        public async Task<IActionResult> Result(int id)
        {
            var cv = await _db.CandidateCVs.FindAsync(id);
            if (cv is null) return NotFound();

            return View(cv);
        }
    }
}
