using Godot;
using RoomPlanner3D.Skills;

namespace RoomPlanner3D
{
	/// <summary>
	/// RoomBuilder — Generación paramétrica procedural del suelo y 4 muros.
	/// Cada superficie tiene su propio <see cref="StandardMaterial3D"/> independiente.
	/// Se crea la jerarquía:
	/// <code>
	///   Room (Node3D)
	///   ├── Floor (StaticBody3D → MeshInstance3D + CollisionShape3D)
	///   ├── WallNorth
	///   ├── WallSouth
	///   ├── WallEast
	///   └── WallWest
	/// </code>
	/// </summary>
	[Tool]
	public partial class RoomBuilder : Node3D
	{
		// ── Parámetros exportados ─────────────────────────────────────────────

		[Export] public float RoomWidth      = 4.5f;
		[Export] public float RoomLength     = 6.0f;
		[Export] public float RoomHeight     = 2.6f;
		[Export] public float WallThickness  = 0.15f;

		[Export] public Color FloorColor     = new Color(0.784f, 0.722f, 0.604f);
		[Export] public Color WallNorthColor = new Color(0.910f, 0.863f, 0.784f);
		[Export] public Color WallSouthColor = new Color(0.910f, 0.863f, 0.784f);
		[Export] public Color WallEastColor  = new Color(0.831f, 0.773f, 0.682f);
		[Export] public Color WallWestColor  = new Color(0.227f, 0.486f, 0.647f);

		[Export] public bool  ShowGrid       = true;
		[Export] public float GridStep       = 0.5f;

		// ── Nodos generados ───────────────────────────────────────────────────

		private StaticBody3D? _floor;
		private Node3D?       _gridOverlay;
		private Node3D?       _accessPointsNode;
		private StaticBody3D? _wallNorth;
		private StaticBody3D? _wallSouth;
		private StaticBody3D? _wallEast;
		private StaticBody3D? _wallWest;

		/// <summary>Puntos de acceso (puertas y ventanas) reconocidos en los muros.</summary>
		public System.Collections.Generic.List<SkillLayoutOptimizer.AccessPoint> AccessPoints { get; set; } = new()
		{
			new SkillLayoutOptimizer.AccessPoint { Wall = SkillLayoutOptimizer.WallSide.South, PositionAlongWall = 1.0f, Width = 0.9f },
			new SkillLayoutOptimizer.AccessPoint { Wall = SkillLayoutOptimizer.WallSide.East,  PositionAlongWall = 2.0f, Width = 1.2f }
		};

		// ── Señales ───────────────────────────────────────────────────────────

		[Signal] public delegate void RoomBuiltEventHandler(float width, float length, float height);
		[Signal] public delegate void RoomDemolishedEventHandler();

		// ── Ciclo de vida ─────────────────────────────────────────────────────

		public override void _Ready()
		{
			Build();
		}

		// ── API pública ───────────────────────────────────────────────────────

		/// <summary>Construye la habitación completa desde cero.</summary>
		public void Build()
		{
			Demolish();
			_floor            = CreateFloor();
			_gridOverlay      = CreateGridOverlay();
			_accessPointsNode = CreateAccessPoints();

			_wallNorth = CreateWall("WallNorth", WallNorthColor,
				new Vector3(RoomWidth, WallThickness, WallLength),
				new Vector3(RoomWidth * 0.5f, RoomHeight * 0.5f, WallThickness * 0.5f));
			_wallSouth = CreateWall("WallSouth", WallSouthColor,
				new Vector3(RoomWidth, WallThickness, WallLength),
				new Vector3(RoomWidth * 0.5f, RoomHeight * 0.5f, RoomLength - WallThickness * 0.5f));
			_wallWest  = CreateWall("WallWest", WallWestColor,
				new Vector3(WallThickness, WallLength, RoomLength),
				new Vector3(WallThickness * 0.5f, RoomHeight * 0.5f, RoomLength * 0.5f));
			_wallEast  = CreateWall("WallEast", WallEastColor,
				new Vector3(WallThickness, WallLength, RoomLength),
				new Vector3(RoomWidth - WallThickness * 0.5f, RoomHeight * 0.5f, RoomLength * 0.5f));

			AddChild(_floor);
			if (_gridOverlay != null)
				AddChild(_gridOverlay);
			if (_accessPointsNode != null)
				AddChild(_accessPointsNode);

			AddChild(_wallNorth);
			AddChild(_wallSouth);
			AddChild(_wallWest);
			AddChild(_wallEast);

			EmitSignal(SignalName.RoomBuilt, RoomWidth, RoomLength, RoomHeight);
			GD.Print($"[RoomBuilder] Habitación construida: {RoomWidth}m × {RoomLength}m × {RoomHeight}m");
		}

