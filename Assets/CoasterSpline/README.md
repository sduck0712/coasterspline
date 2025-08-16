# Coaster Spline - README

Welcome to the **CoasterSpline**! This tool allows you to design and customize rollercoasters in Unity with an intuitive spline-based editor, customizable supports, and a built-in physics system. Follow the guide below to get started and explore all the features.

## Features
- **Customizable Splines and Supports**: Generate tracks and support structures from your own models or provided assets.
- **Advanced Physics and Control**: Program block sections, speed, and orientation for a realistic coaster experience.
- **Interactive Editor Controls**: Easily select, align, and manipulate multiple anchors to customize your coaster layout.

## Usage Guide

### Basic Controls
- **Multi-Select Anchors**: Hold `Shift` and click to select multiple anchors.
- **Align Anchors**: Press `Ctrl+J` to set the selected anchors to the same position and orientation. Useful for connecting chains.
- **Move Multiple Anchors**: Selected anchors can be moved together, making track adjustments more efficient.
- **Delete Anchors**: you can delete selected anchors by pressing delete.
- **Insert Anchor**: you can insert an anchor by clicking the red point.

### Track Setup
1. **Loops**: To create a full loop, you need at least **two chains**, as a single chain cannot connect to itself.
2. **Chain Length Limit**: Each chain has a maximum length due to vertex count restrictions. If your track exceeds this, break it into multiple chains.
3. **Snapping Objects**: Use the **Coaster Snapper** to snap any object to the closest point on the track.
4. **Verical tracks**: due to how 3d rotations work the spline might bugg out when going vertical. We solved this in this asset by changing the up directions where anchors are vertical. It is important to place an anchor at those points.

### Provided Assets
- Example assets, including a **Boomerang coaster** layout, are included in the package. You can use these for quick setups or as inspiration for creating your own assets.

## Troubleshooting & Support
For feature requests, bug reports, or any other inquiries, please contact us at **mrdeegames@gmail.com**.

Happy coaster building!
