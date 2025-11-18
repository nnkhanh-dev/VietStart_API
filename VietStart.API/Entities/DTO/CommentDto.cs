namespace VietStart.API.Entities.DTO
{
    public class CommentDto
    {
        public int Id { get; set; }
        public string UserId { get; set; }
        public string UserFullName { get; set; }
        public string UserAvatar { get; set; }
        public int StartUpId { get; set; }
        public string Content { get; set; }
        public int? ParentCommentId { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public List<CommentDto> Replies { get; set; }
    }

    public class CreateCommentDto
    {
        public int StartUpId { get; set; }
        public string Content { get; set; }
        public int? ParentCommentId { get; set; }
    }

    public class UpdateCommentDto
    {
        public string Content { get; set; }
    }
}
