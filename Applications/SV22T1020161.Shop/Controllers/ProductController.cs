using Microsoft.AspNetCore.Mvc;
using SV22T1020161.BusinessLayers;
using SV22T1020161.Models.Catalog;
using SV22T1020161.Models.Common;

namespace SV22T1020161.Shop.Controllers
{
    /// <summary>
    /// Controller xử lý các chức năng liên quan đến sản phẩm
    /// </summary>
    public class ProductController : Controller
    {
        private const int PAGE_SIZE = 12;

        /// <summary>
        /// Giao diện tìm kiếm và hiển thị danh sách sản phẩm
        /// </summary>
        /// <param name="page"></param>
        /// <param name="categoryID"></param>
        /// <param name="searchValue"></param>
        /// <returns></returns>
        public async Task<IActionResult> Index(int page = 1, int categoryID = 0, string searchValue = "")
        {
            var input = new ProductSearchInput()
            {
                Page = page,
                PageSize = PAGE_SIZE,
                SearchValue = searchValue ?? "",
                CategoryID = categoryID,
                MinPrice = 0,
                MaxPrice = 0
            };
            ViewBag.Categories = await CatalogDataService.ListCategoriesAsync(new PaginationSearchInput { Page = 1, PageSize = 100 });
            return View(input);
        }

        /// <summary>
        /// Tìm kiếm và trả về danh sách sản phẩm (dưới dạng Partial View)
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
        /// Giao diện hiển thị thông tin chi tiết của một sản phẩm
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public async Task<IActionResult> Detail(int id)
        {
            var product = await CatalogDataService.GetProductAsync(id);
            if (product == null) return RedirectToAction("Index");

            ViewBag.Photos = await CatalogDataService.ListPhotosAsync(id);
            ViewBag.Attributes = await CatalogDataService.ListAttributesAsync(id);
            
            if (product.SupplierID.HasValue && product.SupplierID.Value > 0)
                ViewBag.Supplier = await PartnerDataService.GetSupplierAsync(product.SupplierID.Value);
                
            if (product.CategoryID.HasValue && product.CategoryID.Value > 0)
                ViewBag.Category = await CatalogDataService.GetCategoryAsync(product.CategoryID.Value);

            return View(product);
        }
    }
}
