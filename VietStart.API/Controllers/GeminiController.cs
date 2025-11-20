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
        private readonly IWebHostEnvironment _environment;

        public GeminiController(IConfiguration configuration, HttpClient httpClient, IWebHostEnvironment environment)
        {
            _configuration = configuration;
            _httpClient = httpClient;
            _environment = environment;
        }

        [Authorize(Roles = "Client")]
        [HttpPost("format")]
        public async Task<IActionResult> FormatInput([FromBody] string clientAnswer)
        {
            if (string.IsNullOrWhiteSpace(clientAnswer))
                return BadRequest("clientAnswer cannot be empty.");

            string apiKey = _configuration["Gemini:Key"];
            var url = $"https://generativelanguage.googleapis.com/v1beta/models/gemini-2.0-flash-exp:generateContent?key={apiKey}";

            // BƯỚC 1: Kiểm tra vi phạm pháp luật
            string validationPrompt = @"
🚨 NHIỆM VỤ KIỂM TRA VI PHẠM:
Bạn là chuyên gia pháp lý startup Việt Nam. Phân tích input và kiểm tra xem startup có vi phạm:

❌ VI PHẠM PHÁP LUẬT:
• Kinh doanh cá độ, cờ bạc, casino online
• Đa cấp, ponzi, lừa đảo tài chính
• Tiền ảo, cryptocurrency không được cấp phép
• Vũ khí, ma túy, chất cấm
• Nội dung đồi trụy, khiêu dâm
• Vi phạm bản quyền rõ ràng
• Bán hàng cấm (thuốc lá điện tử, thuốc không phép)
• Phá hoại an ninh quốc gia, phân biệt chủng tộc

❌ VI PHẠM QUY CHUẨN:
• Thiếu giấy phép bắt buộc (y tế, tài chính, giáo dục)
• Tuyên bố y tế không có chứng cứ
• Lừa dối khách hàng rõ ràng
• Thông tin sai lệch nghiêm trọng

⚙️ QUY TẮC:
✓ Chỉ trả về JSON
✓ Nếu VI PHẠM: isValid = false, message = lý do cụ thể
✓ Nếu HỢP LỆ: isValid = true, message = ""

INPUT: " + clientAnswer + @"

JSON OUTPUT:
{
    ""isValid"": true/false,
    ""message"": ""lý do vi phạm (nếu có)""
}
";

            var validationRequestBody = new
            {
                contents = new[]
                {
                    new {
                        parts = new[] { new { text = validationPrompt } }
                    }
                }
            };

            var validationContent = new StringContent(JsonSerializer.Serialize(validationRequestBody), Encoding.UTF8, "application/json");
            var validationResponse = await _httpClient.PostAsync(url, validationContent);

            if (!validationResponse.IsSuccessStatusCode)
                return StatusCode((int)validationResponse.StatusCode, await validationResponse.Content.ReadAsStringAsync());

            var validationJsonResponse = await validationResponse.Content.ReadAsStringAsync();
            using (var validationDoc = JsonDocument.Parse(validationJsonResponse))
            {
                if (validationDoc.RootElement.TryGetProperty("candidates", out var validationCandidates))
                {
                    string validationResultText = validationCandidates[0]
                        .GetProperty("content")
                        .GetProperty("parts")[0]
                        .GetProperty("text")
                        .GetString() ?? "";

                    string cleanedValidationJson = validationResultText.Replace("```json", "").Replace("```", "").Trim();
                    
                    try
                    {
                        var validationResult = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(cleanedValidationJson);
                        if (validationResult != null && 
                            validationResult.TryGetValue("isValid", out var isValidElement) && 
                            !isValidElement.GetBoolean())
                        {
                            string violationMessage = validationResult.TryGetValue("message", out var msgElement) 
                                ? msgElement.GetString() ?? "Startup vi phạm quy định" 
                                : "Startup vi phạm quy định";
                            return BadRequest(new { error = violationMessage });
                        }
                    }
                    catch
                    {
                        // Nếu parse lỗi, coi như hợp lệ và tiếp tục
                    }
                }
            }

            // BƯỚC 2: Format thông tin startup
            string prompt = @"
Bạn là hệ thống chuẩn hóa thông tin Startup Việt Nam. Phân tích mô tả của người dùng và trích xuất thành JSON có đúng 5 trường:

📋 TRƯỜNG THÔNG TIN:
- Team: Thành phần đội sáng lập (tên, vai trò, kinh nghiệm)
- Idea: Ý tưởng cốt lõi (mô tả ngắn, problem-solution)
- Prototype: MVP/sản phẩm (trạng thái phát triển, URL demo nếu có)
- Plan: Kế hoạch phát triển (giai đoạn, timeline, mục tiêu)
- Relationships: Quan hệ chiến lược (đối tác, nhà đầu tư, advisor)

⚙️ QUY TẮC BẮT BUỘC:
✓ Chỉ trả về JSON hợp lệ
✓ Không giải thích, không Markdown
✓ Giữ nguyên ý chính từ input
✓ Nếu thiếu thông tin: để chuỗi rỗng
✓ Viết tiếng Việt, clear và chuyên nghiệp

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

            // --- Retry khi gặp 503 ---
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

            // Loại bỏ markdown nếu có và deserialize
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
            var url = $"https://generativelanguage.googleapis.com/v1beta/models/gemini-2.0-flash-exp:generateContent?key={apiKey}";

            var filePath = Path.Combine(_environment.ContentRootPath, "Data", "Data.json");

            if (!System.IO.File.Exists(filePath))
            {
                return BadRequest("File Data.json không tồn tại: " + filePath);
            }

            var example = System.IO.File.ReadAllText(filePath);

            string prompt = $@"
Bạn là chuyên gia đầu tư startup early-stage tại Việt Nam.
Chấm điểm startup dựa trên tiêu chí sau (tổng 100 điểm):

📊 TIÊU CHÍ ĐÁNH GIÁ:

1️⃣ TEAM (20 điểm):
   • Năng lực chuyên môn/kỹ thuật: 8 điểm
   • Kinh nghiệm đa lĩnh vực: 6 điểm
   • Cam kết (FT/PT/Advisor): 6 điểm

2️⃣ IDEA (20 điểm):
   • Tính mới/đột phá: 8 điểm
   • Tính khả thi: 6 điểm
   • Quy mô thị trường: 6 điểm

3️⃣ PROTOTYPE/MVP (30 điểm):
   • Có MVP/prototype: 10 điểm
   • Tính năng cốt lõi hoạt động: 10 điểm
   • Demo chạy được: 10 điểm

4️⃣ KẾ HOẠCH (15 điểm):
   • Có người dùng thử: 7 điểm
   • Timeline rõ ràng (6M-1Y-3Y): 8 điểm

5️⃣ QUAN HỆ CHIẾN LƯỢC (15 điểm):
   • Hợp tác B2B/Ecosystem: 8 điểm
   • Nhà đầu tư/Advisor: 7 điểm

📌 Ví dụ chấm điểm:
{example}

📋 THÔNG TIN STARTUP:
Team: {info.Team}
Idea: {info.Idea}
Prototype: {info.Prototype}
Plan: {info.Plan}
Relationships: {info.Relationships}

⚙️ QUY TẮC:
✓ Chỉ trả JSON, không giải thích
✓ Điểm phải là số nguyên
✓ TotalScore = sum(Team+Idea+Prototype+Plan+Relationships)

JSON OUTPUT:
{{
    ""Team"": 0-20,
    ""Idea"": 0-20,
    ""Prototype"": 0-30,
    ""Plan"": 0-15,
    ""Relationships"": 0-15,
    ""TotalScore"": 0-100
}}
";

            var requestBody = new
            {
                contents = new[]
                {
                    new { parts = new[] { new { text = prompt } } }
                }
            };

            var content = new StringContent(JsonSerializer.Serialize(requestBody), Encoding.UTF8, "application/json");

            // Retry khi gặp 503
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

            // Loại bỏ markdown nếu có ```json```
            string cleanedJson = resultText.Replace("```json", "").Replace("```", "").Trim();

            // Deserialize JSON thành object điểm
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
            var url = $"https://generativelanguage.googleapis.com/v1beta/models/gemini-2.0-flash-exp:generateContent?key={apiKey}";

            string prompt = @"
Bạn là cố vấn startup kỳ cựu, chuyên sửa & viết lại profile startup để phục vụ pitch nhà đầu tư.

⚠️ NHIỆM VỤ:
Viết lại thông tin startup dưới đây để:
  ✓ Chuyên nghiệp, rõ ràng, thuyết phục hơn
  ✓ Có số liệu cụ thể (nếu có)
  ✓ Giữ nguyên thực chất, thêm context
  ✓ Phù hợp với nhà đầu tư Việt Nam & quốc tế
  ✓ Tránh từ quá generic, thêm USP (Unique Selling Point)

📋 THÔNG TIN HIỆN TẠI:
Team: " + info.Team + @"
Idea: " + info.Idea + @"
Prototype: " + info.Prototype + @"
Plan: " + info.Plan + @"
Relationships: " + info.Relationships + @"

⚙️ QUY TẮC:
✓ Chỉ trả JSON, không markdown, không giải thích
✓ 5 trường: Team, Idea, Prototype, Plan, Relationships
✓ Nếu input rỗng: output cũng rỗng
✓ Giữ length hợp lý (200-300 ký tự/trường)
✓ Tiếng Việt, chuyên ngành

JSON OUTPUT:
{
    ""Team"": ""...(đã cải thiện)"",
    ""Idea"": ""...(đã cải thiện)"",
    ""Prototype"": ""...(đã cải thiện)"",
    ""Plan"": ""...(đã cải thiện)"",
    ""Relationships"": ""...(đã cải thiện)""
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

            // Retry khi gặp 503
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
            var url = $"https://generativelanguage.googleapis.com/v1beta/models/gemini-2.0-flash-exp:generateContent?key={apiKey}";

            var filePath = Path.Combine(_environment.ContentRootPath, "Data", "DataSuggest.json");

            if (!System.IO.File.Exists(filePath))
            {
                return BadRequest("File DataSuggest.json không tồn tại: " + filePath);
            }

            var example = System.IO.File.ReadAllText(filePath);

            string prompt = $@"
Bạn là mentor startup, phân tích và đưa gợi ý cải thiện cho từng lĩnh vực.

📊 THÔNG TIN STARTUP:
Team: {(string.IsNullOrWhiteSpace(info.Team) ? "[THIẾU]" : info.Team)}
Idea: {(string.IsNullOrWhiteSpace(info.Idea) ? "[THIẾU]" : info.Idea)}
Prototype: {(string.IsNullOrWhiteSpace(info.Prototype) ? "[THIẾU]" : info.Prototype)}
Plan: {(string.IsNullOrWhiteSpace(info.Plan) ? "[THIẾU]" : info.Plan)}
Relationships: {(string.IsNullOrWhiteSpace(info.Relationships) ? "[THIẾU]" : info.Relationships)}

📌 VÍ DỤ:
{example}

⚙️ YÊU CẦU:
• Phân tích liên kết giữa các trường
• Đưa gợi ý cụ thể, khả thi
• Nếu thiếu thông tin → gợi ý bổ sung
• Nếu đã có → gợi ý cải thiện

GỢI Ý CHO 5 LĨNH VỰC:

1️⃣ Team: Phân tích kỹ năng hiện có, đề xuất vai trò cần bổ sung phù hợp với Idea/Prototype

2️⃣ Idea: Đánh giá khả thi, đề xuất cải tiến dựa trên Team/Market

3️⃣ Prototype: Gợi ý features và tech stack phù hợp với Team/Plan

4️⃣ Plan: Đề xuất roadmap và milestones dựa trên Prototype/Resources

5️⃣ Relationships: Gợi ý partners/investors cụ thể phù hợp với domain

JSON OUTPUT (chỉ trả JSON, không markdown):
{{
    ""Team"": ""gợi ý team (200-300 ký tự)"",
    ""Idea"": ""gợi ý idea (200-300 ký tự)"",
    ""Prototype"": ""gợi ý prototype (200-300 ký tự)"",
    ""Plan"": ""gợi ý plan (200-300 ký tự)"",
    ""Relationships"": ""gợi ý relationships (200-300 ký tự)""
}}
";

            var requestBody = new
            {
                contents = new[]
                {
                    new { parts = new[] { new { text = prompt } } }
                }
            };

            var content = new StringContent(JsonSerializer.Serialize(requestBody), Encoding.UTF8, "application/json");

            // Retry khi gặp 503
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
            catch (Exception ex)
            {
                return BadRequest(new { error = "Failed to parse Gemini response", details = ex.Message, raw = cleanedJson });
            }

            return Ok(new
            {
                original = info,
                suggestions
            });
        }
    }
}