namespace BackendToolsServer;

// Define request/response types for the tool
internal sealed class RestaurantSearchRequest
{
    public string Location { get; set; } = string.Empty;
    public string Cuisine { get; set; } = "any";
}