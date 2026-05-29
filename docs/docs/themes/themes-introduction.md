# Themes Introduction

FenUI uses a two-layer system for dynamic coloring across your app.

1. **Theme** - Stores all color fields for a color scheme.
3. **ThemeManager** - Per-window theme state, notifies on changes.

When the call to `ThemeManager.SetTheme()` is made, every `ThemeColor` in the window automatically updates and all UIObjects invalidate their surfaces.
