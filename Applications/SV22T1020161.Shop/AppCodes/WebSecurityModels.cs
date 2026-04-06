using Microsoft.AspNetCore.Authentication.Cookies;
using System.Security.Claims;

namespace SV22T1020161.Shop
{
    /// <summary>
    /// Thông tin tài khoản khách hàng được lưu trong phiên đăng nhập (cookie) của Shop
    /// </summary>
    public class WebUserData
    {
        public string? UserId { get; set; }
        public string? UserName { get; set; }
        public string? DisplayName { get; set; }
        public string? Photo { get; set; }
        public List<string>? Roles { get; set; }

        /// <summary>
        /// Tạo danh sách các claim chứa thông tin
        /// </summary>
        private List<Claim> Claims
        {
            get
            {
                var claims = new List<Claim>
                {
                    new Claim(ClaimTypes.NameIdentifier, UserId ?? ""),
                    new Claim(ClaimTypes.Name, UserName ?? ""),
                    new Claim(nameof(DisplayName), DisplayName ?? ""),
                    new Claim(nameof(Photo), Photo ?? "")
                };
                if (Roles != null)
                {
                    foreach (var role in Roles)
                    {
                        claims.Add(new Claim(ClaimTypes.Role, role));
                    }
                }
                return claims;
            }
        }

        /// <summary>
        /// Tạo Principal dựa trên thông tin phiên đăng nhập
        /// </summary>
        /// <returns></returns>
        public ClaimsPrincipal CreatePrincipal()
        {
            var identity = new ClaimsIdentity(Claims, CookieAuthenticationDefaults.AuthenticationScheme);
            return new ClaimsPrincipal(identity);
        }
    }

    /// <summary>
    /// Các phương thức mở rộng liên quan đến bảo mật
    /// </summary>
    public static class WebUserExtensions
    {
        /// <summary>
        /// Đọc thông tin của phiên đăng nhập từ Principal
        /// </summary>
        /// <param name="principal"></param>
        /// <returns></returns>
        public static WebUserData? GetUserData(this ClaimsPrincipal principal)
        {
            if (principal == null || principal.Identity == null || !principal.Identity.IsAuthenticated)
                return null;

            var userData = new WebUserData
            {
                UserId = principal.FindFirstValue(ClaimTypes.NameIdentifier),
                UserName = principal.FindFirstValue(ClaimTypes.Name),
                DisplayName = principal.FindFirstValue("DisplayName"),
                Photo = principal.FindFirstValue("Photo")
            };

            userData.Roles = new List<string>();
            foreach (var claim in principal.FindAll(ClaimTypes.Role))
            {
                userData.Roles.Add(claim.Value);
            }

            return userData;
        }
    }
}
