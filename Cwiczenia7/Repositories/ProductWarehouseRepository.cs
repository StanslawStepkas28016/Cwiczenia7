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

    public int AddProduct(ProductWarehouse product)
        // Async w nazwie metody, jeżeli metoda korzysta z jakichkolwiek metod asynchronicznych, żeby dać 
        // znać programiście, że metoda korzysta z metod asynchronicznych. 
    {
        if (!DoesProductAndWarehouseExistAndAmountGreaterThanZero(product).Result)
        {
            return -1;
        }

        if (!DoesOrderExist(product).Result)
        {
            return -1;
        }

        if (DoesOrderAlreadyExistInWarehouse(product).Result)
        {
            return -1;
        }

        // Aktualizacja kolumny FulfilledAt na aktualną datę (UPDATE),
        // metoda zwraca idOrder, potrzebne do wykonania INSERT INTO Product_Warehouse.
        int idOrder = UpdateOrderFulfilledAt(product).Result;
        if (idOrder == -1)
        {
            return -1;
        }

        // Wstawienie rekordu do tabeli Product_Warehouse,
        // - Kolumna [Product_Warehouse] Price ma odpowiadać [Product_Warehouse] Amount * [Product] Price,
        // - Kolumna [Product_Warehouse] CreatedAt ma mieć aktualny czas,
        var idProductWarehouse = InsertIntoProduct_Warehouse(product, idOrder).Result;
        if (idProductWarehouse == -1)
        {
            return -1;
        }

        return idProductWarehouse;
    }

    private async Task<int> InsertIntoProduct_Warehouse(ProductWarehouse product, int idOrder)
    {
        using (var connection = new SqlConnection(_connectionString))
        {
            await connection.OpenAsync();
            var transaction = connection.BeginTransaction();

            try
            {
                string productPriceQuery = @"SELECT Price AS ProductPrice FROM Product WHERE IdProduct = @IdProduct;";
                double totalProductPrice = product.Amount;

                using (SqlCommand productPriceCommand = new SqlCommand(productPriceQuery, connection, transaction))
                {
                    productPriceCommand.Parameters.AddWithValue("@IdProduct", product.IdProduct);

                    using (var reader = await productPriceCommand.ExecuteReaderAsync())
                    {
                        if (await reader.ReadAsync())
                        {
                            var price = (decimal)reader["ProductPrice"];
                            totalProductPrice *= (double)price;
                        }
                    }
                }


                string insertQuery =
                    @"INSERT INTO Product_Warehouse(IdWarehouse, IdProduct, IdOrder, Amount, Price, CreatedAt)
                                VALUES(@IdWarehouse, @IdProduct, @IdOrder, @Amount, @Price, CURRENT_TIMESTAMP);
                                SELECT SCOPE_IDENTITY();";

                using (SqlCommand insertQueryCommand = new SqlCommand(insertQuery, connection, transaction))
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
        }

        return -1;
    }

    private async Task<int> UpdateOrderFulfilledAt(ProductWarehouse product)
    {
        using (var connection = new SqlConnection(_connectionString))
        {
            await connection.OpenAsync();
            var transaction = connection.BeginTransaction();

            try
            {
                string updateQuery = @"
                    UPDATE [Order]
                    SET FulfilledAt = CURRENT_TIMESTAMP
                    OUTPUT INSERTED.IdOrder
                    WHERE IdProduct = @IdProduct;";

                using (SqlCommand updateCommand = new SqlCommand(updateQuery, connection, transaction))
                {
                    updateCommand.Parameters.AddWithValue("@IdProduct", product.IdProduct);

                    using (var reader = await updateCommand.ExecuteReaderAsync())
                    {
                        if (await reader.ReadAsync())
                        {
                            int idOrder = (int)reader["IdOrder"];
                            await reader.CloseAsync();
                            transaction.Commit();
                            return idOrder;
                        }
                    }
                }
            }
            catch (Exception e)
            {
                transaction.Rollback();
                await Console.Out.WriteLineAsync(e.Message);
                throw new Exception("Failed to \'UpdateOrderFulfilledAt\'! " + e.Message + " " + e);
            }
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

        string query = @"SELECT (SELECT 1 FROM Product WHERE IdProduct = @IdProduct) AS ProductExists,
                                (SELECT 1 FROM Warehouse WHERE IdWarehouse = @IdWarehouse) AS WarehouseExists;";

        SqlConnection connection = new SqlConnection(_connectionString);
        SqlCommand command = new SqlCommand(query, connection);
        command.Parameters.AddWithValue("@IdProduct", product.IdProduct);
        command.Parameters.AddWithValue("@IdWarehouse", product.IdWarehouse);
        connection.Open();

        using (var reader = await command.ExecuteReaderAsync())
        {
            if (await reader.ReadAsync())
            {
                bool productExists = reader["ProductExists"] != DBNull.Value;
                bool warehouseExists = reader["WarehouseExists"] != DBNull.Value;

                return productExists && warehouseExists;
            }
        }

        return false;
    }

    private async Task<bool> DoesOrderExist(ProductWarehouse product)
    {
        // Sprawdzenie, czy istnieje rekord w tabeli Order, który
        // zawiera Id i Amount z obiektu (modelu) podanego w argumencie.
        string query = @"SELECT 1 AS OrderExists FROM [Order] WHERE IdProduct = @IdWarehouse AND Amount = @Amount;";

        SqlConnection connection = new SqlConnection(_connectionString);
        SqlCommand command = new SqlCommand(query, connection);
        command.Parameters.AddWithValue("@IdWarehouse", product.IdWarehouse);
        command.Parameters.AddWithValue("@Amount", product.Amount);

        await connection.OpenAsync();

        using (var reader = await command.ExecuteReaderAsync())
        {
            if (await reader.ReadAsync())
            {
                return reader["OrderExists"] != DBNull.Value;
            }
        }

        return false;
    }

    private bool IsCreationDateEarlierThanProvidedDate(ProductWarehouse productWarehouse)
    {
        // Sprawdzenie, czy data utworzenia Order (CreatedAt) jest wcześniejsza,
        // niż data z obiektu (modelu) (CreatedAt)
        return true;
    }

    private async Task<bool> DoesOrderAlreadyExistInWarehouse(ProductWarehouse product)
    {
        // Sprawdzenie, czy nie istnieje rekord w tabeli Product_Warehouse,
        // który ma takie samo IdOrder.
        string query = @"SELECT 1 AS AlreadyExists FROM Product_Warehouse 
                            WHERE IdWarehouse = @IdWarehouse 
                            AND IdProduct = @IdProduct;";

        SqlConnection connection = new SqlConnection(_connectionString);
        SqlCommand command = new SqlCommand(query, connection);

        command.Parameters.AddWithValue("@IdWarehouse", product.IdWarehouse);
        command.Parameters.AddWithValue("@IdProduct", product.IdProduct);

        await connection.OpenAsync();

        using (var reader = await command.ExecuteReaderAsync())
        {
            if (await reader.ReadAsync())
            {
                return reader["AlreadyExists"] != DBNull.Value;
            }
        }

        return false;
    }
}