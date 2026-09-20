# Hearthwait

Valheim tells you that the mead is fermenting. It does not tell you for how much
longer. Hearthwait adds that one missing line to the hover text you already read,
so you can glance at the homestead and know what is worth waiting for.

No panel, no HUD, no combat readouts. Just the clock the game was already keeping.

**By blackhearthx.** Client only — nothing to install on the server, and players
without it see the game exactly as before.

## What it reads

- **Mead fermenters.** Time to ready, and a warning when rain or a missing roof
  has paused the batch instead of a countdown that would never move.
- **Kilns, smelters, blast furnaces, eitr refineries, spinning wheels, windmills.**
  Time for the whole queue, time for the next bar, bars already waiting to be
  collected, and a plain "needs fuel" or "waiting on the wind" when that is the
  real answer.
- **Cooking stations and ovens.** What is done, what is still cooking, and how
  long before the first piece burns.
- **Beehives and sap collectors.** Time to the next unit and time until full.
- **Crops and saplings.** Time to grown — or "won't grow like this" when the
  plant is in the wrong biome, on uncultivated ground, under a roof or boxed in,
  because those never finish.
- **Bushes, berries and mushrooms.** What you will get when it is ripe, and the
  countdown while it grows back.
- **Fires, torches and braziers.** How long the fuel will last.
- **Eggs.** Time to hatch, or why it is not hatching.
- **Young animals.** Time until they grow up.
- **Taming and feeding.** Time left to tame, and how long until they are hungry
  again.
- **Pregnancy.** Time until the offspring is born, and love points before that.

Colors follow the state: warm amber while you wait, soft green when it is ready,
rose when something is about to burn, gray-blue when the wait is paused.

## About the clock

Every timer is real time, not game time. Valheim's world clock runs at one real
second per second, so a berry bush that says five hours means five hours — it
just only counts while the world is running. Sleeping fast-forwards it, and you
do not need to stand nearby for the time to pass.

## Install

Use Gale, r2modman or the Thunderstore Mod Manager and let it pull BepInEx and
Jötunn with it.

Manually: drop `Hearthwait.dll` and the `Translations` folder into
`BepInEx/plugins`.

## Language

Follows the game language. English and Portuguese ship with the mod. To add your
own, copy `Translations/English/hearthwait.json` into a folder named after your
language and translate the values.

## Config

Written to `BepInEx/config/com.blackhearthx.hearthwait.cfg` on first run. Every
category of timer has its own switch, so you can keep the mead and turn off the
rest.
