using System.Text.Json.Serialization;

namespace BackendToolsServer;

// JSON serialization context for source generation
[JsonSerializable(typeof(RestaurantSearchRequest))]
[JsonSerializable(typeof(RestaurantSearchResponse))]
internal sealed partial class SampleJsonSerializerContext : JsonSerializerContext;