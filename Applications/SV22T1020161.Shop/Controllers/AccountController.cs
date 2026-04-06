using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using SV22T1020161.BusinessLayers;
using SV22T1020161.Models.Partner;
using SV22T1020161.Models.Security;
using SV22T1020161.Shop;
using SV22T1020161.Shop.Models;

namespace SV22T1020161.Shop.Controllers
{
    /// <summary>
    /// Controller xử lý các chức năng liên quan đến tài khoản khách hàng (Đăng nhập, Đăng ký, Profile)
    /// </summary>
    public class AccountController : Controller
    {
        /// <summary>
        /// Xem thông tin mật khẩu phục vụ mục đích kiểm tra (Debug)
        /// </summary>
        /// <param name="email"></param>
        /// <returns></returns>
        [HttpGet]
        public async Task<IActionResult> DebugPassword(string email)
        {
            var sb = new System.Text.StringBuilder();
            sb.AppendLine($"=== DEBUG PASSWORD ===");
            sb.AppendLine($"Email: {email}");
            
            var testPassword = "123123";
            var testHash = SV22T1020161.BusinessLayers.CryptHelper.HashMD5(testPassword);
            sb.AppendLine($"MD5('123123') = {testHash}");
            
            var customers = await PartnerDataService.ListCustomersAsync(new SV22T1020161.Models.Common.PaginationSearchInput() 
            { 
                SearchValue = email, 
                Page = 1, 
                PageSize = 1 
            });
            
            if (customers.DataItems.Any())
            {
                var customer = customers.DataItems.First();
                sb.AppendLine($"\n--- Customer in DB ---");
                sb.AppendLine($"CustomerID: {customer.CustomerID}");
                sb.AppendLine($"Email: {customer.Email}");
                sb.AppendLine($"Password in DB: '{customer.Password}'");
                sb.AppendLine($"IsLocked: {customer.IsLocked}");
                sb.AppendLine($"Password length in DB: {customer.Password?.Length ?? 0}");
                
                if (customer.Password == testHash)
                    sb.AppendLine("\n✓ Password KHỚP!");
                else
                    sb.AppendLine($"\n✗ Password KHÔNG khớp!");
            }
            else
            {
                sb.AppendLine("\n✗ Không tìm thấy customer với email này!");
            }
            
            return Content(sb.ToString().Replace("\n", "<br/>"), "text/html");
        }

        /// <summary>
        /// Kiểm tra kết quả mã hóa MD5
        /// </summary>
        /// <param name="password"></param>
        /// <returns></returns>
        [HttpGet]
        public IActionResult TestMD5(string password = "123123")
        {
            var hash = SV22T1020161.BusinessLayers.CryptHelper.HashMD5(password);
            return Content($"Password: '{password}' => MD5: '{hash}'");
        }

        /// <summary>
        /// Giao diện đăng ký tài khoản mới
        /// </summary>
        /// <param name="returnUrl"></param>
        /// <returns></returns>
        [HttpGet]
        public IActionResult Register(string returnUrl = "")
        {
            ViewBag.ReturnUrl = returnUrl;
            return View();
        }

