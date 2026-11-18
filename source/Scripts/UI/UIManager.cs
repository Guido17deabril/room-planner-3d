using Godot;
using RoomPlanner3D.Skills;

namespace RoomPlanner3D.UI
{
    /// <summary>
    /// UIManager — Orquestador raíz de la interfaz gráfica.
    ///
    /// Responsabilidades:
    /// - Mantener referencias a los nodos 3D clave (RoomBuilder, FurnitureController).
    /// - Escuchar señales Godot de FurnitureController y actualizar el inspector.
    /// - Enrutar eventos del bus UIEvents hacia los controllers 3D apropiados.
    /// - Gestionar el estado global de la UI (qué panel está visible, etc.).
    /// </summary>
    public partial class UIManager : CanvasLayer
    {
        // ── Referencias a nodos 3D (configuradas en Inspector de Godot) ────────

        [Export] public NodePath RoomBuilderPath    = new("../RoomNode");
        [Export] public NodePath FurnitureRootPath  = new("../FurnitureRoot");
        [Export] public NodePath CameraPath         = new("../CameraPivot/Camera3D");

        // ── Referencias a subpaneles UI (hijos de este CanvasLayer) ───────────

        [Export] public NodePath TopBarPath         = new("TopBar");
        [Export] public NodePath CatalogDrawerPath  = new("CatalogDrawer");
        [Export] public NodePath InspectorPanelPath = new("InspectorPanel");
        [Export] public NodePath IAButtonPath       = new("IAOptimizerButton");
        [Export] public NodePath ChatPanelPath      = new("ChatPanel");

        // ── Nodos 3D resueltos ────────────────────────────────────────────────

        private RoomBuilder?          _roomBuilder;
        private FurnitureController?  _furnitureController;
        private CameraController?     _camera;

        // ── Subpaneles ────────────────────────────────────────────────────────

        private TopBarController?      _topBar;
        private CatalogController?     _catalog;
        private InspectorController?   _inspector;
        private IAOptimizerController? _iaButton;
        private ChatController?        _chatPanel;

        // ── Estado ────────────────────────────────────────────────────────────

        private Node3D? _selectedFurniture;
        private bool    _is3DMode = true;
        private readonly System.Collections.Generic.Stack<System.Action> _undoStack = new();

        // ── Ciclo de vida ─────────────────────────────────────────────────────

        public override void _Ready()
        {
            // Resolver nodos 3D
            _roomBuilder         = GetNodeOrNull<RoomBuilder>(RoomBuilderPath);
            _furnitureController = GetNodeOrNull<FurnitureController>(FurnitureRootPath);
            _camera              = GetNodeOrNull<CameraController>(CameraPath);

            // Resolver subpaneles
            _topBar    = GetNodeOrNull<TopBarController>(TopBarPath);
            _catalog   = GetNodeOrNull<CatalogController>(CatalogDrawerPath);
            _inspector = GetNodeOrNull<InspectorController>(InspectorPanelPath);
            _iaButton  = GetNodeOrNull<IAOptimizerController>(IAButtonPath);
            _chatPanel = GetNodeOrNull<ChatController>(ChatPanelPath);

            // Conectar señales de FurnitureController → UI
            if (_furnitureController != null)
            {
                _furnitureController.FurnitureSelected   += OnFurnitureSelected;
                _furnitureController.FurnitureDeselected += OnFurnitureDeselected;
                _furnitureController.FurnitureMoved      += OnFurnitureMoved;
                _furnitureController.FurnitureRotated    += OnFurnitureRotated;
                _furnitureController.CollisionDetected   += OnCollisionDetected;
            }

            // Conectar señales de RoomBuilder → UI
            if (_roomBuilder != null)
            {
                _roomBuilder.RoomBuilt += OnRoomBuilt;
            }

            // Suscribir eventos UIEvents → acciones 3D
            UIEvents.UndoRequested              += HandleUndo;
            UIEvents.FurnitureSpawnRequested    += HandleFurnitureSpawn;
            UIEvents.RoomResizeRequested        += HandleRoomResize;
            UIEvents.RoomRepaintRequested       += HandleRoomRepaint;
            UIEvents.ColorHarmonyRequested      += HandleColorHarmony;
            UIEvents.FurnitureRotateRequested   += HandleFurnitureRotate;
            UIEvents.FurnitureDeleteRequested   += HandleFurnitureDelete;
            UIEvents.FurnitureColorChangeRequested += HandleFurnitureColorChange;
            UIEvents.FurnitureResizeRequested      += HandleFurnitureResize;
            UIEvents.FurnitureTextureChangeRequested += HandleFurnitureTextureChange;
            UIEvents.SaveLayoutRequested        += HandleSaveLayout;
            UIEvents.LoadLayoutRequested        += HandleLoadLayout;
            UIEvents.ViewModeChanged            += HandleViewModeChanged;
            UIEvents.CameraModeChanged          += HandleCameraModeChanged;
            UIEvents.TemplateRequested          += HandleTemplate;
            UIEvents.AutoLayoutRequested        += HandleAutoLayout;

            GD.Print("[UIManager] Inicializado correctamente.");
        }

