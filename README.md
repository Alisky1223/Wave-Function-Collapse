# Wave-Function-Collapse
# WFC Map Generator  
This repository contains a Unity C# script (`WFC.cs`) for generating game maps using the Wave Function Collapse (WFC) algorithm. 
The script allows for flexible map generation by defining various map styles and grid operations, making it suitable for procedural generation in games.  
## Features  
- **Direction Enumeration**: Enum listing possible directions for map tiles.
- **MapTiles Class**: Serializable class representing individual map tiles with properties like coordinates, status, and path eligibility.
- **Grid Operations Interface**: Methods for node and coordinate management, neighbor retrieval, and map tile setting.
- **Gizmo Drawer Interface**: Interface for drawing gizmos in the Unity editor.
- **Map Style Interface**: Interface for applying different map styles.
- **Map Style Implementations**: Several map style classes (`ClassicMapStyle`, `OneEnterMapStyle`, `WorldAMapStyle`, etc.) for diverse map layouts.
- **WFC Class**: Core class managing the WFC process, including initialization, side setups, and map collapse.
- **Map Style Factory**: Factory pattern for selecting and applying different map styles.
## How to Use  
1. **Setup**: Attach the `WFC` script to a GameObject in your Unity scene.
2. **Configure Map Tiles**: Define your map tiles and set their properties in the Unity Inspector.
3. **Select Map Style**: Choose a map style using the `MapStyle` enumeration.
4. **Run the Game**: The script will automatically generate a map based on the selected style and the WFC algorithm during the game start.
## Contributing  Feel free to fork this repository and submit pull requests. For major changes, please open an issue first to discuss what you would like to change.  
## License  This project is licensed under the GPL3 License - see the LICENSE.md file for details.
