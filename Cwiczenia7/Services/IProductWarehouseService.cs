using Cwiczenia7.Model;

namespace Cwiczenia7.Services;

public interface IProductWarehouseService
{
    public Task<int> AddProduct(ProductWarehouse product);
}