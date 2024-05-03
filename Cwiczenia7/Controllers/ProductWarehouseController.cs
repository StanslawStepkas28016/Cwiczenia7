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
    public IActionResult AddProduct(ProductWarehouse product)
    {
        var res = _productWarehouseService.AddProduct(product);

        if (!(res > 0))
        {
            return StatusCode(StatusCodes.Status403Forbidden);
        }
        
        return StatusCode(StatusCodes.Status201Created);

    }
}