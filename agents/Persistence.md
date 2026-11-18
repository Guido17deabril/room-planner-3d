# Persistence — Especificación del Agente

## Rol y Responsabilidad
Agente de persistencia encargado de guardar y cargar el estado completo de la habitación en formato JSON. Gestiona el ciclo de vida de los archivos de proyecto del usuario.

## Capacidades

### 1. Serialización
- Convierte el estado del SceneTree al modelo de datos `RoomLayoutData`.
- Serializa a JSON usando `System.Text.Json` con indentado legible.
- Calcula un hash SHA-256 del contenido para integridad de archivo.

### 2. Deserialización
- Lee un archivo `.roomplan` (JSON renombrado) desde el disco.
- Valida estructura mínima antes de reconstruir la escena.
- Emite señal `layout_loaded` con el `RoomLayoutData` deserializado.

### 3. Estructura del Modelo de Datos
```csharp
public class RoomLayoutData {
    public string Version { get; set; }       // "1.0"
    public string Name { get; set; }
    public RoomDimensions Room { get; set; }
    public ColorPalette Palette { get; set; }
    public List<FurnitureItem> Furniture { get; set; }
    public List<AccessPoint> AccessPoints { get; set; }
    public DateTime LastModified { get; set; }
    public string Checksum { get; set; }
}
```

### 4. Rutas de Persistencia
| Tipo       | Ruta                                      |
|------------|-------------------------------------------|
| Autosave   | `user://autosave.roomplan`                |
| Proyectos  | `user://projects/<nombre>.roomplan`       |
| Exportación JSON | `user://exports/<nombre>_export.json` |

### 5. Señales Godot
```
layout_saved(string path)
layout_loaded(RoomLayoutData data)
save_failed(string error)
load_failed(string error)
```

## Dependencias Internas
- `SkillJsonParser.cs` — Motor de serialización/deserialización.

## Versión
- v1.0.0 — Persistencia básica local con validación de integridad.
