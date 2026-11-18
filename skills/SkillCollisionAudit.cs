using System;
using System.Collections.Generic;
using Godot;

namespace RoomPlanner3D.Skills
{
    /// <summary>
    /// SkillCollisionAudit — Auditoría AABB para detectar solapamientos
    /// y validar distancias mínimas entre muebles.
    /// </summary>
    public static class SkillCollisionAudit
    {
        // ── Constantes ergonómicas ────────────────────────────────────────────
        /// <summary>Pasillo mínimo entre muebles (>= 0.70 m).</summary>
        public const float MinPassageway = 0.70f;

        /// <summary>Despeje mínimo frente a accesos puertas/ventanas (>= 0.90 m).</summary>
        public const float MinAccessClearance = 0.90f;

        // ── Estructuras de resultado ──────────────────────────────────────────

        public struct AuditResult
        {
            public bool HasOverlap;
            public bool PassagewayViolation;
            public float MinDistanceFound;
            public List<OverlapPair> Overlaps;
            public List<string> Warnings;
        }

        public struct OverlapPair
        {
            public string IdA;
            public string IdB;
            public float PenetrationDepth;
            public string Axis;
        }

        // ── AABB helpers ──────────────────────────────────────────────────────

        /// <summary>
        /// Construye un AABB centrado en <paramref name="center"/> con
        /// semitamaños tomados de <paramref name="halfExtents"/>.
        /// </summary>
        public static Aabb BuildAabb(Vector3 center, Vector3 halfExtents)
        {
            return new Aabb(center - halfExtents, halfExtents * 2f);
        }

        /// <summary>
        /// Construye el AABB de un nodo MeshInstance3D usando su
        /// AABB local transformado al espacio global.
        /// </summary>
        public static Aabb GetWorldAabb(MeshInstance3D node)
        {
            if (node?.Mesh == null)
                return new Aabb(node?.GlobalPosition ?? Vector3.Zero, Vector3.Zero);

            Aabb local = node.Mesh.GetAabb();
            // Aplicar escala global del nodo
            Vector3 pos  = node.GlobalPosition;
            Vector3 size = local.Size * node.GlobalTransform.Basis.Scale;
            return new Aabb(pos - size * 0.5f, size);
        }

        // ── Auditoría de colisiones ───────────────────────────────────────────

        /// <summary>
        /// Comprueba si dos AABB se solapan.
        /// Incluye un margen de expansión (<paramref name="margin"/>) en metros.
        /// </summary>
        public static bool Overlaps(Aabb a, Aabb b, float margin = 0f)
        {
            Aabb expanded = a.Grow(margin);
            return expanded.Intersects(b);
        }

        /// <summary>
        /// Distancia mínima entre las superficies de dos AABB.
        /// Devuelve 0 si se solapan.
        /// </summary>
        public static float MinSurfaceDistance(Aabb a, Aabb b)
        {
            float dx = Mathf.Max(0f, Mathf.Max(a.Position.X - (b.Position.X + b.Size.X),
                                               b.Position.X - (a.Position.X + a.Size.X)));
            float dy = Mathf.Max(0f, Mathf.Max(a.Position.Y - (b.Position.Y + b.Size.Y),
                                               b.Position.Y - (a.Position.Y + a.Size.Y)));
            float dz = Mathf.Max(0f, Mathf.Max(a.Position.Z - (b.Position.Z + b.Size.Z),
                                               b.Position.Z - (a.Position.Z + a.Size.Z)));
            return Mathf.Sqrt(dx * dx + dy * dy + dz * dz);
        }

        /// <summary>
        /// Profundidad de penetración en cada eje (solo si hay solapamiento).
        /// </summary>
        public static Vector3 PenetrationDepth(Aabb a, Aabb b)
        {
            if (!a.Intersects(b)) return Vector3.Zero;

            Aabb intersection = a.Intersection(b);
            return new Vector3(intersection.Size.X, intersection.Size.Y, intersection.Size.Z);
        }

        // ── Auditoría de lista completa ───────────────────────────────────────

        /// <summary>
        /// Audita una lista de muebles representados como pares (id, AABB).
        /// Devuelve un <see cref="AuditResult"/> con todos los problemas encontrados.
        /// </summary>
        public static AuditResult AuditAll(IReadOnlyList<(string Id, Aabb Box)> items,
                                           float minimumGap = MinPassageway)
        {
            var result = new AuditResult
            {
                Overlaps  = new List<OverlapPair>(),
                Warnings  = new List<string>(),
                MinDistanceFound = float.MaxValue
            };

            for (int i = 0; i < items.Count; i++)
            {
                for (int j = i + 1; j < items.Count; j++)
                {
                    var (idA, boxA) = items[i];
                    var (idB, boxB) = items[j];

                    if (Overlaps(boxA, boxB))
                    {
                        result.HasOverlap = true;
                        Vector3 pen = PenetrationDepth(boxA, boxB);
                        string axis = pen.X > pen.Z ? "X" : "Z";
                        result.Overlaps.Add(new OverlapPair
                        {
                            IdA = idA,
                            IdB = idB,
                            PenetrationDepth = Mathf.Max(pen.X, pen.Z),
                            Axis = axis
                        });
                        result.Warnings.Add($"OVERLAP: '{idA}' ↔ '{idB}' (pen={Mathf.Max(pen.X, pen.Z):F3}m en {axis})");
                    }
                    else
                    {
                        float dist = MinSurfaceDistance(boxA, boxB);
                        if (dist < result.MinDistanceFound)
                            result.MinDistanceFound = dist;

                        if (dist < minimumGap)
                        {
                            result.PassagewayViolation = true;
                            result.Warnings.Add(
                                $"PASILLO: '{idA}' ↔ '{idB}' dist={dist:F3}m < mín={minimumGap:F3}m");
                        }
                    }
                }
            }

            if (result.MinDistanceFound == float.MaxValue)
                result.MinDistanceFound = 0f;

            return result;
        }

        /// <summary>
        /// Comprueba si un AABB candidato viola la distancia mínima con algún
        /// AABB de la lista. Útil para validación previa al posicionamiento.
        /// </summary>
        public static bool ViolatesMinDistance(Aabb candidate,
                                               IReadOnlyList<Aabb> existing,
                                               float minimumGap = MinPassageway)
        {
            foreach (var box in existing)
            {
                if (Overlaps(candidate, box)) return true;
                if (MinSurfaceDistance(candidate, box) < minimumGap) return true;
            }
            return false;
        }
    }
}
