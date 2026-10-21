# First Playable Prototype Specification

## Goal
Deliver the smallest practical WarForge build that lets a player control a tank and shoot a target on Android.

## Platform
- Android phone
- Landscape orientation
- Unity 6
- Universal Render Pipeline

## Required gameplay
1. The player can move the tank forward and backward.
2. The player can turn the tank left and right.
3. The camera follows the tank and keeps the action visible.
4. The turret can rotate independently for aiming.
5. The player can fire a projectile.
6. A target reacts when hit.
7. The player can restart the test.

## Controls
Use on-screen touch controls suitable for a phone. Keep movement and aiming separate where practical.

## Initial environment
Use a small, simple arena with basic placeholder materials. Do not delay the prototype for detailed graphics, complex terrain, or purchased assets.

## Acceptance criteria
- The Unity project opens without missing scripts or required assets.
- The tank responds to movement and turning input.
- The camera follows the tank correctly.
- The turret aims independently.
- Firing produces a projectile that can hit the target.
- The target visibly reacts to a hit.
- The player can restart the test.
- An Android build is installed and tested on a real device, or the inability to build/test is documented.

## Out of scope
- Online multiplayer
- Aircraft and ships
- Large open worlds
- Advanced ballistics
- Real-money purchases
- Final-quality art and audio