        public override void _ExitTree()
        {
            // Desuscribir para evitar fugas de memoria
            UIEvents.UndoRequested                 -= HandleUndo;
            UIEvents.FurnitureSpawnRequested       -= HandleFurnitureSpawn;
            UIEvents.RoomResizeRequested           -= HandleRoomResize;
            UIEvents.RoomRepaintRequested          -= HandleRoomRepaint;
            UIEvents.ColorHarmonyRequested         -= HandleColorHarmony;
            UIEvents.FurnitureRotateRequested      -= HandleFurnitureRotate;
            UIEvents.FurnitureDeleteRequested      -= HandleFurnitureDelete;
            UIEvents.FurnitureColorChangeRequested -= HandleFurnitureColorChange;
            UIEvents.FurnitureResizeRequested      -= HandleFurnitureResize;
            UIEvents.FurnitureTextureChangeRequested -= HandleFurnitureTextureChange;
            UIEvents.SaveLayoutRequested           -= HandleSaveLayout;
            UIEvents.LoadLayoutRequested           -= HandleLoadLayout;
            UIEvents.ViewModeChanged               -= HandleViewModeChanged;
            UIEvents.CameraModeChanged             -= HandleCameraModeChanged;
            UIEvents.TemplateRequested             -= HandleTemplate;
            UIEvents.AutoLayoutRequested           -= HandleAutoLayout;
        }

        // ── Manejadores de señales 3D → UI ────────────────────────────────────

        private void OnFurnitureSelected(Node3D furniture)
        {
            _selectedFurniture = furniture;
            _inspector?.ShowFurnitureSection(furniture);
            GD.Print($"[UIManager] Mueble seleccionado: {furniture.Name}");
        }

        private void OnFurnitureDeselected()
        {
            _selectedFurniture = null;
            _inspector?.HideFurnitureSection();
        }

        private void OnFurnitureMoved(Node3D furniture, Vector3 newPos)
        {
            _inspector?.UpdateFurniturePosition(newPos);
        }

        private void OnFurnitureRotated(Node3D furniture, float newRotation)
        {
            _inspector?.UpdateFurnitureRotation(newRotation);
        }

        private void OnCollisionDetected(Node3D furniture)
        {
            _inspector?.ShowCollisionWarning();
        }

        private void OnRoomBuilt(float width, float length, float height)
        {
            _inspector?.UpdateRoomDimensions(width, length, height);
            _topBar?.UpdateRoomInfo(width, length, height);
            _camera?.SetPivot(new Vector3(width * 0.5f, 0f, length * 0.5f));
        }

        // ── Manejadores de UIEvents → acciones 3D ─────────────────────────────

