using Microsoft.AspNetCore.Http;
using System.ComponentModel.DataAnnotations;

namespace CVAnalyzer.Models
{
    // CV analiz formu için giriş modeli
    public class CVAnalyzeViewModel
    {
        public IFormFile? CVFile { get; set; }
        public string TargetRole { get; set; } = string.Empty;
        public List<JobRole> AvailableRoles { get; set; } = new();
    }

    // Analiz sonucu sayfası için model
    public class CVResultViewModel
    {
        public int OverallScore { get; set; }
        public int JobMatchScore { get; set; }
        public string ScoreBadge { get; set; } = string.Empty;
        public string TargetRole { get; set; } = string.Empty;

        public List<string> FoundSkills { get; set; } = new();
        public List<string> MissingSkills { get; set; } = new();
        public List<SuggestionItem> Suggestions { get; set; } = new();
        public Dictionary<string, bool> Sections { get; set; } = new();
        public List<MatchCriterion> MatchCriteria { get; set; } = new();
        public Dictionary<string, int> SkillLevels { get; set; } = new();
    }

    // Öneri öğesi
    public class SuggestionItem
    {
        public string Type { get; set; } = "tip"; // ok, warn, tip
        public string Title { get; set; } = string.Empty;
        public string Text { get; set; } = string.Empty;
    }

    // Eşleşme kriteri
    public class MatchCriterion
    {
        public string Label { get; set; } = string.Empty;
        public int Score { get; set; }
    }


    // ─── Auth Modelleri ──────────────────────────────────────────
    public class LoginViewModel
    {
        [Required(ErrorMessage = "E-posta adresi zorunludur.")]
        [EmailAddress(ErrorMessage = "Geçerli bir e-posta adresi giriniz.")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "Şifre zorunludur.")]
        [DataType(DataType.Password)]
        public string Password { get; set; } = string.Empty;
    }

    public class RegisterViewModel
    {
        [Required(ErrorMessage = "Ad Soyad zorunludur.")]
        public string FullName { get; set; } = string.Empty;

        [Required(ErrorMessage = "E-posta adresi zorunludur.")]
        [EmailAddress(ErrorMessage = "Geçerli bir e-posta adresi giriniz.")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "Şifre zorunludur.")]
        [DataType(DataType.Password)]
        [MinLength(6, ErrorMessage = "Şifre en az 6 karakter olmalıdır.")]
        public string Password { get; set; } = string.Empty;

        [Required(ErrorMessage = "Rol seçimi zorunludur.")]
        public string Role { get; set; } = "User"; // Varsayılan User
    }

    public class ProfileViewModel
    {
        [Required(ErrorMessage = "Ad Soyad alanı zorunludur.")]
        [Display(Name = "Ad Soyad")]
        public string FullName { get; set; } = string.Empty;

        public string Email { get; set; } = string.Empty;

        // --- Yeni Profil Alanları ---
        [Display(Name = "Doğum Tarihi")]
        [DataType(DataType.Date)]
        public DateTime? BirthDate { get; set; }

        [Display(Name = "Doğum Yeri")]
        [MaxLength(100, ErrorMessage = "Doğum yeri çok uzun.")]
        public string? BirthPlace { get; set; }

        [Display(Name = "Cinsiyet")]
        [MaxLength(20)]
        public string? Gender { get; set; }

        [Display(Name = "Medeni Durum")]
        [MaxLength(50)]
        public string? MaritalStatus { get; set; }

        [Display(Name = "Hakkımda (Biyografi)")]
        [MaxLength(1000, ErrorMessage = "Biyografi en fazla 1000 karakter olabilir.")]
        public string? Biography { get; set; }

        [Display(Name = "Unvan / Meslek (Headline)")]
        [MaxLength(200)]
        public string? Headline { get; set; }

        [Display(Name = "Telefon Numarası")]
        [MaxLength(50)]
        public string? Phone { get; set; }

        [Display(Name = "Şehir / Konum")]
        [MaxLength(100)]
        public string? Location { get; set; }

        [Display(Name = "LinkedIn Profil URL")]
        [MaxLength(255)]
        public string? LinkedinUrl { get; set; }

        public List<string> Skills { get; set; } = new();

        [Display(Name = "Yetenekler (Virgülle ayırarak yazın)")]
        [MaxLength(1000)]
        public string? SkillsText { get; set; }
        // -----------------------------
        
        // Kullanıcının Geçmiş CV'leri
        public List<CandidateCV> CandidateCVs { get; set; } = new();

        [DataType(DataType.Password)]
        [Display(Name = "Mevcut Şifre")]
        public string? CurrentPassword { get; set; }

        [DataType(DataType.Password)]
        [Display(Name = "Yeni Şifre")]
        [MinLength(6, ErrorMessage = "Yeni şifreniz en az 6 karakter olmalıdır.")]
        public string? NewPassword { get; set; }

        [DataType(DataType.Password)]
        [Display(Name = "Yeni Şifre (Tekrar)")]
        [Compare("NewPassword", ErrorMessage = "Yeni şifreler eşleşmiyor.")]
        public string? ConfirmNewPassword { get; set; }
    }

    // ─── Admin Modelleri ─────────────────────────────────────────
    public class AdminDashboardViewModel
    {
        public int TotalCandidates { get; set; }
        public int TotalCVs { get; set; }
        public List<CandidateDto> Candidates { get; set; } = new();
    }

    public class CandidateDto
    {
        public int UserId { get; set; }
        public string FullName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string? Headline { get; set; }
        public string? Location { get; set; }
        public List<string> Skills { get; set; } = new();
        public DateTime? LastCVSync { get; set; }
    }
}
