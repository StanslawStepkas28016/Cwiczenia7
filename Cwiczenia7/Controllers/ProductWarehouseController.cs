using Microsoft.AspNetCore.Mvc;
using Cwiczenia7.Model;
using Cwiczenia7.Services;

namespace Cwiczenia7.Controllers;

[Route("api/[controller]")]
[ApiController]
public class ProductWarehouseController : ControllerBase
{
    private IProductWarehouseService _productWarehouseService;

    public ProductWarehouseController(IProductWarehouseService productWarehouseService)
    {
        _productWarehouseService = productWarehouseService;
    }

    [HttpPost]
    public async Task<IActionResult> AddProduct(ProductWarehouse product)
    {
        var res = await _productWarehouseService.AddProduct(product);

        if (!(res > 0))
        {
            return Problem("There was a problem with the product you want to add!");
        }
        
        return StatusCode(StatusCodes.Status201Created); // czyli działa, to samo pokazują też moje zapytania do bazy

    }
}