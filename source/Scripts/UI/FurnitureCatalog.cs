using Godot;
using RoomPlanner3D.Skills;

namespace RoomPlanner3D.UI
{
    /// <summary>
    /// FurnitureCatalog — Catálogo estático de muebles disponibles para instanciar.
    /// Centraliza dimensiones por defecto y metadatos para el CatalogDrawer.
    /// </summary>
    public static class FurnitureCatalog
    {
        public struct CatalogEntry
        {
            public string Id;
            public string Label;
            public string Category;  // "Dormitorio" | "Oficina" | "Living"
            public string Emoji;
            public float  Width;
            public float  Depth;
            public float  Height;
            public float  Elevation; // Altura de montaje (ej. 2.05m para aire acondicionado en pared)
            public string DefaultColor;
            public bool   PerimeterOnly;
            public SkillLayoutOptimizer.WallSide PreferredWall;
        }

        public static readonly CatalogEntry[] Entries =
        {
            // ── Dormitorio ────────────────────────────────────────────────────
            new() { Id = "bed_double",   Label = "Cama Doble",    Category = "Dormitorio", Emoji = "🛏",
                    Width = 1.6f, Depth = 2.0f, Height = 0.5f, Elevation = 0f, DefaultColor = "#D4C5AE",
                    PerimeterOnly = true, PreferredWall = SkillLayoutOptimizer.WallSide.North },
            new() { Id = "bed_single",   Label = "Cama Simple",   Category = "Dormitorio", Emoji = "🛏",
                    Width = 0.9f, Depth = 2.0f, Height = 0.5f, Elevation = 0f, DefaultColor = "#D4C5AE",
                    PerimeterOnly = true, PreferredWall = SkillLayoutOptimizer.WallSide.North },
            new() { Id = "wardrobe",     Label = "Armario",        Category = "Dormitorio", Emoji = "🗄",
                    Width = 1.8f, Depth = 0.6f, Height = 2.2f, Elevation = 0f, DefaultColor = "#8B7355",
                    PerimeterOnly = true, PreferredWall = SkillLayoutOptimizer.WallSide.West },
            new() { Id = "dresser",      Label = "Cómoda",         Category = "Dormitorio", Emoji = "🗂",
                    Width = 1.0f, Depth = 0.45f, Height = 0.85f, Elevation = 0f, DefaultColor = "#A08060",
                    PerimeterOnly = true, PreferredWall = SkillLayoutOptimizer.WallSide.East },
            new() { Id = "nightstand",   Label = "Mesita de Noche",Category = "Dormitorio", Emoji = "📦",
                    Width = 0.5f, Depth = 0.4f, Height = 0.55f, Elevation = 0f, DefaultColor = "#C8AA82",
                    PerimeterOnly = false, PreferredWall = SkillLayoutOptimizer.WallSide.None },

            // ── Oficina ───────────────────────────────────────────────────────
            new() { Id = "desk_large",   Label = "Escritorio Grande", Category = "Oficina", Emoji = "🖥",
                    Width = 1.6f, Depth = 0.8f, Height = 0.75f, Elevation = 0f, DefaultColor = "#6B8E9F",
                    PerimeterOnly = true, PreferredWall = SkillLayoutOptimizer.WallSide.North },
            new() { Id = "desk_small",   Label = "Escritorio Pequeño",Category = "Oficina", Emoji = "💻",
                    Width = 1.2f, Depth = 0.6f, Height = 0.75f, Elevation = 0f, DefaultColor = "#7A9BAD",
                    PerimeterOnly = true, PreferredWall = SkillLayoutOptimizer.WallSide.East },
            new() { Id = "bookshelf",    Label = "Estantería",      Category = "Oficina", Emoji = "📚",
                    Width = 1.0f, Depth = 0.3f, Height = 2.0f, Elevation = 0f, DefaultColor = "#8B7355",
                    PerimeterOnly = true, PreferredWall = SkillLayoutOptimizer.WallSide.West },
            new() { Id = "office_chair", Label = "Silla de Oficina",Category = "Oficina", Emoji = "🪑",
                    Width = 0.65f, Depth = 0.65f, Height = 1.2f, Elevation = 0f, DefaultColor = "#2C3E50",
                    PerimeterOnly = false, PreferredWall = SkillLayoutOptimizer.WallSide.None },
            new() { Id = "filing_cabinet",Label = "Archivero",      Category = "Oficina", Emoji = "🗃",
                    Width = 0.4f, Depth = 0.55f, Height = 1.3f, Elevation = 0f, DefaultColor = "#95A5A6",
                    PerimeterOnly = true, PreferredWall = SkillLayoutOptimizer.WallSide.East },

            // ── Living ────────────────────────────────────────────────────────
            new() { Id = "sofa_3seat",   Label = "Sofá 3 Plazas",  Category = "Living", Emoji = "🛋",
                    Width = 2.2f, Depth = 0.9f, Height = 0.85f, Elevation = 0f, DefaultColor = "#7A6652",
                    PerimeterOnly = true, PreferredWall = SkillLayoutOptimizer.WallSide.South },
            new() { Id = "sofa_2seat",   Label = "Sofá 2 Plazas",  Category = "Living", Emoji = "🛋",
                    Width = 1.6f, Depth = 0.85f, Height = 0.85f, Elevation = 0f, DefaultColor = "#8B7A6A",
                    PerimeterOnly = false, PreferredWall = SkillLayoutOptimizer.WallSide.None },
            new() { Id = "coffee_table", Label = "Mesa de Centro",  Category = "Living", Emoji = "☕",
                    Width = 1.2f, Depth = 0.6f, Height = 0.45f, Elevation = 0f, DefaultColor = "#9B8B7B",
                    PerimeterOnly = false, PreferredWall = SkillLayoutOptimizer.WallSide.None },
            new() { Id = "tv_stand",     Label = "Mueble TV",       Category = "Living", Emoji = "📺",
                    Width = 1.5f, Depth = 0.45f, Height = 0.5f, Elevation = 0f, DefaultColor = "#4A4A4A",
                    PerimeterOnly = true, PreferredWall = SkillLayoutOptimizer.WallSide.North },
            new() { Id = "armchair",     Label = "Sillón",          Category = "Living", Emoji = "🪑",
                    Width = 0.85f, Depth = 0.85f, Height = 0.9f, Elevation = 0f, DefaultColor = "#6B7A52",
                    PerimeterOnly = false, PreferredWall = SkillLayoutOptimizer.WallSide.None },
            new() { Id = "dining_table", Label = "Mesa Comedor",    Category = "Living", Emoji = "🍽",
                    Width = 1.6f, Depth = 0.9f, Height = 0.75f, Elevation = 0f, DefaultColor = "#8B7355",
                    PerimeterOnly = false, PreferredWall = SkillLayoutOptimizer.WallSide.None },

            // ── Climatización y Confort ───────────────────────────────────────
            new() { Id = "ac_split",     Label = "Aire Acondicionado Split", Category = "Climatización", Emoji = "❄️",
                    Width = 0.95f, Depth = 0.25f, Height = 0.32f, Elevation = 2.05f, DefaultColor = "#F5F5F7",
                    PerimeterOnly = true, PreferredWall = SkillLayoutOptimizer.WallSide.North },
            new() { Id = "radiator",     Label = "Radiador Térmico",         Category = "Climatización", Emoji = "♨️",
                    Width = 0.80f, Depth = 0.15f, Height = 0.60f, Elevation = 0.10f, DefaultColor = "#E2E5E8",
                    PerimeterOnly = true, PreferredWall = SkillLayoutOptimizer.WallSide.East },
            new() { Id = "ceiling_fan",  Label = "Ventilador de Techo",      Category = "Climatización", Emoji = "🌀",
                    Width = 1.10f, Depth = 1.10f, Height = 0.35f, Elevation = 2.25f, DefaultColor = "#2C2C2E",
                    PerimeterOnly = false, PreferredWall = SkillLayoutOptimizer.WallSide.None },

            // ── Aberturas Arquitectónicas ─────────────────────────────────────
            new() { Id = "window_wall",  Label = "Ventana Panorámica",       Category = "Aberturas", Emoji = "🪟",
                    Width = 1.40f, Depth = 0.16f, Height = 1.20f, Elevation = 0.90f, DefaultColor = "#7BA6C8",
                    PerimeterOnly = true, PreferredWall = SkillLayoutOptimizer.WallSide.East },
            new() { Id = "door_interior",Label = "Puerta Interior",          Category = "Aberturas", Emoji = "🚪",
                    Width = 0.90f, Depth = 0.16f, Height = 2.10f, Elevation = 0.0f, DefaultColor = "#5C3A21",
                    PerimeterOnly = true, PreferredWall = SkillLayoutOptimizer.WallSide.South },
        };

