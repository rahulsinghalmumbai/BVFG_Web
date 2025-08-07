using System.Text;
using BVFG_Web.Models.AdminModel;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;

namespace BVFG_Web.Controllers
{
    public class AdminController : Controller
    {
        private readonly string baseUrl = "http://195.250.31.98:2030"; 
        private readonly HttpClient client = new HttpClient();

        [HttpGet]
        public IActionResult Index()
        {
            return View(); // Not required for login logic now
        }

        [HttpGet]
        public IActionResult Login()
        {
            return View();
        }

        [HttpPost]
        public IActionResult Login(Admin admin)
        {
            
            var json = $"{{\"Mobile1\":\"{admin.Mobile1}\"}}";
            var stringContent = new StringContent(json, Encoding.UTF8, "application/json");

            string loginApiUrl = $"{baseUrl}/api/MstMember/login";  

            HttpResponseMessage response = client.PostAsync(loginApiUrl, stringContent).Result;

            if (response.IsSuccessStatusCode)
            {
                //Get response data
                string responseData = response.Content.ReadAsStringAsync().Result;
                var result = JsonConvert.DeserializeObject<Admin>(responseData);

                // store in session
                HttpContext.Session.SetString("Mobile", result?.Mobile1 ?? "");

                return RedirectToAction("Index");
            }
            else
            {
                ViewBag.Message = "Login Failed";
                return View();
            }
        }
    }
}
