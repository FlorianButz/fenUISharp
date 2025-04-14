# Themes Introduction

Themes are a great way to dynamically change the colors of your app without having to change them individually across all your components.
FenUISharp uses a combination of three layers for the dynamic coloring of your app to work.

1. The `Theme` class. It stores all the color fields
2. The `ThemeColor` class. It dynamically provides the wanted color of the current theme.
3. The `ThemeManager` class. It provides the window with the current theme and notifies when it changes.