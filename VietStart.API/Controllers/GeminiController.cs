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
            var url = $"https://generativelanguage.googleapis.com/v1beta/models/gemini-2.0-flash-exp:generateContent?key={apiKey}";

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

            string prompt = @"
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

📋 THÔNG TIN STARTUP:
Team: " + info.Team + @"
Idea: " + info.Idea + @"
Prototype: " + info.Prototype + @"
Plan: " + info.Plan + @"
Relationships: " + info.Relationships + @"

⚙️ QUY TẮC:
✓ Chỉ trả JSON, không giải thích
✓ Điểm phải là số nguyên (0-20, 0-30...)
✓ TotalScore = sum(Team+Idea+Prototype+Plan+Relationships)
✓ Nếu thiếu info: điểm 0 cho mục đó

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

            string prompt = @"
Bạn là mentor startup kỳ cựu, chuyên tư vấn chiến lược phát triển startup Việt Nam.

⚠️ NHIỆM VỤ:
Phân tích startup và đưa ra gợi ý cụ thể, khả thi để:
  • Tăng cơ hội nhận funding
  • Giải quyết bottleneck hiện tại
  • Accelerate growth
  • Xây dựng sustainable business
  • **ĐẶC BIỆT: Nếu trường nào THIẾU hoặc KHÔNG ĐỦ thông tin → Đưa ra gợi ý CỤ THỂ để bổ sung**

📊 THÔNG TIN STARTUP:
Team: " + (string.IsNullOrWhiteSpace(info.Team) ? "[THIẾU THÔNG TIN]" : info.Team) + @"
Idea: " + (string.IsNullOrWhiteSpace(info.Idea) ? "[THIẾU THÔNG TIN]" : info.Idea) + @"
Prototype: " + (string.IsNullOrWhiteSpace(info.Prototype) ? "[THIẾU THÔNG TIN]" : info.Prototype) + @"
Plan: " + (string.IsNullOrWhiteSpace(info.Plan) ? "[THIẾU THÔNG TIN]" : info.Plan) + @"
Relationships: " + (string.IsNullOrWhiteSpace(info.Relationships) ? "[THIẾU THÔNG TIN]" : info.Relationships) + @"

📝 GỢI Ý THEO 5 LĨNH VỰC:

**QUY TẮC QUAN TRỌNG:**
- Nếu trường có thông tin đầy đủ → Đưa ra gợi ý NÂNG CAO
- Nếu trường THIẾU hoặc MƠ HỒ → Đưa ra gợi ý BỔ SUNG CỤ THỂ với ví dụ minh họa

1️⃣ Team 🧑‍💼 Gợi ý:
  
  **Nếu THIẾU thông tin team:**
  • Liệt kê CỤ THỂ các vai trò cần thiết (VD: CEO với kinh nghiệm 5+ năm trong fintech, CTO biết Flutter/React Native, CMO có background marketing digital)
  • Gợi ý số lượng thành viên lý tưởng cho giai đoạn hiện tại
  • Đề xuất kênh tìm kiếm (TopDev, LinkedIn, sự kiện startup VN)
  
  **Nếu ĐÃ CÓ thông tin team:**
  • Đánh giá điểm mạnh/yếu
  • Gợi ý tuyển dụng vai trò còn thiếu
  • Đề xuất advisor phù hợp (ngành nào, tìm ở đâu)

2️⃣ Idea 💡 Gợi ý:
  
  **Nếu THIẾU thông tin idea:**
  • Gợi ý CỤ THỂ cách mô tả idea (Problem-Solution-Market Size)
  • Đưa ra ví dụ về USP (Unique Selling Point)
  • Gợi ý nghiên cứu competitors và phân tích điểm khác biệt
  
  **Nếu ĐÃ CÓ thông tin idea:**
  • Đề xuất mở rộng target market
  • Xác định rõ USP so với đối thủ
  • Gợi ý pivot hoặc optimize business model (B2B/B2C/B2B2C)