        private void HandleUndo()
        {
            if (_undoStack.Count > 0)
            {
                var undo = _undoStack.Pop();
                undo.Invoke();
                GD.Print($"[UIManager] Deshacer ejecutado. Restantes en historial: {_undoStack.Count}");
            }
            else
            {
                GD.Print("[UIManager] Historial de deshacer vacío.");
            }
        }

        private void HandleCameraModeChanged(string mode)
        {
            _camera?.SetCameraPreset(mode);
            GD.Print($"[UIManager] Preset de cámara aplicado: {mode}");
        }

        private void HandleFurnitureSpawn(string typeId, string label)
        {
            if (_furnitureController == null) return;

            // Crear FurnitureItemData con dimensiones por defecto según tipo
            var data = FurnitureCatalog.CreateItemData(typeId, label);
            Vector3 spawn = GetSpawnPosition();
            float elevation = data.Position.Y; // Conserva altura si es montado en pared (ej. aire acondicionado a 2.05m)
            data.Position = SkillJsonParser.FromVector3(new Vector3(spawn.X, elevation, spawn.Z));

            var spawned = _furnitureController.SpawnFurniture(data);
            if (spawned != null)
            {
                _undoStack.Push(() =>
                {
                    if (IsInstanceValid(spawned))
                    {
                        if (_selectedFurniture == spawned)
                        {
                            _selectedFurniture = null;
                            _inspector?.HideFurnitureSection();
                        }
                        spawned.QueueFree();
                        GD.Print($"[UIManager] Deshacer: mueble '{spawned.Name}' removido.");
                    }
                });
            }
            GD.Print($"[UIManager] Mueble '{label}' ({typeId}) instanciado en escena.");
        }

        private void HandleRoomResize(float w, float l, float h)
        {
            if (_roomBuilder == null) return;
            float prevW = _roomBuilder.RoomWidth;
            float prevL = _roomBuilder.RoomLength;
            float prevH = _roomBuilder.RoomHeight;

            _undoStack.Push(() =>
            {
                _roomBuilder?.Resize(prevW, prevL, prevH);
                _inspector?.UpdateRoomDimensions(prevW, prevL, prevH);
                _topBar?.UpdateRoomInfo(prevW, prevL, prevH);
            });

            _roomBuilder.Resize(w, l, h);
        }

        private void HandleRoomRepaint(Color floor, Color north, Color south, Color east, Color west)
        {
            if (_roomBuilder == null) return;
            Color prevFloor = _roomBuilder.FloorColor;
            Color prevNorth = _roomBuilder.WallNorthColor;
            Color prevSouth = _roomBuilder.WallSouthColor;
            Color prevEast  = _roomBuilder.WallEastColor;
            Color prevWest  = _roomBuilder.WallWestColor;

            _undoStack.Push(() =>
            {
                _roomBuilder?.Repaint(prevFloor, prevNorth, prevSouth, prevEast, prevWest);
            });

            _roomBuilder.Repaint(floor, north, south, east, west);
        }

        private void HandleColorHarmony(Color baseColor, string mode)
        {
            var harmonyMode = mode switch
            {
                "Complementary"      => SkillColorHarmony.HarmonyMode.Complementary,
                "Triadic"            => SkillColorHarmony.HarmonyMode.Triadic,
                "SplitComplementary" => SkillColorHarmony.HarmonyMode.SplitComplementary,
                "Tetradic"           => SkillColorHarmony.HarmonyMode.Tetradic,
                _                    => SkillColorHarmony.HarmonyMode.Analogous
            };

            var palette = SkillColorHarmony.GeneratePalette(baseColor, harmonyMode);
            _inspector?.ApplyHarmonyPalette(palette);

            // Repintar habitación con la paleta generada
            HandleRoomRepaint(
                palette.Dominant,
                palette.Dominant,
                palette.Dominant,
                palette.Secondary,
                palette.Accent
            );
        }

