# KnuxLib

A repository for me to push various bits of code for random file formats in random games I'm tinkering with, alongside basic command line tools for format conversion where applicable.

# Supported:

## CarZ Engine

Known games:

- Big Rigs: Over the Road Racing (PC)

Supported formats:

- [Material Library](KnuxLib/Engines/CarZ/MaterialLibrary.cs) reading, writing, MTL conversion and Assimp based importing.

- [SCO Model](KnuxLib/Engines/CarZ/SCO.cs) reading, writing, OBJ conversion and Assimp based importing.

Notes:

- Uncertain if this is the engine's name. Certain files refer to `r3d`, which might be the engine's actual shorthand name.

- The meaning of the SCO extension is unknown. Something Something Object?

- Definitely used by other Stellar Stone games, but I have yet to obtain and look at them myself.

## Gods Engine

Known games:

- Ninjabread Man (PC, PS2, Wii)

Supported formats:

- [WAD Archive](KnuxLib/Engines/Gods/WAD.cs) reading and data extraction.

Notes:

- Definitely used by other Data Design Interactive games, but I have yet to obtain and look at them myself.

## Project M Engine

Known games:

- Metroid Other M (Wii)

Supported formats:

- [Message Table](KnuxLib/Engines/ProjectM/MessageTable.cs) reading, writing, JSON seralisiation and JSON deseralisiation.

Notes:

- Uncertain if this is the engine's name.