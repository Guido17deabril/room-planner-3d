using System.Collections.Generic;
using Godot;
using RoomPlanner3D.Skills;

namespace RoomPlanner3D
{
    /// <summary>
    /// FurnitureController — Gestiona la selección, arrastre, rotación y
    /// feedback visual de muebles en la escena 3D.
    ///
    /// Controles:
    /// - Clic izquierdo: seleccionar mueble.
    /// - Mantener clic izquierdo + arrastrar: mover mueble con snapping.
    /// - R: rotar 45° (con Shift: 90°).
    /// - Escape / clic en vacío: deseleccionar.
    /// </summary>
    public partial class FurnitureController : Node3D
    {
        // ── Parámetros exportados ─────────────────────────────────────────────

        [Export] public NodePath CameraPath   = new("CameraPivot/Camera3D");
        [Export] public float    GridSize     = SkillGridSnapping.GridNormal;
        [Export] public float    DragPlaneY   = 0f; // altura del plano de arrastre

        // Colores de feedback visual
        [Export] public Color ColorNormal    = new(0.7f, 0.5f, 0.3f);
        [Export] public Color ColorSelected  = new(0.2f, 0.7f, 1.0f);
        [Export] public Color ColorCollision = new(1.0f, 0.2f, 0.2f);

        // ── Estado interno ────────────────────────────────────────────────────

        private Camera3D? _camera;
        private Node3D?   _selected;
        private bool      _isDragging;
        private Vector3   _dragOffset;

        private readonly List<(string Id, Aabb Box)> _furnitureBoxes = new();

        // ── Señales ───────────────────────────────────────────────────────────

        [Signal] public delegate void FurnitureSelectedEventHandler(Node3D furniture);
        [Signal] public delegate void FurnitureDeselectedEventHandler();
        [Signal] public delegate void FurnitureMovedEventHandler(Node3D furniture, Vector3 newPosition);
        [Signal] public delegate void FurnitureRotatedEventHandler(Node3D furniture, float newRotation);
        [Signal] public delegate void CollisionDetectedEventHandler(Node3D furniture);

        // ── Ciclo de vida ─────────────────────────────────────────────────────

        public override void _Ready()
        {
            SkillGridSnapping.SetGridSize(GridSize);
            _camera = GetNodeOrNull<Camera3D>(CameraPath);

            if (_camera == null)
                GD.PushWarning("[FurnitureController] Camera3D no encontrada en la ruta especificada.");
        }

        public override void _UnhandledInput(InputEvent @event)
        {
            if (@event is InputEventMouseButton mb)
                HandleMouseButton(mb);

            if (@event is InputEventMouseMotion mm)
                HandleMouseMotion(mm);

            if (@event is InputEventKey key && key.Pressed)
                HandleKey(key);
        }

        // ── Manejo de entrada ─────────────────────────────────────────────────

        private void HandleMouseButton(InputEventMouseButton mb)
        {
            if (mb.ButtonIndex != MouseButton.Left) return;

            if (mb.Pressed)
            {
                var hit = Raycast(mb.Position);
                if (hit != null && hit is Node3D furniture3D && IsFurniture(furniture3D))
                {
                    SelectFurniture(furniture3D);
                    _isDragging  = true;

                    // Calcular offset entre la posición del mueble y el punto de clic
                    var floorHit = SkillGridSnapping.RaycastToFloor(_camera!, mb.Position);
                    if (floorHit.HasValue)
                        _dragOffset = _selected!.GlobalPosition - floorHit.Value;
                }
                else
                {
                    Deselect();
                }
            }
            else
            {
                _isDragging = false;
            }
        }

        private void HandleMouseMotion(InputEventMouseMotion mm)
        {
            if (!_isDragging || _selected == null || _camera == null) return;

            var floorHit = SkillGridSnapping.RaycastToFloor(_camera, mm.Position);
            if (!floorHit.HasValue) return;

            Vector3 newPos = SkillGridSnapping.Snap(floorHit.Value + _dragOffset);
            newPos.Y = _selected.Position.Y; // Conserva la altura propia del objeto (ej. aire acondicionado a 2.05m)

            _selected.GlobalPosition = newPos;

            // Auditoría de colisiones en tiempo real
            bool hasCollision = CheckCollisions(_selected);
            ApplyFeedbackColor(_selected, hasCollision ? ColorCollision : ColorSelected);

            if (hasCollision)
                EmitSignal(SignalName.CollisionDetected, _selected);

            EmitSignal(SignalName.FurnitureMoved, _selected, newPos);
        }

        private void HandleKey(InputEventKey key)
        {
            if (_selected == null) return;

            if (key.Keycode == Key.R)
            {
                float increment = key.ShiftPressed ? 90f : 45f;
                _selected.RotateY(Mathf.DegToRad(increment));

                // Snappear a múltiplo de increment
                float newDeg = Mathf.Round(_selected.RotationDegrees.Y / increment) * increment;
                _selected.RotationDegrees = new Vector3(0f, newDeg, 0f);

                EmitSignal(SignalName.FurnitureRotated, _selected, newDeg);
                GD.Print($"[FurnitureController] '{_selected.Name}' rotado a {newDeg}°");
            }

            if (key.Keycode == Key.Escape)
                Deselect();
        }

