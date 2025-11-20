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

        [HttpPost("format")]
        [Authorize(Roles = "Client")]
        public async Task<IActionResult> FormatInput([FromBody] string clientAnswer)
        {
            if (string.IsNullOrWhiteSpace(clientAnswer))
                return BadRequest("clientAnswer cannot be empty.");

            string apiKey = _configuration["Gemini:Key"];
            var url = $"https://generativelanguage.googleapis.com/v1beta/models/gemini-2.5-flash-lite:generateContent?key={apiKey}";


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
            var url = $"https://generativelanguage.googleapis.com/v1beta/models/gemini-2.5-flash-lite:generateContent?key={apiKey}";

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

[
  {{
    ""startupId"": ""VS-001"",
    ""Team"": ""1. Nguyễn Văn Vỹ - Kỹ sư phần mềm, 5 năm kinh nghiệm\n2. Phạm Anh Thư - Chuyên gia marketing, 4 năm kinh nghiệm"",
    ""Idea"": ""Trợ lý ảo (AI) chuyên ngành luật - sử dụng GPT-4 để tư vấn pháp lý cho cá nhân và doanh nghiệp nhỏ"",
    ""Prototype"": ""Link demo: https://lawbot-demo.com - Chatbot đã có khả năng trả lời 100+ câu hỏi pháp lý phổ biến"",
    ""Plan"": ""Q1: Phát triển MVP, Q2: Test 50 users, Q3: Ra mắt chính thức, Q4: Mở rộng 3 tỉnh"",
    ""Relationship"": ""Công ty Luật ABC - đối tác nội dung, Đại học Luật TP.HCM - cố vấn"",
    ""TeamPoint"": ""18/20"",
    ""IdeaPoint"": ""18/20"",
    ""PrototypePoint"": ""28/30"",
    ""PlanPoint"": ""9/10"",
    ""RelationshipPoint"": ""18/20""
  }},
  {{
    ""startupId"": ""VS-002"",
    ""Team"": ""1. Trần Minh - Dev\n2. x y z"",
    ""Idea"": ""Tư vấn bảo mật mạng / cybersecurity cho SME"",
    ""Prototype"": """",
    ""Plan"": ""Làm xong rồi bán"",
    ""Relationship"": """",
    ""TeamPoint"": ""8/20"",
    ""IdeaPoint"": ""12/20"",
    ""PrototypePoint"": ""0/30"",
    ""PlanPoint"": ""2/10"",
    ""RelationshipPoint"": ""0/20""
  }},
  {{
    ""startupId"": ""VS-003"",
    ""Team"": """",
    ""Idea"": ""Dịch vụ chữa bệnh truyền hình (telemedicine) kết nối bác sĩ và bệnh nhân qua video call"",
    ""Prototype"": ""App mobile prototype trên Figma với đầy đủ flow: đăng ký, tìm bác sĩ, đặt lịch, video call"",
    ""Plan"": """",
    ""Relationship"": ""Bệnh viện Đa khoa Medlatec - đối tác bác sĩ"",
    ""TeamPoint"": ""0/20"",
    ""IdeaPoint"": ""16/20"",
    ""PrototypePoint"": ""22/30"",
    ""PlanPoint"": ""0/10"",
    ""RelationshipPoint"": ""15/20""
  }},
  {{
    ""startupId"": ""VS-004"",
    ""Team"": ""abc xyz 123"",
    ""Idea"": ""asdfghjkl qwertyuiop"",
    ""Prototype"": ""zxcvbnm"",
    ""Plan"": ""!@#$%^&*()"",
    ""Relationship"": ""aaaaa bbbbb ccccc"",
    ""TeamPoint"": ""0/20"",
    ""IdeaPoint"": ""0/20"",
    ""PrototypePoint"": ""0/30"",
    ""PlanPoint"": ""0/10"",
    ""RelationshipPoint"": ""0/20""
  }},
  {{
    ""startupId"": ""VS-005"",
    ""Team"": """",
    ""Idea"": ""Cho thuê văn phòng ảo (virtual office) với địa chỉ đăng ký doanh nghiệp, nhận thư, phòng họp theo giờ"",
    ""Prototype"": """",
    ""Plan"": """",
    ""Relationship"": """",
    ""TeamPoint"": ""0/20"",
    ""IdeaPoint"": ""14/20"",
    ""PrototypePoint"": ""0/30"",
    ""PlanPoint"": ""0/10"",
    ""RelationshipPoint"": ""0/20""
  }},
  {{
    ""startupId"": ""VS-006"",
    ""Team"": ""1. Lê Văn Hùng - Kỹ sư cơ khí, 3 năm\n2. Phạm Thu Hà - Marketing, 2 năm\n3. Đỗ Minh Tuấn - Pilot drone chuyên nghiệp"",
    ""Idea"": ""Dịch vụ giao hàng bằng drone cho hàng hoá nhỏ trong nội thành, giao trong 30 phút"",
    ""Prototype"": ""Video demo drone giao hàng thành công 5 lần, phạm vi 3km. Link: https://youtube.com/demo-drone"",
    ""Plan"": ""Tháng 1-3: Test pilot tại quận 1. Tháng 4-6: Mở rộng 5 quận. Tháng 7-12: Scale toàn TP.HCM. Dự kiến 200 đơn/ngày cuối năm"",
    ""Relationship"": ""Sở GTVT TP.HCM - đang làm việc về giấy phép bay. Viettel Post - đối tác logistics"",
    ""TeamPoint"": ""17/20"",
    ""IdeaPoint"": ""17/20"",
    ""PrototypePoint"": ""26/30"",
    ""PlanPoint"": ""9/10"",
    ""RelationshipPoint"": ""17/20""
  }},
  {{
    ""startupId"": ""VS-007"",
    ""Team"": ""Nguyễn A"",
    ""Idea"": ""In 3D sản phẩm tuỳ chỉnh (custom items)"",
    ""Prototype"": """",
    ""Plan"": """",
    ""Relationship"": """",
    ""TeamPoint"": ""4/20"",
    ""IdeaPoint"": ""13/20"",
    ""PrototypePoint"": ""0/30"",
    ""PlanPoint"": ""0/10"",
    ""RelationshipPoint"": ""0/20""
  }},
  {{
    ""startupId"": ""VS-008"",
    ""Team"": ""1. Trần Thị Mai - Du lịch học, 6 năm làm travel agent\n2. Hoàng Văn Bình - Kỹ sư môi trường"",
    ""Idea"": ""Du lịch xanh (sustainable tourism) - tour du lịch sinh thái, không rác thải, hỗ trợ cộng đồng địa phương"",
    ""Prototype"": ""Website booking với 3 tour pilot: Sapa, Phú Quốc, Đà Lạt. Đã có 20 khách hàng đầu tiên"",
    ""Plan"": ""Q1 2024: Launch 5 tour mới. Q2: Hợp tác 10 homestay địa phương. Q3-Q4: Mở rộng Miền Trung"",
    ""Relationship"": """",
    ""TeamPoint"": ""15/20"",
    ""IdeaPoint"": ""16/20"",
    ""PrototypePoint"": ""24/30"",
    ""PlanPoint"": ""8/10"",
    ""RelationshipPoint"": ""0/20""
  }},
  {{
    ""startupId"": ""VS-009"",
    ""Team"": """",
    ""Idea"": ""Cửa hàng đồ tái sử dụng / zero-waste bán sản phẩm thân thiện môi trường, không bao bì nhựa"",
    ""Prototype"": """",
    ""Plan"": """",
    ""Relationship"": """",
    ""TeamPoint"": ""0/20"",
    ""IdeaPoint"": ""13/20"",
    ""PrototypePoint"": ""0/30"",
    ""PlanPoint"": ""0/10"",
    ""RelationshipPoint"": ""0/20""
  }},
  {{
    ""startupId"": ""VS-010"",
    ""Team"": ""1. Ngô Thị Lan - Kiến trúc sư nội thất\n2. Phan Văn Đức - Thợ mộc 10 năm kinh nghiệm"",
    ""Idea"": ""Cửa hàng nội thất bền vững, sinh thái sử dụng gỗ tái chế và vật liệu thân thiện môi trường"",
    ""Prototype"": ""Showroom nhỏ với 15 mẫu sản phẩm. Instagram @eco.furniture có 2000 followers"",
    ""Plan"": """",
    ""Relationship"": ""Công ty Gỗ Việt - cung cấp nguyên liệu tái chế"",
    ""TeamPoint"": ""14/20"",
    ""IdeaPoint"": ""15/20"",
    ""PrototypePoint"": ""20/30"",
    ""PlanPoint"": ""0/10"",
    ""RelationshipPoint"": ""12/20""
  }},
  {{
    ""startupId"": ""VS-011"",
    ""Team"": """",
    ""Idea"": ""Dịch vụ thu gom & tái chế rác tại hộ gia đình, phân loại rác tận nhà"",
    ""Prototype"": """",
    ""Plan"": """",
    ""Relationship"": """",
    ""TeamPoint"": ""0/20"",
    ""IdeaPoint"": ""14/20"",
    ""PrototypePoint"": ""0/30"",
    ""PlanPoint"": ""0/10"",
    ""RelationshipPoint"": ""0/20""
  }},
  {{
    ""startupId"": ""VS-012"",
    ""Team"": ""1. Lý Minh Tâm - Kỹ sư vật liệu, 4 năm R&D\n2. Vũ Thu Hương - Kinh doanh, 3 năm"",
    ""Idea"": ""Sản xuất bao bì có thể tái sử dụng từ sợi tre và tinh bột sắn, thay thế túi ni-lông"",
    ""Prototype"": ""Mẫu thử nghiệm 3 loại túi đã qua test độ bền. Có video demo phân hủy sinh học"",
    ""Plan"": ""Tháng 1-2: Hoàn thiện công thức. Tháng 3-4: Tìm nhà máy sản xuất. Tháng 5-12: Bán B2B cho siêu thị"",
    ""Relationship"": ""Siêu thị Co.opMart - quan tâm pilot 1000 túi"",
    ""TeamPoint"": ""15/20"",
    ""IdeaPoint"": ""17/20"",
    ""PrototypePoint"": ""23/30"",
    ""PlanPoint"": ""8/10"",
    ""RelationshipPoint"": ""14/20""
  }},
  {{
    ""startupId"": ""VS-013"",
    ""Team"": ""Một người nào đó"",
    ""Idea"": ""Trạm sạc xe điện nhỏ / địa phương"",
    ""Prototype"": ""Có ý tưởng thôi"",
    ""Plan"": ""Chưa biết làm gì"",
    ""Relationship"": """",
    ""TeamPoint"": ""2/20"",
    ""IdeaPoint"": ""11/20"",
    ""PrototypePoint"": ""0/30"",
    ""PlanPoint"": ""1/10"",
    ""RelationshipPoint"": ""0/20""
  }},
  {{
    ""startupId"": ""VS-014"",
    ""Team"": """",
    ""Idea"": ""Huấn luyện viên thể hình trực tuyến (online fitness) với chương trình tập cá nhân hóa qua app"",
    ""Prototype"": """",
    ""Plan"": """",
    ""Relationship"": """",
    ""TeamPoint"": ""0/20"",
    ""IdeaPoint"": ""14/20"",
    ""PrototypePoint"": ""0/30"",
    ""PlanPoint"": ""0/10"",
    ""RelationshipPoint"": ""0/20""
  }},
  {{
    ""startupId"": ""VS-015"",
    ""Team"": ""1. Đinh Thị Ngọc - Chuyên gia dinh dưỡng, 5 năm\n2. Trương Văn An - Developer iOS"",
    ""Idea"": ""Tư vấn dinh dưỡng / chế độ ăn cá nhân hóa dựa trên AI phân tích chỉ số sức khỏe"",
    ""Prototype"": ""App MVP trên iOS với tính năng: nhập thông tin, AI gợi ý thực đơn. 50 beta users"",
    ""Plan"": ""Q1: Android version. Q2: Tích hợp với thiết bị đeo. Q3-Q4: Hợp tác phòng gym"",
    ""Relationship"": ""Phòng khám dinh dưỡng Dr. Nutrition - cố vấn chuyên môn"",
    ""TeamPoint"": ""16/20"",
    ""IdeaPoint"": ""17/20"",
    ""PrototypePoint"": ""25/30"",
    ""PlanPoint"": ""8/10"",
    ""RelationshipPoint"": ""13/20""
  }},
  {{
    ""startupId"": ""VS-016"",
    ""Team"": ""aaa bbb ccc"",
    ""Idea"": """",
    ""Prototype"": """",
    ""Plan"": """",
    ""Relationship"": """",
    ""TeamPoint"": ""0/20"",
    ""IdeaPoint"": ""0/20"",
    ""PrototypePoint"": ""0/30"",
    ""PlanPoint"": ""0/10"",
    ""RelationshipPoint"": ""0/20""
  }},
  {{
    ""startupId"": ""VS-017"",
    ""Team"": ""1. Hồ Thị Linh - Dược sĩ chuyên mỹ phẩm\n2. Bùi Văn Khoa - Marketing digital"",
    ""Idea"": ""Bán mỹ phẩm tự nhiên / hữu cơ từ nguyên liệu Việt Nam, không paraben, không hóa chất độc hại"",
    ""Prototype"": ""3 sản phẩm đầu tiên: sữa rửa mặt, kem dưỡng, serum. Website bán hàng đã có 100 đơn"",
    ""Plan"": ""2024: Ra mắt 5 sản phẩm mới. Mở 2 cửa hàng offline. Doanh thu mục tiêu 500 triệu"",
    ""Relationship"": """",
    ""TeamPoint"": ""14/20"",
    ""IdeaPoint"": ""15/20"",
    ""PrototypePoint"": ""22/30"",
    ""PlanPoint"": ""7/10"",
    ""RelationshipPoint"": ""0/20""
  }},
  {{
    ""startupId"": ""VS-018"",
    ""Team"": """",
    ""Idea"": ""Phát triển phần mềm giáo dục cho trẻ em dạy toán, tiếng Anh qua game tương tác"",
    ""Prototype"": """",
    ""Plan"": """",
    ""Relationship"": """",
    ""TeamPoint"": ""0/20"",
    ""IdeaPoint"": ""15/20"",
    ""PrototypePoint"": ""0/30"",
    ""PlanPoint"": ""0/10"",
    ""RelationshipPoint"": ""0/20""
  }},
  {{
    ""startupId"": ""VS-019"",
    ""Team"": ""1. Mai Văn Tùng - Giảng viên đại học, 8 năm\n2. Lê Thị Hồng - Content creator"",
    ""Idea"": ""Tạo khóa học trực tuyến (e-learning) về kỹ năng mềm, lập trình, marketing trên nền tảng riêng"",
    ""Prototype"": ""Website với 2 khóa học pilot đã có 200 học viên đăng ký, rating 4.5/5"",
    ""Plan"": ""6 tháng đầu: Tạo 10 khóa học mới. 6 tháng sau: Marketing ads, target 5000 học viên"",
    ""Relationship"": """",
    ""TeamPoint"": ""16/20"",
    ""IdeaPoint"": ""16/20"",
    ""PrototypePoint"": ""24/30"",
    ""PlanPoint"": ""7/10"",
    ""RelationshipPoint"": ""0/20""
  }},
  {{
    ""startupId"": ""VS-020"",
    ""Team"": ""123"",
    ""Idea"": ""Kinh doanh sách điện tử / eBook"",
    ""Prototype"": ""456"",
    ""Plan"": ""789"",
    ""Relationship"": ""000"",
    ""TeamPoint"": ""0/20"",
    ""IdeaPoint"": ""11/20"",
    ""PrototypePoint"": ""0/30"",
    ""PlanPoint"": ""0/10"",
    ""RelationshipPoint"": ""0/20""
  }},
  {{
    ""startupId"": ""VS-021"",
    ""Team"": ""1. Phạm Văn Dũng - Designer 4 năm"",
    ""Idea"": ""Kinh doanh in theo yêu cầu (Print-on-Demand) áo thun, mug, túi vải với design độc đáo"",
    ""Prototype"": ""Shop Shopee có 500 sản phẩm, đã bán 200 đơn trong 2 tháng"",
    ""Plan"": ""Mở rộng sang Lazada, Tiki. Thuê 1 designer thêm. Target 100 đơn/tháng"",
    ""Relationship"": ""Xưởng in Minh Anh - đối tác sản xuất"",
    ""TeamPoint"": ""10/20"",
    ""IdeaPoint"": ""14/20"",
    ""PrototypePoint"": ""20/30"",
    ""PlanPoint"": ""6/10"",
    ""RelationshipPoint"": ""11/20""
  }},
  {{
    ""startupId"": ""VS-022"",
    ""Team"": """",
    ""Idea"": ""Kênh YouTube chuyên một ngách (ví dụ kỹ thuật, DIY, giáo dục) về sửa chữa điện tử"",
    ""Prototype"": """",
    ""Plan"": """",
    ""Relationship"": """",
    ""TeamPoint"": ""0/20"",
    ""IdeaPoint"": ""13/20"",
    ""PrototypePoint"": ""0/30"",
    ""PlanPoint"": ""0/10"",
    ""RelationshipPoint"": ""0/20""
  }},
  {{
    ""startupId"": ""VS-023"",
    ""Team"": ""1. Trần Thị Kim - Blogger, 3 năm kinh nghiệm affiliate"",
    ""Idea"": ""Affiliate marketing (tiếp thị liên kết) website review sản phẩm công nghệ, kiếm tiền từ hoa hồng"",
    ""Prototype"": ""Website với 50 bài review, traffic 5000 visit/tháng, thu nhập 10 triệu/tháng"",
    ""Plan"": ""Tăng content lên 100 bài. SEO optimization. Target 20 triệu/tháng sau 6 tháng"",
    ""Relationship"": """",
    ""TeamPoint"": ""11/20"",
    ""IdeaPoint"": ""13/20"",
    ""PrototypePoint"": ""21/30"",
    ""PlanPoint"": ""6/10"",
    ""RelationshipPoint"": ""0/20""
  }},
  {{
    ""startupId"": ""VS-024"",
    ""Team"": """",
    ""Idea"": ""Dịch vụ trợ lý ảo (virtual assistant) hỗ trợ doanh nghiệp nhỏ làm admin, email, lịch họp"",
    ""Prototype"": ""Portfolio với 3 khách hàng hiện tại"",
    ""Plan"": """",
    ""Relationship"": """",
    ""TeamPoint"": ""0/20"",
    ""IdeaPoint"": ""14/20"",
    ""PrototypePoint"": ""15/30"",
    ""PlanPoint"": ""0/10"",
    ""RelationshipPoint"": ""0/20""
  }},
  {{
    ""startupId"": ""VS-025"",
    ""Team"": """",
    ""Idea"": ""Viết content / copywriting tự do cho website, quảng cáo, mạng xã hội"",
    ""Prototype"": """",
    ""Plan"": """",
    ""Relationship"": """",
    ""TeamPoint"": ""0/20"",
    ""IdeaPoint"": ""12/20"",
    ""PrototypePoint"": ""0/30"",
    ""PlanPoint"": ""0/10"",
    ""RelationshipPoint"": ""0/20""
  }},
  {{
    ""startupId"": ""VS-026"",
    ""Team"": ""1. Lê Thị Oanh - Biên tập viên 6 năm"",
    ""Idea"": ""Dịch vụ chỉnh sửa, biên tập nội dung (proofreading) tiếng Anh và tiếng Việt cho doanh nghiệp"",
    ""Prototype"": ""Website giới thiệu dịch vụ, đã làm cho 10 khách hàng, testimonials tốt"",
    ""Plan"": ""Thuê thêm 2 editor. Đẩy mạnh marketing LinkedIn. Target 30 khách/tháng"",
    ""Relationship"": """",
    ""TeamPoint"": ""12/20"",
    ""IdeaPoint"": ""14/20"",
    ""PrototypePoint"": ""18/30"",
    ""PlanPoint"": ""6/10"",
    ""RelationshipPoint"": ""0/20""
  }},
  {{
    ""startupId"": ""VS-027"",
    ""Team"": ""xxxxxxx"",
    ""Idea"": ""Dịch vụ ghi âm lồng tiếng (voice-over)"",
    ""Prototype"": """",
    ""Plan"": ""yyyyyyy"",
    ""Relationship"": """",
    ""TeamPoint"": ""0/20"",
    ""IdeaPoint"": ""12/20"",
    ""PrototypePoint"": ""0/30"",
    ""PlanPoint"": ""0/10"",
    ""RelationshipPoint"": ""0/20""
  }},
  {{
    ""startupId"": ""VS-028"",
    ""Team"": ""1. Đặng Văn Hải - Voice artist 5 năm"",
    ""Idea"": ""Dịch vụ chuyển lời nói thành văn bản (transcription) cho video, podcast, phỏng vấn"",
    ""Prototype"": ""Đã transcribe 50 video, có 5 khách hàng thường xuyên"",
    ""Plan"": """",
    ""Relationship"": """",
    ""TeamPoint"": ""11/20"",
    ""IdeaPoint"": ""13/20"",
    ""PrototypePoint"": ""16/30"",
    ""PlanPoint"": ""0/10"",
    ""RelationshipPoint"": ""0/20""
  }},
  {{
    ""startupId"": ""VS-029"",
    ""Team"": """",
    ""Idea"": ""Thiết kế đồ hoạ freelance cho logo, branding, social media content"",
    ""Prototype"": """",
    ""Plan"": """",
    ""Relationship"": """",
    ""TeamPoint"": ""0/20"",
    ""IdeaPoint"": ""12/20"",
    ""PrototypePoint"": ""0/30"",
    ""PlanPoint"": ""0/10"",
    ""RelationshipPoint"": ""0/20""
  }},
  {{
    ""startupId"": ""VS-030"",
    ""Team"": ""1. Ngô Thị Mai - Graphic designer 3 năm"",
    ""Idea"": ""Canva template market (bán template thiết kế) cho presentation, social media, resume"",
    ""Prototype"": ""Đã upload 30 templates lên Creative Market, bán được 50 bản"",
    ""Plan"": ""Tạo 100 templates trong 3 tháng. Mở rộng sang Etsy. Passive income 500 USD/tháng"",
    ""Relationship"": """",
    ""TeamPoint"": ""12/20"",
    ""IdeaPoint"": ""14/20"",
    ""PrototypePoint"": ""19/30"",
    ""PlanPoint"": ""7/10"",
    ""RelationshipPoint"": ""0/20""
  }},
  {{
    ""startupId"": ""VS-031"",
    ""Team"": ""1. Vũ Văn Thành - Kỹ sư điện, 7 năm\n2. Hoàng Thị Lan - Tài chính, 4 năm"",
    ""Idea"": ""Tư vấn năng lượng tái tạo cho gia đình / doanh nghiệp nhỏ, lắp đặt điện mặt trời"",
    ""Prototype"": ""Đã lắp 5 hệ thống điện mặt trời, khách hàng hài lòng. Portfolio photos"",
    ""Plan"": ""Năm 2024: Lắp 50 hệ thống. Thuê 2 kỹ thuật viên. Doanh thu 2 tỷ"",
    ""Relationship"": ""Công ty Điện Năng Lượng Xanh - nhà phân phối thiết bị"",
    ""TeamPoint"": ""16/20"",
    ""IdeaPoint"": ""16/20"",
    ""PrototypePoint"": ""21/30"",
    ""PlanPoint"": ""8/10"",
    ""RelationshipPoint"": ""13/20""
  }},
  {{
    ""startupId"": ""VS-032"",
    ""Team"": """",
    ""Idea"": ""Nền tảng kết nối nông dân & nhà hàng (nông sản bền vững)"",
    ""Prototype"": """",
    ""Plan"": """",
    ""Relationship"": """",
    ""TeamPoint"": ""0/20"",
    ""IdeaPoint"": ""15/20"",
    ""PrototypePoint"": ""0/30"",
    ""PlanPoint"": ""0/10"",
    ""RelationshipPoint"": ""0/20""
  }},
  {{
    ""startupId"": ""VS-033"",
    ""Team"": ""1. Phan Văn Long - Kỹ sư nông nghiệp\n2. Đinh Thị Hoa - Data scientist"",
    ""Idea"": ""Ứng dụng dự báo mùa vụ bằng AI (nông nghiệp công nghệ) phân tích thời tiết, giá cả, tư vấn nông dân"",
    ""Prototype"": ""App prototype với model AI dự báo thời tiết và giá lúa, độ chính xác 75%"",
    ""Plan"": ""Q1: Cải thiện model lên 85%. Q2: Test 100 nông dân. Q3-Q4: Scale 5 tỉnh đồng bằng"",
    ""Relationship"": ""Sở Nông nghiệp Long An - đối tác dữ liệu"",
    ""TeamPoint"": ""16/20"",
    ""IdeaPoint"": ""18/20"",
    ""PrototypePoint"": ""25/30"",
    ""PlanPoint"": ""9/10"",
    ""RelationshipPoint"": ""14/20""
  }},
  {{
    ""startupId"": ""VS-034"",
    ""Team"": ""xyz"",
    ""Idea"": ""Dịch vụ chatbot AI cho website nhỏ"",
    ""Prototype"": """",
    ""Plan"": """",
    ""Relationship"": """",
    ""TeamPoint"": ""0/20"",
    ""IdeaPoint"": ""13/20"",
    ""PrototypePoint"": ""0/30"",
    ""PlanPoint"": ""0/10"",
    ""RelationshipPoint"": ""0/20""
  }},
  {{
    ""startupId"": ""VS-035"",
    ""Team"": """",
    ""Idea"": ""Ứng dụng quản lý chi tiêu cá nhân bằng AI, tự động phân loại và gợi ý tiết kiệm"",
    ""Prototype"": """",
    ""Plan"": """",
    ""Relationship"": """",
    ""TeamPoint"": ""0/20"",
    ""IdeaPoint"": ""14/20"",
    ""PrototypePoint"": ""0/30"",
    ""PlanPoint"": ""0/10"",
    ""RelationshipPoint"": ""0/20""
  }},
  {{
    ""startupId"": ""VS-036"",
    ""Team"": ""1. Lý Văn Minh - Giáo viên, 5 năm\n2. Trần Thị Thu - UX designer"",
    ""Idea"": ""Nền tảng micro-learning (học ngắn, nhỏ mỗi ngày) với bài học 5-10 phút về kỹ năng sống"",
    ""Prototype"": ""App có 50 bài học, 300 users active, retention rate 60% sau 1 tháng"",
    ""Plan"": ""Tăng nội dung lên 200 bài. Gamification với streak & badges. Target 5000 users năm 2024"",
    ""Relationship"": """",
    ""TeamPoint"": ""15/20"",
    ""IdeaPoint"": ""16/20"",
    ""PrototypePoint"": ""23/30"",
    ""PlanPoint"": ""8/10"",
    ""RelationshipPoint"": ""0/20""
  }},
  {{
    ""startupId"": ""VS-037"",
    ""Team"": ""1. Nguyễn Thị Dung - Podcaster 2 năm"",
    ""Idea"": ""Dịch vụ podcast chuyên niên nhỏ (niche podcast) về sách kinh doanh, mỗi tuần 1 tập"",
    ""Prototype"": ""Đã phát hành 10 tập trên Spotify, mỗi tập 500-1000 lượt nghe"",
    ""Plan"": ""52 tập trong năm. Tìm sponsor. Kiếm tiền từ ads & affiliate"",
    ""Relationship"": """",
    ""TeamPoint"": ""10/20"",
    ""IdeaPoint"": ""13/20"",
    ""PrototypePoint"": ""17/30"",
    ""PlanPoint"": ""6/10"",
    ""RelationshipPoint"": ""0/20""
  }},
  {{
    ""startupId"": ""VS-038"",
    ""Team"": """",
    ""Idea"": ""Cửa hàng quần áo second-hand online chuyên đồ hiệu chất lượng cao"",
    ""Prototype"": """",
    ""Plan"": """",
    ""Relationship"": """",
    ""TeamPoint"": ""0/20"",
    ""IdeaPoint"": ""13/20"",
    ""PrototypePoint"": ""0/30"",
    ""PlanPoint"": ""0/10"",
    ""RelationshipPoint"": ""0/20""
  }},
  {{
    ""startupId"": ""VS-039"",
    ""Team"": ""1. Hà Thị Ngọc - Thời trang, 3 năm\n2. Phạm Văn Tuấn - Logistics"",
    ""Idea"": ""Dịch vụ thu gom quần áo cũ + tái chế thời trang, biến đồ cũ thành sản phẩm mới"",
    ""Prototype"": ""Đã thu gom 500kg quần áo, tái chế thành 50 sản phẩm túi xách và gấu bông"",
    ""Plan"": ""Mở 3 điểm thu gom. Hợp tác trường học. Target 2 tấn quần áo/tháng"",
    ""Relationship"": ""Trường THPT Lê Quý Đôn - điểm thu gom"",
    ""TeamPoint"": ""14/20"",
    ""IdeaPoint"": ""16/20"",
    ""PrototypePoint"": ""21/30"",
    ""PlanPoint"": ""7/10"",
    ""RelationshipPoint"": ""11/20""
  }},
  {{
    ""startupId"": ""VS-040"",
    ""Team"": """",
    ""Idea"": ""Ứng dụng chia sẻ sách / đồ dùng học tập giữa sinh viên trong khu vực"",
    ""Prototype"": """",
    ""Plan"": """",
    ""Relationship"": """",
    ""TeamPoint"": ""0/20"",
    ""IdeaPoint"": ""14/20"",
    ""PrototypePoint"": ""0/30"",
    ""PlanPoint"": ""0/10"",
    ""RelationshipPoint"": ""0/20""
  }},
  {{
    ""startupId"": ""VS-041"",
    ""Team"": ""1. Bùi Văn Hùng - Research analyst 4 năm"",
    ""Idea"": ""Nền tảng nghiên cứu thị trường nhỏ cho startup mới, khảo sát và phân tích nhanh với giá rẻ"",
    ""Prototype"": ""Đã làm 3 dự án market research cho startup, báo cáo chuyên nghiệp"",
    ""Plan"": ""Marketing qua startup community. Target 10 project/quý. Giá 5-10 triệu/project"",
    ""Relationship"": """",
    ""TeamPoint"": ""12/20"",
    ""IdeaPoint"": ""15/20"",
    ""PrototypePoint"": ""19/30"",
    ""PlanPoint"": ""7/10"",
    ""RelationshipPoint"": ""0/20""
  }},
  {{
    ""startupId"": ""VS-042"",
    ""Team"": """",
    ""Idea"": ""Ứng dụng tìm việc freelance vi mô (micro freelance) cho công việc nhỏ, nhanh, linh hoạt"",
    ""Prototype"": """",
    ""Plan"": """",
    ""Relationship"": """",
    ""TeamPoint"": ""0/20"",
    ""IdeaPoint"": ""14/20"",
    ""PrototypePoint"": ""0/30"",
    ""PlanPoint"": ""0/10"",
    ""RelationshipPoint"": ""0/20""
  }},
  {{
    ""startupId"": ""VS-043"",
    ""Team"": ""1. Lê Thị Hương - Tư vấn tài chính 5 năm"",
    ""Idea"": ""Dịch vụ tư vấn tài chính cá nhân cho Gen Z: tiết kiệm, đầu tư, quản lý nợ"",
    ""Prototype"": ""Trang TikTok 10k followers chia sẻ tips tài chính, đã tư vấn 1-1 cho 15 khách"",
    ""Plan"": ""Launch khóa học online. Tạo ebook. Target 100 khách tư vấn/năm"",
    ""Relationship"": """",
    ""TeamPoint"": ""13/20"",
    ""IdeaPoint"": ""15/20"",
    ""PrototypePoint"": ""18/30"",
    ""PlanPoint"": ""6/10"",
    ""RelationshipPoint"": ""0/20""
  }},
  {{
    ""startupId"": ""VS-044"",
    ""Team"": ""1. Trần Văn Đức - Marketing 3 năm"",
    ""Idea"": ""Dịch vụ quản lý mạng xã hội cho micro business: đăng bài, tương tác, phân tích"",
    ""Prototype"": ""Đang quản lý Facebook/Instagram cho 5 shop nhỏ, content calendar template"",
    ""Plan"": ""Tìm 10 khách hàng mới. Thuê 1 designer. Package 3 triệu/tháng/khách"",
    ""Relationship"": """",
    ""TeamPoint"": ""11/20"",
    ""IdeaPoint"": ""14/20"",
    ""PrototypePoint"": ""17/30"",
    ""PlanPoint"": ""7/10"",
    ""RelationshipPoint"": ""0/20""
  }},
  {{
    ""startupId"": ""VS-045"",
    ""Team"": """",
    ""Idea"": ""Ứng dụng booking trải nghiệm du lịch địa phương (local experiences) kết nối du khách với người dân"",
    ""Prototype"": """",
    ""Plan"": """",
    ""Relationship"": """",
    ""TeamPoint"": ""0/20"",
    ""IdeaPoint"": ""15/20"",
    ""PrototypePoint"": ""0/30"",
    ""PlanPoint"": ""0/10"",
    ""RelationshipPoint"": ""0/20""
  }},
  {{
    ""startupId"": ""VS-046"",
    ""Team"": ""1. Hoàng Văn Phúc - Data analyst 4 năm\n2. Ngô Thị Mai - Business consultant"",
    ""Idea"": ""Dịch vụ phân tích dữ liệu nhỏ cho doanh nghiệp vừa & nhỏ: bán hàng, khách hàng, inventory"",
    ""Prototype"": ""Dashboard demo với Power BI, đã làm cho 2 cửa hàng, report insights hữu ích"",
    ""Plan"": ""Tạo template cho 5 ngành khác nhau. Marketing B2B. Target 20 khách/năm đầu"",
    ""Relationship"": ""Hiệp hội SME Việt Nam - networking partner"",
    ""TeamPoint"": ""15/20"",
    ""IdeaPoint"": ""15/20"",
    ""PrototypePoint"": ""20/30"",
    ""PlanPoint"": ""7/10"",
    ""RelationshipPoint"": ""12/20""
  }},
  {{
    ""startupId"": ""VS-047"",
    ""Team"": """",
    ""Idea"": ""App theo dõi sức khoẻ tinh thần (mental wellness) với meditation, mood tracking, therapy tips"",
    ""Prototype"": """",
    ""Plan"": """",
    ""Relationship"": """",
    ""TeamPoint"": ""0/20"",
    ""IdeaPoint"": ""15/20"",
    ""PrototypePoint"": ""0/30"",
    ""PlanPoint"": ""0/10"",
    ""RelationshipPoint"": ""0/20""
  }},
  {{
    ""startupId"": ""VS-048"",
    ""Team"": ""1. Vũ Thị Lan - Yoga instructor & mindfulness coach 6 năm"",
    ""Idea"": ""Dịch vụ huấn luyện mindfulness / thiền trực tuyến qua Zoom, group & 1-1 sessions"",
    ""Prototype"": ""Đã dạy 20 buổi online, có 30 học viên regular, rating 4.8/5"",
    ""Plan"": ""Tạo membership program. Record video courses. Target 100 members paying monthly"",
    ""Relationship"": """",
    ""TeamPoint"": ""13/20"",
    ""IdeaPoint"": ""15/20"",
    ""PrototypePoint"": ""19/30"",
    ""PlanPoint"": ""7/10"",
    ""RelationshipPoint"": ""0/20""
  }},
  {{
    ""startupId"": ""VS-049"",
    ""Team"": ""asdf qwer"",
    ""Idea"": ""Nền tảng kết nối người chia sẻ xe điện hoặc xe đạp điện"",
    ""Prototype"": ""zxcv"",
    ""Plan"": """",
    ""Relationship"": ""tyui"",
    ""TeamPoint"": ""0/20"",
    ""IdeaPoint"": ""14/20"",
    ""PrototypePoint"": ""0/30"",
    ""PlanPoint"": ""0/10"",
    ""RelationshipPoint"": ""0/20""
  }},
  {{
    ""startupId"": ""VS-050"",
    ""Team"": ""1. Đặng Văn Hải - AI engineer 4 năm\n2. Lê Thị Ngọc - Special education teacher 5 năm"",
    ""Idea"": ""Ứng dụng hỗ trợ người khuyết tật (nghe, nói) bằng AI: speech-to-text, text-to-speech realtime"",
    ""Prototype"": ""App prototype với tính năng chuyển đổi giọng nói sang text độ chính xác 80%, đã test với 20 người"",
    ""Plan"": ""Q1-Q2: Nâng độ chính xác lên 90%, thêm tính năng dịch ngôn ngữ ký hiệu. Q3-Q4: Launch public beta 1000 users. Tìm kiếm grant từ tổ chức phi lợi nhuận"",
    ""Relationship"": ""Trung tâm Hỗ trợ Người Khuyết Tật TP.HCM - partner testing & feedback. Hội Người Khiếm Thính Việt Nam - cố vấn"",
    ""TeamPoint"": ""18/20"",
    ""IdeaPoint"": ""19/20"",
    ""PrototypePoint"": ""27/30"",
    ""PlanPoint"": ""9/10"",
    ""RelationshipPoint"": ""18/20""
  }}]


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
            var url = $"https://generativelanguage.googleapis.com/v1beta/models/gemini-2.5-flash-lite:generateContent?key={apiKey}";

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
            var url = $"https://generativelanguage.googleapis.com/v1beta/models/gemini-2.5-flash-lite:generateContent?key={apiKey}";

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