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

        public WhatsAppPlaywrightService()
        {
            _userDataDir = Path.Combine(Directory.GetCurrentDirectory(), "playwright_userdata");
            Directory.CreateDirectory(_userDataDir);
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

                _playwright = await Playwright.CreateAsync();
                var chromium = _playwright.Chromium;

                // Use existing context if available
                if (_context == null)
                {
                    _context = await chromium.LaunchPersistentContextAsync(_userDataDir, new BrowserTypeLaunchPersistentContextOptions
                    {
                        Headless = !headful,
                        Args = new[] {
                            "--no-sandbox",
                            "--disable-setuid-sandbox",
                            "--disable-web-security",
                            "--disable-features=site-per-process"
                        },
                        ViewportSize = new ViewportSize { Width = 1280, Height = 720 },
                        IgnoreHTTPSErrors = true
                    });
                }

                // Use existing page or create new one
                if (_page == null || _page.IsClosed)
                {
                    _page = _context.Pages.Count > 0 ? _context.Pages[0] : await _context.NewPageAsync();
                }

                // Only navigate if not already on WhatsApp
                var currentUrl = _page.Url;
                if (!currentUrl.Contains("web.whatsapp.com"))
                {
                    await _page.GotoAsync("https://web.whatsapp.com", new PageGotoOptions
                    {
                        WaitUntil = WaitUntilState.DOMContentLoaded,
                        Timeout = 30000
                    });
                }

                _initialized = true;
                _isInitializing = false;
            }
            catch (Exception ex)
            {
                _isInitializing = false;
                throw new Exception($"WhatsApp initialization failed: {ex.Message}", ex);
            }
            finally
            {
                _initLock.Release();
            }
        }

        public async Task<string> GetQrImageBase64Async()
        {
            if (!_initialized && !_isInitializing)
                await InitializeAsync(headful: true);

            // Wait for page to be ready
            await _page.WaitForLoadStateAsync(LoadState.DOMContentLoaded);

            try
            {
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
            catch { }

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
                var screenshot = await _page.ScreenshotAsync(new PageScreenshotOptions
                {
                    Type = ScreenshotType.Png,
                    FullPage = true
                });
                return Convert.ToBase64String(screenshot);
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
                var screenshot = await _page.ScreenshotAsync(new PageScreenshotOptions
                {
                    Type = ScreenshotType.Png,
                    FullPage = true
                });
                return Convert.ToBase64String(screenshot);
            }

            var buffer = await qrElement.ScreenshotAsync(new ElementHandleScreenshotOptions
            {
                Type = ScreenshotType.Png
            });
            return Convert.ToBase64String(buffer);
        }

        public async Task<bool> IsReadyAsync()
        {
            if (!_initialized && !_isInitializing)
                await InitializeAsync();

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
            if (_page != null)
            {
                try { await _page.CloseAsync(); } catch { }
                _page = null;
            }
            if (_context != null)
            {
                try { await _context.CloseAsync(); } catch { }
                _context = null;
            }
            if (_playwright != null)
            {
                try { _playwright.Dispose(); } catch { }
                _playwright = null;
            }
            _initialized = false;
            _isInitializing = false;
        }

        public async ValueTask DisposeAsync()
        {
            await DisposeAsyncCore();
            GC.SuppressFinalize(this);
        }
    }
}