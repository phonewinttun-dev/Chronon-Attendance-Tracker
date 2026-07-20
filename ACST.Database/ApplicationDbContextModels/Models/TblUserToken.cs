namespace ACST.Database.ApplicationDbContextModels.Models
{
    public partial class TblUsertoken
    {
        public int UserTokenId { get; set; }

        public int? UserId { get; set; }

        public string RefreshToken { get; set; } = null!;

        public bool? IsRevoked { get; set; }

        public DateTime? ExpiresAt { get; set; }

        public DateTime? CreatedAt { get; set; }

        public DateTime? UpdatedAt { get; set; }

        public bool? DeleteFlag { get; set; }

        public virtual TblUser? User { get; set; }
    }
}
