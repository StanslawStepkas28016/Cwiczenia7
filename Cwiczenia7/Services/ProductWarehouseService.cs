using Cwiczenia7.Model;
using Cwiczenia7.Repositories;
using Microsoft.AspNetCore.Mvc;

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

    public async Task<int> AddProductProcedure(ProductWarehouse product)
    {
        return await _productWarehouseRepository.AddProductProcedure(product);
    }
}