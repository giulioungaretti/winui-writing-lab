# PRD: Undo/Redo

## Overview
Implement undo and redo functionality to allow users to reverse and reapply actions, providing a safety net for experimentation and mistake correction.

## Goals
1. Undo the last action (stroke, erase, clear)
2. Redo previously undone actions
3. Support keyboard shortcuts (Ctrl+Z, Ctrl+Shift+Z)
4. Visual feedback for undo/redo availability
5. Reasonable history limit for memory management

## User Stories

### US1: Undo Action
As a user, I want to undo my last action to correct mistakes quickly.

**Acceptance Criteria:**
- Undo button in toolbar
- Ctrl+Z / Cmd+Z keyboard shortcut
- Undo last stroke
- Undo erase action
- Undo clear canvas
- Button disabled when nothing to undo

### US2: Redo Action
As a user, I want to redo an undone action if I change my mind.

**Acceptance Criteria:**
- Redo button in toolbar
- Ctrl+Shift+Z / Cmd+Shift+Z shortcut
- Redo previously undone action
- Redo stack clears on new action
- Button disabled when nothing to redo

### US3: History Limit
As a user, I expect undo to work reliably without impacting performance.

**Acceptance Criteria:**
- Reasonable history limit (e.g., 50-100 actions)
- Oldest actions dropped when limit reached
- Clear history on page/notebook change (optional)

## Technical Approach

### Architecture Decision
Atrament draws directly to canvas bitmap - it doesn't maintain a vector model. For undo/redo, we need to:

**Option A: Stroke Recording (Recommended)**
- Enable `recordStrokes` in Atrament
- Store strokes in an array
- Undo: remove last stroke, redraw all remaining
- Pro: Accurate reproduction
- Con: Redraw cost on undo

**Option B: Canvas Snapshots**
- Save canvas ImageData before each action
- Undo: restore previous snapshot
- Pro: Simple implementation
- Con: High memory usage

**Chosen Approach: Stroke Recording**
Since Atrament supports stroke recording and programmatic replay, this is the more memory-efficient and accurate approach.

### History Stack Model
```typescript
interface HistoryState {
  strokes: Stroke[];      // All strokes up to this point
  action: ActionType;     // What changed
}

interface HistoryManager {
  past: HistoryState[];   // Undo stack
  present: HistoryState;  // Current state
  future: HistoryState[]; // Redo stack
  
  push(state: HistoryState): void;
  undo(): HistoryState | null;
  redo(): HistoryState | null;
  canUndo(): boolean;
  canRedo(): boolean;
  clear(): void;
}
```

### Action Types
```typescript
type ActionType = 
  | 'stroke'      // Drew a stroke
  | 'erase'       // Erased content
  | 'clear'       // Cleared canvas
  | 'paste'       // Pasted content
  | 'transform';  // Moved/scaled selection
```

### Redraw Strategy
```typescript
function redrawCanvas(strokes: Stroke[]) {
  atrament.clear();
  for (const stroke of strokes) {
    atrament.mode = stroke.mode;
    atrament.weight = stroke.weight;
    atrament.color = stroke.color;
    atrament.smoothing = stroke.smoothing;
    atrament.adaptiveStroke = stroke.adaptiveStroke;
    
    const segments = [...stroke.segments];
    const first = segments.shift()!;
    atrament.beginStroke(first.point.x, first.point.y);
    
    let prev = first.point;
    for (const seg of segments) {
      const result = atrament.draw(
        seg.point.x, seg.point.y,
        prev.x, prev.y,
        seg.pressure
      );
      prev = result;
    }
    
    atrament.endStroke(prev.x, prev.y);
  }
}
```

## Performance Considerations

### Optimizations
- **Incremental Drawing**: For redo, only draw the new stroke
- **Debounced History**: Don't create entry for every segment
- **Background Redraw**: Use offscreen canvas for undo prep
- **History Compression**: Merge rapid sequential actions

### Memory Management
- Limit history to 50-100 entries
- Clear redo stack on new action
- Option to disable for memory-constrained devices

## UI Components

### Toolbar Buttons
- Undo button (arrow pointing left/back)
- Redo button (arrow pointing right/forward)
- Disabled state styling
- Tooltip with shortcut hint

### Visual Feedback
- Brief flash/highlight on undo/redo
- Toast notification (optional)

## Implementation Checklist

### Core
- [ ] Create HistoryManager class/hook
- [ ] Implement push/undo/redo methods
- [ ] Set up history limit

### Stroke Tracking
- [ ] Enable stroke recording in Atrament
- [ ] Capture strokes on strokerecorded event
- [ ] Store stroke data in history

### Redraw
- [ ] Implement redrawCanvas function
- [ ] Handle different stroke modes
- [ ] Optimize for performance

### Undo/Redo Logic
- [ ] Implement undo action
- [ ] Implement redo action
- [ ] Clear redo stack on new stroke
- [ ] Handle edge cases (empty history)

### UI
- [ ] Add undo/redo buttons to toolbar
- [ ] Style disabled states
- [ ] Add keyboard shortcuts
- [ ] Add tooltips

### Integration
- [ ] Connect to Sketchpad component
- [ ] Test with all drawing modes
- [ ] Test with eraser
- [ ] Test with clear

## Dependencies
- Requires MVP Sketchpad
- Benefits from Drawing Tools (eraser mode)

## Future Considerations
- History visualization (timeline view)
- Named checkpoints/save points
- Branching history (like git)
- Collaborative undo (per-user history)
- Selective undo (undo specific stroke)
