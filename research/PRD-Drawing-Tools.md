# PRD: Drawing Tools

## Overview
Expand the sketchpad with multiple drawing tools including eraser, color selection, and different brush types (pen, pencil, highlighter). This creates a more complete drawing experience.

## Goals
1. Implement eraser tool for removing strokes
2. Add color picker with preset and custom colors
3. Create different brush types with unique characteristics
4. Provide quick tool switching via toolbar and shortcuts
5. Remember tool settings between sessions

## User Stories

### US1: Eraser Tool
As a user, I want to erase parts of my drawing to correct mistakes.

**Acceptance Criteria:**
- Switch to eraser mode
- Erase where I draw over existing strokes
- Adjustable eraser size
- Visual indicator of eraser cursor
- Quick toggle with keyboard shortcut (E)

### US2: Color Selection
As a user, I want to choose different colors for my strokes.

**Acceptance Criteria:**
- Preset color palette (8-12 colors)
- Custom color picker
- Recently used colors
- Color indicator on toolbar
- Opacity/alpha support

### US3: Pen Tool
As a user, I want a pen tool that creates clean, consistent strokes.

**Acceptance Criteria:**
- Smooth, solid strokes
- Pressure affects width
- Sharp, defined edges
- Default drawing tool

### US4: Pencil Tool
As a user, I want a pencil tool that feels like sketching on paper.

**Acceptance Criteria:**
- Slightly textured appearance
- Lower opacity than pen
- Pressure affects width and opacity
- Good for rough sketching

### US5: Highlighter Tool
As a user, I want a highlighter tool for emphasis and annotations.

**Acceptance Criteria:**
- Semi-transparent strokes
- Wide, flat brush shape
- Limited color palette (yellow, pink, green, blue)
- Strokes blend/overlay existing content

## Tool Specifications

### Pen
```typescript
{
  type: 'pen',
  weight: 3,
  opacity: 1.0,
  smoothing: 0.85,
  adaptiveStroke: true,
  pressureEffect: 'width'
}
```

### Pencil
```typescript
{
  type: 'pencil',
  weight: 2,
  opacity: 0.8,
  smoothing: 0.5,
  adaptiveStroke: true,
  pressureEffect: 'width+opacity',
  texture: true
}
```

### Highlighter
```typescript
{
  type: 'highlighter',
  weight: 20,
  opacity: 0.4,
  smoothing: 0.9,
  adaptiveStroke: false,
  pressureEffect: 'none',
  blendMode: 'multiply'
}
```

### Eraser
```typescript
{
  type: 'eraser',
  weight: 20,
  mode: 'erase'  // Uses Atrament's MODE_ERASE
}
```

## Color Palette

### Default Colors
```
Black    #000000
White    #FFFFFF
Red      #EF4444
Orange   #F97316
Yellow   #EAB308
Green    #22C55E
Blue     #3B82F6
Purple   #A855F7
Pink     #EC4899
Brown    #92400E
Gray     #6B7280
```

### Highlighter Colors
```
Yellow   #FEF08A (40% opacity)
Pink     #FBCFE8 (40% opacity)
Green    #BBF7D0 (40% opacity)
Blue     #BFDBFE (40% opacity)
Orange   #FED7AA (40% opacity)
```

## Technical Approach

### Tool State Management
```typescript
interface ToolState {
  activeTool: ToolType;
  color: string;
  tools: {
    pen: PenSettings;
    pencil: PencilSettings;
    highlighter: HighlighterSettings;
    eraser: EraserSettings;
  };
}
```

### Atrament Integration
- Use `MODE_DRAW` for pen, pencil, highlighter
- Use `MODE_ERASE` for eraser
- Adjust Atrament properties per tool

### Pencil Texture (Optional)
- Use pattern fill or noise overlay
- Consider WebGL for performance
- Fallback to simpler rendering

## UI Components

### Toolbar
- Tool icons with active state
- Color swatch/picker
- Size slider
- Settings popover per tool

### Keyboard Shortcuts
- `P` or `1` - Pen
- `L` or `2` - Pencil
- `H` or `3` - Highlighter
- `E` or `4` - Eraser
- `[` / `]` - Decrease/Increase size
- `C` - Color picker

## Implementation Checklist

### Tool System
- [ ] Define tool types and settings interfaces
- [ ] Create useTools hook
- [ ] Implement tool switching logic
- [ ] Apply tool settings to Atrament

### Eraser
- [ ] Implement eraser mode toggle
- [ ] Add eraser size control
- [ ] Create eraser cursor indicator

### Colors
- [ ] Create color palette component
- [ ] Implement color picker
- [ ] Add recent colors tracking
- [ ] Create highlighter color presets

### Brush Types
- [ ] Configure pen settings
- [ ] Configure pencil settings
- [ ] Configure highlighter settings
- [ ] Implement pencil texture (stretch)

### UI
- [ ] Create Toolbar component
- [ ] Add tool buttons with icons
- [ ] Add size slider
- [ ] Add color selector
- [ ] Create tool settings popovers

### Shortcuts
- [ ] Implement keyboard shortcuts
- [ ] Add shortcut hints in UI

### Persistence
- [ ] Save active tool preference
- [ ] Save tool settings
- [ ] Save recent colors

## Dependencies
- Requires MVP Sketchpad

## Future Considerations
- Custom brush creation
- Brush presets/favorites
- Import brushes
- Calligraphy brush
- Watercolor effect
- Spray paint tool