        private void HandleFurnitureRotate(float degrees)
        {
            if (_selectedFurniture == null) return;
            var target = _selectedFurniture;
            float prevRot = target.RotationDegrees.Y;

            _undoStack.Push(() =>
            {
                if (IsInstanceValid(target))
                {
                    target.RotationDegrees = new Vector3(0f, prevRot, 0f);
                    _inspector?.UpdateFurnitureRotation(prevRot);
                }
            });

            target.RotateY(Mathf.DegToRad(degrees));
            float snapped = Mathf.Round(target.RotationDegrees.Y / degrees) * degrees;
            target.RotationDegrees = new Vector3(0f, snapped, 0f);
            _inspector?.UpdateFurnitureRotation(snapped);
        }

        private void HandleFurnitureDelete()
        {
            if (_selectedFurniture == null) return;
            var target = _selectedFurniture;

            // Guardar datos para permitir deshacer
            MeshInstance3D? mesh = target is MeshInstance3D m ? m : target.GetNodeOrNull<MeshInstance3D>("Mesh");
            var box = mesh?.Mesh as BoxMesh;
            var mat = mesh?.MaterialOverride as StandardMaterial3D;
            var restoreData = new Skills.FurnitureItemData
            {
                Id = target.Name.ToString().Replace("Furniture_", ""),
                Type = "generic",
                Label = target.Name,
                Position = SkillJsonParser.FromVector3(target.Position),
                Rotation = target.RotationDegrees.Y,
                Dimensions = new Skills.FurnitureDimensions
                {
                    Width = box?.Size.X ?? 1f,
                    Height = box?.Size.Y ?? 1f,
                    Depth = box?.Size.Z ?? 1f
                },
                Color = mat != null ? SkillJsonParser.ColorToHex(mat.AlbedoColor) : "#888888"
            };

            _undoStack.Push(() =>
            {
                if (_furnitureController != null)
                {
                    var restored = _furnitureController.SpawnFurniture(restoreData);
                    GD.Print($"[UIManager] Deshacer: mueble restaurado '{restored.Name}'.");
                }
            });

            target.QueueFree();
            _selectedFurniture = null;
            _inspector?.HideFurnitureSection();
        }

        private void HandleFurnitureColorChange(Color color)
        {
            if (_selectedFurniture == null) return;
            _furnitureController?.SetFurnitureCustomColor(_selectedFurniture, color);
        }

        private void HandleFurnitureResize(float width, float depth, float height)
        {
            if (_selectedFurniture == null) return;
            _furnitureController?.SetFurnitureDimensions(_selectedFurniture, width, depth, height);
        }

        private void HandleFurnitureTextureChange(string imagePath)
        {
            if (_selectedFurniture == null) return;
            _furnitureController?.SetFurnitureTexture(_selectedFurniture, imagePath);
        }

