using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace CVAnalyzer.Models
{
    // ─── Kullanıcı ─────────────────────────────────────────────
    public class User
    {
        public int Id { get; set; }

        [Required, MaxLength(150)]
        public string FullName { get; set; } = string.Empty;

        [Required, EmailAddress, MaxLength(200)]
        public string Email { get; set; } = string.Empty;

        [Required, MaxLength(255)]
        public string PasswordHash { get; set; } = string.Empty;

        [Required, MaxLength(50)]
        public string Role { get; set; } = "User";

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public ICollection<CandidateCV> CandidateCVs { get; set; } = new List<CandidateCV>();

        public UserProfile? UserProfile { get; set; }
    }

    // ─── Kullanıcı Profili ──────────────────────────────────────
    public class UserProfile
    {
        public int Id { get; set; }
        
        public int UserId { get; set; }
        public User? User { get; set; }

        public DateTime? BirthDate { get; set; }
        
        [MaxLength(200)]
        public string? Headline { get; set; }
        
        [MaxLength(50)]
        public string? Phone { get; set; }
        
        [MaxLength(100)]
        public string? Location { get; set; }
        
        [MaxLength(255)]
        public string? LinkedinUrl { get; set; }
        
        [MaxLength(100)]
        public string? BirthPlace { get; set; }
        
        [MaxLength(20)]
        public string? Gender { get; set; }
        
        [MaxLength(50)]
        public string? MaritalStatus { get; set; }
        
        [MaxLength(1000)]
        public string? Biography { get; set; }

        public List<string>? Skills { get; set; } = new();

        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    }

    // ─── Merkezi Aday CV Havuzu (ATS) ───────────────────────────
    public class CandidateCV
    {
        public int Id { get; set; }
        public int? UserId { get; set; }
        public User? User { get; set; }

        public string RawText { get; set; } = string.Empty;

        [MaxLength(500)]
        public string? PdfFilePath { get; set; }

        // PostgreSQL JSONB olarak saklanır
        [Column(TypeName = "jsonb")]
        public ATSParsedData ParsedData { get; set; } = new();

        [Column(TypeName = "vector(3072)")]
        public Pgvector.Vector? Embedding { get; set; }

        [Column(TypeName = "vector(3072)")]
        public Pgvector.Vector? SkillsEmbedding { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }

    public class ATSParsedData
    {
        public ATSPersonal Personal { get; set; } = new();
        public string Headline { get; set; } = string.Empty;
        public string Summary { get; set; } = string.Empty;
        public List<ATSSkill> Skills { get; set; } = new();
        public List<ATSExperience> Experience { get; set; } = new();
        public List<ATSEducation> Education { get; set; } = new();
        public List<ATSLanguage> Languages { get; set; } = new();
        public List<ATSCertification> Certifications { get; set; } = new();
        public List<ATSProject> Projects { get; set; } = new();

        [JsonPropertyName("normalized_skills")]
        public List<string> NormalizedSkills { get; set; } = new();

        [JsonPropertyName("embedding_text")]
        public string EmbeddingText { get; set; } = string.Empty;

        [JsonPropertyName("skills_embedding_text")]
        public string SkillsEmbeddingText { get; set; } = string.Empty;

        public ATSMetadata Metadata { get; set; } = new();
    }

    public class ATSPersonal
    {
        [JsonPropertyName("full_name")] public string FullName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Phone { get; set; } = string.Empty;
        public string Location { get; set; } = string.Empty;
        [JsonPropertyName("linkedin_url")] public string LinkedinUrl { get; set; } = string.Empty;
        [JsonPropertyName("portfolio_url")] public string PortfolioUrl { get; set; } = string.Empty;
    }

    public class ATSSkill
    {
        public string Name { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
        public string Level { get; set; } = string.Empty;
        [JsonPropertyName("years_of_experience")] public int YearsOfExperience { get; set; }
    }

    public class ATSExperience
    {
        public string Title { get; set; } = string.Empty;
        public string Company { get; set; } = string.Empty;
        public string Location { get; set; } = string.Empty;
        [JsonPropertyName("start_date")] public string StartDate { get; set; } = string.Empty;
        [JsonPropertyName("end_date")] public string EndDate { get; set; } = string.Empty;
        [JsonPropertyName("is_current")] public bool IsCurrent { get; set; }
        public string Description { get; set; } = string.Empty;
        public List<string> Achievements { get; set; } = new();
        [JsonPropertyName("skills_used")] public List<string> SkillsUsed { get; set; } = new();
    }

    public class ATSEducation
    {
        public string Degree { get; set; } = string.Empty;
        public string School { get; set; } = string.Empty;
        public string Field { get; set; } = string.Empty;
        [JsonPropertyName("start_year")] public string StartYear { get; set; } = string.Empty;
        [JsonPropertyName("end_year")] public string EndYear { get; set; } = string.Empty;
    }

    public class ATSLanguage
    {
        public string Name { get; set; } = string.Empty;
        public string Level { get; set; } = string.Empty;
    }

    public class ATSCertification
    {
        public string Name { get; set; } = string.Empty;
        public string Issuer { get; set; } = string.Empty;
        public string Year { get; set; } = string.Empty;
    }

    public class ATSProject
    {
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public List<string> Skills { get; set; } = new();
        public string Url { get; set; } = string.Empty;
    }

    public class ATSMetadata
    {
        [JsonPropertyName("cv_language")] public string CvLanguage { get; set; } = string.Empty;
        [JsonPropertyName("confidence_score")] public double ConfidenceScore { get; set; }
        [JsonPropertyName("parsed_with")] public string ParsedWith { get; set; } = string.Empty;
    }

    // ─── İş Pozisyonları ────────────────────────────────────────
    public class JobRole
    {
        public int Id { get; set; }

        [Required, MaxLength(100)]
        public string RoleName { get; set; } = string.Empty;

        // Virgülle ayrılmış keyword listesi
        public string RequiredSkills { get; set; } = string.Empty;

        public string IconClass { get; set; } = "ti-briefcase";
        public string Description { get; set; } = string.Empty;
        public bool IsActive { get; set; } = true;
    }

    // ─── Smart Search Modelleri ────────────────────────────────────────
    
    // ─── Arama Filtreleri (V2) ───────────────────────────────────
    public class SearchFilters
    {
        [JsonPropertyName("skills")] public List<string> Skills { get; set; } = new();
        [JsonPropertyName("location")] public string Location { get; set; } = string.Empty;
        [JsonPropertyName("min_experience_years")] public int MinExperienceYears { get; set; }
        [JsonPropertyName("is_remote")] public bool? IsRemote { get; set; }
    }

    public class SmartSearchResultViewModel
    {
        public CandidateCV Candidate { get; set; } = null!;
        public int MatchScore { get; set; }
        public string Reason { get; set; } = string.Empty;
        public double VectorDistance { get; set; }
        // Exact Match details
        public List<string> MatchedSkills { get; set; } = new();
        public bool IsLocationMatched { get; set; }
        public bool IsExperienceMatched { get; set; }
    }

    public class CandidateRankResult
    {
        public int CandidateId { get; set; }
        public int MatchScore { get; set; }
        public string Reason { get; set; } = string.Empty;
    }
}
