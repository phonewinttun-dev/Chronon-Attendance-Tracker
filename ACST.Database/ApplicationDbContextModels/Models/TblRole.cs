namespace ACST.Database.ApplicationDbContextModels.Models
{
    public partial class TblRole
    {
        public int RoleId { get; set; }

        public string? RoleName { get; set; }

        public bool? DeleteFlag { get; set; }

        public virtual ICollection<TblRolepermission> TblRolepermissions { get; set; } = new List<TblRolepermission>();

        public virtual ICollection<TblUser> TblUsers { get; set; } = new List<TblUser>();
    }
}
