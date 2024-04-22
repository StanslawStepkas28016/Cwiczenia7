using Cwiczenia7.Model;

namespace Cwiczenia7.Repositories;

public class ProductWarehouseRepository : IProductWarehouseRepository
{
    private readonly string _connectionString;

    public ProductWarehouseRepository(IConfiguration configuration)
    {
        _connectionString = configuration.GetConnectionString("DefaultConnection");
    }

    public int AddProduct(ProductWarehouse productWarehouse)
    {
        // Walidacja danych.
        if (!DoesProductAndWarehouseExistAndAmountGreaterThanZero(productWarehouse))
        {
            return -1;
        }

        if (!DoesOrderExist(productWarehouse))
        {
            return -1;
        }

        if (!DoesOrderExistInWarehouse(productWarehouse))
        {
            return -1;
        }


        // Aktualizacja kolumny FulfilledAt na aktualną datę (UPDATE).
        // To do.....

        // Wstawienie rekordu do tabeli Product_Warehouse,
        // - Kolumna [Product_Warehouse] Price ma odpowiadać [Product_Warehouse] Amount * [Product] Price,
        // - Kolumna [Product_Warehouse] CreatedAt ma mieć aktualny czas,
        // To do.....

        return 0;
    }

    private bool DoesProductAndWarehouseExistAndAmountGreaterThanZero(ProductWarehouse productWarehouse)
    {
        // Sprawdzenie, czy produkt i magazyn o podanym Id
        // istnieje oraz sprawdzenie, czy przekazana ilość 
        // (Amount), jest większa niż 0.
        return true;
    }

    private bool DoesOrderExist(ProductWarehouse productWarehouse)
    {
        // Sprawdzenie, czy istnieje rekord w tabeli Order, który
        // zawiera Id i Amount z obiektu (modelu) podanego w argumencie.
        return true;
    }

    private bool IsCreationDateEarlierThanProvidedDate(ProductWarehouse productWarehouse)
    {
        // Sprawdzenie, czy data utworzenia Order (CreatedAt) jest wcześniejsza,
        // niż data z obiektu (modelu) (CreatedAt)
        return true;
    }

    private bool DoesOrderExistInWarehouse(ProductWarehouse productWarehouse)
    {
        // Sprawdzenie, czy nie istnieje rekord w tabeli Product_Warehouse,
        // który ma takie samo IdOrder
        return true;
    }
}