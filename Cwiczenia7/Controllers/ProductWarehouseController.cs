using Microsoft.AspNetCore.Mvc;
using Cwiczenia7.Model;
using Cwiczenia7.Repositories;
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

    [HttpPost("AddProduct")]
    public async Task<IActionResult> AddProduct(ProductWarehouse product)
    {
        var res = await _productWarehouseService.AddProduct(product);

        if (!(res > 0))
        {
            return Conflict("Issue with the data you provided!");
        }

        return StatusCode(StatusCodes.Status201Created); // czyli działa, to samo pokazują też moje zapytania do bazy

    }

    [HttpPost("AddProductProcedure")]
    public async Task<IActionResult> AddProductProcedure(ProductWarehouse product)
    {
        var res = await _productWarehouseService.AddProductProcedure(product);

        if ((int)ProductWarehouseRepository.ProductWarehouseError.NoOrderToFulfillWithProvidedData == res)
        {
            return Conflict("No order to fulfill with the data provided!");
        }
        
        if ((int)ProductWarehouseRepository.ProductWarehouseError.InvalidProductId == res)
        {
            return Conflict("Invalid product Id!");
        }
        
        return StatusCode(StatusCodes.Status201Created);
    }
}