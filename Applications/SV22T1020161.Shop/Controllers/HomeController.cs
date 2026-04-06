using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using SV22T1020161.Shop.Models;
using SV22T1020161.BusinessLayers;
using SV22T1020161.Models.Catalog;
using SV22T1020161.Models.Common;

namespace SV22T1020161.Shop.Controllers
{
    /// <summary>
    /// Controller trang chủ của website (hiển thị danh sách sản phẩm)
    /// </summary>
    public class HomeController : Controller
    {
        private const int PAGE_SIZE = 12;

        /// <summary>
        /// Giao diện trang chủ hiển thị danh sách sản phẩm và bộ lọc
        /// </summary>
        /// <param name="page"></param>
        /// <param name="categoryID"></param>
        /// <param name="searchValue"></param>
        /// <param name="minPrice"></param>
        /// <param name="maxPrice"></param>
        /// <returns></returns>
        public async Task<IActionResult> Index(
            int page = 1,
            int categoryID = 0,
            string searchValue = "",
            decimal minPrice = 0,
            decimal maxPrice = 0)
        {
            var categories = await CatalogDataService.ListCategoriesAsync(
                new PaginationSearchInput { Page = 1, PageSize = 100 });

            var input = new ProductSearchInput
            {
                Page = page,
                PageSize = PAGE_SIZE,
                SearchValue = searchValue ?? "",
                CategoryID = categoryID,
                MinPrice = minPrice,
                MaxPrice = maxPrice
            };
            var products = await CatalogDataService.ListProductsAsync(input);

            ViewBag.Categories = categories;
            ViewBag.ProductResult = products;
            ViewBag.CurrentCategoryID = categoryID;
            ViewBag.CurrentSearchValue = searchValue;
            ViewBag.CurrentMinPrice = minPrice;
            ViewBag.CurrentMaxPrice = maxPrice;

            return View();
        }

        /// <summary>
        /// Tìm kiếm và trả về danh sách sản phẩm (Ajax)
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
        public async Task<IActionResult> Search(ProductSearchInput input)
        {
            if (input.MaxPrice > 0 && input.MinPrice > 0 && input.MaxPrice < input.MinPrice)
            {
                input.MaxPrice = 0;
            }
            if (input.MinPrice < 0) input.MinPrice = 0;
            if (input.MaxPrice < 0) input.MaxPrice = 0;

            input.PageSize = PAGE_SIZE;
            var data = await CatalogDataService.ListProductsAsync(input);
            return PartialView("_ProductGrid", data);
        }

        /// <summary>
        /// Giao diện chính sách bảo mật
        /// </summary>
        /// <returns></returns>
        public IActionResult Privacy()
        {
            return View();
        }

        /// <summary>
        /// Giao diện thông báo lỗi
        /// </summary>
        /// <returns></returns>
        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
