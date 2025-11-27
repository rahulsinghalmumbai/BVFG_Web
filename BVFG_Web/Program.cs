using BVFG_Web.Services.AdminService;
using OfficeOpenXml;

namespace BVFG_Web
{
    public class Program
    {
        public static void Main(string[] args)
        {
            // ✅ EPPlus License Setting - YEH NAYI LINE ADD KI HAI
           // ExcelPackage.LicenseContext = LicenseContext.NonCommercial;

            var builder = WebApplication.CreateBuilder(args);

            // Add services to the container.
            builder.Services.AddControllersWithViews();
            builder.Services.AddScoped<IAdminService, AdminService>();
            builder.Services.AddDistributedMemoryCache();
            builder.Services.AddSingleton<WhatsAppPlaywrightService>();

            // ✅ Added: clearly structured session setup (NOTHING REMOVED)
            builder.Services.AddSession(options =>
            {
                options.IdleTimeout = TimeSpan.FromMinutes(30); // session timeout
                options.Cookie.HttpOnly = true;
                options.Cookie.IsEssential = true;
            });

            // ✅ Added: Allow large files/uploads if needed later
            builder.Services.Configure<IISServerOptions>(options =>
            {
                options.MaxRequestBodySize = 104857600; // 100MB
            });

            var app = builder.Build();

            if (app.Environment.IsDevelopment())
            {
                app.UseDeveloperExceptionPage(); // Shows detailed error info
            }
            else
            {
                app.UseExceptionHandler("/Home/Error"); // Production handler
                app.UseHsts();
            }
            app.UseHttpsRedirection();
            app.UseStaticFiles();

            // ✅ Added: Authentication placeholder (future-proof)
            // app.UseAuthentication();  // Only uncomment when implementing Auth

            // ⭐ IMPORTANT FIX: Session must come BEFORE Authorization  
            app.UseSession();   // <-- moved above UseAuthorization (NOT removed, only repositioned)

            app.UseRouting();

            app.UseAuthorization();

            // Default route (your original route kept SAME)
            app.MapControllerRoute(
                name: "default",
                pattern: "{controller=Admin}/{action=Login}/{id?}");

            // ✅ Global 404 handler (Only added, nothing removed)
            app.MapFallback(async context =>
            {
                context.Response.Redirect("/Home/Error");
            });

            app.Run();
        }
    }
}