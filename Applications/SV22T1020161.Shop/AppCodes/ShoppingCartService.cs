using SV22T1020161.Shop.Models;

namespace SV22T1020161.Shop
{
    /// <summary>
    /// Cung cấp các chức năng xử lý trên giỏ hàng (giỏ hàng lưu trong session)
    /// </summary>
    public static class ShoppingCartService
    {
        private const string CART_SESSION_KEY = "UserCart";

        /// <summary>
        /// Lấy giỏ hàng từ session
        /// </summary>
        /// <returns></returns>
        public static List<CartItem> GetCart()
        {
            var cart = ApplicationContext.GetSessionData<List<CartItem>>(CART_SESSION_KEY);
            if (cart == null)
            {
                cart = new List<CartItem>();
                ApplicationContext.SetSessionData(CART_SESSION_KEY, cart);
            }
            return cart;
        }

        /// <summary>
        /// Lưu giỏ hàng vào session
        /// </summary>
        /// <param name="cart"></param>
        public static void SaveCart(List<CartItem> cart)
        {
            ApplicationContext.SetSessionData(CART_SESSION_KEY, cart);
        }

        /// <summary>
        /// Tính tổng số lượng các mặt hàng trong giỏ hàng
        /// </summary>
        /// <returns></returns>
        public static int GetCartCount()
        {
            var cart = GetCart();
            return cart.Count;
        }

        /// <summary>
        /// Xóa sạch giỏ hàng khỏi session
        /// </summary>
        public static void ClearCart()
        {
            ApplicationContext.ClearSessionData(CART_SESSION_KEY);
        }
    }
}
