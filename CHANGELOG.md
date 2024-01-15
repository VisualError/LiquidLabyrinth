# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]
- Liquid API (For Developers)
- Custom Dungeon additions.
- Tools for liquid identification
- Circulation system
- Lore

## Version [0.0.9]
### Fixed
- Fixed bug where players couldn't shake bottles if a player is already shaking one.
- Fixed bug where bottle sell value would be zero for every moon.
- Fixed bottle `rarity` config not working.
- Fixed bug where bottles would sometimes phase out of existence (probably)

### Changed
- Bottles now break if they collide with other objects at high speed (still needs fine tweaking)
- Bottles value now range from `5` to `100` credits.

## Version [0.0.8]
### Added
- Heads now rotate to players if it hears any speak.
- Build badges in readme.md

### Fixed
- Item popup notification.
- Placing items in the cupboard fixed for v47

### Removed
- Set as shop items config.

## Version [0.0.7]
### Fixed
- Fixed RPC Error from head item

### Changed
- Changed liquid material.
- Head texture should now have a bottom texture.

### Added
- Added shake to make bottle liquid glow
- Liquids should now float and glow when listening to boomboxes

## Version [0.0.6]
### Fixed
- Position syncing issues

### Changed
- Lowered Spawn rates

## Version [0.0.5]
### Fixed
- Somewhat fixed enemy pickups
- Saves (again)
- Placing items
- Shop config not working
- Selling the head item now gives you at least two credits

### Added
- Enemies can now detect sounds made by the bottle `be careful~
- Probaby added more bugs.

## Version [0.0.4]
### Added
- Added Spawn random enemy on revive functionality on config.

## Version [0.0.3]

### Fixed
- Latest LethalSettings version should now work with the mod.

## Version [0.0.2]

### Added

- Added randomly generated names
- Added head item.. :)
- Fixed saves (Will randomize saves on older versions, but that doesn't matter)
- More settings to mess around with.

## Version [0.0.1]

### Added

- `Bottles`: Throwing, Drinking
- Revive with bottle break (configurable)
- Ingame config using LethalSettings