        /// <summary>Busca una entrada del catálogo a partir del ID o nombre del nodo.</summary>
        public static CatalogEntry? FindEntry(string nameOrId)
        {
            string lower = nameOrId.ToLowerInvariant();
            foreach (var entry in Entries)
            {
                if (lower.Contains(entry.Id.ToLowerInvariant()))
                    return entry;
            }
            return null;
        }

        /// <summary>Crea un FurnitureItemData con dimensiones por defecto del catálogo.</summary>
        public static FurnitureItemData CreateItemData(string typeId, string label)
        {
            foreach (var entry in Entries)
            {
                if (entry.Id != typeId) continue;
                return new FurnitureItemData
                {
                    Id    = $"{typeId}_{System.DateTime.UtcNow.Ticks % 10000}",
                    Type  = typeId,
                    Label = label,
                    Dimensions = new FurnitureDimensions
                    {
                        Width  = entry.Width,
                        Depth  = entry.Depth,
                        Height = entry.Height
                    },
                    Position = new PositionData
                    {
                        X = 0f,
                        Y = entry.Elevation,
                        Z = 0f
                    },
                    Color    = entry.DefaultColor,
                    Rotation = 0f
                };
            }

            // Fallback genérico
            return new FurnitureItemData
            {
                Id    = $"generic_{System.DateTime.UtcNow.Ticks % 10000}",
                Type  = typeId,
                Label = label,
                Dimensions = new FurnitureDimensions { Width = 1.0f, Depth = 0.8f, Height = 0.75f },
                Color = "#888888"
            };
        }
    }
}
