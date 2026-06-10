using CVAnalyzer.Models;
using Newtonsoft.Json;

namespace CVAnalyzer.Services
{
    public interface ICVAnalysisService
    {
        CVResultViewModel Analyze(string cvText, JobRole role);
        CVResultViewModel AnalyzeWithKeywords(string cvText, JobRole role);
    }

    public class CVAnalysisService : ICVAnalysisService
    {
        // ─── Temel keyword bazlı analiz (AI olmadan çalışır) ────────────
        public CVResultViewModel AnalyzeWithKeywords(string cvText, JobRole role)
        {
            var cv = cvText.ToLower();
            var requiredSkills = role.RequiredSkills
                .Split(',', StringSplitOptions.RemoveEmptyEntries)
                .Select(s => s.Trim())
                .ToList();

            // Skill eşleşmesi
            var found = requiredSkills.Where(s => cv.Contains(s.ToLower())).ToList();
            var missing = requiredSkills.Where(s => !cv.Contains(s.ToLower())).Take(6).ToList();

            // CV bölüm kontrolü
            var sections = new Dictionary<string, bool>
            {
                ["contact"]    = cv.Contains("@") || cv.Contains("phone") || cv.Contains("tel") || cv.Contains("email"),
                ["education"]  = cv.Contains("university") || cv.Contains("üniversite") || cv.Contains("degree") || cv.Contains("bachelor") || cv.Contains("master") || cv.Contains("lisans"),
                ["experience"] = cv.Contains("experience") || cv.Contains("deneyim") || cv.Contains("worked") || cv.Contains("çalıştım") || cv.Contains("position"),
                ["skills"]     = cv.Contains("skill") || cv.Contains("beceri") || cv.Contains("technology") || found.Count > 0,
                ["summary"]    = cv.Contains("summary") || cv.Contains("özet") || cv.Contains("objective") || cv.Contains("about")
            };

            // Puan hesaplama
            int skillScore   = (int)((double)found.Count / Math.Max(requiredSkills.Count, 1) * 40);
            int sectionScore = (int)((double)sections.Values.Count(v => v) / sections.Count * 25);
            int lengthScore  = Math.Min(cv.Length / 50, 20); // max 20
            int contactScore = sections["contact"] ? 15 : 0;

            int total = Math.Min(skillScore + sectionScore + lengthScore + contactScore, 100);

            // Match kriterleri
            var matchCriteria = new List<MatchCriterion>
            {
                new() { Label = "Teknik Beceriler",  Score = Math.Min((int)((double)found.Count / Math.Max(requiredSkills.Count, 1) * 100), 100) },
                new() { Label = "Deneyim",           Score = sections["experience"] ? 70 : 20 },
                new() { Label = "Eğitim",            Score = sections["education"]  ? 80 : 10 },
                new() { Label = "Profil Bütünlüğü",  Score = (int)((double)sections.Values.Count(v => v) / sections.Count * 100) }
            };

            // Skill seviyeleri (basit tahmin)
            var skillLevels = new Dictionary<string, int>();
            foreach (var skill in found.Take(5))
            {
                int count = CountOccurrences(cv, skill.ToLower());
                skillLevels[skill] = Math.Min(30 + count * 15, 95);
            }

            // Öneriler
            var suggestions = GenerateSuggestions(sections, missing, found, total);

            string badge = total >= 80 ? "Mükemmel" : total >= 60 ? "İyi" : total >= 40 ? "Geliştirilmeli" : "Zayıf";

            return new CVResultViewModel
            {
                OverallScore    = total,
                JobMatchScore   = matchCriteria.Average(c => c.Score) is double avg ? (int)avg : total,
                ScoreBadge      = badge,
                TargetRole      = role.RoleName,
                FoundSkills     = found,
                MissingSkills   = missing,
                Suggestions     = suggestions,
                Sections        = sections,
                MatchCriteria   = matchCriteria,
                SkillLevels     = skillLevels
            };
        }

        public CVResultViewModel Analyze(string cvText, JobRole role)
        {
            // Varsayılan olarak keyword analizi kullanır.
            // OpenAI veya başka bir AI entegrasyonu için bu metodu genişletin.
            return AnalyzeWithKeywords(cvText, role);
        }

        // ─── Yardımcı metodlar ──────────────────────────────────────────
        private static int CountOccurrences(string text, string keyword)
        {
            int count = 0, idx = 0;
            while ((idx = text.IndexOf(keyword, idx, StringComparison.OrdinalIgnoreCase)) >= 0)
            {
                count++;
                idx += keyword.Length;
            }
            return count;
        }

        private static List<SuggestionItem> GenerateSuggestions(
            Dictionary<string, bool> sections,
            List<string> missing,
            List<string> found,
            int score)
        {
            var list = new List<SuggestionItem>();

            if (!sections["contact"])
                list.Add(new() { Type = "warn", Title = "İletişim Bilgileri Eksik", Text = "E-posta, telefon ve LinkedIn URL adresinizi CV'nize ekleyin." });

            if (!sections["summary"])
                list.Add(new() { Type = "tip", Title = "Profesyonel Özet Ekleyin", Text = "CV başına 2-3 cümlelik güçlü bir profil özeti işverenlerin dikkatini çeker." });

            if (!sections["experience"])
                list.Add(new() { Type = "warn", Title = "İş Deneyimi Bölümü Bulunamadı", Text = "Varsa iş deneyimlerinizi, yoksa staj veya proje deneyimlerinizi ekleyin." });

            if (missing.Count > 0)
                list.Add(new() { Type = "tip", Title = "Eksik Beceriler", Text = $"Bu pozisyon için önemli beceriler ekleyebilirsiniz: {string.Join(", ", missing.Take(4))}." });

            if (found.Count >= 5)
                list.Add(new() { Type = "ok", Title = "Güçlü Teknik Profil", Text = $"CV'nizde {found.Count} teknik beceri tespit edildi. Bunları deneyimlerinizle destekleyin." });

            if (score < 50)
                list.Add(new() { Type = "warn", Title = "CV'niz Güçlendirilmeli", Text = "Sayısal başarılar (örn. %20 verimlilik artışı) ve ölçülebilir sonuçlar eklemek puanınızı artırır." });
            else
                list.Add(new() { Type = "ok", Title = "İyi Bir Başlangıç Noktası", Text = "CV'niz temel kriterleri karşılıyor. Eksik bölümleri tamamlayarak daha da güçlendirebilirsiniz." });

            return list.Take(5).ToList();
        }
    }
}
