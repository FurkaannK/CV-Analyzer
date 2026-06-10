using CVAnalyzer.Models;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Net.Http.Headers;
using System.Text;

namespace CVAnalyzer.Services
{
    public interface IGeminiParsingService
    {
        Task<ATSParsedData?> ParseCVAsync(string rawText);
        Task<float[]> GenerateEmbeddingAsync(string text);
        Task<List<CandidateRankResult>> RankCandidatesAsync(string searchQuery, string candidatesJson);
        Task<SearchFilters?> ExtractSearchFiltersAsync(string searchQuery);
    }

    public class GeminiParsingService : IGeminiParsingService
    {
        private readonly HttpClient _httpClient;
        private readonly string _apiKey;

        public GeminiParsingService(HttpClient httpClient, IConfiguration configuration)
        {
            _httpClient = httpClient;
            _apiKey = configuration["GeminiApiKey"] ?? throw new ArgumentNullException("GeminiApiKey is missing!");
            _httpClient.BaseAddress = new Uri("https://generativelanguage.googleapis.com/");
        }

        public async Task<ATSParsedData?> ParseCVAsync(string rawText)
        {
            var prompt = @"
You are an expert ATS (Applicant Tracking System) parser. I will provide you with the raw text extracted from a CV.
Extract the information into the exact JSON structure provided below. Do not include markdown code blocks (like ```json), just output the raw valid JSON.

CRITICAL INSTRUCTION FOR DATA NORMALIZATION:
Regardless of the language of the CV, YOU MUST TRANSLATE AND NORMALIZE all extracted data into STANDARD ENGLISH. 
- Job titles (e.g. 'Yazılımcı' -> 'Software Developer')
- Locations (e.g. 'İstanbul' -> 'Istanbul')
- Summaries, descriptions, headlines, and skills MUST all be in English.
- Keep technical terms (e.g. 'React', 'C#') exactly as they are.

Required JSON Structure:
{
  ""personal"": {
    ""full_name"": """",
    ""email"": """",
    ""phone"": """",
    ""location"": """",
    ""linkedin_url"": """",
    ""portfolio_url"": """"
  },
  ""headline"": """",
  ""summary"": """",
  ""skills"": [
    {
      ""name"": """",
      ""category"": ""technical or soft"",
      ""level"": ""beginner, intermediate, advanced or native"",
      ""years_of_experience"": 0
    }
  ],
  ""experience"": [
    {
      ""title"": """",
      ""company"": """",
      ""location"": """",
      ""start_date"": ""YYYY-MM or YYYY"",
      ""end_date"": ""YYYY-MM, YYYY or Present"",
      ""is_current"": false,
      ""description"": """",
      ""achievements"": [""""],
      ""skills_used"": [""""]
    }
  ],
  ""education"": [
    {
      ""degree"": """",
      ""school"": """",
      ""field"": """",
      ""start_year"": """",
      ""end_year"": """"
    }
  ],
  ""languages"": [
    {
      ""name"": """",
      ""level"": """"
    }
  ],
  ""certifications"": [
    {
      ""name"": """",
      ""issuer"": """",
      ""year"": """"
    }
  ],
  ""projects"": [
    {
      ""name"": """",
      ""description"": """",
      ""skills"": [""""],
      ""url"": """"
    }
  ],
  ""normalized_skills"": [""""],
  ""embedding_text"": """",
  ""skills_embedding_text"": """",
  ""metadata"": {
    ""cv_language"": """",
    ""confidence_score"": 0.95,
    ""parsed_with"": ""gemini-3.1-flash-lite""
  }
}

Rules:
1. Ensure all normalized_skills are lowercase strings.
2. For embedding_text, generate a single coherent paragraph summarizing the candidate's core expertise, role, and main technologies/skills. This will be used for general semantic vector search.
3. For skills_embedding_text, write a comma-separated list of ONLY the candidate's normalized technical skills, tools, and exact job titles. DO NOT include filler words or generic text. This will be used for a dedicated precise skill vector match.
4. For years_of_experience in skills: Calculate exactly how many years each skill was used based on the work experience entries (start_date to end_date). If a skill isn't tied to a specific job but is generally known, estimate realistically based on their career, or default to 0.
5. If a field is not found in the CV, leave it as an empty string or empty array. DO NOT invent information.
6. Output ONLY valid JSON.

Here is the raw CV text:
" + rawText;

            var requestBody = new
            {
                contents = new[]
                {
                    new
                    {
                        parts = new[]
                        {
                            new { text = prompt }
                        }
                    }
                },
                generationConfig = new
                {
                    temperature = 0.1,
                    responseMimeType = "application/json"
                }
            };

            var jsonContent = new StringContent(JsonSerializer.Serialize(requestBody), Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync($"/v1beta/models/gemini-3.1-flash-lite:generateContent?key={_apiKey}", jsonContent);

            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync();
                throw new Exception($"Gemini API Error: {error}");
            }

            var responseData = await response.Content.ReadAsStringAsync();
            
            try
            {
                // Parse the Gemini response wrapper
                using var doc = JsonDocument.Parse(responseData);
                var root = doc.RootElement;
                
                var candidates = root.GetProperty("candidates");
                if (candidates.GetArrayLength() > 0)
                {
                    var textResponse = candidates[0]
                        .GetProperty("content")
                        .GetProperty("parts")[0]
                        .GetProperty("text")
                        .GetString();

                    if (!string.IsNullOrEmpty(textResponse))
                    {
                        // Clean markdown if Gemini ignored the instruction
                        if (textResponse.StartsWith("```json"))
                        {
                            textResponse = textResponse.Substring(7);
                            if (textResponse.EndsWith("```"))
                            {
                                textResponse = textResponse.Substring(0, textResponse.Length - 3);
                            }
                        }

                        var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                        var parsedData = JsonSerializer.Deserialize<ATSParsedData>(textResponse, options);
                        return parsedData;
                    }
                }
                