        private void HandleSaveLayout(string path)
        {
            if (_roomBuilder == null) return;

            var layout = new Skills.RoomLayoutData
            {
                Name = "Layout guardado desde UI",
                Room = new Skills.RoomDimensions
                {
                    Width         = _roomBuilder.RoomWidth,
                    Length        = _roomBuilder.RoomLength,
                    Height        = _roomBuilder.RoomHeight,
                    WallThickness = _roomBuilder.WallThickness
                },
                Surfaces = new Skills.SurfacesData
                {
                    Floor     = new Skills.SurfaceEntry { Color = SkillJsonParser.ColorToHex(_roomBuilder.FloorColor) },
                    WallNorth = new Skills.SurfaceEntry { Color = SkillJsonParser.ColorToHex(_roomBuilder.WallNorthColor) },
                    WallSouth = new Skills.SurfaceEntry { Color = SkillJsonParser.ColorToHex(_roomBuilder.WallSouthColor) },
                    WallEast  = new Skills.SurfaceEntry { Color = SkillJsonParser.ColorToHex(_roomBuilder.WallEastColor) },
                    WallWest  = new Skills.SurfaceEntry { Color = SkillJsonParser.ColorToHex(_roomBuilder.WallWestColor) }
                }
            };

            // Recopilar todos los muebles activos
            if (_furnitureController != null)
            {
                foreach (Node child in _furnitureController.GetChildren())
                {
                    if (child is not Node3D node3D || !node3D.IsInGroup("furniture")) continue;

                    MeshInstance3D? mesh = node3D as MeshInstance3D ?? node3D.GetNodeOrNull<MeshInstance3D>("Mesh");
                    var box = mesh?.Mesh as BoxMesh;
                    var mat = mesh?.MaterialOverride as StandardMaterial3D;
                    Color fColor = node3D.HasMeta("CustomColor")
                        ? node3D.GetMeta("CustomColor").AsColor()
                        : (mat?.AlbedoColor ?? Colors.Gray);

                    string id = node3D.Name.ToString().Replace("Furniture_", "");
                    layout.Furniture.Add(new Skills.FurnitureItemData
                    {
                        Id = id,
                        Type = id.Contains('_') ? id.Substring(0, id.LastIndexOf('_')) : id,
                        Label = node3D.Name,
                        Dimensions = new Skills.FurnitureDimensions
                        {
                            Width  = box?.Size.X ?? 1.0f,
                            Height = box?.Size.Y ?? 1.0f,
                            Depth  = box?.Size.Z ?? 1.0f
                        },
                        Position = SkillJsonParser.FromVector3(node3D.Position),
                        Rotation = node3D.RotationDegrees.Y,
                        Color    = SkillJsonParser.ColorToHex(fColor)
                    });
                }
            }

            try
            {
                SkillJsonParser.SaveToFile(layout, path);
                _topBar?.ShowSaveConfirmation(path);
                GD.Print($"[UIManager] Layout guardado exitosamente en: '{path}' con {layout.Furniture.Count} muebles.");
            }
            catch (System.Exception ex)
            {
                GD.PushError($"[UIManager] Error al guardar: {ex.Message}");
            }
        }

        private void HandleLoadLayout(string path)
        {
            try
            {
                var layout = SkillJsonParser.LoadFromFile(path);
                _roomBuilder?.ApplyLayout(layout);
                _inspector?.UpdateRoomDimensions(layout.Room.Width, layout.Room.Length, layout.Room.Height);
                _topBar?.UpdateRoomInfo(layout.Room.Width, layout.Room.Length, layout.Room.Height);
                _camera?.SetPivot(new Vector3(layout.Room.Width * 0.5f, 0f, layout.Room.Length * 0.5f));

                // Limpiar muebles actuales
                if (_furnitureController != null)
                {
                    if (_selectedFurniture != null)
                    {
                        _selectedFurniture = null;
                        _inspector?.HideFurnitureSection();
                    }

                    foreach (Node child in _furnitureController.GetChildren())
                    {
                        if (child is Node3D n && n.IsInGroup("furniture"))
                            n.QueueFree();
                    }

                    // Reinstanciar muebles cargados desde el archivo
                    foreach (var item in layout.Furniture)
                    {
                        _furnitureController.SpawnFurniture(item);
                    }
                }

                GD.Print($"[UIManager] Layout cargado: '{layout.Name}' ({layout.Furniture.Count} muebles).");
            }
            catch (System.Exception ex)
            {
                GD.PushError($"[UIManager] Error al cargar: {ex.Message}");
            }
        }

        private void HandleViewModeChanged(bool is3D)
        {
            _is3DMode = is3D;
            if (_camera == null) return;

            if (is3D)
                _camera.ResetView();
            else
                _camera.SetZoom(12f); // vista cenital para 2D top-down

            GD.Print($"[UIManager] Modo de vista: {(is3D ? "3D" : "2D Top-Down")}");
        }

