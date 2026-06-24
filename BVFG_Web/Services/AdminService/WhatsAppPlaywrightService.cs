using Microsoft.Playwright;
using System.Web;

namespace BVFG_Web.Services.AdminService
{
    public class WhatsAppPlaywrightService : IAsyncDisposable
    {
        private readonly string _userDataDir;
        private IBrowserContext _context;
        private IPage _page;
        private IPlaywright _playwright;
        private bool _initialized = false;
        private bool _isInitializing = false;
        private readonly SemaphoreSlim _initLock = new SemaphoreSlim(1, 1);
        private readonly bool _isProduction;
        private readonly bool _isLocal;
        private readonly string _sessionId;

        public WhatsAppPlaywrightService(IWebHostEnvironment env)
        {
            // ✅ UNIQUE SESSION DIRECTORY WITH TIMESTAMP TO AVOID CONFLICTS
            _sessionId = $"{Guid.NewGuid():N}_{DateTime.Now:yyyyMMddHHmmss}";
            _userDataDir = Path.Combine(Directory.GetCurrentDirectory(), "playwright_temp", _sessionId);

            // ✅ CREATE DIRECTORY WITH PROPER PERMISSIONS HANDLING
            try
            {
                if (!Directory.Exists(_userDataDir))
                {
                    Directory.CreateDirectory(_userDataDir);
                }
            }
            catch (Exception ex)
            {
                // Fallback to temp directory if main directory fails
                _userDataDir = Path.Combine(Path.GetTempPath(), "whatsapp_sessions", _sessionId);
                Directory.CreateDirectory(_userDataDir);
            }

            _isProduction = env.IsProduction();
            _isLocal = env.IsDevelopment() || env.EnvironmentName == "Local";
        }

        public async Task InitializeAsync(bool headful = true)
        {
            // Avoid multiple initializations
            if (_initialized) return;

            await _initLock.WaitAsync();
            try
            {
                if (_initialized) return;
                _isInitializing = true;

                Console.WriteLine($"🚀 Starting WhatsApp initialization for session: {_sessionId}");

                // ✅ FIRST CLEANUP ANY EXISTING RESOURCES
                await ForceCleanup();

                // ✅ SIMPLIFIED BROWSER PATH RESOLUTION
                string browserPath = FindBrowserPath();
                if (string.IsNullOrEmpty(browserPath) && _isProduction)
                {
                    throw new Exception("❌ Playwright browser not found. Please ensure browsers are installed.");
                }

                _playwright = await Playwright.CreateAsync();
                var chromium = _playwright.Chromium;

                // ✅ BROWSER LAUNCH WITH FIXED ARGUMENTS FOR HOSTING ENVIRONMENT
                var launchOptions = new BrowserTypeLaunchPersistentContextOptions
                {
                    Headless = !headful,
                    Args = new[] {
                        "--no-sandbox",
                        "--disable-setuid-sandbox",
                        "--disable-web-security",
                        "--disable-features=site-per-process",
                        "--disable-dev-shm-usage",
                        "--disable-gpu",
                        "--no-first-run",
                        "--no-default-browser-check",
                        "--disable-default-apps",
                        "--disable-background-timer-throttling",
                        "--disable-renderer-backgrounding",
                        "--disable-backgrounding-occluded-windows",
                        "--remote-debugging-port=0",
                        "--disable-component-extensions-with-background-pages",
                        "--disable-background-networking",
                        "--disable-sync",
                        "--disable-translate",
                        "--disable-extensions",
                        "--disable-component-update",
                        "--disable-domain-reliability",
                        "--disable-client-side-phishing-detection",
                        "--disable-hang-monitor",
                        "--disable-crash-reporter",
                        "--disable-breakpad",
                        "--noerrdialogs",
                        "--disable-logging"
                    },
                    ViewportSize = new ViewportSize { Width = 1280, Height = 720 },
                    IgnoreHTTPSErrors = true,
                    Timeout = 60000
                };

                // ✅ SET BROWSER PATH ONLY IF FOUND
                if (!string.IsNullOrEmpty(browserPath))
                {
                    launchOptions.ExecutablePath = browserPath;
                }

                Console.WriteLine("🔄 Launching browser with unique session...");
                _context = await chromium.LaunchPersistentContextAsync(_userDataDir, launchOptions);
                Console.WriteLine("✅ Browser launched successfully");

                // ✅ SIMPLE PAGE CREATION
                Console.WriteLine("🔄 Creating new page...");
                _page = await _context.NewPageAsync();
                Console.WriteLine("✅ Page created successfully");

                // ✅ SIMPLE NAVIGATION
                Console.WriteLine("🔄 Navigating to WhatsApp Web...");
                await _page.GotoAsync("https://web.whatsapp.com", new PageGotoOptions
                {
                    WaitUntil = WaitUntilState.DOMContentLoaded,
                    Timeout = 60000
                });
                Console.WriteLine("✅ Navigation completed");

                // ✅ WAIT FOR STABILITY
                await _page.WaitForTimeoutAsync(2000);

                _initialized = true;
                _isInitializing = false;
                Console.WriteLine($"✅ WhatsApp initialization completed for session: {_sessionId}");
            }
            catch (Exception ex)
            {
                _isInitializing = false;
                Console.WriteLine($"❌ WhatsApp initialization failed: {ex.Message}");
                await ForceCleanup();
                throw new Exception($"WhatsApp initialization failed: {ex.Message}", ex);
            }
            finally
            {
                _initLock.Release();
            }
        }

