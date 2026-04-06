using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using SV22T1020161.Models.Constants;
using SV22T1020161.Models.Security;

namespace SV22T1020161.Admin
{
    /// <summary>
    /// Lớp cung cấp các tiện ích liên quan đến ngữ cảnh của ứng dụng web
    /// </summary>
    public static class ApplicationContext
    {
        private static IHttpContextAccessor? _httpContextAccessor;
        private static IWebHostEnvironment? _webHostEnvironment;
        private static IConfiguration? _configuration;

        /// <summary>
        /// Gọi hàm này trong Program
        /// </summary>
        /// <param name="httpContextAccessor">app.Services.GetRequiredService<IHttpContextAccessor>()</param>
        /// <param name="webHostEnvironment">app.Services.GetRequiredService<IWebHostEnvironment>()</param>
        /// <param name="configuration"><app.Configuration/param>
        /// <exception cref="ArgumentNullException"></exception>
        public static void Configure(IHttpContextAccessor httpContextAccessor, IWebHostEnvironment webHostEnvironment, IConfiguration configuration)
        {
            _httpContextAccessor = httpContextAccessor ?? throw new ArgumentNullException();
            _webHostEnvironment = webHostEnvironment ?? throw new ArgumentNullException();
            _configuration = configuration ?? throw new ArgumentNullException();
        }

        /// <summary>
        /// HttpContext
        /// </summary>
        public static HttpContext? HttpContext => _httpContextAccessor?.HttpContext;
        /// <summary>
        /// WebHostEnviroment
        /// </summary>
        public static IWebHostEnvironment? WebHostEnviroment => _webHostEnvironment;
        /// <summary>
        /// Configuration
        /// </summary>
        public static IConfiguration? Configuration => _configuration;

        /// <summary>
        /// URL của website, kết thúc bởi dấu / (ví dụ: https://mywebsite.com/)
        /// </summary>
        public static string BaseURL => $"{HttpContext?.Request.Scheme}://{HttpContext?.Request.Host}/";
        /// <summary>
        /// Đường dẫn vật lý đến thư mục wwwroot
        /// </summary>
        public static string WWWRootPath => _webHostEnvironment?.WebRootPath ?? string.Empty;
        /// <summary>
        /// Đường dẫn vật lý đến thư mục gốc lưu ứng dụng Web
        /// </summary>
        public static string ApplicationRootPath => _webHostEnvironment?.ContentRootPath ?? string.Empty;        

        /// <summary>
        /// Ghi dữ liệu vào session
        /// </summary>
        /// <param name="key"></param>
        /// <param name="value"></param>
        public static void SetSessionData(string key, object value)
        {
            try
            {
                string sValue = JsonConvert.SerializeObject(value);
                if (!string.IsNullOrEmpty(sValue))
                {
                    _httpContextAccessor?.HttpContext?.Session.SetString(key, sValue);
                }
            }
            catch
            {
            }
        }

        /// <summary>
        /// Đọc dữ liệu từ session
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="key"></param>
        /// <returns></returns>
        public static T? GetSessionData<T>(string key) where T : class
        {
            try
            {
                string sValue = _httpContextAccessor?.HttpContext?.Session.GetString(key) ?? "";
                if (!string.IsNullOrEmpty(sValue))
                {
                    return JsonConvert.DeserializeObject<T>(sValue);
                }
            }
            catch
            {
            }
            return null;
        }

        /// <summary>
        /// Lấy chuỗi giá trị của cấu hình trong appsettings.json
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public static string GetConfigValue(string name)
        {
            return _configuration?[name] ?? "";
        }

        /// <summary>
        /// Lấy đối tượng có kiểu là T trong phần cấu hình có tên là name trong appsettings.json
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="name"></param>
        /// <returns></returns>
        public static T GetConfigSection<T>(string name) where T : new()
        {
            var value = new T();
            _configuration?.GetSection(name).Bind(value);
            return value;
        }

        /// <summary>
        /// Lấy WebUser của người dùng hiện tại từ HttpContext.
        /// Cung cấp các phương thức kiểm tra quyền.
        /// </summary>
        public static WebUser? CurrentUser
        {
            get
            {
                var user = HttpContext?.User;
                if (user == null || user.Identity?.IsAuthenticated != true)
                    return null;
                return new WebUser(user);
            }
        }

        /// <summary>
        /// Kiểm tra nhanh người dùng hiện tại có quyền cụ thể hay không.
        /// </summary>
        /// <param name="permission">Mã quyền</param>
        public static bool HasPermission(string permission)
        {
            return CurrentUser?.HasPermission(permission) ?? false;
        }

