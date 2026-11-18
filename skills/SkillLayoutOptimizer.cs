using System;
using System.Collections.Generic;
using System.Linq;
using Godot;

namespace RoomPlanner3D.Skills
{
    /// <summary>
    /// SkillLayoutOptimizer — Heurística funcional que ubica muebles respetando:
    /// - Pasillos mínimos >= 0.70 m entre objetos.
    /// - Despeje de accesos >= 0.90 m frente a puertas y ventanas.
    /// - Anclaje perimetral para muebles de apoyo (armarios, escritorios, etc.).
    /// </summary>
    public static class SkillLayoutOptimizer
    {
        // ── Constantes ────────────────────────────────────────────────────────
        public const float MinPassageway     = 0.70f;
        public const float MinAccessClearance = 0.90f;
        public const float WallOffset        = 0.075f; // mitad del grosor de muro estándar

        // ── Estructuras de datos ──────────────────────────────────────────────

        public enum WallSide { North, South, East, West, None }

        public struct RoomBounds
        {
            public float Width;   // Eje X
            public float Length;  // Eje Z
            public float Height;  // Eje Y
            public float WallThickness;

            public float InnerWidth  => Width  - WallThickness * 2f;
            public float InnerLength => Length - WallThickness * 2f;
        }

        public struct AccessPoint
        {
            public WallSide Wall;
            public float    PositionAlongWall; // metros desde esquina izq (mirando muro)
            public float    Width;
        }

        public struct FurnitureSpec
        {
            public string   Id;
            public Vector3  Size;          // dimensiones reales (x, y, z)
            public bool     PerimeterOnly; // si debe anclarse a muro
            public WallSide PreferredWall; // preferencia de anclaje
            public int      Priority;      // orden de colocación (menor = antes)
        }

        public struct PlacedFurniture
        {
            public string    Id;
            public Vector3   Position; // centro en plano XZ, Y=0
            public float     Rotation; // grados alrededor de Y
            public WallSide  AnchoredWall;
            public bool      PlacedSuccessfully;
        }

        public struct LayoutResult
        {
            public List<PlacedFurniture> Placed;
            public List<string>          Unplaced;
            public float                 ErgonomicScore; // 0–100
            public List<string>          Warnings;
        }

        // ── Punto de entrada principal ────────────────────────────────────────

        /// <summary>
        /// Ejecuta el algoritmo de auto-layout para la habitación y lista de muebles.
        /// Devuelve un <see cref="LayoutResult"/> con posiciones y puntuación ergonómica.
        /// </summary>
        public static LayoutResult Optimize(RoomBounds room,
                                            IReadOnlyList<FurnitureSpec> furniture,
                                            IReadOnlyList<AccessPoint> accessPoints)
        {
            var result = new LayoutResult
            {
                Placed   = new List<PlacedFurniture>(),
                Unplaced = new List<string>(),
                Warnings = new List<string>()
            };

            // Zonas de exclusión por acceso (rectángulos en XZ)
            var exclusionZones = BuildExclusionZones(room, accessPoints);

            // Ordenar muebles: primero los de mayor prioridad (menor número)
            var sorted = furniture.OrderBy(f => f.Priority).ToList();

            // Lista de AABB ya colocados para auditoría
            var placedBoxes = new List<(string Id, Aabb Box)>();

            foreach (var spec in sorted)
            {
                PlacedFurniture placement = TryPlace(spec, room, placedBoxes, exclusionZones);
                if (placement.PlacedSuccessfully)
                {
                    placedBoxes.Add((spec.Id, BuildAabb(placement.Position, spec.Size)));
                    result.Placed.Add(placement);
                }
                else
                {
                    result.Unplaced.Add(spec.Id);
                    result.Warnings.Add($"No se pudo colocar '{spec.Id}' respetando restricciones ergonómicas.");
                }
            }

            result.ErgonomicScore = CalculateScore(result.Placed, placedBoxes, room, accessPoints);
            return result;
        }

        // ── Colocación individual ─────────────────────────────────────────────

        private static PlacedFurniture TryPlace(FurnitureSpec spec,
                                                RoomBounds room,
                                                List<(string Id, Aabb Box)> existing,
                                                List<Aabb> exclusionZones)
        {
            // Generar candidatos según tipo de mueble
            var candidates = spec.PerimeterOnly
                ? GeneratePerimeterCandidates(spec, room)
                : GenerateGridCandidates(spec, room);

            foreach (var (pos, rot) in candidates)
            {
                Aabb candidateBox = BuildAabb(pos, RotatedSize(spec.Size, rot));

                // Verificar que cabe dentro de la habitación
                if (!FitsInRoom(candidateBox, room)) continue;

                // Verificar colisiones con muebles ya colocados
                if (SkillCollisionAudit.ViolatesMinDistance(
                        candidateBox,
                        existing.Select(e => e.Box).ToList(),
                        MinPassageway)) continue;

                // Verificar zonas de exclusión de accesos
                if (IntersectsExclusion(candidateBox, exclusionZones)) continue;

                // Éxito
                WallSide anchored = spec.PerimeterOnly ? spec.PreferredWall : WallSide.None;
                return new PlacedFurniture
                {
                    Id               = spec.Id,
                    Position         = pos,
                    Rotation         = rot,
                    AnchoredWall     = anchored,
                    PlacedSuccessfully = true
                };
            }

            return new PlacedFurniture { Id = spec.Id, PlacedSuccessfully = false };
        }

        // ── Generadores de candidatos ─────────────────────────────────────────

