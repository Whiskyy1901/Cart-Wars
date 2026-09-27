Cart Wars is a multiplayer supermarket simulation for up to 4 players, built in Unity using Mirror networking. Each player runs their own store — buying stock, restocking shelves, and serving customers — but it's not just about who sells the most. You can sabotage your friends' stores, so staying profitable means playing offense and defense at once.

Status: Actively in development.

## Implemented Systems

1. Active ragdolls for NPCs/customers
2. Networked interaction system (buy menu, dialogue menu)
3. Grappling hook — traverse the environment and pull customers toward your shop
4. Shotgun — fight with other players

## Planned Features

1. Restocking & selling loop (crate → shelf → register)
2. Per player economy (money, purchasing stock, earning from sales)
3. Customer AI to shop and pay
4. Sabotages (stealing stock, cutting power supply, poaching customers, etc.)
5. Separate store spaces per player
6. Simple host/join lobby flow
7. Win condition

## How to Run

1. Download the latest version from the [Cart Wars Itch.io page](https://whiskyyyy.itch.io/cart-wars)
2. Extract the downloaded zip file
3. Run 2 instances of `CartWars.exe`
4. On one instance, select Host(server + client) — this creates a local server and player
5. On the other instance, select Client — this connects to the host

Note: Cross-device connections aren't supported yet. To test multiplayer features, run two instances on the same machine.

## Controls

Move -> WASD
Look -> Mouse
Interact -> Hold E
Pick up -> F
Grapple -> Hold Left Click
Shoot -> Right Click

## Known Limitations

No cross-device networking yet, testing is local-instance only
Some features may not work as intended; this is an active WIP
