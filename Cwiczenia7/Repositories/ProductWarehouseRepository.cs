using System.Data.SqlClient;
using Cwiczenia7.Model;

namespace Cwiczenia7.Repositories;

public class ProductWarehouseRepository : IProductWarehouseRepository
{
    private readonly string _connectionString;

    public ProductWarehouseRepository(IConfiguration configuration)
    {
        _connectionString = configuration.GetConnectionString("DefaultConnection");
    }

    public async Task<int> AddProduct(ProductWarehouse product)
        // Async w nazwie metody, jeżeli metoda korzysta z jakichkolwiek metod asynchronicznych, żeby dać 
        // znać programiście, że metoda korzysta z metod asynchronicznych. 
    {
        if (await DoesProductAndWarehouseExistAndAmountGreaterThanZero(product) == false)
        {
            return -1;
        }

        if (await DoesOrderExist(product) == false)
        {
            return -1;
        }

        if (await DoesOrderAlreadyExistInWarehouse(product))
        {
            return -1;
        }

        if (await IsCreationDateEarlierThanProvidedDate(product) == false)
        {
            return -1;
        }

        // Aktualizacja kolumny FulfilledAt na aktualną datę (UPDATE),
        // metoda zwraca idOrder, potrzebne do wykonania INSERT INTO Product_Warehouse.
        var idOrder = await UpdateOrderFulfilledAt(product);
        if (idOrder == -1)
        {
            return -1;
        }

        // Wstawienie rekordu do tabeli Product_Warehouse,
        // - Kolumna [Product_Warehouse] Price ma odpowiadać [Product_Warehouse] Amount * [Product] Price,
        // - Kolumna [Product_Warehouse] CreatedAt ma mieć aktualny czas,
        var idProductWarehouse = await InsertIntoProduct_Warehouse(product, idOrder);
        if (idProductWarehouse == -1)
        {
            return -1;
        }

        return idProductWarehouse;
    }

    private async Task<int> InsertIntoProduct_Warehouse(ProductWarehouse product, int idOrder)
    {
        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync();
        var transaction = connection.BeginTransaction();

        try
        {
            const string productPriceQuery = "SELECT Price AS ProductPrice FROM Product WHERE IdProduct = @IdProduct;";
            double totalProductPrice = product.Amount;

            await using (var productPriceCommand = new SqlCommand(productPriceQuery, connection, transaction))
            {
                productPriceCommand.Parameters.AddWithValue("@IdProduct", product.IdProduct);

                await using (var reader = await productPriceCommand.ExecuteReaderAsync())
                {
                    if (await reader.ReadAsync())
                    {
                        var price = (decimal)reader["ProductPrice"];
                        totalProductPrice *= (double)price;
                    }
                }
            }


            const string insertQuery = """
                                       INSERT INTO Product_Warehouse(IdWarehouse, IdProduct, IdOrder, Amount, Price, CreatedAt)
                                                                       VALUES(@IdWarehouse, @IdProduct, @IdOrder, @Amount, @Price, CURRENT_TIMESTAMP);
                                                                       SELECT SCOPE_IDENTITY();
                                       """;

            await using (var insertQueryCommand = new SqlCommand(insertQuery, connection, transaction))
            {
                insertQueryCommand.Parameters.AddWithValue("@IdWarehouse", product.IdWarehouse);
                insertQueryCommand.Parameters.AddWithValue("@IdProduct", product.IdProduct);
                insertQueryCommand.Parameters.AddWithValue("@IdOrder", idOrder);
                insertQueryCommand.Parameters.AddWithValue("@Amount", product.Amount);
                insertQueryCommand.Parameters.AddWithValue("@Price", totalProductPrice);

                // Tutaj ma być zwrócone IdProductWarehouse, które jest z IDENTITY.
                var res = await insertQueryCommand.ExecuteScalarAsync();

                if (res != null)
                {
                    transaction.Commit();
                    return Convert.ToInt32(res);
                }
            }
        }
        catch (Exception e)
        {
            transaction.Rollback();
            throw new Exception("Failed to \'InsertIntoProduct_Warehouse\'!");
        }

        return -1;
    }

