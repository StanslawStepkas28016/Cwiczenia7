using Cwiczenia7.Model;
using Cwiczenia7.Repositories;

namespace Cwiczenia7.Services;

public class ProductWarehouseService : IProductWarehouseService
{
    private readonly IProductWarehouseRepository _productWarehouseRepository;

    public ProductWarehouseService(IProductWarehouseRepository productWarehouseRepository)
    {
        _productWarehouseRepository = productWarehouseRepository;
    }

    public async Task<int> AddProduct(ProductWarehouse product)
    {
        return await _productWarehouseRepository.AddProduct(product);
    }
}