        // ── Selección ─────────────────────────────────────────────────────────

        private void SelectFurniture(Node3D furniture)
        {
            if (_selected != null && _selected != furniture)
                RestoreCustomColor(_selected);

            _selected = furniture;
            ApplyFeedbackColor(_selected, ColorSelected);
            EmitSignal(SignalName.FurnitureSelected, _selected);
            GD.Print($"[FurnitureController] Seleccionado: '{furniture.Name}'");
        }

        private void Deselect()
        {
            if (_selected != null)
            {
                RestoreCustomColor(_selected);
                _selected = null;
                _isDragging = false;
                EmitSignal(SignalName.FurnitureDeselected);
            }
        }

        private void RestoreCustomColor(Node3D node)
        {
            Color original = node.HasMeta("CustomColor")
                ? node.GetMeta("CustomColor").AsColor()
                : ColorNormal;
            ApplyFeedbackColor(node, original);
        }

        // ── Raycast ───────────────────────────────────────────────────────────

        private GodotObject? Raycast(Vector2 screenPos)
        {
            if (_camera == null) return null;

            var space = GetWorld3D().DirectSpaceState;
            var query = PhysicsRayQueryParameters3D.Create(
                _camera.ProjectRayOrigin(screenPos),
                _camera.ProjectRayOrigin(screenPos) + _camera.ProjectRayNormal(screenPos) * 100f
            );
            query.CollideWithBodies = true;

            var result = space.IntersectRay(query);
            if (result.Count == 0) return null;

            return result["collider"].AsGodotObject();
        }

        // ── Utilidades de muebles ─────────────────────────────────────────────

        /// <summary>
        /// Comprueba si un nodo es un mueble registrado
        /// (busca el grupo "furniture" o prefijo de nombre).
        /// </summary>
        private static bool IsFurniture(Node3D node)
        {
            return node.IsInGroup("furniture") || node.Name.ToString().StartsWith("Furniture_");
        }

        /// <summary>Registra un nodo como mueble controlable.</summary>
        public void RegisterFurniture(Node3D furniture)
        {
            furniture.AddToGroup("furniture");
            ApplyFeedbackColor(furniture, ColorNormal);
            GD.Print($"[FurnitureController] Registrado: '{furniture.Name}'");
        }

        /// <summary>Actualiza el color personalizado almacenado en el mueble.</summary>
        public void SetFurnitureCustomColor(Node3D node, Color color)
        {
            node.SetMeta("CustomColor", color);
            ApplyFeedbackColor(node, color);
        }

        /// <summary>Modifica las dimensiones 3D de un mueble existente y actualiza su colisión.</summary>
        public void SetFurnitureDimensions(Node3D node, float width, float depth, float height)
        {
            MeshInstance3D? mesh = node is MeshInstance3D m ? m : node.GetNodeOrNull<MeshInstance3D>("Mesh");
            if (mesh != null)
            {
                if (mesh.Mesh is BoxMesh boxMesh)
                {
                    boxMesh.Size = new Vector3(width, height, depth);
                }
                else
                {
                    mesh.Mesh = new BoxMesh { Size = new Vector3(width, height, depth) };
                }
                mesh.Position = new Vector3(0f, height * 0.5f, 0f);
            }

            CollisionShape3D? col = node.GetNodeOrNull<CollisionShape3D>("Collision");
            if (col != null)
            {
                if (col.Shape is BoxShape3D boxShape)
                {
                    boxShape.Size = new Vector3(width, height, depth);
                }
                else
                {
                    col.Shape = new BoxShape3D { Size = new Vector3(width, height, depth) };
                }
                col.Position = new Vector3(0f, height * 0.5f, 0f);
            }

            node.SetMeta("Width", width);
            node.SetMeta("Depth", depth);
            node.SetMeta("Height", height);
            GD.Print($"[FurnitureController] '{node.Name}' redimensionado a {width:F2}m x {depth:F2}m x {height:F2}m");
        }

        /// <summary>Aplica una textura/imagen desde disco al material del mueble.</summary>
        public bool SetFurnitureTexture(Node3D node, string imagePath)
        {
            if (string.IsNullOrEmpty(imagePath) || !FileAccess.FileExists(imagePath))
            {
                GD.PrintErr($"[FurnitureController] No se encontró la imagen: {imagePath}");
                return false;
            }

            var img = Image.LoadFromFile(imagePath);
            if (img == null)
            {
                GD.PrintErr($"[FurnitureController] Error al cargar la imagen: {imagePath}");
                return false;
            }

            var tex = ImageTexture.CreateFromImage(img);
            MeshInstance3D? mesh = node is MeshInstance3D m ? m : node.GetNodeOrNull<MeshInstance3D>("Mesh");
            if (mesh != null)
            {
                StandardMaterial3D mat;
                if (mesh.MaterialOverride is StandardMaterial3D existing)
                {
                    mat = existing;
                }
                else
                {
                    mat = new StandardMaterial3D();
                    mesh.MaterialOverride = mat;
                }

                mat.AlbedoTexture = tex;
                mat.AlbedoColor = Colors.White;
                node.SetMeta("CustomColor", Colors.White);
                node.SetMeta("TexturePath", imagePath);
                GD.Print($"[FurnitureController] Textura aplicada exitosamente a '{node.Name}': {imagePath}");
                return true;
            }

            return false;
        }

