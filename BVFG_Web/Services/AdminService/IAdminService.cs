using BVFG_Web.Models.AdminModel;
using BVFG_Web.Models.Dtos.AdminDto;

namespace BVFG_Web.Services.AdminService
{
    public interface IAdminService
    {
        Task<ResponseModle> AdminLogin(AdminLoginDto adminLogin);
        Task<ResponseModle> AddMember(Admin member);
        Task<ResponseModle> GetAllMember();
        Task<ResponseModle> GetMemberById(long  id);
        Task<ResponseModle> ApprovedChanges(AdminApprovedDto approve);
    }
}
