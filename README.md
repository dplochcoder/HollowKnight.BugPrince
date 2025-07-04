# Bug Prince

Bug Prince is a [Randomizer 4](https://github.com/homothetyhk/RandomizerMod) connection mod that integrates mechanics from [Blue Prince](https://store.steampowered.com/app/1569580/Blue_Prince/) into Hollow Knight transition randomizer.

This mod has many options and includes significant content, please read thoroughly before diving in.

## Transition Choices

Enabling transition choices means that whenever the player enters an unexplored, randomized transition, instead of loading a new scene the player is instead presented with several choices of scenes to enter into, like entering a new room in Blue Prince. Like the aforementioned, once entered, that transition is permanently fixed and cannot be changed.

All choices offered are _safe_, and so unlike in Blue Prince, you cannot hard lock your randomizer run by choosing your way into a dead end, or denying yourself critical progression. Any choice you are offered is guaranteed to lead to a logically completable seed.

### Randomization Modes

Bug Prince supports all types of transition rando, including room rando, map area rando, full area, as well as connection-provided modes like [Door Rando](https://github.com/flibber-hk/HollowKnight.TrandoPlus), or [MoreDoors TRando](https://github.com/dplochcoder/HollowKnight.MoreDoors?tab=readme-ov-file#rando-settings).

Since the number of randomized transitions varies considerably with settings, you should ensure to set appropriate numbers of collectibles and tolerances for those settings. The installed defaults are intended for room rando.

The matching and coupled settings are also respected. In Coupled mode, selecting one direction of a transition pair also locks in the opposite pair. Only matching directions (left<->right, top<->bot) are offered if matching is requested.

If connected areas are specified, you will be very likely to see at least 1 connected area scene whenever making a choice, but it is of course not guaranteed.

### ItemSync Integration

Bug Prince can be played with ItemSync, allowing multiple players to share choices within the same world. Since different choices made at the same time could cause _conflicts_, Bug Prince uses a 'host' model to resolve them.

Whoever is the first player to join the ItemSync room (first in the names list) is permanently deemed the host for the save; whomever has the strongest internet connection should likely be the host. The host will always be able to choose transitions, but other players will be unable to if they lose connection to the ItemSync server _or_ if the host loses connection to the ItemSync server.

#### Resource Sharing

Coins and Gems are _shared_ resources in an item sync save. If any player makes a transition purchase that costs resources, all players lose those resources.

Push Pins are also a _shared_ resource, but the pinned scenes are not. This means that two players can have two different scenes pinned at the same time, but it costs 2 pins to do so.

Dice Totems are the exception, and are an _instanced_ resource like Simple Keys are traditionally. Any player who obtains a dice totem increases everyone's total, but when used, only the spending player loses a totem.

#### Race Conditions

Whether you are host or not, you may encounter a situation where you enter a new transition, but another player makes a transition selection before you do. BugPrince will do its best to preserve your decision transparently, but this is not always possible.

-   You might both have entered the same transition and made different choices; in this case, the first choice wins.
-   Your choice might no longer be available because your partner used it elsewhere.
-   Your choice may have become logically invalid due to a choice made by your partner.
-   Your choice may have become financially invalid due to your partner spending coins/gems first.

If any of these apply, you'll either be spat out anyway (because your partner made a choice for your transition first), or you'll hear three 'tinks' indicating the conflict, followed by a new selection UI.

### Choices and Refresh Cycle

For different experiences, you can set the number of offered rooms anywhere between 2 and 5. The default is 3 and should be good for most modes.

Every time you _reject_ an offered scene by choosing a different one, the refresh cycle kicks in. You will not see the rejected scene offered again until you have visited N more transitions, unless logic gives no choice. Lower refresh cycles mean more repeats on average.

### Dice Totems and Push Pins

Dice Totems and Push Pins are new items in the Relics pool that are _consumeables_, able to be used (once) to affect room selection RNG. You can choose to start the run with some and you can control how many exist in the world.

A dice totem allows you to _reroll_ your room choices at a specific transition, which you are otherwise not allowed to do. When rerolling, the engine will do its best to avoid repeating any choices offered in the first roll, but this is not guaranteed.

A push pin allows you to _reserve_ a room choice for later use. At all future transitions, if the pinned room choice is available, it will be offered. Only one room choice may be pinned at a time, and the only way to free your pinned slot once in use is to choose that room at an open transition. You can pin a new room at the same time as freeing your pre-existing pin, but you can never finish a room selection with more than one room pinned.

### Coins and Gems

Coins and Gems are new items within the Keys pool, that can be used to purchase specific transitions of value. If the 'Enable Coins and Gems' setting is disabled, no rooms will have costs, and no coins nor gems will be placed.

If a room with a cost is offered and you do not have the requisite coins/gems to pay for it, then you cannot select that room and will either need to pin it until you have the necessary payment, or wait for it to roll again. Bug Prince will never give you more than one choice with a cost unless it has to.

It takes approximately 12 coins and 12 gems to purchase access to every room in room rando. In map area rando, it takes about 6 gems and no coins, and in full area rando a few extra gems and a few coins. Some rooms have costs added, or increased, if certain connections are enabled. For example, Spirit's Glade costs 2 gems to access if Ghost Essence is randomized.

Because the transitions and the total costs among them vary considerably over different settings, the total number of coins and gems is also variable to compensate, so long as keys are randomized. If you try to combine certain transition settings with the Keys pool staying vanilla, seed generation may fail infinitely.

#### Tolerance and Duplicates

Tolerance gems/coins and duplicate gems/coins both add extra gems/coins to the item pool, with the difference that tolerance gems/coins are always _in-logic_ before purchases must be made, where as duplicates are _random_ and could be placed in useless locations inaccessible without all the preceding gems/coins.

Unlike simple keys in base rando, Bug Prince does _not_ require that you have access to every collectible before you are required to spend some of them, so you can expect to make purchases steadily throughout your run. The choice engine ensures your seed remains completable no matter which offered purchases you take, in what order.

## Locations

To offset the weight of the many new items that Bug Prince adds to the game, Bug Prince also adds many new locations of different varieties. These are separated into several unique categories.

### Basic Locations

The majority of the locations; these are abandoned corners and other otherwise mundane spots within Hallownest that needed something to fill them.

### Advanced Locations

New locations added to the map, obscure and difficult to access or _impossible_ to access without map modifications. Finding these can be difficult without guidance.

### Shaman Puzzles

Unique puzzles added to each of the three Shaman Mounds. If stumped, check the SPOILER directory for solutions.

### Map Shop

An extension of Iselda's shop which adds a map cost to items in the shop, similar to the way Salubra's shop items sometimes have charm costs.

### The Vault

A new room located in right-side City of Tears, accessible through a door below Pleasure House. It contains 5 chests, each with a cost to access, and each accessible via different movement requirements.

### Gemstone Cavern

A new room located in upper-right Crystal Peaks, accessible through a breakable wall above the entrance to the Crystal Heart room. It contains a single large boulder that contains many embedded items.

Items are embedded at different depths within the boulder, each of which require a minimum nail quality to mine. The final tier requires Nail 3, or all but the last nail upgrade. Damage-increasing charms like Fragile Strength and Fury of the Fallen increase the speed at which the boulder can be mined, but do not grant access to more items.

TODO: Provide a mechanism to mine the boulder with a loaner nail, to avoid permanent nail upgrades.

#### Nail Upgrade Logic

Logic for nail upgrades is compatible both with base rando and with RandoPlus's randomized nail upgrades. In both cases, Nail 2 + Nail 3 are never in logic until you have some form of vertical.

There is a built-in tolerance of 1 randomized nail upgrade (for RandoPlus), or 1 Pale Ore for base rando. That is, Nail 3 is not required until you have logical access to all 4 randomized nail upgrades, or 4 pale ore in base rando.

## Edge Cases

This mod introduces a lot of complexity to Hollow Knight randomizer and in some sense strains its capabilities. Some issues and shortcomings may be encountered.

### MultiWorld

MultiWorld will likely never be supported due to its architecture. Because BugPrince does not, and cannot know the logical structure of _other_ players' worlds, it must ensure all remote items never move into higher progression spheres, which severely limits options.

### Map Mod Lag

[Rando Map Mod](<https://github.com/syyePhenomenol/RandoMapMod>) has an advanced search algorithm to help you navigate the twisted labyrinth that is transition randomizer. Unfortunately, it largely assumes that the map does not change, an assumption that BugPrince violates.

You may notice some game lag when opening the map; this is caused by BugPrince "rebuilding" Rando Map Mod's search indices so it becomes aware of all the transition choices you have made. You can disable this feature to reduce lag at the expense of breaking pathfinder.

### Empty Selections

You may at times encounter a selection menu with _no_ options, or at least, no _affordable_ options, in which case the screen will fade to red after a few seconds and boot you back to your last bench.

This is a consequence of the safety promise, and generally means that the only _safe_ option for that transition is one you can't purchase, either because (a) you haven't acquired the logically accessible coins/gems to afford it, or (b) you must purchase a different transition first to avoid hard-locking the save.

## Credits

Custom art assets done by by [Kadala](https://ko-fi.com/kadala_ka)!
