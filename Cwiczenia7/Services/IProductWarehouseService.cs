using Cwiczenia7.Model;
using Microsoft.AspNetCore.Mvc;

namespace Cwiczenia7.Services;

public interface IProductWarehouseService
{
    public Task<int> AddProduct(ProductWarehouse product);
    public Task<int> AddProductProcedure(ProductWarehouse product);
}