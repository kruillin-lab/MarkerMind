using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace MarkerMind;

public class DataStore
{
    private readonly string basePath;
    private readonly JsonSerializerOptions jsonOptions;
    
    public DataStore()
    {
        basePath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "XIVLauncher", "pluginConfigs", "MarkerMind"
        );
        
        Directory.CreateDirectory(basePath);
        Directory.CreateDirectory(Path.Combine(basePath, "encounters"));
        Directory.CreateDirectory(Path.Combine(basePath, "telemetry"));
        
        jsonOptions = new JsonSerializerOptions
        {
            WriteIndented = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };
        jsonOptions.Converters.Add(new Vector3JsonConverter());
    }
    
    public EncounterData? LoadEncounter(string encounterId)
    {
        string path = Path.Combine(basePath, "encounters", $"{encounterId}.json");
        if (!File.Exists(path)) return null;
        
        var json = File.ReadAllText(path);
        return JsonSerializer.Deserialize<EncounterData>(json, jsonOptions);
    }
    
    public void SaveEncounter(string encounterId, EncounterData data)
    {
        string path = Path.Combine(basePath, "encounters", $"{encounterId}.json");
        data.LastUpdated = DateTime.UtcNow;
        var json = JsonSerializer.Serialize(data, jsonOptions);
        File.WriteAllText(path, json);
    }
}

public class EncounterData
{
    [JsonPropertyName("schemaVersion")]
    public string SchemaVersion { get; set; } = "1.0";
    
    [JsonPropertyName("lastUpdated")]
    public DateTime LastUpdated { get; set; }
    
    [JsonPropertyName("encounterId")]
    public string EncounterId { get; set; } = string.Empty;
    
    [JsonPropertyName("territoryId")]
    public uint TerritoryId { get; set; }
    
    [JsonPropertyName("mechanics")]
    public Dictionary<string, MechanicData> Mechanics { get; set; } = new();
}

public class MechanicData
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;
    
    [JsonPropertyName("observations")]
    public int Observations { get; set; }
    
    [JsonPropertyName("successRate")]
    public float SuccessRate { get; set; }
    
    [JsonPropertyName("clusters")]
    public List<ClusterData> Clusters { get; set; } = new();
    
    [JsonPropertyName("avoidZones")]
    public List<ZoneData> AvoidZones { get; set; } = new();
    
    [JsonPropertyName("confidence")]
    public float Confidence { get; set; }
    
    [JsonPropertyName("lastSeen")]
    public DateTime LastSeen { get; set; }
}

public class ClusterData
{
    [JsonPropertyName("centroid")]
    public Vector3 Centroid { get; set; }
    
    [JsonPropertyName("radius")]
    public float Radius { get; set; }
    
    [JsonPropertyName("sampleCount")]
    public int SampleCount { get; set; }
    
    [JsonPropertyName("role")]
    public string Role { get; set; } = "unknown";
}

public class ZoneData
{
    [JsonPropertyName("x")]
    public float X { get; set; }
    
    [JsonPropertyName("y")]
    public float Y { get; set; }
    
    [JsonPropertyName("z")]
    public float Z { get; set; }
    
    [JsonPropertyName("radius")]
    public float Radius { get; set; }
}

public class Vector3JsonConverter : JsonConverter<Vector3>
{
    public override Vector3 Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        using var doc = JsonDocument.ParseValue(ref reader);
        var root = doc.RootElement;
        return new Vector3(
            root.GetProperty("x").GetSingle(),
            root.GetProperty("y").GetSingle(),
            root.GetProperty("z").GetSingle()
        );
    }
    
    public override void Write(Utf8JsonWriter writer, Vector3 value, JsonSerializerOptions options)
    {
        writer.WriteStartObject();
        writer.WriteNumber("x", value.X);
        writer.WriteNumber("y", value.Y);
        writer.WriteNumber("z", value.Z);
        writer.WriteEndObject();
    }
}