        private async Task ForceCleanup()
        {
            try
            {
                // ✅ CLEANUP IN PROPER ORDER
                if (_page != null)
                {
                    try
                    {
                        if (!_page.IsClosed)
                            await _page.CloseAsync();
                    }
                    catch { }
                    _page = null;
                }

                if (_context != null)
                {
                    try
                    {
                        await _context.CloseAsync();
                    }
                    catch { }
                    _context = null;
                }

                if (_playwright != null)
                {
                    try
                    {
                        _playwright.Dispose();
                    }
                    catch { }
                    _playwright = null;
                }

                // ✅ DELETE SESSION DIRECTORY WITH RETRY LOGIC
                await DeleteDirectoryWithRetry(_userDataDir);

                // ✅ ADD SMALL DELAY TO ENSURE CLEANUP COMPLETES
                await Task.Delay(500);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"⚠️ Force cleanup error: {ex.Message}");
            }
            finally
            {
                _initialized = false;
                _isInitializing = false;
            }
        }

        private async Task DeleteDirectoryWithRetry(string directoryPath, int maxRetries = 3)
        {
            if (!Directory.Exists(directoryPath)) return;

            for (int i = 0; i < maxRetries; i++)
            {
                try
                {
                    Directory.Delete(directoryPath, true);
                    Console.WriteLine($"✅ Successfully deleted directory: {directoryPath}");
                    return;
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"⚠️ Attempt {i + 1} to delete directory failed: {ex.Message}");
                    if (i < maxRetries - 1)
                    {
                        await Task.Delay(1000 * (i + 1));
                    }
                }
            }

