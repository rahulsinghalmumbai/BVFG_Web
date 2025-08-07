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
            return View();
        }
        [HttpGet]
        public async Task<IActionResult> AddMember()
        {
            return View();
        }
        [HttpPost]
        public async Task<IActionResult> AddMember(Admin member)
        {
            if (!ModelState.IsValid)
            {
                TempData["SuccessMessage"] = "Invalid input.";
                return View();
            }
            //get the session value
            var userIdStr = HttpContext.Session.GetString("UserId");

            member.CreatedBy = Convert.ToInt32(userIdStr);
            member.UpdatedBy=Convert.ToInt32(userIdStr);
            member.DOB = DateTime.ParseExact("2025-08-08", "yyyy-MM-dd", CultureInfo.InvariantCulture);

            var response = await _adminService.AddMember(member);
            if (response != null && response.Status == "Success")
            {
                TempData["SuccessMessage"] = "Member added successfully!";
                return RedirectToAction("AddMember");
            }
            else
            {
                ViewBag.Message = response?.Message ?? "Failed to add member";
                return View(member);
            }
        }
        [HttpGet]
        public async Task<IActionResult> GetAllMember()
        {
            var response=await _adminService.GetAllMember();
            if(response!= null && response.Status=="Success")
            {
                return View(response.Data);
            }
            else
            {
                ViewBag.ErrorMessage = response?.Message ?? "Failed to load members";
                return View(new List<MstMember_Edit>()); 
            }
        }
        [HttpGet]
        public async Task<IActionResult> GetMemberById(long Id)
        {
            var response = await _adminService.GetMemberById(Id);
            if(response!= null && response.Status=="Success")
            {
                return View(response.Data);
            }
            else
            {
                ViewBag.ErrorMessage = response?.Message ?? "Failed to load members";
                return View(new List<EditedMemberChange>());
            }
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