3️⃣ Prototype 🛠️ Gợi ý:
  
  **Nếu THIẾU thông tin prototype:**
  • Gợi ý CỤ THỂ các tính năng core cho MVP (liệt kê 3-5 features ưu tiên)
  • Đề xuất công nghệ phù hợp (tech stack: Frontend, Backend, Database)
  • Gợi ý timeline phát triển (VD: 2-3 tháng cho MVP đầu tiên)
  • Đề xuất cách demo sản phẩm (video, live demo, mockup)
  
  **Nếu ĐÃ CÓ prototype:**
  • Đề xuất tính năng tiếp theo cần phát triển
  • Gợi ý metrics đo lường (DAU, retention rate, NPS)
  • Tối ưu UX/UI dựa trên user feedback

4️⃣ Plan 📅 Gợi ý:
  
  **Nếu THIẾU thông tin plan:**
  • Gợi ý CỤ THỂ roadmap theo quý (Q1-Q4) với milestone cụ thể
  • Đề xuất KPIs đo lường (VD: 1000 users trong 3 tháng, $10K MRR sau 6 tháng)
  • Gợi ý thời điểm và số tiền fundraising (VD: Pre-seed $50K-100K sau 6 tháng)
  • Đề xuất go-to-market strategy
  
  **Nếu ĐÃ CÓ plan:**
  • Tối ưu timeline và milestone
  • Đề xuất revenue targets cụ thể
  • Gợi ý chiến lược fundraising (loại investor, số tiền, thời điểm)

5️⃣ Relationships 🤝 Gợi ý:
  
  **Nếu THIẾU thông tin relationships:**
  • Liệt kê CỤ THỂ các loại đối tác cần tìm (VD: payment gateway như Momo/VNPay, logistics như GHN/GHTK)
  • Gợi ý tên các investors/funds phù hợp (VD: 500 Startups Vietnam, Touchstone Partners, Do Ventures)
  • Đề xuất accelerator programs (VD: Topica Founder Institute, VinTech City, VIISA)
  • Gợi ý cách networking (sự kiện nào, group nào)
  
  **Nếu ĐÃ CÓ relationships:**
  • Đánh giá chất lượng partnerships hiện tại
  • Đề xuất mở rộng ecosystem
  • Gợi ý strategic partnerships mới

⚙️ QUY TẮC OUTPUT:
• Chỉ trả về JSON, không markdown, không giải thích
• Gợi ý phải: Specific, Actionable, Measurable, CÓ VÍ DỤ CỤ THỂ
• Nếu trường thiếu info → Gợi ý BỔ SUNG chi tiết với ví dụ
• Nếu trường đã có info → Gợi ý NÂNG CAO
• Length: 200-400 ký tự/trường (dài hơn nếu cần thiết để đưa ví dụ)
• Tiếng Việt, ngôn ngữ mentor, thân thiện nhưng chuyên nghiệp

JSON OUTPUT:
{
    ""Team"": ""Gợi ý cụ thể cho team...(nếu thiếu: vai trò gì, kỹ năng gì, tìm ở đâu + ví dụ; nếu đã có: đánh giá và gợi ý nâng cao)"",
    ""Idea"": ""Gợi ý phát triển idea...(nếu thiếu: cách mô tả, ví dụ USP; nếu đã có: market mới, pivot strategy)"",
    ""Prototype"": ""Gợi ý cải thiện sản phẩm...(nếu thiếu: features MVP, tech stack, timeline + ví dụ; nếu đã có: features tiếp theo, metrics)"",
    ""Plan"": ""Gợi ý kế hoạch...(nếu thiếu: roadmap Q1-Q4, KPIs cụ thể, fundraising timeline; nếu đã có: tối ưu milestone, revenue target)"",
    ""Relationships"": ""Gợi ý tìm partner...(nếu thiếu: loại partner cụ thể + tên, investors/funds cụ thể, accelerators + ví dụ; nếu đã có: mở rộng ecosystem)""
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