        /// <summary>
        /// Xử lý đăng ký tài khoản khách hàng mới
        /// </summary>
        /// <param name="data"></param>
        /// <param name="confirmPassword"></param>
        /// <param name="returnUrl"></param>
        /// <returns></returns>
        [HttpPost]
        public async Task<IActionResult> Register(Customer data, string confirmPassword, string returnUrl = "")
        {
            if (string.IsNullOrWhiteSpace(data.CustomerName))
                ModelState.AddModelError(nameof(data.CustomerName), "Họ tên không được để trống.");
            else if (data.CustomerName.Trim().Length < 2)
                ModelState.AddModelError(nameof(data.CustomerName), "Họ tên phải có ít nhất 2 ký tự.");

            if (string.IsNullOrWhiteSpace(data.Email))
                ModelState.AddModelError(nameof(data.Email), "Vui lòng nhập Email.");
            else if (!data.Email.Contains('@') || !data.Email.Contains('.'))
                ModelState.AddModelError(nameof(data.Email), "Email không đúng định dạng.");

            if (string.IsNullOrWhiteSpace(data.Phone))
                ModelState.AddModelError(nameof(data.Phone), "Số điện thoại không được để trống.");
            else
            {
                string digits = new string(data.Phone.Where(char.IsDigit).ToArray());
                if (digits.Length < 10 || digits.Length > 11)
                    ModelState.AddModelError(nameof(data.Phone), "Số điện thoại phải từ 10-11 chữ số.");
            }

            if (string.IsNullOrWhiteSpace(data.Province))
                ModelState.AddModelError(nameof(data.Province), "Vui lòng chọn tỉnh/thành phố.");

            if (string.IsNullOrWhiteSpace(data.Password))
                ModelState.AddModelError(nameof(data.Password), "Vui lòng nhập mật khẩu.");
            else
            {
                if (data.Password.Length < 6)
                    ModelState.AddModelError(nameof(data.Password), "Mật khẩu phải có ít nhất 6 ký tự.");
            }

            if (data.Password != confirmPassword)
                ModelState.AddModelError(string.Empty, "Mật khẩu xác nhận không khớp.");

            if (!string.IsNullOrWhiteSpace(data.Email))
            {
                bool isEmailValid = await PartnerDataService.ValidateCustomerEmailAsync(data.Email);
                if (!isEmailValid)
                    ModelState.AddModelError(nameof(data.Email), "Email này đã được sử dụng. Vui lòng dùng email khác.");
            }

            if (!ModelState.IsValid)
            {
                ViewBag.ReturnUrl = returnUrl;
                return View(data);
            }

            try
            {
                data.ContactName = data.CustomerName;
                data.IsLocked = false;
                await PartnerDataService.AddCustomerAsync(data);

                TempData["SuccessMessage"] = "Đăng ký tài khoản thành công! Vui lòng đăng nhập.";

                if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
                    return RedirectToAction("Login", new { returnUrl });
                return RedirectToAction("Login");
            }
            catch
            {
                ModelState.AddModelError(string.Empty, "Đã xảy ra lỗi hệ thống, vui lòng thử lại sau.");
                ViewBag.ReturnUrl = returnUrl;
                return View(data);
            }
        }

        /// <summary>
        /// Giao diện đăng nhập
        /// </summary>
        /// <param name="returnUrl"></param>
        /// <returns></returns>
        [HttpGet]
        public IActionResult Login(string returnUrl = "")
        {
            ViewBag.ReturnUrl = returnUrl;
            return View();
        }

        /// <summary>
        /// Xử lý đăng nhập khách hàng
        /// </summary>
        /// <param name="email"></param>
        /// <param name="password"></param>
        /// <param name="rememberMe"></param>
        /// <param name="returnUrl"></param>
        /// <returns></returns>
        [HttpPost]
        public async Task<IActionResult> Login(string email, string password, bool rememberMe = false, string returnUrl = "")
        {
            if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
            {
                ModelState.AddModelError(string.Empty, "Vui lòng nhập đầy đủ email và mật khẩu.");
                return View();
            }

            var account = await SecurityDataService.AuthorizeAsync(email, password, UserTypes.Customer);
            if (account == null)
            {
                ModelState.AddModelError(string.Empty, "Email hoặc mật khẩu không chính xác.");
                ViewData["Email"] = email;
                return View();
            }

            var customer = await PartnerDataService.GetCustomerAsync(int.Parse(account.UserID));
            if (customer != null && customer.IsLocked == true)
            {
                ModelState.AddModelError(string.Empty, "Tài khoản của bạn đã bị khóa. Vui lòng liên hệ bộ phận hỗ trợ.");
                ViewData["Email"] = email;
                return View();
            }

            var userData = new WebUserData
            {
                UserId = account.UserID,
                UserName = account.Email,
                DisplayName = account.FullName,
                Photo = account.Photo,
                Roles = new List<string> { "customer" }
            };

            var authProperties = new AuthenticationProperties
            {
                IsPersistent = rememberMe,
                ExpiresUtc = rememberMe
                    ? DateTimeOffset.UtcNow.AddDays(30)
                    : DateTimeOffset.UtcNow.AddDays(7)
            };

            await HttpContext.SignInAsync(userData.CreatePrincipal(), authProperties);

            if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
                return Redirect(returnUrl);
            return RedirectToAction("Index", "Home");
        }

        /// <summary>
        /// Giao diện hiển thị thông tin cá nhân
        /// </summary>
        /// <returns></returns>
        [Authorize]
        [HttpGet]
        public async Task<IActionResult> Profile()
        {
            var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!int.TryParse(userIdStr, out int userId))
                return RedirectToAction("Login");

            var customer = await PartnerDataService.GetCustomerAsync(userId);
            if (customer == null)
                return RedirectToAction("Login");

