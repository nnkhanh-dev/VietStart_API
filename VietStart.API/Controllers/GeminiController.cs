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

            string prompt = $@"
                Bạn là hệ thống chuẩn hóa dữ liệu Startup.
                Hãy phân tích mô tả của người dùng và xuất ra JSON gồm đúng 5 phần:

                - Team: đội ngũ của startup
                - Idea: ý tưởng cốt lõi
                - Prototype: các sản phẩm đã làm được hoặc link demo nếu có
                - Plan: kế hoạch phát triển tương lai
                - Relationships: các mối quan hệ, đối tác, nhà đầu tư

                ⚠️ BẮT BUỘC:
                - Chỉ trả về JSON.
                - Không kèm giải thích.
                - Không thêm trường khác.
                - Nếu không có thông tin, trả về chuỗi rỗng """" cho mục đó.

                Cấu trúc JSON:
                {{
                    ""Team"": ""..."",
                    ""Idea"": ""..."",
                    ""Prototype"": ""..."",
                    ""Plan"": ""..."",
                    ""Relationships"": ""...""
                }}

                Input: ""{clientAnswer}""
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
            var url = $"https://generativelanguage.googleapis.com/v1beta/models/gemini-2.5-flash:generateContent?key={apiKey}";

            // Prompt yêu cầu chấm điểm theo từng tiêu chí
            string prompt = $@"
            Bạn là chuyên gia đầu tư startup early-stage.
            Hãy chấm điểm startup theo các yếu tố sau (tổng 100 điểm):

            1. Team (20 điểm): 
               - Năng lực chuyên môn / kỹ thuật 15
               - Kỹ năng đa ngành / đa lĩnh vực 10
               - Đầu tư tâm huyết (fulltime/part-time) 10

            2. Ý tưởng (20 điểm):
               - Ý tưởng mới / đột phá 10
               - Khả thi 5
               - Quy mô thị trường tiềm năng 5

            3. Prototype / MVP (30 điểm):
               - Có MVP hoặc prototype 10
               - MVP thể hiện chức năng cốt lõi 10
               - Chạy được demo 10

            4. Kế hoạch triển khai / bán hàng (10 điểm):
               - Có người dùng thử 5
               - Có kế hoạch 6 tháng / 1 năm / 3 năm / 5 năm 5

            5. Quan hệ chiến lược (20 điểm):
               - Nằm trong mũi nhọn lĩnh vực được đầu tư 5
               - Hợp tác với cơ sở / doanh nghiệp 5

            ⚠️ BẮT BUỘC:
            - Chỉ trả về JSON với cấu trúc:
            {{
                ""Team"": int,
                ""Idea"": int,
                ""Prototype"": int,
                ""Plan"": int,
                ""Relationships"": int,
                ""TotalScore"": int
            }}
            - Không giải thích thêm.

            Thông tin startup: 
            Team: ""{info.Team}""
            Idea: ""{info.Idea}""
            Prototype: ""{info.Prototype}""
            Plan: ""{info.Plan}""
            Relationships: ""{info.Relationships}""
            ";

            var requestBody = new
            {
                contents = new[]
                {
            new { parts = new[] { new { text = prompt } } }
        }
            };

            var content = new StringContent(JsonSerializer.Serialize(requestBody), Encoding.UTF8, "application/json");

            // Gửi request
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
            var url = $"https://generativelanguage.googleapis.com/v1beta/models/gemini-2.5-flash:generateContent?key={apiKey}";

            string prompt = $@"
            Bạn là cố vấn chiến lược cho startup early-stage.
            Dựa trên thông tin startup được cung cấp, hãy đưa ra gợi ý cải thiện từng phần.
            Trả lại thông tin startup được cải thiện, chi tiết hơn và chuyên nghiệp hơn.

            ⚠️ BẮT BUỘC:
            - Chỉ trả về JSON với đúng 5 trường: Team, Idea, Prototype, Plan, Relationships
            - Cải thiện từng phần dựa trên input
            - Giữ nguyên ý chính nhưng viết chi tiết, rõ ràng và chuyên nghiệp hơn
            - Không thêm trường khác
            - Nếu phần nào quá sơ sài, hãy gợi ý cách cải thiện

            Thông tin startup hiện tại:
            Team: ""{info.Team}""
            Idea: ""{info.Idea}""
            Prototype: ""{info.Prototype}""
            Plan: ""{info.Plan}""
            Relationships: ""{info.Relationships}""

            Cấu trúc JSON trả về:
            {{
                ""Team"": ""...(đã cải thiện)"",
                ""Idea"": ""...(đã cải thiện)"",
                ""Prototype"": ""...(đã cải thiện)"",
                ""Plan"": ""...(đã cải thiện)"",
                ""Relationships"": ""...(đã cải thiện)""
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

            string prompt = $@"
            Bạn là cố vấn chiến lược kỳ cựu cho startup.
            Dựa trên thông tin startup được cung cấp, hãy đưa ra gợi ý cụ thể để startup phát triển mạnh mẽ hơn.

            ⚠️ BẮT BUỘC:
            - Chỉ trả về JSON với đúng 5 trường: Team, Idea, Prototype, Plan, Relationships
            - Mỗi gợi ý phải cụ thể, khả thi và có giá trị cao
            - Gợi ý nên tập trung vào cách cải thiện từng khía cạnh
            - Không thêm trường khác
            - Nếu thông tin còn thiếu, hãy gợi ý những yếu tố cần bổ sung

            Thông tin startup:
            Team: ""{info.Team}""
            Idea: ""{info.Idea}""
            Prototype: ""{info.Prototype}""
            Plan: ""{info.Plan}""
            Relationships: ""{info.Relationships}""

            Cấu trúc JSON trả về (gợi ý chi tiết):
            {{
                ""Team"": ""Gợi ý cải thiện đội ngũ (tuyển dụng kỹ năng nào, cơ cấu tối ưu, v.v.)"",
                ""Idea"": ""Gợi ý phát triển ý tưởng (mở rộng thị trường, xác định USP, v.v.)"",
                ""Prototype"": ""Gợi ý cải thiện sản phẩm (tính năng ưu tiên, tối ưu UX, v.v.)"",
                ""Plan"": ""Gợi ý kế hoạch (milestone, timeline, ngân sách, v.v.)"",
                ""Relationships"": ""Gợi ý tìm kiếm đối tác (loại đối tác, nơi tìm kiếm, v.v.)""
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
