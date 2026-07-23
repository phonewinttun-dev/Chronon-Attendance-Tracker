namespace ACST.Database.ApplicationDbContextModels.Models
{
    public partial class TblPermission
    {
        public int PermissionId { get; set; }

        public string? PermissionName { get; set; }

        public bool? DeleteFlag { get; set; }

        public virtual ICollection<TblRolepermission> TblRolepermissions { get; set; } = new List<TblRolepermission>();
    }
}
