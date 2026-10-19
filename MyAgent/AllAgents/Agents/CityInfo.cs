using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text;
using System.Text.Json.Serialization;

namespace MyAgent.AllAgents.Agents
{
    /// <summary>
    /// Represents information about a city, including its name.
    /// </summary>
    [Description("Information about a city")]
    public sealed class CityInfo
    {
        [JsonPropertyName("name")]
        public string? Name { get; set; }
    }
}