        /// <summary>
        /// Kiểm tra nhanh người dùng hiện tại có vai trò cụ thể hay không.
        /// </summary>
        /// <param name="role">Tên vai trò</param>
        public static bool HasRole(string role)
        {
            return CurrentUser?.HasRole(role) ?? false;
        }
    }
}

namespace SV22T1020161.Admin
{
    /// <summary>
    /// Requirement cho kiểm tra quyền: user cần có ÍT NHẤT MỘT trong danh sách permissions (OR logic)
    /// </summary>
    public class PermissionRequirement : IAuthorizationRequirement
    {
        public IReadOnlyList<string> Permissions { get; }
        public PermissionRequirement(params string[] permissions)
        {
            Permissions = permissions.ToList().AsReadOnly();
        }
    }

    /// <summary>
    /// Handler kiểm tra quyền: user cần có ÍT NHẤT MỘT permission trong danh sách
    /// </summary>
    public class PermissionHandler : AuthorizationHandler<PermissionRequirement>
    {
        protected override Task HandleRequirementAsync(
            AuthorizationHandlerContext context,
            PermissionRequirement requirement)
        {
            foreach (var perm in requirement.Permissions)
            {
                if (context.User.HasClaim("Permission", perm))
                {
                    context.Succeed(requirement);
                    return Task.CompletedTask;
                }
            }
            return Task.CompletedTask;
        }
    }

    /// <summary>
    /// Attribute yêu cầu user có ÍT NHẤT MỘT permission trong danh sách (OR logic).
    /// </summary>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true)]
    public class AuthorizePermissionAttribute : AuthorizeAttribute
    {
        public AuthorizePermissionAttribute(params string[] permissions)
        {
            var sortedPerms = permissions.OrderBy(p => p).ToList();
            var combinedKey = string.Join("_", sortedPerms).Replace(":", "_");
            Policy = "Permission_" + combinedKey;
        }
    }

    /// <summary>
    /// Attribute yêu cầu user phải có TẤT CẢ permissions trong danh sách (AND logic).
    /// </summary>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true)]
    public class AuthorizeAllPermissionsAttribute : AuthorizeAttribute
    {
        public AuthorizeAllPermissionsAttribute(params string[] permissions)
        {
            var sortedPerms = permissions.OrderBy(p => p).ToList();
            var combinedKey = string.Join("_", sortedPerms).Replace(":", "_");
            Policy = "AllPermissions_" + combinedKey;
        }
    }

    /// <summary>
    /// Cung cấp Authorization Policy động cho các permission.
    /// Hỗ trợ policy đơn lẻ và kết hợp mà không cần đăng ký trước.
    /// </summary>
    public class DynamicPermissionPolicyProvider : IAuthorizationPolicyProvider
    {
        private const string PermissionPrefix = "Permission_";
        private readonly DefaultAuthorizationPolicyProvider _fallback;

        public DynamicPermissionPolicyProvider(IOptions<AuthorizationOptions> options)
        {
            _fallback = new DefaultAuthorizationPolicyProvider(options);
        }

        public Task<AuthorizationPolicy> GetDefaultPolicyAsync()
            => _fallback.GetDefaultPolicyAsync();

        public Task<AuthorizationPolicy?> GetFallbackPolicyAsync()
            => _fallback.GetFallbackPolicyAsync();

        public Task<AuthorizationPolicy?> GetPolicyAsync(string policyName)
        {
            if (policyName.StartsWith(PermissionPrefix, StringComparison.OrdinalIgnoreCase))
            {
                var combinedKey = policyName[PermissionPrefix.Length..];
                var permissions = ParsePermissionsFromKey(combinedKey);
                if (permissions.Length > 0)
                {
                    var policy = new AuthorizationPolicyBuilder()
                        .AddRequirements(new PermissionRequirement(permissions))
                        .Build();
                    return Task.FromResult<AuthorizationPolicy?>(policy);
                }
            }
            return _fallback.GetPolicyAsync(policyName);
        }

        private static string[] ParsePermissionsFromKey(string combinedKey)
        {
            var segments = combinedKey.Split('_');
            if (segments.Length % 2 != 0)
                return new[] { combinedKey.Replace('_', ':') };
            var result = new List<string>();
            for (int i = 0; i < segments.Length; i += 2)
                result.Add($"{segments[i]}:{segments[i + 1]}");
            return result.ToArray();
        }
    }
}