        /// <summary>Obtiene las dimensiones actuales del mueble (Width, Depth, Height).</summary>
        public Vector3 GetFurnitureDimensions(Node3D node)
        {
            if (node.HasMeta("Width") && node.HasMeta("Depth") && node.HasMeta("Height"))
            {
                return new Vector3(
                    node.GetMeta("Width").AsSingle(),
                    node.GetMeta("Depth").AsSingle(),
                    node.GetMeta("Height").AsSingle()
                );
            }

            MeshInstance3D? mesh = node is MeshInstance3D m ? m : node.GetNodeOrNull<MeshInstance3D>("Mesh");
            if (mesh?.Mesh is BoxMesh box)
            {
                return new Vector3(box.Size.X, box.Size.Z, box.Size.Y);
            }
            return new Vector3(1f, 1f, 1f);
        }

        // ── Colisiones ────────────────────────────────────────────────────────

        private bool CheckCollisions(Node3D target)
        {
            MeshInstance3D? targetMesh = target as MeshInstance3D ?? target.GetNodeOrNull<MeshInstance3D>("Mesh");
            if (targetMesh == null) return false;

            Aabb targetBox = SkillCollisionAudit.GetWorldAabb(targetMesh);
            var others = new List<Aabb>();

            foreach (Node child in GetChildren())
            {
                if (child == target) continue;
                MeshInstance3D? otherMesh = child as MeshInstance3D ?? child.GetNodeOrNull<MeshInstance3D>("Mesh");
                if (otherMesh == null) continue;
                others.Add(SkillCollisionAudit.GetWorldAabb(otherMesh));
            }

            return SkillCollisionAudit.ViolatesMinDistance(targetBox, others, 0f);
        }

        // ── Feedback visual ───────────────────────────────────────────────────

        private static void ApplyFeedbackColor(Node3D node, Color color)
        {
            // Buscar MeshInstance3D hijo o el nodo mismo
            MeshInstance3D? meshNode = null;

            if (node is MeshInstance3D direct)
                meshNode = direct;
            else
                meshNode = node.GetNodeOrNull<MeshInstance3D>("Mesh");

            if (meshNode?.MaterialOverride is StandardMaterial3D mat)
                mat.AlbedoColor = color;
            else if (meshNode != null)
            {
                var newMat = new StandardMaterial3D { AlbedoColor = color, Roughness = 0.8f };
                meshNode.MaterialOverride = newMat;
            }
        }

        // ── Creación de muebles ───────────────────────────────────────────────

        /// <summary>
        /// Crea un mueble simple (caja) y lo registra en el controlador.
        /// Útil para instanciar desde <see cref="SkillJsonParser.FurnitureItemData"/>.
        /// </summary>
        public Node3D SpawnFurniture(Skills.FurnitureItemData data)
        {
            var body = new StaticBody3D
            {
                Name     = $"Furniture_{data.Id}",
                Position = SkillJsonParser.ToVector3(data.Position)
            };
            body.RotationDegrees = new Vector3(0f, data.Rotation, 0f);
            body.AddToGroup("furniture");

            var color = SkillJsonParser.ParseColor(data.Color);
            body.SetMeta("CustomColor", color);
            body.SetMeta("Width", data.Dimensions.Width);
            body.SetMeta("Depth", data.Dimensions.Depth);
            body.SetMeta("Height", data.Dimensions.Height);

            var mesh    = new MeshInstance3D { Name = "Mesh" };
            var boxMesh = new BoxMesh
            {
                Size = new Vector3(data.Dimensions.Width, data.Dimensions.Height, data.Dimensions.Depth)
            };
            var mat = new StandardMaterial3D
            {
                AlbedoColor = color,
                Roughness   = 0.75f
            };
            mesh.Mesh             = boxMesh;
            mesh.MaterialOverride = mat;
            mesh.Position         = new Vector3(0f, data.Dimensions.Height * 0.5f, 0f);
            body.AddChild(mesh);

            var collision = new CollisionShape3D { Name = "Collision" };
            collision.Shape = new BoxShape3D
            {
                Size = new Vector3(data.Dimensions.Width, data.Dimensions.Height, data.Dimensions.Depth)
            };
            collision.Position = new Vector3(0f, data.Dimensions.Height * 0.5f, 0f);
            body.AddChild(collision);

            AddChild(body);
            return body;
        }
    }
}