                return null;
            }
            catch (Exception ex)
            {
                throw new Exception($"Failed to parse Gemini response to ATSParsedData. Error: {ex.Message}\nRaw Response: {responseData}");
            }
        }

        public async Task<float[]> GenerateEmbeddingAsync(string text)
        {
            var url = $"v1beta/models/gemini-embedding-2:embedContent?key={_apiKey}";

            var requestBody = new
            {
                model = "models/gemini-embedding-2",
                content = new
                {
                    parts = new[]
                    {
                        new { text = text }
                    }
                }
            };

            var jsonContent = new StringContent(JsonSerializer.Serialize(requestBody), Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync(url, jsonContent);
            var responseData = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                throw new Exception($"Gemini Embedding API failed with status {response.StatusCode}: {responseData}");
            }

            try
            {
                using var jsonDoc = JsonDocument.Parse(responseData);
                var root = jsonDoc.RootElement;

                var embeddingElement = root.GetProperty("embedding");
                var valuesElement = embeddingElement.GetProperty("values");

                var floats = new float[valuesElement.GetArrayLength()];
                for (int i = 0; i < valuesElement.GetArrayLength(); i++)
                {
                    floats[i] = valuesElement[i].GetSingle();
                }

                return floats;
            }
            catch (Exception ex)
            {
                throw new Exception($"Failed to parse Gemini Embedding response. Error: {ex.Message}\nRaw Response: {responseData}");
            }
        }

        public async Task<List<CandidateRankResult>> RankCandidatesAsync(string searchQuery, string candidatesJson)
        {
            var prompt = $@"
You are an expert HR Technical Recruiter.
A hiring manager is searching for candidates with the following query:
""{searchQuery}""

Here is a JSON array containing the top candidates that matched the vector search. Each candidate has a 'CandidateId' and their parsed CV data.
{candidatesJson}

Your task is to rank these candidates from 0 to 100 based on how well they match the hiring manager's query.
Output ONLY a valid JSON array of objects with the exact structure below. No markdown formatting.

[
  {{
    ""candidate_id"": 1,
    ""match_score"": 95,
    ""reason"": ""Tek cümlelik kısa ve etkili Türkçe bir neden yaz.""
  }}
]
";

            var requestBody = new
            {
                contents = new[]
                {
                    new
                    {
                        parts = new[]
                        {
                            new { text = prompt }
                        }
                    }
                },
                generationConfig = new
                {
                    temperature = 0.2,
                    responseMimeType = "application/json"
                }
            };

            var jsonContent = new StringContent(JsonSerializer.Serialize(requestBody), Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync($"/v1beta/models/gemini-3.1-flash-lite:generateContent?key={_apiKey}", jsonContent);

            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync();
                throw new Exception($"Gemini API Error: {error}");
            }

            var responseData = await response.Content.ReadAsStringAsync();

            try
            {
                using var jsonDoc = JsonDocument.Parse(responseData);
                var root = jsonDoc.RootElement;

                var candidatesArrayElement = root.GetProperty("candidates");
                if (candidatesArrayElement.GetArrayLength() > 0)
                {
                    var textResponse = candidatesArrayElement[0]
                        .GetProperty("content")
                        .GetProperty("parts")[0]
                        .GetProperty("text")
                        .GetString();

                    if (!string.IsNullOrEmpty(textResponse))
                    {
                        var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                        var results = JsonSerializer.Deserialize<List<CandidateRankResult>>(textResponse, options);
                        return results ?? new List<CandidateRankResult>();
                    }
                }

                return new List<CandidateRankResult>();
            }
            catch (Exception ex)
            {
                throw new Exception($"Failed to parse Gemini Rank response. Error: {ex.Message}\nRaw Response: {responseData}");
            }
        }

        public async Task<SearchFilters?> ExtractSearchFiltersAsync(string searchQuery)
        {
            var url = $"https://generativelanguage.googleapis.com/v1beta/models/gemini-3.1-flash-lite:generateContent?key={_apiKey}";

            var prompt = $@"
You are an expert HR recruitment assistant. Extract structured filtering criteria from the following search query.

IMPORTANT INSTRUCTIONS:
1. Translate any job titles or generic terms in 'skills' to standard English (e.g., 'yazılımcı' -> 'Software Developer', 'mühendis' -> 'Engineer'). Keep technical terms like 'React' exactly as they are.
2. For 'location', convert Turkish characters to standard English characters (e.g., 'İstanbul' -> 'Istanbul', 'Ankara' -> 'Ankara').

Search Query: '{searchQuery}'

Extract the data into this EXACT JSON structure. DO NOT return markdown, DO NOT return ```json. ONLY return the raw JSON object.
{{
  ""skills"": [""skill1"", ""skill2""], 
  ""location"": ""city name"", 
  ""min_experience_years"": 0, 
  ""is_remote"": false 
}}";

            var requestBody = new
            {
                contents = new[]
                {
                    new
                    {
                        parts = new[] { new { text = prompt } }
                    }
                },
                generationConfig = new
                {
                    temperature = 0.0,
                    response_mime_type = "application/json"
                }
            };

            var content = new StringContent(JsonSerializer.Serialize(requestBody), Encoding.UTF8, "application/json");
            var response = await _httpClient.PostAsync(url, content);
            var responseData = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                throw new Exception($"Gemini API error (Extract Filters): {response.StatusCode} - {responseData}");
            }

            try
            {
                using var doc = JsonDocument.Parse(responseData);
                if (doc.RootElement.TryGetProperty("candidates", out var candidates) &&
                    candidates.GetArrayLength() > 0 &&
                    candidates[0].TryGetProperty("content", out var contentElement) &&
                    contentElement.TryGetProperty("parts", out var parts) &&
                    parts.GetArrayLength() > 0 &&
                    parts[0].TryGetProperty("text", out var textElement))
                {
                    var textResponse = textElement.GetString()?.Trim();
                    
                    if (!string.IsNullOrEmpty(textResponse))
                    {
                        // Clean markdown if Gemini still returns it despite instructions
                        if (textResponse.StartsWith("```json")) textResponse = textResponse.Substring(7);
                        if (textResponse.StartsWith("```")) textResponse = textResponse.Substring(3);
                        if (textResponse.EndsWith("```")) textResponse = textResponse.Substring(0, textResponse.Length - 3);
                        textResponse = textResponse.Trim();

                        var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                        var filters = JsonSerializer.Deserialize<SearchFilters>(textResponse, options);
                        return filters;
                    }
                }

                return null;
            }
            catch (Exception ex)
            {
                throw new Exception($"Failed to parse Gemini Filter response. Error: {ex.Message}\nRaw Response: {responseData}");
            }
        }
    }
}
