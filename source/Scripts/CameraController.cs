using Godot;

namespace RoomPlanner3D
{
    /// <summary>
    /// CameraController — Cámara orbital con:
    /// - Rotación Y libre (arrastre con botón derecho).
    /// - Pitch restringido en X (clamp entre -80° y -5°).
    /// - Zoom suave clamped con rueda del ratón.
    /// El nodo debe ser hijo de un <see cref="Node3D"/> pivote (CameraPivot).
    /// </summary>
    public partial class CameraController : Camera3D
    {
        // ── Parámetros exportados ─────────────────────────────────────────────

        [Export] public float ZoomMin        = 2.0f;
        [Export] public float ZoomMax        = 20.0f;
        [Export] public float ZoomSpeed      = 1.5f;
        [Export] public float ZoomSmoothness = 8.0f;

        [Export] public float OrbitSpeed     = 0.25f;   // grados por pixel
        [Export] public float PitchMin       = -89.9f;  // límite inferior (mirando casi vertical hacia abajo)
        [Export] public float PitchMax       = -5.0f;   // límite superior (casi horizontal)

        [Export] public Vector3 PivotOffset  = new(2.25f, 0f, 3.0f); // centro de la habitación

        // ── Estado interno ────────────────────────────────────────────────────

        private float   _yaw        = -30f;   // rotación horizontal actual (grados)
        private float   _pitch      = -40f;   // rotación vertical actual (grados)
        private float   _zoomTarget = 8f;     // distancia objetivo
        private float   _zoomCurrent = 8f;    // distancia interpolada
        private bool    _isDragging = false;
        private Vector2 _lastMousePos;

        // ── Pivote ────────────────────────────────────────────────────────────

        private Node3D? _pivot;

        // ── Ciclo de vida ─────────────────────────────────────────────────────

        public override void _Ready()
        {
            // Crear o referenciar el pivote padre
            if (GetParent() is Node3D parent)
                _pivot = parent;

            ApplyOrbit();
        }

        public override void _UnhandledInput(InputEvent @event)
        {
            // Arrastre con botón derecho — orbitar
            if (@event is InputEventMouseButton mb)
            {
                if (mb.ButtonIndex == MouseButton.Right)
                    _isDragging = mb.Pressed;

                // Zoom con rueda
                if (mb.ButtonIndex == MouseButton.WheelUp)
                    _zoomTarget -= ZoomSpeed;
                if (mb.ButtonIndex == MouseButton.WheelDown)
                    _zoomTarget += ZoomSpeed;

                _zoomTarget = Mathf.Clamp(_zoomTarget, ZoomMin, ZoomMax);
            }

            if (@event is InputEventMouseMotion mm && _isDragging)
            {
                _yaw   -= mm.Relative.X * OrbitSpeed;
                _pitch -= mm.Relative.Y * OrbitSpeed;
                _pitch  = Mathf.Clamp(_pitch, PitchMin, PitchMax);
            }
        }

        public override void _Process(double delta)
        {
            // Zoom suave
            _zoomCurrent = Mathf.Lerp(_zoomCurrent, _zoomTarget, (float)delta * ZoomSmoothness);
            ApplyOrbit();
        }

        // ── Lógica de órbita ──────────────────────────────────────────────────

        private void ApplyOrbit()
        {
            if (_pivot == null)
            {
                // Si no hay pivote, el nodo actúa como pivote propio
                _pivot = this.GetParentOrNull<Node3D>();
                if (_pivot == null) return;
            }

            // Mover el pivote al centro de la habitación
            _pivot.Position = PivotOffset;

            // Calcular posición de cámara en coordenadas esféricas
            float yawRad   = Mathf.DegToRad(_yaw);
            float pitchRad = Mathf.DegToRad(_pitch);

            float x = _zoomCurrent * Mathf.Cos(pitchRad) * Mathf.Sin(yawRad);
            float y = _zoomCurrent * Mathf.Sin(-pitchRad); // invertir para que pitch negativo = abajo
            float z = _zoomCurrent * Mathf.Cos(pitchRad) * Mathf.Cos(yawRad);

            // Posición relativa al pivote
            Position = new Vector3(x, y, z);

            // Apuntar siempre al pivote (origen local)
            LookAt(_pivot.GlobalPosition, Vector3.Up);
        }

        // ── API pública ───────────────────────────────────────────────────────

        /// <summary>Recentra la órbita sobre un punto del mundo.</summary>
        public void SetPivot(Vector3 worldPosition)
        {
            PivotOffset = worldPosition;
        }

        /// <summary>Reestablece la cámara a los valores por defecto.</summary>
        public void ResetView()
        {
            _yaw         = -30f;
            _pitch       = -40f;
            _zoomTarget  = 8f;
        }

        /// <summary>Zoom programático (sin interpolación).</summary>
        public void SetZoom(float distance)
        {
            _zoomTarget  = Mathf.Clamp(distance, ZoomMin, ZoomMax);
            _zoomCurrent = _zoomTarget;
        }

        /// <summary>Cambia la orientación y perspectiva de la cámara según un modo predeterminado.</summary>
        public void SetCameraPreset(string modeName)
        {
            switch (modeName.Trim().ToLowerInvariant())
            {
                case "cenital":
                case "cenital (2d)":
                case "📐 cenital (2d)":
                case "topdown":
                case "top-down":
                case "2d":
                    _yaw = 0f;
                    _pitch = -89.9f;
                    _zoomTarget = Mathf.Clamp(Mathf.Max(PivotOffset.X, PivotOffset.Z) * 2.4f, ZoomMin, ZoomMax);
                    break;

                case "isométrica":
                case "isometrica":
                case "📐 isométrica":
                case "isometric":
                    _yaw = 45f;
                    _pitch = -35.264f;
                    _zoomTarget = 9.0f;
                    break;

                case "frontal":
                case "👁 frontal":
                case "front":
                    _yaw = 0f;
                    _pitch = -8.0f;
                    _zoomTarget = 8.0f;
                    break;

                case "órbita":
                case "orbita":
                case "🎥 órbita 3d":
                case "orbit":
                default:
                    ResetView();
                    break;
            }
        }
    }
}
