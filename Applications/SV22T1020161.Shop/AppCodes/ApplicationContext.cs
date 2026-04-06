using Newtonsoft.Json;
using SV22T1020161.Shop.Models;

namespace SV22T1020161.Shop
{
    /// <summary>
    /// Lớp cung cấp các tiện ích liên quan đến ngữ cảnh của ứng dụng (context)
    /// </summary>
    public static class ApplicationContext
    {
        private static IHttpContextAccessor? _httpContextAccessor;
        private static IWebHostEnvironment? _webHostEnvironment;
        private static IConfiguration? _configuration;

        /// <summary>
        /// Phương thức khởi tạo cấu hình cho ApplicationContext
        /// </summary>
        /// <param name="httpContextAccessor"></param>
        /// <param name="webHostEnvironment"></param>
        /// <param name="configuration"></param>
        public static void Configure(IHttpContextAccessor httpContextAccessor, IWebHostEnvironment webHostEnvironment, IConfiguration configuration)
        {
            _httpContextAccessor = httpContextAccessor;
            _webHostEnvironment = webHostEnvironment;
            _configuration = configuration;
        }

        /// <summary>
        /// HttpContext hiện tại
        /// </summary>
        public static HttpContext? HttpContext => _httpContextAccessor?.HttpContext;

        /// <summary>
        /// Configuration
        /// </summary>
        public static IConfiguration? Configuration => _configuration;

        /// <summary>
        /// Phục vụ cho việc đọc dữ liệu từ Session
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="key"></param>
        /// <returns></returns>
        public static T? GetSessionData<T>(string key)
        {
            var session = HttpContext?.Session;
            if (session == null) return default;
            var data = session.GetString(key);
            return string.IsNullOrEmpty(data) ? default : JsonConvert.DeserializeObject<T>(data);
        }

        /// <summary>
        /// Phục vụ cho việc ghi dữ liệu vào Session
        /// </summary>
        /// <param name="key"></param>
        /// <param name="data"></param>
        public static void SetSessionData(string key, object data)
        {
            var session = HttpContext?.Session;
            if (session == null) return;
            session.SetString(key, JsonConvert.SerializeObject(data));
        }

        /// <summary>
        /// Xóa dữ liệu khỏi session
        /// </summary>
        /// <param name="key"></param>
        public static void ClearSessionData(string key)
        {
            HttpContext?.Session.Remove(key);
        }
    }
}
