namespace VietStart.API.Entities.DTO
{
    public class OAuth2Token
    {
        public string token_type { get; set; } = "Bearer";
        public string access_token { get; set; }
        public int expires_in { get; set; }
        public string refresh_token { get; set; }
        public string scope { get; set; }
    }
}
