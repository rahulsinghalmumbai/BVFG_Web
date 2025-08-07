namespace BVFG_Web.Models.AdminModel
{
    public class EditedMemberChange
    {
        public long? MemberId { get; set; }
        public string? ColumnName { get; set; }
        public string? OldValue { get; set; }
        public string? NewValue { get; set; }
    }
}