		/// <summary>
		/// Redimensiona la habitación sin destruir los nodos.
		/// Actualiza meshes y posiciones de todos los elementos.
		/// </summary>
		public void Resize(float width, float length, float height)
		{
			RoomWidth  = SkillGridSnapping.SnapAxis(width);
			RoomLength = SkillGridSnapping.SnapAxis(length);
			RoomHeight = height;
			Build(); // Reconstrucción limpia
		}

		/// <summary>Actualiza solo los materiales/colores sin reconstruir la geometría.</summary>
		public void Repaint(Color floor, Color north, Color south, Color east, Color west)
		{
			FloorColor     = floor;
			WallNorthColor = north;
			WallSouthColor = south;
			WallEastColor  = east;
			WallWestColor  = west;

			ApplyMaterial(_floor,     FloorColor);
			ApplyMaterial(_wallNorth, WallNorthColor);
			ApplyMaterial(_wallSouth, WallSouthColor);
			ApplyMaterial(_wallEast,  WallEastColor);
			ApplyMaterial(_wallWest,  WallWestColor);
		}

		/// <summary>Elimina todos los nodos hijos generados.</summary>
		public void Demolish()
		{
			foreach (Node child in GetChildren())
				child.QueueFree();

			_floor = _wallNorth = _wallSouth = _wallEast = _wallWest = null;
			_gridOverlay = _accessPointsNode = null;
			EmitSignal(SignalName.RoomDemolished);
		}

		// ── Builders internos ─────────────────────────────────────────────────

		private Node3D? CreateGridOverlay()
		{
			if (!ShowGrid) return null;

			var gridNode = new Node3D { Name = "GridOverlay" };
			var meshInstance = new MeshInstance3D { Name = "GridMesh" };

			var imm = new ImmediateMesh();
			var mat = new StandardMaterial3D
			{
				ShadingMode  = BaseMaterial3D.ShadingModeEnum.Unshaded,
				AlbedoColor  = new Color(0.12f, 0.15f, 0.22f, 0.22f),
				Transparency = BaseMaterial3D.TransparencyEnum.Alpha
			};

			imm.SurfaceBegin(Mesh.PrimitiveType.Lines, mat);
			float y = 0.003f; // Elevado para evitar z-fighting con el suelo

			for (float x = 0; x <= RoomWidth + 0.01f; x += GridStep)
			{
				imm.SurfaceAddVertex(new Vector3(x, y, 0f));
				imm.SurfaceAddVertex(new Vector3(x, y, RoomLength));
			}

			for (float z = 0; z <= RoomLength + 0.01f; z += GridStep)
			{
				imm.SurfaceAddVertex(new Vector3(0f, y, z));
				imm.SurfaceAddVertex(new Vector3(RoomWidth, y, z));
			}

			imm.SurfaceEnd();

			meshInstance.Mesh = imm;
			gridNode.AddChild(meshInstance);
			return gridNode;
		}

		private Node3D CreateAccessPoints()
		{
			var root = new Node3D { Name = "AccessPoints" };

			foreach (var ap in AccessPoints)
			{
				if (ap.Wall == SkillLayoutOptimizer.WallSide.South)
				{
					// Puerta en muro sur: marco de madera + hoja
					var doorNode = new Node3D { Name = "Door_South" };
					var frame = new MeshInstance3D
					{
						Name = "DoorFrame",
						Mesh = new BoxMesh { Size = new Vector3(ap.Width, 2.1f, WallThickness * 1.05f) },
						MaterialOverride = new StandardMaterial3D
						{
							AlbedoColor = new Color(0.42f, 0.28f, 0.18f),
							Roughness = 0.65f
						},
						Position = new Vector3(ap.PositionAlongWall + ap.Width * 0.5f, 1.05f, RoomLength - WallThickness * 0.5f)
					};
					doorNode.AddChild(frame);
					root.AddChild(doorNode);
				}
				else if (ap.Wall == SkillLayoutOptimizer.WallSide.East)
				{
					// Ventana en muro este: marco + cristal traslúcido
					var winNode = new Node3D { Name = "Window_East" };
					var glass = new MeshInstance3D
					{
						Name = "WindowGlass",
						Mesh = new BoxMesh { Size = new Vector3(WallThickness * 1.05f, 1.2f, ap.Width) },
						MaterialOverride = new StandardMaterial3D
						{
							AlbedoColor  = new Color(0.65f, 0.85f, 1.0f, 0.45f),
							Transparency = BaseMaterial3D.TransparencyEnum.Alpha,
							Roughness    = 0.1f,
							Metallic     = 0.2f
						},
						Position = new Vector3(RoomWidth - WallThickness * 0.5f, 1.5f, ap.PositionAlongWall + ap.Width * 0.5f)
					};
					winNode.AddChild(glass);
					root.AddChild(winNode);
				}
			}

			return root;
		}