        private void HandleTemplate(string templateName)
        {
            if (_roomBuilder == null || _furnitureController == null) return;

            // Limpiar muebles existentes
            if (_selectedFurniture != null)
            {
                _selectedFurniture = null;
                _inspector?.HideFurnitureSection();
            }

            foreach (Node child in _furnitureController.GetChildren())
            {
                if (child is Node3D n && n.IsInGroup("furniture"))
                    n.QueueFree();
            }

            switch (templateName.ToLowerInvariant())
            {
                case "dormitorio":
                    _roomBuilder.Resize(4.5f, 6.0f, 2.6f);
                    HandleRoomRepaint(
                        new Color(0.78f, 0.72f, 0.60f), // suelo roble
                        new Color(0.91f, 0.86f, 0.78f), // norte
                        new Color(0.91f, 0.86f, 0.78f), // sur
                        new Color(0.83f, 0.77f, 0.68f), // este
                        new Color(0.23f, 0.49f, 0.65f)  // oeste accent
                    );

                    SpawnTemplateItem("bed_double", "Cama Doble", new Vector3(2.25f, 0f, 1.2f), 0f);
                    SpawnTemplateItem("nightstand", "Mesita Izquierda", new Vector3(1.05f, 0f, 1.2f), 0f);
                    SpawnTemplateItem("nightstand", "Mesita Derecha", new Vector3(3.45f, 0f, 1.2f), 0f);
                    SpawnTemplateItem("wardrobe", "Armario", new Vector3(1.2f, 0f, 5.5f), 180f);
                    SpawnTemplateItem("desk_large", "Escritorio", new Vector3(4.0f, 0f, 3.2f), 270f);
                    break;

                case "oficina":
                    _roomBuilder.Resize(4.0f, 4.2f, 2.6f);
                    HandleRoomRepaint(
                        new Color(0.72f, 0.76f, 0.78f),
                        new Color(0.87f, 0.89f, 0.91f),
                        new Color(0.87f, 0.89f, 0.91f),
                        new Color(0.80f, 0.84f, 0.86f),
                        new Color(0.25f, 0.38f, 0.45f)
                    );

                    SpawnTemplateItem("desk_large", "Escritorio Principal", new Vector3(2.0f, 0f, 1.5f), 0f);
                    SpawnTemplateItem("office_chair", "Silla Ejecutiva", new Vector3(2.0f, 0f, 2.2f), 0f);
                    SpawnTemplateItem("bookshelf", "Estantería", new Vector3(0.5f, 0f, 3.5f), 90f);
                    SpawnTemplateItem("filing_cabinet", "Archivero", new Vector3(3.6f, 0f, 3.5f), 270f);
                    break;

                case "living":
                    _roomBuilder.Resize(5.0f, 6.5f, 2.6f);
                    HandleRoomRepaint(
                        new Color(0.82f, 0.77f, 0.70f),
                        new Color(0.94f, 0.92f, 0.89f),
                        new Color(0.94f, 0.92f, 0.89f),
                        new Color(0.88f, 0.85f, 0.80f),
                        new Color(0.48f, 0.32f, 0.23f)
                    );

                    SpawnTemplateItem("sofa_3seat", "Sofá 3 Plazas", new Vector3(2.5f, 0f, 4.8f), 180f);
                    SpawnTemplateItem("coffee_table", "Mesa de Centro", new Vector3(2.5f, 0f, 3.5f), 0f);
                    SpawnTemplateItem("tv_stand", "Mueble TV", new Vector3(2.5f, 0f, 1.0f), 0f);
                    SpawnTemplateItem("armchair", "Sillón", new Vector3(1.1f, 0f, 3.5f), 45f);
                    SpawnTemplateItem("dining_table", "Mesa Comedor", new Vector3(4.0f, 0f, 2.5f), 0f);
                    break;

                case "vaciar":
                default:
                    GD.Print("[UIManager] Habitación vaciada.");
                    break;
            }

            _inspector?.UpdateRoomDimensions(_roomBuilder.RoomWidth, _roomBuilder.RoomLength, _roomBuilder.RoomHeight);
            _topBar?.UpdateRoomInfo(_roomBuilder.RoomWidth, _roomBuilder.RoomLength, _roomBuilder.RoomHeight);
            _camera?.SetPivot(new Vector3(_roomBuilder.RoomWidth * 0.5f, 0f, _roomBuilder.RoomLength * 0.5f));
            GD.Print($"[UIManager] Plantilla '{templateName}' aplicada.");
        }