        private static List<(Vector3 Pos, float Rot)> GeneratePerimeterCandidates(
            FurnitureSpec spec, RoomBounds room)
        {
            var candidates = new List<(Vector3, float)>();
            float margin = WallOffset + spec.Size.X * 0.5f;

            WallSide[] wallOrder = spec.PreferredWall == WallSide.None
                ? new[] { WallSide.North, WallSide.South, WallSide.East, WallSide.West }
                : new[] { spec.PreferredWall, WallSide.North, WallSide.South, WallSide.East, WallSide.West };

            foreach (var wall in wallOrder.Distinct())
            {
                float rot = WallToRotation(wall);
                Vector3 half = RotatedSize(spec.Size, rot) * 0.5f;

                switch (wall)
                {
                    case WallSide.North:
                        for (float x = half.X; x <= room.InnerWidth - half.X; x += SkillGridSnapping.GridNormal)
                            candidates.Add((new Vector3(x, 0f, half.Z + WallOffset), rot));
                        break;
                    case WallSide.South:
                        for (float x = half.X; x <= room.InnerWidth - half.X; x += SkillGridSnapping.GridNormal)
                            candidates.Add((new Vector3(x, 0f, room.InnerLength - half.Z - WallOffset), rot));
                        break;
                    case WallSide.West:
                        for (float z = half.Z; z <= room.InnerLength - half.Z; z += SkillGridSnapping.GridNormal)
                            candidates.Add((new Vector3(half.X + WallOffset, 0f, z), rot));
                        break;
                    case WallSide.East:
                        for (float z = half.Z; z <= room.InnerLength - half.Z; z += SkillGridSnapping.GridNormal)
                            candidates.Add((new Vector3(room.InnerWidth - half.X - WallOffset, 0f, z), rot));
                        break;
                }
            }
            return candidates;
        }

        private static List<(Vector3 Pos, float Rot)> GenerateGridCandidates(
            FurnitureSpec spec, RoomBounds room)
        {
            var candidates = new List<(Vector3, float)>();
            float step = SkillGridSnapping.GridNormal;
            float hw = spec.Size.X * 0.5f;
            float hd = spec.Size.Z * 0.5f;

            for (float x = hw + step; x <= room.InnerWidth - hw - step; x += step)
                for (float z = hd + step; z <= room.InnerLength - hd - step; z += step)
                    candidates.Add((new Vector3(x, 0f, z), 0f));

            return candidates;
        }

        // ── Utilidades de geometría ───────────────────────────────────────────

        private static Aabb BuildAabb(Vector3 center, Vector3 size)
        {
            return new Aabb(center - size * 0.5f, size);
        }

        private static Vector3 RotatedSize(Vector3 size, float rotDeg)
        {
            // Solo rotaciones de 0° y 90° relevantes para layout perimetral
            if (Mathf.Abs(rotDeg % 180f - 90f) < 1f)
                return new Vector3(size.Z, size.Y, size.X);
            return size;
        }

        private static float WallToRotation(WallSide wall)
        {
            return wall is WallSide.East or WallSide.West ? 90f : 0f;
        }

        private static bool FitsInRoom(Aabb box, RoomBounds room)
        {
            float margin = WallOffset;
            return box.Position.X >= margin
                && box.Position.Z >= margin
                && box.Position.X + box.Size.X <= room.InnerWidth  - margin
                && box.Position.Z + box.Size.Z <= room.InnerLength - margin;
        }

        private static bool IntersectsExclusion(Aabb box, List<Aabb> exclusions)
        {
            foreach (var ex in exclusions)
                if (box.Intersects(ex)) return true;
            return false;
        }

        private static List<Aabb> BuildExclusionZones(RoomBounds room, IReadOnlyList<AccessPoint> points)
        {
            var zones = new List<Aabb>();
            foreach (var ap in points)
            {
                Aabb zone = ap.Wall switch
                {
                    WallSide.North => new Aabb(
                        new Vector3(ap.PositionAlongWall, 0f, 0f),
                        new Vector3(ap.Width, room.Height, MinAccessClearance)),
                    WallSide.South => new Aabb(
                        new Vector3(ap.PositionAlongWall, 0f, room.InnerLength - MinAccessClearance),
                        new Vector3(ap.Width, room.Height, MinAccessClearance)),
                    WallSide.West => new Aabb(
                        new Vector3(0f, 0f, ap.PositionAlongWall),
                        new Vector3(MinAccessClearance, room.Height, ap.Width)),
                    WallSide.East => new Aabb(
                        new Vector3(room.InnerWidth - MinAccessClearance, 0f, ap.PositionAlongWall),
                        new Vector3(MinAccessClearance, room.Height, ap.Width)),
                    _ => new Aabb()
                };
                zones.Add(zone);
            }
            return zones;
        }

        // ── Puntuación ergonómica ─────────────────────────────────────────────

        private static float CalculateScore(List<PlacedFurniture> placed,
                                            List<(string Id, Aabb Box)> boxes,
                                            RoomBounds room,
                                            IReadOnlyList<AccessPoint> accessPoints)
        {
            if (placed.Count == 0) return 0f;

            float score = 100f;

            // Penalización por muebles no colocados ya no aplica aquí (todos los placed)
            // Auditar distancias mínimas
            var audit = SkillCollisionAudit.AuditAll(boxes, MinPassageway);
            score -= audit.Overlaps.Count * 15f;
            score -= audit.Warnings.Count(w => w.StartsWith("PASILLO")) * 5f;

            // Bonus por ocupación eficiente (target 25–40 %)
            float roomArea = room.InnerWidth * room.InnerLength;
            float usedArea = boxes.Sum(b => b.Box.Size.X * b.Box.Size.Z);
            float ratio    = usedArea / roomArea;
            if (ratio >= 0.25f && ratio <= 0.40f) score += 5f;

            return Mathf.Clamp(score, 0f, 100f);
        }
    }
}