    private async Task<int> UpdateOrderFulfilledAt(ProductWarehouse product)
    {
        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync();
        var transaction = connection.BeginTransaction();

        try
        {
            const string updateQuery = """
                                       
                                                           UPDATE [Order]
                                                           SET FulfilledAt = CURRENT_TIMESTAMP
                                                           OUTPUT INSERTED.IdOrder
                                                           WHERE IdProduct = @IdProduct;
                                       """;

            await using var updateCommand = new SqlCommand(updateQuery, connection, transaction);
            updateCommand.Parameters.AddWithValue("@IdProduct", product.IdProduct);
            await using var reader = await updateCommand.ExecuteReaderAsync();
            
            if (await reader.ReadAsync())
            {
                var idOrder = (int)reader["IdOrder"];
                await reader.CloseAsync();
                transaction.Commit();
                return idOrder;
            }
        }
        catch (Exception e)
        {
            transaction.Rollback();
            await Console.Out.WriteLineAsync(e.Message);
            throw new Exception("Failed to \'UpdateOrderFulfilledAt\'! " + e.Message + " " + e);
        }

        return -1;
    }

    private async Task<bool> DoesProductAndWarehouseExistAndAmountGreaterThanZero(ProductWarehouse product)
    {
        // Sprawdzenie, czy produkt i magazyn o podanym Id
        // istnieje oraz sprawdzenie, czy przekazana ilość 
        // (Amount), jest większa niż 0.
        if (!(product.Amount > 0))
        {
            return false;
        }

        const string query = """
                             SELECT (SELECT 1 FROM Product WHERE IdProduct = @IdProduct) AS ProductExists,
                                                             (SELECT 1 FROM Warehouse WHERE IdWarehouse = @IdWarehouse) AS WarehouseExists;
                             """;

        await using var connection = new SqlConnection(_connectionString);
        await using var command = new SqlCommand(query, connection);
        command.Parameters.AddWithValue("@IdProduct", product.IdProduct);
        command.Parameters.AddWithValue("@IdWarehouse", product.IdWarehouse);

        await connection.OpenAsync();
        await using var reader = await command.ExecuteReaderAsync();
        
        if (await reader.ReadAsync())
        {
            var productExists = reader["ProductExists"] != DBNull.Value;
            var warehouseExists = reader["WarehouseExists"] != DBNull.Value;

            return productExists && warehouseExists;
        }

        return false;
    }

    private async Task<bool> DoesOrderExist(ProductWarehouse product)
    {
        // Sprawdzenie, czy istnieje rekord w tabeli Order, który
        // zawiera Id i Amount z obiektu (modelu) podanego w argumencie.
        const string query =
            "SELECT 1 AS OrderExists FROM [Order] WHERE IdProduct = @IdWarehouse AND Amount = @Amount;";

        await using var connection = new SqlConnection(_connectionString);
        await using var command = new SqlCommand(query, connection);
        command.Parameters.AddWithValue("@IdWarehouse", product.IdWarehouse);
        command.Parameters.AddWithValue("@Amount", product.Amount);

        await connection.OpenAsync();

        await using var reader = await command.ExecuteReaderAsync();

        if (await reader.ReadAsync())
        {
            return reader["OrderExists"] != DBNull.Value;
        }

        return false;
    }

    private async Task<bool> IsCreationDateEarlierThanProvidedDate(ProductWarehouse productWarehouse)
    {
        // Sprawdzenie, czy data utworzenia Order (CreatedAt) jest wcześniejsza,
        // niż data z obiektu (modelu) (CreatedAt).
        const string query = """
                             SELECT 1 AS CreationEarlier FROM [Order]
                                                   WHERE CreatedAt < @CreatedAt;
                             """;

        await using var connection = new SqlConnection(_connectionString);
        await using var command = new SqlCommand(query, connection);

        command.Parameters.AddWithValue("@CreatedAt", productWarehouse.CreatedAt);

        await connection.OpenAsync();
        await using var reader = await command.ExecuteReaderAsync();

        if (await reader.ReadAsync())
        {
            return reader["CreationEarlier"] != DBNull.Value;
        }

        return false;
    }

    private async Task<bool> DoesOrderAlreadyExistInWarehouse(ProductWarehouse product)
    {
        // Sprawdzenie, czy nie istnieje rekord w tabeli Product_Warehouse,
        // który ma takie samo IdOrder.
        const string query = """
                             SELECT 1 AS AlreadyExists FROM Product_Warehouse
                                                         WHERE IdWarehouse = @IdWarehouse
                                                         AND IdProduct = @IdProduct;
                             """;

        await using var connection = new SqlConnection(_connectionString);
        var command = new SqlCommand(query, connection);

        command.Parameters.AddWithValue("@IdWarehouse", product.IdWarehouse);
        command.Parameters.AddWithValue("@IdProduct", product.IdProduct);

        await connection.OpenAsync();

        await using var reader = await command.ExecuteReaderAsync();
        if (await reader.ReadAsync())
        {
            return reader["AlreadyExists"] != DBNull.Value;
        }

        return false;
    }
}