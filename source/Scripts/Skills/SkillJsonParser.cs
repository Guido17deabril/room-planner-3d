using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Godot;

namespace RoomPlanner3D.Skills
{
    // ── Modelos de datos ──────────────────────────────────────────────────────

    public class RoomLayoutData
    {
        [JsonPropertyName("version")]
        public string Version { get; set; } = "1.0";

        [JsonPropertyName("name")]
        public string Name { get; set; } = "Nueva habitación";

        [JsonPropertyName("lastModified")]
        public DateTime LastModified { get; set; } = DateTime.UtcNow;

        [JsonPropertyName("checksum")]
        public string Checksum { get; set; } = string.Empty;

        [JsonPropertyName("room")]
        public RoomDimensions Room { get; set; } = new();

        [JsonPropertyName("palette")]
        public ColorPaletteData Palette { get; set; } = new();

        [JsonPropertyName("surfaces")]
        public SurfacesData Surfaces { get; set; } = new();

        [JsonPropertyName("accessPoints")]
        public List<AccessPointData> AccessPoints { get; set; } = new();

        [JsonPropertyName("furniture")]
        public List<FurnitureItemData> Furniture { get; set; } = new();

        [JsonPropertyName("layoutMetrics")]
        public LayoutMetrics Metrics { get; set; } = new();
    }

    public class RoomDimensions
    {
        [JsonPropertyName("width")]
        public float Width { get; set; } = 4.0f;

        [JsonPropertyName("length")]
        public float Length { get; set; } = 5.0f;

        [JsonPropertyName("height")]
        public float Height { get; set; } = 2.6f;

        [JsonPropertyName("wallThickness")]
        public float WallThickness { get; set; } = 0.15f;
    }

    public class ColorPaletteData
    {
        [JsonPropertyName("dominant")]
        public string Dominant { get; set; } = "#E8DCC8";

        [JsonPropertyName("secondary")]
        public string Secondary { get; set; } = "#7A6652";

        [JsonPropertyName("accent")]
        public string Accent { get; set; } = "#3A7CA5";

        [JsonPropertyName("style")]
        public string Style { get; set; } = "contemporary";
    }

    public class SurfacesData
    {
        [JsonPropertyName("floor")]
        public SurfaceEntry Floor { get; set; } = new() { Color = "#C8B89A", Material = "hardwood_oak" };

        [JsonPropertyName("wallNorth")]
        public SurfaceEntry WallNorth { get; set; } = new() { Color = "#E8DCC8", Material = "matte_paint" };

        [JsonPropertyName("wallSouth")]
        public SurfaceEntry WallSouth { get; set; } = new() { Color = "#E8DCC8", Material = "matte_paint" };

        [JsonPropertyName("wallEast")]
        public SurfaceEntry WallEast { get; set; } = new() { Color = "#D4C5AE", Material = "matte_paint" };

        [JsonPropertyName("wallWest")]
        public SurfaceEntry WallWest { get; set; } = new() { Color = "#3A7CA5", Material = "matte_paint" };
    }

    public class SurfaceEntry
    {
        [JsonPropertyName("color")]
        public string Color { get; set; } = "#FFFFFF";

        [JsonPropertyName("material")]
        public string Material { get; set; } = "matte_paint";

        [JsonPropertyName("isAccentWall")]
        public bool IsAccentWall { get; set; } = false;
    }

    public class AccessPointData
    {
        [JsonPropertyName("id")]
        public string Id { get; set; } = string.Empty;

        [JsonPropertyName("type")]
        public string Type { get; set; } = "door"; // "door" | "window"

        [JsonPropertyName("wall")]
        public string Wall { get; set; } = "south";

        [JsonPropertyName("positionAlongWall")]
        public float PositionAlongWall { get; set; }

        [JsonPropertyName("width")]
        public float Width { get; set; } = 0.9f;

        [JsonPropertyName("height")]
        public float Height { get; set; } = 2.1f;

        [JsonPropertyName("clearanceRequired")]
        public float ClearanceRequired { get; set; } = 0.9f;
    }

    public class FurnitureItemData
    {
        [JsonPropertyName("id")]
        public string Id { get; set; } = string.Empty;

        [JsonPropertyName("type")]
        public string Type { get; set; } = string.Empty;

        [JsonPropertyName("label")]
        public string Label { get; set; } = string.Empty;

        [JsonPropertyName("dimensions")]
        public FurnitureDimensions Dimensions { get; set; } = new();

        [JsonPropertyName("position")]
        public PositionData Position { get; set; } = new();

        [JsonPropertyName("rotation")]
        public float Rotation { get; set; } = 0f;

        [JsonPropertyName("color")]
        public string Color { get; set; } = "#FFFFFF";

        [JsonPropertyName("material")]
        public string Material { get; set; } = string.Empty;

        [JsonPropertyName("anchoredWall")]
        public string? AnchoredWall { get; set; } = null;

        [JsonPropertyName("ergonomicScore")]
        public int ErgonomicScore { get; set; } = 0;
    }

    public class FurnitureDimensions
    {
        [JsonPropertyName("width")]
        public float Width { get; set; }

        [JsonPropertyName("depth")]
        public float Depth { get; set; }

        [JsonPropertyName("height")]
        public float Height { get; set; }
    }

