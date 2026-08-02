# Map Border Generated Tile Atlas

- Asset: `MapBorder_TileAtlas.png`
- Grid: 10 columns × 10 rows; every cell is 16×16 pixels.
- Unity import: point filtering, no mipmaps, 16 pixels per unit.
- Atlas rows are listed top-to-bottom below. Sprite names are also stored in the Unity `.meta` sprite sheet for deterministic lookup.
- Transition rows blend left-to-right and can be rotated by future placement code.

| Row | Sprite names | Contents |
|---:|---|---|
| 0 | `sea_base_00..09` | sea base variants |
| 1 | `sea_transition_to_desert_00..09` | left-to-right sea → desert transition variants |
| 2 | `desert_base_00..09` | desert base variants |
| 3 | `desert_transition_to_tundra_00..09` | left-to-right desert → tundra transition variants |
| 4 | `tundra_base_00..09` | tundra base variants |
| 5 | `tundra_transition_to_steppe_00..09` | left-to-right tundra → steppe transition variants |
| 6 | `steppe_base_00..09` | steppe base variants |
| 7 | `steppe_transition_to_rocky_00..09` | left-to-right steppe → rocky transition variants |
| 8 | `rocky_base_00..09` | rocky base variants |
| 9 | `rocky_transition_to_sea_00..09` | left-to-right rocky → sea transition variants |
