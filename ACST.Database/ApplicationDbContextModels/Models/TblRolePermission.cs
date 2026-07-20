namespace ACST.Database.ApplicationDbContextModels.Models
{
    public partial class TblRolepermission
    {
        public int RolePermissionId { get; set; }

        public int? RoleId { get; set; }

        public int? PermissionId { get; set; }

        public bool? DeleteFlag { get; set; }

        public virtual TblPermission? Permission { get; set; }

        public virtual TblRole? Role { get; set; }
    }
}
