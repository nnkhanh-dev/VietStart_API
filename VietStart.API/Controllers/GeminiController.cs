using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Net;
using System.Text;
using System.Text.Json;
using VietStart.API.Entities.DTO;

namespace VietStart.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class GeminiController : ControllerBase
    {
        private readonly IConfiguration _configuration;
        private readonly HttpClient _httpClient;

        public GeminiController(IConfiguration configuration)
        {
            _configuration = configuration;
            _httpClient = new HttpClient();
        }

        [Authorize(Roles = "Client")]
        [HttpPost("format")]
        public async Task<IActionResult> FormatInput([FromBody] string clientAnswer)
        {
            if (string.IsNullOrWhiteSpace(clientAnswer))
                return BadRequest("clientAnswer cannot be empty.");

            string apiKey = _configuration["Gemini:Key"];
            var url = $"https://generativelanguage.googleapis.com/v1beta/models/gemini-2.5-flash:generateContent?key={apiKey}";

            string prompt = @"
B?n là h? th?ng chu?n hóa thông tin Startup Vi?t Nam. Phân tích mô t? c?a ng??i dùng và trích xu?t thành JSON có ?úng 5 tr??ng:

?? TR??NG THÔNG TIN:
- Team: Thành ph?n ??i sáng l?p (tên, vai trò, kinh nghi?m)
- Idea: Ý t??ng c?t lõi (mô t? ng?n, problem-solution)
- Prototype: MVP/s?n ph?m (tr?ng thái phát tri?n, URL demo n?u có)
- Plan: K? ho?ch phát tri?n (giai ?o?n, timeline, m?c tiêu)
- Relationships: Quan h? chi?n l??c (??i tác, nhà ??u t?, advisor)

?? QUY T?C B?T BU?C:
? Ch? tr? v? JSON h?p l?
? Không gi?i thích, không Markdown
? Gi? nguyên ý chính t? input
? N?u thi?u thông tin: ?? chu?i r?ng
? Vi?t ti?ng Vi?t, clear và chuyên nghi?p

JSON OUTPUT:
{
    ""Team"": ""..."",
    ""Idea"": ""..."",
    ""Prototype"": ""..."",
    ""Plan"": ""..."",
    ""Relationships"": ""...""
}

INPUT: " + clientAnswer + @"
";

            var requestBody = new
            {
                contents = new[]
                {
                    new {
                        parts = new[] { new { text = prompt } }
                    }
                }
            };

            var content = new StringContent(JsonSerializer.Serialize(requestBody), Encoding.UTF8, "application/json");

            // --- Retry khi g?p 503 ---
            int maxRetries = 3;
            int delayMs = 2000;
            HttpResponseMessage? response = null;

            for (int i = 0; i < maxRetries; i++)
            {
                response = await _httpClient.PostAsync(url, content);
                if (response.IsSuccessStatusCode)
                    break;

                if ((int)response.StatusCode == 503)
                    await Task.Delay(delayMs);
                else
                    return StatusCode((int)response.StatusCode, await response.Content.ReadAsStringAsync());
            }

            if (response == null || !response.IsSuccessStatusCode)
                return StatusCode((int)(response?.StatusCode ?? HttpStatusCode.InternalServerError), await response!.Content.ReadAsStringAsync());

            var jsonResponse = await response.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(jsonResponse);

            if (!doc.RootElement.TryGetProperty("candidates", out var candidates))
                return BadRequest("Gemini: No candidates returned.");

            string resultText = candidates[0]
                .GetProperty("content")
                .GetProperty("parts")[0]
                .GetProperty("text")
                .GetString() ?? "";

            // Lo?i b? markdown n?u có và deserialize
            string cleanedJson = resultText.Replace("```json", "").Replace("```", "").Trim();

            StartupInfo formatted;
            try
            {
                formatted = JsonSerializer.Deserialize<StartupInfo>(cleanedJson) ?? new StartupInfo();
            }
            catch
            {
                formatted = new StartupInfo { Team = cleanedJson };
            }

            return Ok(new
            {
                original = clientAnswer,
                formatted
            });
        }

        [HttpPost("point")]
        [Authorize(Roles = "Client")]
        public async Task<IActionResult> Point([FromBody] StartupInfo info)
        {
            if (info == null)
                return BadRequest("Startup info cannot be null.");

            string apiKey = _configuration["Gemini:Key"];
            var url = $"https://generativelanguage.googleapis.com/v1beta/models/gemini-2.5-flash:generateContent?key={apiKey}";

            string prompt = @"
B?n là chuyên gia ??u t? startup early-stage t?i Vi?t Nam.
Ch?m ?i?m startup d?a trên tiêu chí sau (t?ng 100 ?i?m):

?? TIÊU CHÍ ?ÁNH GIÁ:

1?? TEAM (20 ?i?m):
   • N?ng l?c chuyên môn/k? thu?t: 8 ?i?m
   • Kinh nghi?m ?a l?nh v?c: 6 ?i?m
   • Cam k?t (FT/PT/Advisor): 6 ?i?m

2?? IDEA (20 ?i?m):
   • Tính m?i/??t phá: 8 ?i?m
   • Tính kh? thi: 6 ?i?m
   • Quy mô th? tr??ng: 6 ?i?m

3?? PROTOTYPE/MVP (30 ?i?m):
   • Có MVP/prototype: 10 ?i?m
   • Tính n?ng c?t lõi ho?t ??ng: 10 ?i?m
   • Demo ch?y ???c: 10 ?i?m

4?? K? HO?CH (15 ?i?m):
   • Có ng??i dùng th?: 7 ?i?m
   • Timeline rõ ràng (6M-1Y-3Y): 8 ?i?m

5?? QUAN H? CHI?N L??C (15 ?i?m):
   • H?p tác B2B/Ecosystem: 8 ?i?m
   • Nhà ??u t?/Advisor: 7 ?i?m

?? THÔNG TIN STARTUP:
Team: " + info.Team + @"
Idea: " + info.Idea + @"
Prototype: " + info.Prototype + @"
Plan: " + info.Plan + @"
Relationships: " + info.Relationships + @"

?? QUY T?C:
? Ch? tr? JSON, không gi?i thích
? ?i?m ph?i là s? nguyên (0-20, 0-30...)
? TotalScore = sum(Team+Idea+Prototype+Plan+Relationships)
? N?u thi?u info: ?i?m 0 cho m?c ?ó

JSON OUTPUT:
{
    ""Team"": 0-20,
    ""Idea"": 0-20,
    ""Prototype"": 0-30,
    ""Plan"": 0-15,
    ""Relationships"": 0-15,
    ""TotalScore"": 0-100
}
";

            var requestBody = new
            {
                contents = new[]
                {
                    new { parts = new[] { new { text = prompt } } }
                }
            };

            var content = new StringContent(JsonSerializer.Serialize(requestBody), Encoding.UTF8, "application/json");

            // G?i request
            var response = await _httpClient.PostAsync(url, content);
            if (!response.IsSuccessStatusCode)
                return StatusCode((int)(response?.StatusCode ?? System.Net.HttpStatusCode.InternalServerError),
                    await response.Content.ReadAsStringAsync());

            var jsonResponse = await response.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(jsonResponse);

            if (!doc.RootElement.TryGetProperty("candidates", out var candidates))
                return BadRequest("Gemini: No candidates returned.");

            string resultText = candidates[0]
                .GetProperty("content")
                .GetProperty("parts")[0]
                .GetProperty("text")
                .GetString() ?? "";

            // Lo?i b? markdown n?u có ```json```
            string cleanedJson = resultText.Replace("```json", "").Replace("```", "").Trim();

            // Deserialize JSON thành object ?i?m
            var score = new Dictionary<string, int>();
            try
            {
                score = JsonSerializer.Deserialize<Dictionary<string, int>>(cleanedJson) ?? new Dictionary<string, int>();
            }
            catch
            {
                return BadRequest("Gemini returned invalid JSON for scoring.");
            }

            return Ok(score);
        }

        [HttpPost("improve")]
        [Authorize(Roles = "Client")]
        public async Task<IActionResult> Improve([FromBody] StartupInfo info)
        {
            if (info == null)
                return BadRequest("Startup info cannot be null.");

            string apiKey = _configuration["Gemini:Key"];
            var url = $"https://generativelanguage.googleapis.com/v1beta/models/gemini-2.5-flash:generateContent?key={apiKey}";

            string prompt = @"
B?n là c? v?n startup k? c?u, chuyên s?a & vi?t l?i profile startup ?? ph?c v? pitch nhà ??u t?.

?? NHI?M V?:
Vi?t l?i thông tin startup d??i ?ây ??:
  ? Chuyên nghi?p, rõ ràng, thuy?t ph?c h?n
  ? Có s? li?u c? th? (n?u có)
  ? Gi? nguyên th?c ch?t, thêm context
  ? Phù h?p v?i nhà ??u t? Vi?t Nam & qu?c t?
  ? Tránh t? quá generic, thêm USP (Unique Selling Point)

?? THÔNG TIN HI?N T?I:
Team: " + info.Team + @"
Idea: " + info.Idea + @"
Prototype: " + info.Prototype + @"
Plan: " + info.Plan + @"
Relationships: " + info.Relationships + @"

?? QUY T?C:
? Ch? tr? JSON, không markdown, không gi?i thích
? 5 tr??ng: Team, Idea, Prototype, Plan, Relationships
? N?u input r?ng: output c?ng r?ng
? Gi? length h?p lý (200-300 ký t?/tr??ng)
? Ti?ng Vi?t, chuyên ngành

JSON OUTPUT:
{
    ""Team"": ""...(?ã c?i thi?n)"",
    ""Idea"": ""...(?ã c?i thi?n)"",
    ""Prototype"": ""...(?ã c?i thi?n)"",
    ""Plan"": ""...(?ã c?i thi?n)"",
    ""Relationships"": ""...(?ã c?i thi?n)""
}
";

            var requestBody = new
            {
                contents = new[]
                {
                    new { parts = new[] { new { text = prompt } } }
                }
            };

            var content = new StringContent(JsonSerializer.Serialize(requestBody), Encoding.UTF8, "application/json");

            // Retry khi g?p 503
            int maxRetries = 3;
            int delayMs = 2000;
            HttpResponseMessage? response = null;

            for (int i = 0; i < maxRetries; i++)
            {
                response = await _httpClient.PostAsync(url, content);
                if (response.IsSuccessStatusCode)
                    break;

                if ((int)response.StatusCode == 503)
                    await Task.Delay(delayMs);
                else
                    return StatusCode((int)response.StatusCode, await response.Content.ReadAsStringAsync());
            }

            if (response == null || !response.IsSuccessStatusCode)
                return StatusCode((int)(response?.StatusCode ?? HttpStatusCode.InternalServerError), await response!.Content.ReadAsStringAsync());

            var jsonResponse = await response.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(jsonResponse);

            if (!doc.RootElement.TryGetProperty("candidates", out var candidates))
                return BadRequest("Gemini: No candidates returned.");

            string resultText = candidates[0]
                .GetProperty("content")
                .GetProperty("parts")[0]
                .GetProperty("text")
                .GetString() ?? "";

            string cleanedJson = resultText.Replace("```json", "").Replace("```", "").Trim();

            StartupInfo improved;
            try
            {
                improved = JsonSerializer.Deserialize<StartupInfo>(cleanedJson) ?? new StartupInfo();
            }
            catch
            {
                improved = new StartupInfo { Team = cleanedJson };
            }

            return Ok(new
            {
                original = info,
                improved
            });
        }

        [HttpPost("suggest")]
        [Authorize(Roles = "Client")]
        public async Task<IActionResult> Suggest([FromBody] StartupInfo info)
        {
            if (info == null)
                return BadRequest("Startup info cannot be null.");

            string apiKey = _configuration["Gemini:Key"];
            var url = $"https://generativelanguage.googleapis.com/v1beta/models/gemini-2.5-flash:generateContent?key={apiKey}";

            string prompt = @"
B?n là mentor startup k? c?u, chuyên t? v?n chi?n l??c phát tri?n startup Vi?t Nam.

?? NHI?M V?:
Phân tích startup và ??a ra g?i ý c? th?, kh? thi ??:
  ? T?ng c? h?i nh?n funding
  ? Gi?i quy?t bottleneck hi?n t?i
  ? Accelerate growth
  ? Xây d?ng sustainable business

?? THÔNG TIN STARTUP:
Team: " + info.Team + @"
Idea: " + info.Idea + @"
Prototype: " + info.Prototype + @"
Plan: " + info.Plan + @"
Relationships: " + info.Relationships + @"

?? G?I Ý THEO 5 L?NH V?C:

Team ? G?i ý:
  • Tuy?n d?ng: k? n?ng, vai trò nào?
  • C?u trúc: ideally bao nhiêu ng??i?
  • Network: tìm advisor/co-founder ? ?âu?

Idea ? G?i ý:
  • M? r?ng market: target users nào ti?p theo?
  • Xác ??nh USP: ?i?m khác bi?t vs competitor?
  • B2B/B2C: model nào phù h?p?

Prototype ? G?i ý:
  • Tính n?ng ?u tiên: MVP c?n gì?
  • Timeline: bao lâu ?? launch?
  • Metrics: measure success th? nào?

Plan ? G?i ý:
  • Milestone c? th?: Q1, Q2, Q3...
  • Revenue target: có th? ??t bao nhiêu?
  • Funding round: nên raise bao nhiêu, khi nào?

Relationships ? G?i ý:
  • Partners: ngành công nghi?p/platform nào phù h?p?
  • Investors: lo?i investor nào (Angel/VC/Corp)?
  • Accelerators: program nào có l?i (Y Combinator, 500 Startups...)?

?? QUY T?C:
? Ch? tr? JSON, không markdown
? G?i ý ph?i: Specific, Actionable, Measurable
? N?u input r?ng: output c?ng r?ng
? Length: 150-300 ký t?/tr??ng
? Ti?ng Vi?t, ngôn ng? mentor

JSON OUTPUT:
{
    ""Team"": ""G?i ý c? th? cho team...(ai, k? n?ng gì, t? ?âu?)"",
    ""Idea"": ""G?i ý phát tri?n idea...(market nào, USP gì?)"",
    ""Prototype"": ""G?i ý c?i thi?n s?n ph?m...(features, timeline)"",
    ""Plan"": ""G?i ý k? ho?ch...(milestone, revenue, funding)"",
    ""Relationships"": ""G?i ý tìm partner...(lo?i partner, n?i tìm)""
}
";

            var requestBody = new
            {
                contents = new[]
                {
                    new { parts = new[] { new { text = prompt } } }
                }
            };

            var content = new StringContent(JsonSerializer.Serialize(requestBody), Encoding.UTF8, "application/json");

            // Retry khi g?p 503
            int maxRetries = 3;
            int delayMs = 2000;
            HttpResponseMessage? response = null;

            for (int i = 0; i < maxRetries; i++)
            {
                response = await _httpClient.PostAsync(url, content);
                if (response.IsSuccessStatusCode)
                    break;

                if ((int)response.StatusCode == 503)
                    await Task.Delay(delayMs);
                else
                    return StatusCode((int)response.StatusCode, await response.Content.ReadAsStringAsync());
            }

            if (response == null || !response.IsSuccessStatusCode)
                return StatusCode((int)(response?.StatusCode ?? HttpStatusCode.InternalServerError), await response!.Content.ReadAsStringAsync());

            var jsonResponse = await response.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(jsonResponse);

            if (!doc.RootElement.TryGetProperty("candidates", out var candidates))
                return BadRequest("Gemini: No candidates returned.");

            string resultText = candidates[0]
                .GetProperty("content")
                .GetProperty("parts")[0]
                .GetProperty("text")
                .GetString() ?? "";

            string cleanedJson = resultText.Replace("```json", "").Replace("```", "").Trim();

            StartupInfo suggestions;
            try
            {
                suggestions = JsonSerializer.Deserialize<StartupInfo>(cleanedJson) ?? new StartupInfo();
            }
            catch
            {
                suggestions = new StartupInfo { Team = cleanedJson };
            }

            return Ok(new
            {
                original = info,
                suggestions
            });
        }
    }
}