        private void SpawnTemplateItem(string typeId, string label, Vector3 position, float rotation)
        {
            if (_furnitureController == null) return;
            var data = FurnitureCatalog.CreateItemData(typeId, label);
            data.Position = SkillJsonParser.FromVector3(position);
            data.Rotation = rotation;
            _furnitureController.SpawnFurniture(data);
        }

        private void HandleAutoLayout()
        {
            if (_roomBuilder == null || _furnitureController == null) return;

            _iaButton?.StartCalculationAnimation();

            var roomBounds = new SkillLayoutOptimizer.RoomBounds
            {
                Width          = _roomBuilder.RoomWidth,
                Length         = _roomBuilder.RoomLength,
                Height         = _roomBuilder.RoomHeight,
                WallThickness  = _roomBuilder.WallThickness
            };

            // Construir specs desde los hijos actuales de FurnitureRoot
            var specs = new System.Collections.Generic.List<SkillLayoutOptimizer.FurnitureSpec>();
            int priority = 0;
            foreach (Node child in _furnitureController.GetChildren())
            {
                if (child is not Node3D node3D) continue;
                if (!node3D.IsInGroup("furniture")) continue;

                MeshInstance3D? mesh = node3D is MeshInstance3D m2 ? m2 : node3D.GetNodeOrNull<MeshInstance3D>("Mesh");
                if (mesh?.Mesh is not BoxMesh box) continue;

                var catalogEntry = FurnitureCatalog.FindEntry(node3D.Name);
                specs.Add(new SkillLayoutOptimizer.FurnitureSpec
                {
                    Id            = node3D.Name,
                    Size          = box.Size,
                    PerimeterOnly = catalogEntry?.PerimeterOnly ?? false,
                    PreferredWall = catalogEntry?.PreferredWall ?? SkillLayoutOptimizer.WallSide.None,
                    Priority      = priority++
                });
            }

            var accessPoints = _roomBuilder.AccessPoints ?? new System.Collections.Generic.List<SkillLayoutOptimizer.AccessPoint>();
            var result = SkillLayoutOptimizer.Optimize(roomBounds, specs, accessPoints);

            // Aplicar posiciones resultantes
            foreach (var placed in result.Placed)
            {
                var node = _furnitureController.GetNodeOrNull<Node3D>(placed.Id);
                if (node == null) continue;
                node.Position         = placed.Position + Vector3.Up * node.Position.Y;
                node.RotationDegrees  = new Vector3(0f, placed.Rotation, 0f);
            }

            _iaButton?.StopCalculationAnimation(result.ErgonomicScore);
            GD.Print($"[UIManager] Auto-layout completado — Score: {result.ErgonomicScore:F1}/100 | Colocados: {result.Placed.Count} | Sin ubicar: {result.Unplaced.Count}");
        }

        // ── Utilidades ────────────────────────────────────────────────────────

        /// <summary>Calcula la posición de spawn frente a la cámara a altura 0.</summary>
        private Vector3 GetSpawnPosition()
        {
            if (_roomBuilder == null) return Vector3.Zero;
            return new Vector3(
                _roomBuilder.RoomWidth  * 0.5f,
                0f,
                _roomBuilder.RoomLength * 0.5f
            );
        }
    }
}
