# PRD: Infinite Canvas

## Overview
Extend the sketchpad with an infinite canvas that allows users to pan and zoom, providing unlimited drawing space without the constraints of a fixed viewport.

## Goals
1. Enable panning in all directions with touch/mouse drag
2. Implement zoom in/out with pinch gestures and scroll wheel
3. Maintain drawing accuracy across different zoom levels
4. Preserve stroke quality when zoomed in
5. Optimize rendering for smooth performance

## User Stories

### US1: Pan Canvas
As a user, I want to drag the canvas to view different areas of my drawing so that I can work on a larger piece than the screen allows.

**Acceptance Criteria:**
- Two-finger drag on touch devices pans the canvas
- Middle-mouse button drag pans the canvas
- Space + drag pans the canvas
- Pan has inertia/momentum for natural feel
- Mini-map shows current viewport position (optional)

### US2: Zoom Canvas
As a user, I want to zoom in for detail work and zoom out to see the whole picture.

**Acceptance Criteria:**
- Pinch-to-zoom on touch devices
- Scroll wheel zooms (centered on cursor position)
- Keyboard shortcuts: Ctrl+Plus/Minus
- Zoom range: 10% to 1000%
- Zoom indicator shows current zoom level
- Fit-to-content button

### US3: Draw While Zoomed
As a user, I want to draw at any zoom level with consistent stroke quality.

**Acceptance Criteria:**
- Stroke weight appears consistent at all zoom levels
- Strokes are stored in world coordinates
- Drawing feels responsive regardless of zoom

## Technical Approach

### Coordinate System
- **World Coordinates**: Absolute position of strokes
- **Screen Coordinates**: Viewport-relative positions
- **Transform Matrix**: Handle pan/zoom transformations

### Rendering Strategy
- Store strokes in world coordinates
- Apply viewport transform during rendering
- Consider tiled rendering for very large canvases
- Implement frustum culling (only render visible strokes)

### State Management
```typescript
interface CanvasViewport {
  pan: { x: number; y: number };  // World offset
  zoom: number;                    // Scale factor (1.0 = 100%)
}
```

### Component Changes
- New `useViewport` hook for pan/zoom state
- Transform layer between pointer events and Atrament
- Gesture handling for touch interactions

## Implementation Checklist

### Core
- [ ] Create useViewport hook
- [ ] Implement coordinate transformation utilities
- [ ] Add viewport transform to canvas rendering

### Pan
- [ ] Implement space+drag panning
- [ ] Implement middle-mouse panning
- [ ] Implement two-finger touch panning
- [ ] Add pan momentum/inertia

### Zoom
- [ ] Implement scroll wheel zoom
- [ ] Implement pinch-to-zoom
- [ ] Add keyboard zoom shortcuts
- [ ] Create zoom indicator UI
- [ ] Add fit-to-content functionality

### Integration
- [ ] Update stroke recording to use world coordinates
- [ ] Ensure drawing works at all zoom levels
- [ ] Performance optimization for large canvases
- [ ] Add viewport bounds limits (optional)

## Dependencies
- Requires MVP Sketchpad to be complete

## Future Considerations
- Layer system for organizing content
- Collaborative real-time editing
- Export at different resolutions
