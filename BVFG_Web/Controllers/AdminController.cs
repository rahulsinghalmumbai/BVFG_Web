using BVFG_Web.Models.AdminModel;
using BVFG_Web.Models.Dtos.AdminDto;
using BVFG_Web.Services.AdminService;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using System.Globalization;
using System.Text;

namespace BVFG_Web.Controllers
{
    public class AdminController : Controller
    {
        private readonly IAdminService _adminService;
        public AdminController(IAdminService adminService)
        {
            _adminService = adminService;
        }

        [HttpGet]
        public IActionResult Login()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Login(AdminLoginDto loginDto)
        {
            if (!ModelState.IsValid)
            {
                ViewBag.Message = "Invalid input.";
                return View();
            }

            var response = await _adminService.AdminLogin(loginDto);

            if (response != null && response.Status == "Success" && response.Data is AdminLogin userData)
            {
                HttpContext.Session.SetString("UserId", userData.Id.ToString());
                HttpContext.Session.SetString("UserName", userData.UserName);

                return RedirectToAction("AdminDashboard");
            }
            else
            {
                ViewBag.Message = response?.Message ?? "Login failed.";
                return View();
            }
        }
        [HttpGet]
        public async Task<IActionResult> AdminDashboard()
        {
            var userIdStr = HttpContext.Session.GetString("UserId");
            if (userIdStr != null)
            {
                return View();
            }
            return RedirectToAction("Login", "Admin");
        }
        [HttpGet]
        public async Task<IActionResult> AddMember()
        {
            var userIdStr = HttpContext.Session.GetString("UserId");
            if (userIdStr != null)
            {
                return View();
            }
            return RedirectToAction("Login", "Admin");
        }
        [HttpPost]
        public async Task<IActionResult> AddMember(Admin member)
        {
            var userIdStr = HttpContext.Session.GetString("UserId");
            if (userIdStr != null)
            {
                if (!ModelState.IsValid)
                {
                    TempData["SuccessMessage"] = "Invalid input.";
                    return View();
                }
                member.CreatedBy = Convert.ToInt32(userIdStr);
                member.UpdatedBy = Convert.ToInt32(userIdStr);
                DateTime dob = DateTime.ParseExact("2025-08-08", "yyyy-MM-dd", CultureInfo.InvariantCulture);
                string isoDob = dob.ToUniversalTime().ToString("yyyy-MM-ddTHH:mm:ss.fffZ");
                member.DOB = dob;
                member.MemberID = 0;

                var response = await _adminService.AddMember(member);
                if (response != null && response.Status == "Success")
                {
                    
                    TempData["SuccessMessage"] = "Member added successfully!";
                    return RedirectToAction("AddMember");
                }
                else
                {
                    TempData["ErrorMessage"] = response?.Message ?? "Failed to add member";
                    return View(member);
                }
            }
            return RedirectToAction("Login", "Admin");
        }
        [HttpGet]
        public async Task<IActionResult> GetAllMember()
        {
            var userIdStr = HttpContext.Session.GetString("UserId");
            if (userIdStr != null)
            {

                var response = await _adminService.GetAllMember();
                if (response != null && response.Status == "Success")
                {
                    return View(response.Data);
                }
                else
                {
                    ViewBag.ErrorMessage = response?.Message ?? "Failed to load members";
                    return View(new List<MstMember_Edit>());
                }
            }
            return RedirectToAction("Login", "Admin");
        }
        [HttpGet]
        public async Task<IActionResult> GetMemberById(long Id)
        {
            var userIdStr = HttpContext.Session.GetString("UserId");
            if (userIdStr != null)
            {

                var response = await _adminService.GetMemberById(Id);
                if (response != null && response.Status == "Success")
                {
                    return View(response.Data);
                }
                else
                {
                    ViewBag.ErrorMessage = response?.Message ?? "Failed to load members";
                    return View(new List<EditedMemberChange>());
                }
            }
            return RedirectToAction("Login", "Admin");
        }
        [HttpPost]
        public async Task<IActionResult> ApprovedChanges([FromBody] AdminApprovedDto approvedata)
        {

            var userIdStr = HttpContext.Session.GetString("UserId");
            if (userIdStr != null)
            {


                approvedata.UpdatedBy = Convert.ToInt32(userIdStr);
                var response = await _adminService.ApprovedChanges(approvedata);
                if (response != null && response.Status == "Success")
                {
                    return Json(new
                    {
                        Status = "Success",
                        Message = response.Message ?? "Update successful"
                    });
                }
                else
                {
                    return Json(new
                    {
                        Status = "Failed",
                        Message = response?.Message ?? "An error occurred"
                    });
                }
            }
            return RedirectToAction("Login", "Admin");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Logout()
        {
            HttpContext.Session.Clear();
            return RedirectToAction("Login", "Admin");
        }
    }
}

