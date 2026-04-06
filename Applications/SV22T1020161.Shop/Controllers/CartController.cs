using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SV22T1020161.BusinessLayers;
using SV22T1020161.Models.Sales;
using SV22T1020161.Models.Common;
using SV22T1020161.Models.Catalog;
using SV22T1020161.Shop.Models;
using System.Security.Claims;

namespace SV22T1020161.Shop.Controllers
{
    /// <summary>
    /// Controller xử lý các chức năng liên quan đến giỏ hàng và đặt hàng của khách hàng
    /// </summary>
    public class CartController : Controller
    {
        private List<CartItem> GetCart() => ShoppingCartService.GetCart();
        private void SaveCart(List<CartItem> cart) => ShoppingCartService.SaveCart(cart);
        private int GetCartItemCount() => ShoppingCartService.GetCartCount();

        private List<CartItem> GetSelectedItems()
        {
            var cart = GetCart();
            if (Request.Cookies.TryGetValue("selectedCartItems", out string? cookieVal) && !string.IsNullOrWhiteSpace(cookieVal))
            {
                var ids = new HashSet<int>();
                foreach (var idStr in cookieVal.Split(','))
                {
                    if (int.TryParse(idStr.Trim(), out int parsed)) ids.Add(parsed);
                }
                if (ids.Count > 0)
                {
                    return cart.Where(i => ids.Contains(i.ProductID)).ToList();
                }
            }
            return cart;
        }

        /// <summary>
        /// Giao diện hiển thị giỏ hàng
        /// </summary>
        /// <returns></returns>
        public IActionResult Index()
        {
            var cart = GetCart();
            return View(cart);
        }

        /// <summary>
        /// Xử lý thêm sản phẩm vào giỏ hàng
        /// </summary>
        /// <param name="id"></param>
        /// <param name="quantity"></param>
        /// <returns></returns>
        [HttpPost]
        public async Task<IActionResult> Add(int id, int quantity = 1)
        {
            if (User?.Identity?.IsAuthenticated != true)
            {
                return Json(new
                {
                    success = false,
                    requireLogin = true,
                    redirectUrl = Url.Action("Login", "Account", new { returnUrl = "/Cart" }),
                    message = "Vui lòng đăng nhập để thêm sản phẩm vào giỏ hàng."
                });
            }

            if (quantity < 1) quantity = 1;

            var cart = GetCart();
            var existing = cart.FirstOrDefault(m => m.ProductID == id);

            if (existing != null)
            {
                existing.Quantity += quantity;
            }
            else
            {
                var product = await CatalogDataService.GetProductAsync(id);
                if (product == null)
                {
                    return Json(new { success = false, message = "Sản phẩm không tồn tại." });
                }
                if (product.IsSelling != true)
                {
                    return Json(new { success = false, message = "Sản phẩm hiện không còn được bán." });
                }

                cart.Add(new CartItem
                {
                    ProductID = product.ProductID,
                    ProductName = product.ProductName,
                    Photo = product.Photo ?? "",
                    Price = product.Price,
                    Unit = product.Unit,
                    Quantity = quantity
                });
            }

            SaveCart(cart);
            var itemCount = GetCartItemCount();
            return Json(new { success = true, itemCount, message = $"Đã thêm \"{cart.Last().ProductName}\" vào giỏ hàng." });
        }

        /// <summary>
        /// Xử lý cập nhật số lượng của một sản phẩm trong giỏ hàng
        /// </summary>
        /// <param name="id"></param>
        /// <param name="quantity"></param>
        /// <returns></returns>
        [HttpPost]
        public IActionResult Update(int id, int quantity)
        {
            if (User?.Identity?.IsAuthenticated != true)
            {
                return Json(new
                {
                    success = false,
                    requireLogin = true,
                    redirectUrl = Url.Action("Login", "Account", new { returnUrl = "/Cart" }),
                    message = "Vui lòng đăng nhập."
                });
            }

            if (quantity < 1) quantity = 1;

            var cart = GetCart();
            var item = cart.FirstOrDefault(m => m.ProductID == id);
            if (item != null)
            {
                item.Quantity = quantity;
                SaveCart(cart);
                return Json(new
                {
                    success = true,
                    subtotal = item.TotalPrice.ToString("N0"),
                    total = cart.Sum(c => c.TotalPrice).ToString("N0")
                });
            }
            return Json(new { success = false });
        }

        /// <summary>
        /// Xử lý xóa sản phẩm khỏi giỏ hàng
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        [HttpPost]
        public IActionResult Remove(int id)
        {
            if (User?.Identity?.IsAuthenticated != true)
            {
                return Json(new
                {
                    success = false,
                    requireLogin = true,
                    redirectUrl = Url.Action("Login", "Account", new { returnUrl = "/Cart" }),
                    message = "Vui lòng đăng nhập."
                });
            }

            var cart = GetCart();
            var item = cart.FirstOrDefault(m => m.ProductID == id);
            if (item != null)
            {
                cart.Remove(item);
                SaveCart(cart);
                return Json(new
                {
                    success = true,
                    total = cart.Sum(c => c.TotalPrice).ToString("N0"),
                    itemCount = cart.Count
                });
            }
            return Json(new { success = false });
        }

