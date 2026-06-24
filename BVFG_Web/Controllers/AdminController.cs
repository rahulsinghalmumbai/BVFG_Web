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
        private readonly WhatsAppPlaywrightService _wa;
        public AdminController(IAdminService adminService, WhatsAppPlaywrightService wa)
        {
            _adminService = adminService;
            _wa = wa;
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

        [HttpGet("admin/whatsapp")]
        public async Task<IActionResult> SendWhatsapp()
        {
            var userIdStr = HttpContext.Session.GetString("UserId");
            if (string.IsNullOrEmpty(userIdStr))
            {
                return RedirectToAction("Login", "Admin");
            }

            var model = new WhatsAppSendModel();
            return View("Whatsapp", model);
        }

        [HttpPost("admin/whatsapp/initialize")]
        public async Task<IActionResult> InitializeWhatsApp()
        {
            try
            {
                await _wa.InitializeAsync(headful: true);

                return Json(new
                {
                    success = true,
                    message = "WhatsApp initialized successfully"
                });
            }
            catch (Exception ex)
            {
                return Json(new
                {
                    success = false,
                    message = ex.Message
                });
            }
        }

        

        [HttpPost("admin/whatsapp/send-single")]
        public async Task<IActionResult> SendSingleMessage([FromBody] WhatsAppSingleMessageDto dto)
        {
            var userIdStr = HttpContext.Session.GetString("UserId");
            if (string.IsNullOrEmpty(userIdStr))
            {
                return Json(new { success = false, message = "Session expired" });
            }

            if (string.IsNullOrWhiteSpace(dto.Mobile) || string.IsNullOrWhiteSpace(dto.Message))
            {
                return Json(new { success = false, message = "Mobile and Message are required" });
            }

            try
            {
                if (dto.Mobile.Contains(","))
                {
                    var numbers = dto.Mobile.Split(',')
                        .Select(n => n.Trim())
                        .Where(n => !string.IsNullOrWhiteSpace(n))
                        .ToList();

                    var successCount = 0;
                    var failedNumbers = new List<string>();
                    var successNumbers = new List<string>();
                    var results = new List<string>();

                    foreach (var number in numbers)
                    {
                        try
                        {
                            string phone = number.Trim();
                            if (!phone.StartsWith("+"))
                                phone = "+91" + phone;

                            await _wa.SendMessageAsync(phone, dto.Message);
                            successCount++;
                            successNumbers.Add(phone);
                            results.Add($"✓ Sent to {phone}");
                        }
                        catch (Exception ex)
                        {
                            if (ex.Message.Contains("not registered on WhatsApp") ||
                                ex.Message.Contains("invalid number") ||
                                ex.Message.Contains("not available on WhatsApp"))
                            {
                                failedNumbers.Add($"{number} (Not on WhatsApp)");
                                results.Add($"✗ {number}: Not registered on WhatsApp");
                            }
                            else
                            {
                                failedNumbers.Add($"{number} ({ex.Message})");
                                results.Add($"✗ Failed to send to {number}: {ex.Message}");
                            }
                        }
                    }

                    var message = $"Sent {successCount} out of {numbers.Count} messages successfully.";
                    if (successNumbers.Count > 0)
                    {
                        message += $"<br><strong>Successful ({successCount}):</strong> " + string.Join(", ", successNumbers);
                    }
                    if (failedNumbers.Count > 0)
                    {
                        message += $"<br><strong>Failed ({failedNumbers.Count}):</strong> " + string.Join(", ", failedNumbers);
                    }

                    return Json(new
                    {
                        success = true,
                        message = message,
                        details = results,
                        successCount = successCount,
                        failedCount = failedNumbers.Count,
                        totalCount = numbers.Count,
                        successNumbers = successNumbers,
                        failedNumbers = failedNumbers
                    });
                }
                else
                {
                    // Original single number logic
                    string phone = dto.Mobile.Trim();
                    if (!phone.StartsWith("+"))
                        phone = "+91" + phone;

                    await _wa.SendMessageAsync(phone, dto.Message);

                    return Json(new
                    {
                        success = true,
                        message = $"Message sent successfully to {phone}",
                        successCount = 1,
                        failedCount = 0,
                        totalCount = 1,
                        successNumbers = new List<string> { phone },
                        failedNumbers = new List<string>()
                    });
                }
            }
            catch (Exception ex)
            {
                // Specific error for invalid WhatsApp numbers
                if (ex.Message.Contains("not registered on WhatsApp") ||
                    ex.Message.Contains("invalid number") ||
                    ex.Message.Contains("not available on WhatsApp"))
                {
                    return Json(new
                    {
                        success = false,
                        message = $"Error: The phone number is not registered on WhatsApp. Please check the number and try again."
                    });
                }

                return Json(new
                {
                    success = false,
                    message = "Failed to send message: " + ex.Message
                });
            }
        }

        [HttpGet("admin/whatsapp/check-status")]
        public async Task<IActionResult> CheckStatus()
        {
            try
            {
                var isReady = await _wa.IsReadyAsync();
                string qrCode = null;
                string connectedNumber = null;

                if (!isReady)
                {
                    var result = await _wa.GetQrAsync();
                    qrCode = result.Base64Qr;
                }
                else
                {
                    connectedNumber = await _wa.GetConnectedNumberAsync();
                }

                return Json(new
                {
                    success = true,
                    isReady = isReady,
                    qrCode = qrCode,
                    connectedNumber = connectedNumber
                });
            }
            catch (Exception ex)
            {
                return Json(new
                {
                    success = false,
                    message = ex.Message
                });
            }
        }

        [HttpGet("admin/whatsapp/download-sample-format")]
        public IActionResult DownloadSampleFormat()
        {
            var filePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "sample", "BulkWhatsAppFormat.xlsx");

            if (!System.IO.File.Exists(filePath))
                return NotFound("Sample format file not found");

            var fileBytes = System.IO.File.ReadAllBytes(filePath);

            return File(
                fileBytes,
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                "BulkWhatsAppFormat.xlsx"
            );
        }

        [HttpPost("admin/whatsapp/send-bulk")]
        public async Task<IActionResult> SendBulkMessage([FromForm] WhatsAppBulkMessageDto dto)
        {
            var userIdStr = HttpContext.Session.GetString("UserId");
            if (string.IsNullOrEmpty(userIdStr))
            {
                return Json(new { success = false, message = "Session expired" });
            }

            if (dto.ExcelFile == null || dto.ExcelFile.Length == 0)
            {
                return Json(new { success = false, message = "Please upload a valid Excel file" });
            }

            if (string.IsNullOrWhiteSpace(dto.Message))
            {
                return Json(new { success = false, message = "Message is required" });
            }

            try
            {
                var mobiles = new List<string>();

                using (var stream = new MemoryStream())
                {
                    await dto.ExcelFile.CopyToAsync(stream);
                    using (var package = new OfficeOpenXml.ExcelPackage(stream))
                    {
                        var worksheet = package.Workbook.Worksheets.FirstOrDefault();
                        if (worksheet == null)
                        {
                            return Json(new { success = false, message = "No worksheet found in Excel" });
                        }

                        int rowCount = worksheet.Dimension?.Rows ?? 0;

                        for (int row = 2; row <= rowCount; row++)
                        {
                            string mobile = worksheet.Cells[row, 1].Text?.Trim();
                            if (!string.IsNullOrEmpty(mobile))
                            {
                                // Add country code if not present
                                if (!mobile.StartsWith("+"))
                                {
                                    mobile = "+91" + mobile;
                                }
                                mobiles.Add(mobile);
                            }
                        }
                    }
                }

                if (mobiles.Count == 0)
                {
                    return Json(new { success = false, message = "No valid mobile numbers found in Excel" });
                }

                // Send messages with delay
                int successCount = 0;
                int failCount = 0;
                var failedNumbers = new List<string>();
                var successNumbers = new List<string>();
                var detailedResults = new List<string>();

                foreach (var mobile in mobiles)
                {
                    try
                    {
                        await _wa.SendMessageAsync(mobile, dto.Message);
                        successCount++;
                        successNumbers.Add(mobile);
                        detailedResults.Add($"✓ Sent to {mobile}");

                        // Delay between messages
                        if (dto.DelaySeconds > 0)
                        {
                            await Task.Delay(dto.DelaySeconds * 1000);
                        }
                    }
                    catch (Exception ex)
                    {
                        failCount++;

                        string errorMessage = ex.Message;
                        if (ex.Message.Contains("not registered on WhatsApp") ||
                            ex.Message.Contains("invalid number") ||
                            ex.Message.Contains("not available on WhatsApp"))
                        {
                            errorMessage = "Not registered on WhatsApp";
                            failedNumbers.Add($"{mobile} (Not on WhatsApp)");
                        }
                        else
                        {
                            failedNumbers.Add($"{mobile} ({ex.Message})");
                        }

                        detailedResults.Add($"✗ Failed to send to {mobile}: {errorMessage}");
                    }
                }

                string resultMessage = $"Bulk sending completed. Success: {successCount}, Failed: {failCount}";

                if (failedNumbers.Count > 0)
                {
                    resultMessage += $"\nFailed numbers: {string.Join(", ", failedNumbers.Take(10))}";
                    if (failedNumbers.Count > 10)
                    {
                        resultMessage += $"... and {failedNumbers.Count - 10} more";
                    }
                }

                return Json(new
                {
                    success = true,
                    message = resultMessage,
                    successCount,
                    failCount,
                    failedNumbers,
                    successNumbers,
                    detailedResults = detailedResults.Take(20).ToList() 
                });
            }
            catch (Exception ex)
            {
                return Json(new
                {
                    success = false,
                    message = "Error processing bulk messages: " + ex.Message
                });
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