            return View(customer);
        }

        /// <summary>
        /// Xử lý cập nhật thông tin cá nhân
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        [Authorize]
        [HttpPost]
        public async Task<IActionResult> UpdateProfile(Customer model)
        {
            var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!int.TryParse(userIdStr, out int userId))
                return Json(new { success = false, message = "Vui lòng đăng nhập lại." });

            var customer = await PartnerDataService.GetCustomerAsync(userId);
            if (customer == null)
                return Json(new { success = false, message = "Không tìm thấy khách hàng." });

            if (string.IsNullOrWhiteSpace(model.CustomerName))
                return Json(new { success = false, message = "Họ tên không được để trống.", field = "CustomerName" });
            
            var nameRegex = new System.Text.RegularExpressions.Regex(@"^[\p{L} ]+$");
            if (!nameRegex.IsMatch(model.CustomerName))
                return Json(new { success = false, message = "Họ tên chỉ được chứa chữ cái và khoảng trắng.", field = "CustomerName" });

            if (model.CustomerName.Trim().Length < 2)
                return Json(new { success = false, message = "Họ tên phải từ 2 ký tự.", field = "CustomerName" });

            if (!string.IsNullOrWhiteSpace(model.Phone))
            {
                string digits = new string(model.Phone.Where(char.IsDigit).ToArray());
                if (digits.Length < 10 || digits.Length > 11 || digits.Length != model.Phone.Length)
                    return Json(new { success = false, message = "Số điện thoại phải từ 10-11 chữ số.", field = "Phone" });
            }

            customer.CustomerName = model.CustomerName.Trim();
            customer.ContactName = model.CustomerName.Trim();
            customer.Phone = model.Phone;
            customer.Province = model.Province;
            customer.Address = model.Address;

            bool ok = await PartnerDataService.UpdateCustomerAsync(customer);
            if (ok)
            {
                var userData = new WebUserData
                {
                    UserId = customer.CustomerID.ToString(),
                    UserName = customer.Email,
                    DisplayName = customer.CustomerName,
                    Photo = User.FindFirst("Photo")?.Value ?? "",
                    Roles = new List<string> { "customer" }
                };
                await HttpContext.SignInAsync(userData.CreatePrincipal());
                return Json(new { success = true, message = "Cập nhật thông tin cá nhân thành công!" });
            }

            return Json(new { success = false, message = "Cập nhật thông tin thất bại. Vui lòng thử lại." });
        }

        /// <summary>
        /// Xử lý thay đổi mật khẩu khách hàng
        /// </summary>
        /// <param name="oldPassword"></param>
        /// <param name="newPassword"></param>
        /// <param name="confirmPassword"></param>
        /// <returns></returns>
        [Authorize]
        [HttpPost]
        public async Task<IActionResult> ChangePassword(string oldPassword, string newPassword, string confirmPassword)
        {
            if (string.IsNullOrWhiteSpace(oldPassword) || string.IsNullOrWhiteSpace(newPassword) || string.IsNullOrWhiteSpace(confirmPassword))
                return Json(new { success = false, message = "Vui lòng điền đầy đủ thông tin." });

            if (newPassword != confirmPassword)
                return Json(new { success = false, message = "Mật khẩu xác nhận không khớp.", field = "confirmPassword" });

            if (newPassword.Length < 6)
                return Json(new { success = false, message = "Mật khẩu mới phải có ít nhất 6 ký tự.", field = "newPassword" });

            var email = User.Identity?.Name;
            if (string.IsNullOrEmpty(email))
                return Json(new { success = false, message = "Vui lòng đăng nhập lại." });

            var account = await SecurityDataService.AuthorizeAsync(email, oldPassword, UserTypes.Customer);
            if (account == null)
                return Json(new { success = false, message = "Mật khẩu cũ không chính xác.", field = "oldPassword" });

            bool result = await SecurityDataService.ChangePasswordAsync(email, newPassword, UserTypes.Customer);
            return Json(new { 
                success = result, 
                message = result ? "Đổi mật khẩu thành công!" : "Đã có lỗi xảy ra khi đổi mật khẩu." 
            });
        }

        /// <summary>
        /// Xử lý đăng xuất khỏi tài khoản
        /// </summary>
        /// <returns></returns>
        [HttpGet]
        public async Task<IActionResult> Logout()
        {
            ShoppingCartService.ClearCart();
            await HttpContext.SignOutAsync();
            return RedirectToAction("Index", "Home");
        }
    }
}
