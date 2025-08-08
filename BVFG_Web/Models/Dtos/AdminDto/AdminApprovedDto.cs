namespace BVFG_Web.Models.Dtos.AdminDto
{
    public class AdminApprovedDto
    {
        public long? MemberID { get; set; }
        public long? UpdatedBy { get; set; }
        public string? ColumnName { get; set; }
        public string? NewValue { get; set; }
        public string? Flag { get; set; }
    }
}