    public class PositionData
    {
        [JsonPropertyName("x")]
        public float X { get; set; }

        [JsonPropertyName("y")]
        public float Y { get; set; }

        [JsonPropertyName("z")]
        public float Z { get; set; }
    }

    public class LayoutMetrics
    {
        [JsonPropertyName("overallErgonomicScore")]
        public float OverallErgonomicScore { get; set; }

        [JsonPropertyName("furnitureCount")]
        public int FurnitureCount { get; set; }

        [JsonPropertyName("occupiedArea")]
        public float OccupiedArea { get; set; }

        [JsonPropertyName("freeCirculationArea")]
        public float FreeCirculationArea { get; set; }

        [JsonPropertyName("occupancyRatio")]
        public float OccupancyRatio { get; set; }
    }

    // ── Skill principal ───────────────────────────────────────────────────────

    /// <summary>
    /// SkillJsonParser — Serialización y deserialización de <see cref="RoomLayoutData"/>.
    /// Usa <c>System.Text.Json</c> con opciones human-readable y validación de checksum SHA-256.
    /// </summary>
    public static class SkillJsonParser
    {
        private static readonly JsonSerializerOptions _options = new()
        {
            WriteIndented            = true,
            PropertyNameCaseInsensitive = true,
            DefaultIgnoreCondition   = JsonIgnoreCondition.Never,
            Encoder                  = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
        };

        // ── Serialización ─────────────────────────────────────────────────────

        /// <summary>
        /// Serializa un <see cref="RoomLayoutData"/> a cadena JSON.
        /// Actualiza <c>LastModified</c> y calcula el <c>Checksum</c>.
        /// </summary>
        public static string Serialize(RoomLayoutData data)
        {
            data.LastModified = DateTime.UtcNow;
            data.Checksum     = string.Empty;

            string json     = JsonSerializer.Serialize(data, _options);
            data.Checksum   = ComputeSha256(json);

            // Segunda pasada con checksum incluido
            return JsonSerializer.Serialize(data, _options);
        }

        /// <summary>Guarda un <see cref="RoomLayoutData"/> al disco en ruta absoluta.</summary>
        public static void SaveToFile(RoomLayoutData data, string absolutePath)
        {
            string json = Serialize(data);
            File.WriteAllText(absolutePath, json, Encoding.UTF8);
            GD.Print($"[SkillJsonParser] Guardado: {absolutePath}");
        }

        // ── Deserialización ───────────────────────────────────────────────────

        /// <summary>
        /// Deserializa un JSON a <see cref="RoomLayoutData"/>.
        /// Lanza <see cref="FormatException"/> si el JSON es inválido.
        /// </summary>
        public static RoomLayoutData Deserialize(string json)
        {
            if (string.IsNullOrWhiteSpace(json))
                throw new ArgumentException("JSON vacío o nulo.", nameof(json));

            var data = JsonSerializer.Deserialize<RoomLayoutData>(json, _options)
                       ?? throw new FormatException("No se pudo deserializar el JSON.");

            return data;
        }

        /// <summary>Lee y deserializa un archivo desde ruta absoluta.</summary>
        public static RoomLayoutData LoadFromFile(string absolutePath)
        {
            if (!File.Exists(absolutePath))
                throw new FileNotFoundException("Archivo no encontrado.", absolutePath);

            string json = File.ReadAllText(absolutePath, Encoding.UTF8);
            return Deserialize(json);
        }

        // ── Validación ────────────────────────────────────────────────────────

        /// <summary>
        /// Verifica que el checksum del JSON coincide con el contenido.
        /// Devuelve <c>true</c> si es válido.
        /// </summary>
        public static bool ValidateChecksum(RoomLayoutData data)
        {
            string storedChecksum = data.Checksum;
            data.Checksum         = string.Empty;
            string json           = JsonSerializer.Serialize(data, _options);
            string computed       = ComputeSha256(json);
            data.Checksum         = storedChecksum; // restaurar
            return string.Equals(computed, storedChecksum, StringComparison.OrdinalIgnoreCase);
        }

        // ── Conversión de tipos Godot ─────────────────────────────────────────

        /// <summary>Convierte un string hex "#RRGGBB" a <see cref="Color"/> de Godot.</summary>
        public static Color ParseColor(string hex)
        {
            try { return Color.FromHtml(hex); }
            catch { return Colors.White; }
        }

        /// <summary>Convierte un <see cref="Color"/> de Godot a hex "#RRGGBB".</summary>
        public static string ColorToHex(Color c) => "#" + c.ToHtml(false).ToUpper();

        /// <summary>Convierte un <see cref="PositionData"/> a <see cref="Vector3"/>.</summary>
        public static Vector3 ToVector3(PositionData p) => new(p.X, p.Y, p.Z);

        /// <summary>Convierte un <see cref="Vector3"/> a <see cref="PositionData"/>.</summary>
        public static PositionData FromVector3(Vector3 v) => new() { X = v.X, Y = v.Y, Z = v.Z };

        // ── SHA-256 ───────────────────────────────────────────────────────────

        private static string ComputeSha256(string content)
        {
            byte[] bytes = SHA256.HashData(Encoding.UTF8.GetBytes(content));
            return BitConverter.ToString(bytes).Replace("-", "").ToLowerInvariant();
        }
    }
}