		private StaticBody3D CreateFloor()
		{
			var body  = new StaticBody3D { Name = "Floor" };
			var mesh  = new MeshInstance3D { Name = "FloorMesh" };
			var plane = new PlaneMesh
			{
				Size = new Vector2(RoomWidth, RoomLength)
			};
			mesh.Mesh              = plane;
			mesh.MaterialOverride  = CreateMaterial(FloorColor, metallic: 0f, roughness: 0.85f);
			mesh.Position          = new Vector3(RoomWidth * 0.5f, 0f, RoomLength * 0.5f);

			var shape = new CollisionShape3D { Name = "FloorCollision" };
			shape.Shape = new BoxShape3D { Size = new Vector3(RoomWidth, 0.01f, RoomLength) };

			body.AddChild(mesh);
			body.AddChild(shape);
			return body;
		}

		private StaticBody3D CreateWall(string wallName, Color color, Vector3 size, Vector3 position)
		{
			var body = new StaticBody3D { Name = wallName };

			var mesh    = new MeshInstance3D { Name = wallName + "Mesh" };
			var boxMesh = new BoxMesh { Size = new Vector3(size.X, RoomHeight, size.Z) };
			mesh.Mesh             = boxMesh;
			mesh.MaterialOverride = CreateMaterial(color, metallic: 0f, roughness: 0.9f);
			mesh.Position         = position;

			var shape = new CollisionShape3D { Name = wallName + "Collision" };
			shape.Shape    = new BoxShape3D { Size = new Vector3(size.X, RoomHeight, size.Z) };
			shape.Position = position;

			body.AddChild(mesh);
			body.AddChild(shape);
			return body;
		}

		private static StandardMaterial3D CreateMaterial(Color albedo,
														  float metallic   = 0f,
														  float roughness  = 0.8f)
		{
			return new StandardMaterial3D
			{
				AlbedoColor = albedo,
				Metallic    = metallic,
				Roughness   = roughness
			};
		}

		private static void ApplyMaterial(StaticBody3D? body, Color color)
		{
			if (body == null) return;
			foreach (Node child in body.GetChildren())
			{
				if (child is MeshInstance3D mesh && mesh.MaterialOverride is StandardMaterial3D mat)
					mat.AlbedoColor = color;
			}
		}

		// ── Utilidades ────────────────────────────────────────────────────────

		/// <summary>Altura de muro = altura de habitación (sin techo independiente).</summary>
		private float WallLength => RoomHeight;

		/// <summary>
		/// Carga y aplica un <see cref="RoomLayoutData"/> completo al builder.
		/// </summary>
		public void ApplyLayout(Skills.RoomLayoutData layout)
		{
			RoomWidth      = layout.Room.Width;
			RoomLength     = layout.Room.Length;
			RoomHeight     = layout.Room.Height;
			WallThickness  = layout.Room.WallThickness;

			FloorColor     = SkillJsonParser.ParseColor(layout.Surfaces.Floor.Color);
			WallNorthColor = SkillJsonParser.ParseColor(layout.Surfaces.WallNorth.Color);
			WallSouthColor = SkillJsonParser.ParseColor(layout.Surfaces.WallSouth.Color);
			WallEastColor  = SkillJsonParser.ParseColor(layout.Surfaces.WallEast.Color);
			WallWestColor  = SkillJsonParser.ParseColor(layout.Surfaces.WallWest.Color);

			if (layout.AccessPoints != null && layout.AccessPoints.Count > 0)
			{
				AccessPoints.Clear();
				foreach (var ap in layout.AccessPoints)
				{
					var wall = ap.Wall.ToLowerInvariant() switch
					{
						"north" => SkillLayoutOptimizer.WallSide.North,
						"south" => SkillLayoutOptimizer.WallSide.South,
						"east"  => SkillLayoutOptimizer.WallSide.East,
						"west"  => SkillLayoutOptimizer.WallSide.West,
						_       => SkillLayoutOptimizer.WallSide.None
					};
					AccessPoints.Add(new SkillLayoutOptimizer.AccessPoint
					{
						Wall              = wall,
						PositionAlongWall = ap.PositionAlongWall,
						Width             = ap.Width
					});
				}
			}

			Build();
		}
	}
}
