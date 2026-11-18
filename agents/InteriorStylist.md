# InteriorStylist — Especificación del Agente

## Rol y Responsabilidad
Agente experto en diseño de interiores que gestiona la coherencia estética de la habitación. Genera y aplica paletas de colores armónicas, estilos decorativos y combinaciones de materiales basados en la regla 60-30-10.

## Capacidades

### 1. Gestión de Paletas de Color
- Generación de paletas armónicas usando la regla **60-30-10**:
  - **60 %** — Color dominante (paredes y suelo).
  - **30 %** — Color secundario (muebles principales).
  - **10 %** — Color de acento (textiles, accesorios).
- Modos de armonía soportados:
  - Complementario
  - Análogo (± 30° en rueda cromática)
  - Triádico
  - Split-Complementario

### 2. Estilos Decorativos
| Estilo       | Paleta base       | Materiales típicos       |
|--------------|-------------------|--------------------------|
| Escandinavo  | Neutros fríos     | Madera clara, lino       |
| Industrial   | Grises y terracota | Metal oxidado, cemento  |
| Mediterráneo | Azules y blancos  | Cerámica, yeso           |
| Contemporáneo | Neutros cálidos  | Vidrio, acero, cuero     |
| Boho         | Tierra y mostaza  | Ratán, textiles étnicos  |

### 3. Aplicación en Runtime
- Serialización de la paleta a `Color[]` (Godot Color struct).
- Asignación a `StandardMaterial3D.AlbedoColor` de cada nodo.
- Previsualización en tiempo real mediante refresh de shader.

### 4. Salida
```json
{
  "palette": {
    "dominant": "#E8DCC8",
    "secondary": "#7A6652",
    "accent": "#3A7CA5"
  },
  "style": "contemporary",
  "material_assignments": {
    "floor": "dominant",
    "walls": "dominant",
    "sofa": "secondary",
    "cushions": "accent"
  }
}
```

## Dependencias Internas
- `SkillColorHarmony.cs` — Cálculo de rueda cromática y transformaciones HSL.

## Versión
- v1.0.0 — Paletas estáticas por estilo + motor de armonía cromática.
