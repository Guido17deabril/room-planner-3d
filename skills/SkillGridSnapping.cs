using Godot;

namespace RoomPlanner3D.Skills
{
    /// <summary>
    /// SkillGridSnapping — Raycast al plano Y=0 y snapping configurable.
    /// Soporta incrementos de 0.05 m y 0.10 m (y cualquier valor personalizado).
    /// </summary>
    public static class SkillGridSnapping
    {
        // ── Constantes de grid ────────────────────────────────────────────────
        public const float GridFine   = 0.05f;  // 5 cm — precisión fina
        public const float GridNormal = 0.10f;  // 10 cm — uso estándar

        private static float _currentGrid = GridNormal;

        /// <summary>Valor de grid actualmente configurado (metros).</summary>
        public static float CurrentGrid => _currentGrid;

        /// <summary>Establece el tamaño de celda del grid (debe ser > 0).</summary>
        public static void SetGridSize(float gridSize)
        {
            if (gridSize > 0f)
                _currentGrid = gridSize;
            else
                GD.PushWarning("[SkillGridSnapping] gridSize debe ser mayor que 0.");
        }

        // ── Snapping ──────────────────────────────────────────────────────────

        /// <summary>
        /// Aplica snapping a una posición 3D sobre el plano Y=0.
        /// La Y de entrada se conserva sin modificar.
        /// </summary>
        public static Vector3 Snap(Vector3 worldPosition)
        {
            return new Vector3(
                SnapAxis(worldPosition.X),
                worldPosition.Y,
                SnapAxis(worldPosition.Z)
            );
        }

        /// <summary>Aplica snapping a un valor escalar en un eje.</summary>
        public static float SnapAxis(float value)
        {
            return Mathf.Round(value / _currentGrid) * _currentGrid;
        }

        /// <summary>
        /// Snapping con grid explícito (sin afectar el estado global).
        /// </summary>
        public static Vector3 SnapWithGrid(Vector3 worldPosition, float gridSize)
        {
            float g = gridSize > 0f ? gridSize : GridNormal;
            return new Vector3(
                Mathf.Round(worldPosition.X / g) * g,
                worldPosition.Y,
                Mathf.Round(worldPosition.Z / g) * g
            );
        }

        // ── Raycast al plano Y=0 ─────────────────────────────────────────────

        /// <summary>
        /// Proyecta un rayo desde la cámara hacia el plano Y=0 y devuelve
        /// el punto de intersección con snapping aplicado.
        /// Devuelve <c>null</c> si el rayo es paralelo al plano.
        /// </summary>
        public static Vector3? RaycastToFloor(Camera3D camera, Vector2 screenPosition)
        {
            if (camera == null)
            {
                GD.PushWarning("[SkillGridSnapping] Camera3D es null.");
                return null;
            }

            Vector3 rayOrigin    = camera.ProjectRayOrigin(screenPosition);
            Vector3 rayDirection = camera.ProjectRayNormal(screenPosition);

            // Intersección rayo–plano Y=0: t = -rayOrigin.Y / rayDirection.Y
            if (Mathf.Abs(rayDirection.Y) < 1e-6f)
                return null;

            float t = -rayOrigin.Y / rayDirection.Y;
            if (t < 0f)
                return null;

            Vector3 hitPoint = rayOrigin + rayDirection * t;
            return Snap(hitPoint);
        }

        /// <summary>
        /// Versión con altura de plano configurable (plano Y = <paramref name="planeY"/>).
        /// </summary>
        public static Vector3? RaycastToPlane(Camera3D camera, Vector2 screenPosition, float planeY)
        {
            if (camera == null) return null;

            Vector3 rayOrigin    = camera.ProjectRayOrigin(screenPosition);
            Vector3 rayDirection = camera.ProjectRayNormal(screenPosition);

            if (Mathf.Abs(rayDirection.Y) < 1e-6f) return null;

            float t = (planeY - rayOrigin.Y) / rayDirection.Y;
            if (t < 0f) return null;

            Vector3 hitPoint = rayOrigin + rayDirection * t;
            return Snap(hitPoint);
        }

        // ── Utilidades ────────────────────────────────────────────────────────

        /// <summary>Calcula la posición de celda (índice entero) para una coordenada.</summary>
        public static Vector2I WorldToCell(Vector3 worldPosition)
        {
            return new Vector2I(
                Mathf.RoundToInt(worldPosition.X / _currentGrid),
                Mathf.RoundToInt(worldPosition.Z / _currentGrid)
            );
        }

        /// <summary>Convierte una celda de vuelta a coordenadas mundiales (Y=0).</summary>
        public static Vector3 CellToWorld(Vector2I cell)
        {
            return new Vector3(cell.X * _currentGrid, 0f, cell.Y * _currentGrid);
        }
    }
}
