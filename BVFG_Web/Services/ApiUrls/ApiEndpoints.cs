namespace BVFG_Web.Services.ApiUrls
{
    public class ApiEndpoints
    {
        //local
         //public const string BaseApiUrl = "http://192.168.1.18:5151/api/";
        //produnction
        public const string BaseApiUrl = "http://195.250.31.98:2030/api/";

    }
    public static class Endpoints
    {
        public const string AdminLogin = "MstMember_Edit/AdminLogin";
        public const string AddMember = "MstMember/UpsertMember";
        public const string GetAllMember = "MstMember_Edit/GetAllEditedMembers";
        public const string GetMemberById = "MstMember_Edit/GetEditedMemberChangesByMemId";
        public const string ApprovedData = "MstMember_Edit/ApprovedByAdminOfMemberRecords";
    }
}