        /// <summary>
        /// Xử lý xóa sạch các mặt hàng đang có trong giỏ hàng
        /// </summary>
        /// <returns></returns>
        [HttpPost]
        public IActionResult Clear()
        {
            ShoppingCartService.ClearCart();
            return RedirectToAction("Index");
        }

        /// <summary>
        /// Giao diện thanh toán và lập đơn hàng
        /// </summary>
        /// <returns></returns>
        [Authorize]
        public async Task<IActionResult> Checkout()
        {
            var cart = GetSelectedItems();
            if (cart.Count == 0)
            {
                TempData["ErrorMessage"] = "Giỏ hàng trống. Vui lòng thêm sản phẩm trước khi thanh toán.";
                return RedirectToAction("Index");
            }

            var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!int.TryParse(userIdStr, out int userId))
                return RedirectToAction("Login", "Account");

            var customer = await PartnerDataService.GetCustomerAsync(userId);
            ViewBag.Cart = cart;
            ViewBag.Shippers = await PartnerDataService.ListShippersAsync(new PaginationSearchInput { PageSize = 100 });

            return View(customer);
        }

        /// <summary>
        /// Xử lý xác nhận đặt hàng và lưu vào cơ sở dữ liệu
        /// </summary>
        /// <param name="recipientName"></param>
        /// <param name="recipientPhone"></param>
        /// <param name="deliveryAddress"></param>
        /// <param name="deliveryProvince"></param>
        /// <param name="shipperID"></param>
        /// <param name="note"></param>
        /// <returns></returns>
        [Authorize]
        [HttpPost]
        public async Task<IActionResult> Confirm(
            string recipientName,
            string recipientPhone,
            string deliveryAddress,
            string deliveryProvince,
            int? shipperID,
            string note = "")
        {
            var cart = GetSelectedItems();
            if (cart.Count == 0) return RedirectToAction("Index");

            var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!int.TryParse(userIdStr, out int userId)) return RedirectToAction("Login", "Account");

            if (string.IsNullOrWhiteSpace(recipientName))
                ModelState.AddModelError("recipientName", "Vui lòng nhập tên người nhận.");
            if (string.IsNullOrWhiteSpace(recipientPhone))
                ModelState.AddModelError("recipientPhone", "Vui lòng nhập số điện thoại người nhận.");
            if (string.IsNullOrWhiteSpace(deliveryAddress))
                ModelState.AddModelError("deliveryAddress", "Vui lòng nhập địa chỉ giao hàng.");
            if (string.IsNullOrWhiteSpace(deliveryProvince))
                ModelState.AddModelError("deliveryProvince", "Vui lòng chọn tỉnh/thành phố giao hàng.");
            if (!shipperID.HasValue || shipperID.Value <= 0)
                ModelState.AddModelError("shipperID", "Vui lòng chọn đơn vị vận chuyển.");

            if (!ModelState.IsValid)
            {
                var customer = await PartnerDataService.GetCustomerAsync(userId);
                ViewBag.Cart = cart;
                ViewBag.Shippers = await PartnerDataService.ListShippersAsync(new PaginationSearchInput { PageSize = 100 });
                ViewData["ValidationErrors"] = ModelState.ToDictionary(
                    kvp => kvp.Key,
                    kvp => kvp.Value?.Errors.Select(e => e.ErrorMessage).ToArray() ?? Array.Empty<string>()
                );
                return View("Checkout", customer);
            }

            var fullDeliveryAddress = $"{recipientName} — {recipientPhone} — {deliveryAddress}";

            var order = new Order
            {
                CustomerID = userId,
                DeliveryAddress = fullDeliveryAddress,
                DeliveryProvince = deliveryProvince,
                ShipperID = shipperID,
                OrderTime = DateTime.Now,
                Status = OrderStatusEnum.New
            };

            int orderID = await SalesDataService.AddOrderAsync(order);

            foreach (var item in cart)
            {
                await SalesDataService.AddDetailAsync(new OrderDetail
                {
                    OrderID = orderID,
                    ProductID = item.ProductID,
                    Quantity = item.Quantity,
                    SalePrice = item.Price
                });
            }

            var fullCart = GetCart();
            foreach (var item in cart)
            {
                var c = fullCart.FirstOrDefault(x => x.ProductID == item.ProductID);
                if (c != null) fullCart.Remove(c);
            }
            SaveCart(fullCart);
            Response.Cookies.Delete("selectedCartItems");

            TempData["SuccessMessage"] = $"Đặt hàng thành công! Mã đơn hàng #{orderID}. Đơn hàng đang được chờ duyệt.";
            return RedirectToAction("Status", "Order", new { id = orderID });
        }

        /// <summary>
        /// Trả về số lượng mặt hàng trong giỏ hàng
        /// </summary>
        /// <returns></returns>
        public IActionResult GetCartCount()
        {
            if (User?.Identity?.IsAuthenticated != true)
                return Json(new { count = 0 });
            return Json(new { count = GetCartItemCount() });
        }
    }
}