            Console.WriteLine($"❌ Failed to delete directory after {maxRetries} attempts: {directoryPath}");
        }

        private string FindBrowserPath()
        {
            // ✅ SIMPLIFIED BROWSER PATH FINDING
            if (_isProduction)
            {
                var productionPaths = new[]
                {
                    @"C:\inetpub\wwwroot\BVGF_Web\ms-playwright\chromium-1200\chrome-win\chrome.exe",
                    @"C:\inetpub\wwwroot\BVGF_Web\ms-playwright\chromium-1194\chrome-win\chrome.exe",
                    Path.Combine(Directory.GetCurrentDirectory(), "ms-playwright", "chromium-1200", "chrome-win", "chrome.exe"),
                    Path.Combine(Directory.GetCurrentDirectory(), "ms-playwright", "chromium-1194", "chrome-win", "chrome.exe")
                };

                foreach (var path in productionPaths)
                {
                    if (File.Exists(path))
                    {
                        Console.WriteLine($"✅ Found browser at: {path}");
                        return path;
                    }
                }

                // Fallback search
                var searchDir = Path.Combine(Directory.GetCurrentDirectory(), "ms-playwright");
                if (Directory.Exists(searchDir))
                {
                    var files = Directory.GetFiles(searchDir, "chrome.exe", SearchOption.AllDirectories);
                    if (files.Length > 0)
                    {
                        Console.WriteLine($"✅ Found browser via search: {files[0]}");
                        return files[0];
                    }
                }
            }

            Console.WriteLine("✅ Using Playwright auto-detect for browser");
            return null;
        }

        public async Task<string> GetQrImageBase64Async()
        {
            if (!_initialized && !_isInitializing)
                await InitializeAsync(headful: true);

            // ✅ CHECK PAGE STATE
            if (_page == null || _page.IsClosed)
            {
                await InitializeAsync(headful: true);
            }

            try
            {
                // Wait for page to be ready
                await _page.WaitForLoadStateAsync(LoadState.NetworkIdle);
                await _page.WaitForTimeoutAsync(1000);

                // Check if already logged in
                var isLoggedIn = await _page.QuerySelectorAsync("div[role='textbox']") != null
                               || await _page.QuerySelectorAsync("._1Flk2") != null
                               || await _page.QuerySelectorAsync("header") != null
                               || await _page.QuerySelectorAsync("[data-testid='conversation-panel-wrapper']") != null;

                if (isLoggedIn)
                {
                    return null;
                }
            }
            catch
            {
                // Continue to QR code
            }

            // Wait for QR code to appear
            try
            {
                await _page.WaitForSelectorAsync("canvas[aria-label='Scan me!']", new PageWaitForSelectorOptions
                {
                    Timeout = 10000
                });
            }
            catch (TimeoutException)
            {
                // If QR code not found, take screenshot of the whole page
                try
                {
                    var screenshot = await _page.ScreenshotAsync(new PageScreenshotOptions
                    {
                        Type = ScreenshotType.Png,
                        FullPage = true
                    });
                    return Convert.ToBase64String(screenshot);
                }
                catch
                {
                    return null;
                }
            }

            var qrSelectorCandidates = new[] {
                "canvas[aria-label='Scan me!']",
                "canvas",
                "img[alt='Scan me']",
                "div[data-ref]"
            };

            IElementHandle qrElement = null;
            foreach (var sel in qrSelectorCandidates)
            {
                try
                {
                    qrElement = await _page.QuerySelectorAsync(sel);
                    if (qrElement != null) break;
                }
                catch { }
            }

            if (qrElement == null)
            {
                try
                {
                    var screenshot = await _page.ScreenshotAsync(new PageScreenshotOptions
                    {
                        Type = ScreenshotType.Png,
                        FullPage = true
                    });
                    return Convert.ToBase64String(screenshot);
                }
                catch
                {
                    return null;
                }
            }

            try
            {
                var buffer = await qrElement.ScreenshotAsync(new ElementHandleScreenshotOptions
                {
                    Type = ScreenshotType.Png
                });
                return Convert.ToBase64String(buffer);
            }
            catch
            {
                return null;
            }
        }

        public async Task<bool> IsReadyAsync()
        {
            if (!_initialized && !_isInitializing)
                await InitializeAsync();

            if (_page == null || _page.IsClosed)
                return false;

            try
            {
                var ready = await _page.QuerySelectorAsync("div[role='textbox']") != null
                         || await _page.QuerySelectorAsync("div[title='Search input textbox']") != null
                         || await _page.QuerySelectorAsync("._2_1wd") != null
                         || await _page.QuerySelectorAsync("[data-testid='conversation-panel-wrapper']") != null;
                return ready;
            }
            catch
            {
                return false;
            }
        }

        public async Task<string> GetConnectedNumberAsync()
        {
            if (!_initialized && !_isInitializing)
                await InitializeAsync();

            if (_page == null || _page.IsClosed)
                return "Browser not available";

            try
            {
                // Try to get connected WhatsApp number
                var numberElement = await _page.QuerySelectorAsync("[data-testid='conversation-info-header'] [title]");
                if (numberElement != null)
                {
                    var title = await numberElement.GetAttributeAsync("title");
                    if (!string.IsNullOrEmpty(title) && title.Contains("+"))
                    {
                        return title;
                    }
                }

                // Alternative selector
                var profileElement = await _page.QuerySelectorAsync("._1lpto");
                if (profileElement != null)
                {
                    var text = await profileElement.TextContentAsync();
                    if (!string.IsNullOrEmpty(text) && text.Contains("+"))
                    {
                        return text.Trim();
                    }
                }
            }
            catch
            {
                // Ignore errors
            }

            return "Connected (Number not detected)";
        }

        public async Task SendMessageAsync(string phoneWithCountry, string message)
        {
            if (string.IsNullOrWhiteSpace(phoneWithCountry))
                throw new ArgumentNullException(nameof(phoneWithCountry));

            if (!_initialized && !_isInitializing)
                await InitializeAsync();

            if (_page == null || _page.IsClosed)
                throw new Exception("WhatsApp is not initialized properly");

            var num = System.Text.RegularExpressions.Regex.Replace(phoneWithCountry, @"[^\d]", "");
            var encoded = HttpUtility.UrlEncode(message ?? "");
            var url = $"https://web.whatsapp.com/send?phone={num}&text={encoded}";

            try
            {
                await _page.WaitForLoadStateAsync(LoadState.NetworkIdle);
                await _page.WaitForTimeoutAsync(1000);

                var currentUrl = _page.Url;
                if (currentUrl.Contains($"phone={num}"))
                {
                    await _page.ReloadAsync(new PageReloadOptions
                    {
                        WaitUntil = WaitUntilState.DOMContentLoaded,
                        Timeout = 30000
                    });
                }
                else
                {
                    await _page.GotoAsync(url, new PageGotoOptions
                    {
                        WaitUntil = WaitUntilState.DOMContentLoaded,
                        Timeout = 30000
                    });
                }

                await _page.WaitForTimeoutAsync(5000);

                var closeButtons = new[] {
                    "button[aria-label='Close']",
                    "button[data-testid='x-viewer']",
                    "div[role='button'][aria-label='Close']"
                };

                foreach (var closeBtn in closeButtons)
                {
                    try
                    {
                        var closeButton = await _page.QuerySelectorAsync(closeBtn);
                        if (closeButton != null)
                        {
                            await closeButton.ClickAsync();
                            await _page.WaitForTimeoutAsync(1000);
                        }
                    }
                    catch { }
                }

                var invalidNumberDetected = false;

                var pageContent = await _page.ContentAsync();
                if (pageContent.Contains("Phone number shared via url is invalid") ||
                    pageContent.Contains("phone number shared via url is invalid"))
                {
                    invalidNumberDetected = true;
                }

                if (!invalidNumberDetected)
                {
                    var invalidSelectors = new[] {
                        "text=Phone number shared via url is invalid",
                        "div[data-testid='invalid-number']",
                        "div[role='dialog']:has-text('invalid')"
                    };

                    foreach (var selector in invalidSelectors)
                    {
                        try
                        {
                            var invalidElement = await _page.WaitForSelectorAsync(selector, new PageWaitForSelectorOptions
                            {
                                Timeout = 3000
                            });
                            if (invalidElement != null)
                            {
                                invalidNumberDetected = true;
                                break;
                            }
                        }
                        catch (TimeoutException)
                        {
                            continue;
                        }
                    }
                }

                if (invalidNumberDetected)
                {
                    var okButton = await _page.QuerySelectorAsync("button:has-text('OK')");
                    if (okButton != null)
                    {
                        await okButton.ClickAsync();
                        await _page.WaitForTimeoutAsync(2000);
                    }

                    throw new Exception($"Phone number {phoneWithCountry} is not registered on WhatsApp.");
                }

                var messageInputSelectors = new[] {
                    "div[contenteditable='true'][data-tab='10']",
                    "div[contenteditable='true'][data-tab='9']",
                    "div[contenteditable='true'][data-tab]",
                    "[contenteditable='true']",
                    "div[title='Type a message']"
                };

                IElementHandle messageInput = null;
                foreach (var selector in messageInputSelectors)
                {
                    try
                    {
                        messageInput = await _page.WaitForSelectorAsync(selector, new PageWaitForSelectorOptions
                        {
                            Timeout = 10000,
                            State = WaitForSelectorState.Visible
                        });
                        if (messageInput != null)
                        {
                            await _page.WaitForTimeoutAsync(1000);
                            break;
                        }
                    }
                    catch { }
                }

                if (messageInput == null)
                {
                    var currentUrlAfter = _page.Url;
                    if (!currentUrlAfter.Contains("web.whatsapp.com"))
                    {
                        throw new Exception("WhatsApp page navigation failed");
                    }
                    throw new Exception("Message input field not found - may be navigation issue");
                }

                // Clear existing text properly
                await messageInput.ClickAsync(new ElementHandleClickOptions { ClickCount = 3 });
                await _page.WaitForTimeoutAsync(500);
                await _page.Keyboard.PressAsync("Backspace");
                await _page.WaitForTimeoutAsync(500);

                await messageInput.TypeAsync(message, new ElementHandleTypeOptions { Delay = 30 });
                await _page.WaitForTimeoutAsync(1000);

                await _page.Keyboard.PressAsync("Enter");
                await _page.WaitForTimeoutAsync(3000);

                var sent = false;
                var sentSelectors = new[] {
                    "span[data-testid='msg-dblcheck']",
                    "span[data-icon='msg-dblcheck']",
                    "div[data-testid='msg-check']"
                };

                foreach (var selector in sentSelectors)
                {
                    try
                    {
                        await _page.WaitForSelectorAsync(selector, new PageWaitForSelectorOptions
                        {
                            Timeout = 5000
                        });
                        sent = true;
                        break;
                    }
                    catch { }
                }

                if (!sent)
                {
                    var currentText = await messageInput.TextContentAsync();
                    if (!string.IsNullOrEmpty(currentText) && currentText != message)
                    {
                        throw new Exception("Message may not have been sent successfully");
                    }
                }
            }
            catch (Exception ex)
            {
                if (ex.Message.Contains("not registered on WhatsApp"))
                {
                    throw;
                }

                if (ex.Message.Contains("Navigation") || ex.Message.Contains("Timeout"))
                {
                    try
                    {
                        await _page.GotoAsync("https://web.whatsapp.com", new PageGotoOptions
                        {
                            WaitUntil = WaitUntilState.DOMContentLoaded,
                            Timeout = 30000
                        });
                        await _page.WaitForTimeoutAsync(5000);

                        await SendMessageAsync(phoneWithCountry, message);
                        return;
                    }
                    catch (Exception retryEx)
                    {
                        throw new Exception($"Failed after retry: {retryEx.Message}");
                    }
                }

                throw new Exception($"Failed to send message to {phoneWithCountry}: {ex.Message}");
            }
        }

        public async Task<(bool Ready, string Base64Qr)> GetQrAsync()
        {
            if (!_initialized && !_isInitializing)
                await InitializeAsync(headful: true);

            bool ready = await IsReadyAsync();
            string qr = null;

            if (!ready)
            {
                qr = await GetQrImageBase64Async();
            }

            return (ready, qr);
        }

        public async Task DisposeAsyncCore()
        {
            await ForceCleanup();
        }

        public async ValueTask DisposeAsync()
        {
            await DisposeAsyncCore();
            GC.SuppressFinalize(this);
        }
    }
}