    using Microsoft.AspNetCore.Mvc;
    using Vault.Core;

    namespace Vault.Features.Ebay;

    [Route("ebay")]
    public sealed class EbayController(EbayService service) : Controller
    {
        // ------------------------------------------------------------------------
        // ENTRY POINT — SHOW LOADER
        // ------------------------------------------------------------------------
        [HttpGet("")]
        public IActionResult Index()
        {
            return View("_Loader", new LoaderModel
            {
                Message = "Scanning eBay listings...",
                TargetUrl = Url.Action("", "ebay"),
                TargetAction = "LoadEbay"
            });
        }

        // ------------------------------------------------------------------------
        // LOADER TARGET — RUN THE OPERATION
        // ------------------------------------------------------------------------
        [HttpPost("")]
        [LoaderAction]
        public async Task<IActionResult> IndexPost(
            [FromHeader(Name = "X-Loader-Action")] string action)
        {
            return action.ToLower() switch
            {
                "loadebay" => await LoadEbayItems(),
                _ => NotFound()
            };
        }

        // ------------------------------------------------------------------------
        // INTERNAL: RUN THE EBAY SCAN AND RETURN JSON FOR THE LOADER
        // ------------------------------------------------------------------------
        private async Task<IActionResult> LoadEbayItems()
        {
            var model = await service.LoadItemsAsync();

            return await ViewModeResult.BuildAsync(
                this,
                service.DefaultViewMode,
                model
            );
        }

    // ---------------------------------------------------------------
    // EXCLUSION POST ENDPOINT (UI only - persistence later)
    [HttpPost("exclude")]
        public IActionResult Exclude([FromForm] EbayExclusionRequest request)
    {
        // request.ItemId = vault item id
        // request.ListingIds = all selected listing IDs

        // You manually set ListingType when writing to storage
        // (e.g., "eBay")

        return RedirectToAction("Index");
    }


}
