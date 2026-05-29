namespace PetShop.Tests.E2E;

// Mirror the API's response shapes for deserialization in tests.
public record ApiEnvelope<T>(bool Success, string? Message, T? Data, string[]? Errors);

public record PetData(int Id, string Name, string? Breed, decimal Price, int? AgeMonths,
    int Status, int CategoryId, string? CategoryName);

public record PagedData<T>(T[] Items, int Page, int PageSize, int TotalCount);
