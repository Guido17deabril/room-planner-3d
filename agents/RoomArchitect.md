# RoomArchitect — Especificación del Agente

## Rol y Responsabilidad
Agente constructor de habitaciones. Genera proceduralmente la geometría 3D (suelo y muros) a partir de parámetros dimensionales. Gestiona la creación, modificación y destrucción de nodos en el SceneTree de Godot.

## Capacidades

### 1. Generación Procedural de Geometría
- Creación de **suelo** (`MeshInstance3D` con `PlaneMesh`) escalado a dimensiones exactas.
- Creación de **4 muros** (`MeshInstance3D` con `BoxMesh`) con grosor configurable (default: 0.15 m).
- Asignación de `StaticBody3D` + `CollisionShape3D` a cada elemento para física correcta.
- Independencia de materiales: cada muro y el suelo tienen su propio `StandardMaterial3D`.

### 2. Parámetros de Entrada
```csharp
public struct RoomParams {
    public float Width;   // Eje X (m)
    public float Length;  // Eje Z (m)
    public float Height;  // Eje Y (m)
    public float WallThickness; // Default 0.15f
    public Color FloorColor;
    public Color[] WallColors;  // [North, South, East, West]
}
```

### 3. Modos de Operación
| Modo         | Descripción                                        |
|--------------|----------------------------------------------------|
| `Build`      | Construye habitación desde cero                    |
| `Resize`     | Redimensiona muros y suelo sin destruir nodos      |
| `Repaint`    | Actualiza solo materiales/colores                  |
| `Demolish`   | Elimina todos los nodos de la habitación           |

### 4. Jerarquía de Nodos Generados
```
Room (Node3D)
├── Floor (StaticBody3D)
│   ├── FloorMesh (MeshInstance3D)
│   └── FloorCollision (CollisionShape3D)
├── WallNorth (StaticBody3D)
│   ├── WallNorthMesh (MeshInstance3D)
│   └── WallNorthCollision (CollisionShape3D)
├── WallSouth (StaticBody3D)
├── WallEast (StaticBody3D)
└── WallWest (StaticBody3D)
```

## Dependencias Internas
- `RoomBuilder.cs` — Implementación de la lógica de construcción.
- `SkillGridSnapping.cs` — Alineación al grid durante resize.

## Versión
- v1.0.0 — Geometría paramétrica con materiales independientes por superficie.
