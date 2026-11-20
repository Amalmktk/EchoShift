# Writing levels

Every level is one text file in `Assets/Resources/Levels/`, named after its id, for example `w1_l05.txt`. The game loads all of them at start-up and sorts them by `number`. Adding a monthly update means adding files here. No code changes are needed.

## Checking levels

Run this from the repository root on any machine with Node 18 or later:

```
node tools/levelcheck.mjs            # every level
node tools/levelcheck.mjs w1_l05     # one level
node tools/levelcheck.mjs --show w1_l05   # print the board after every turn
```

It checks that each file parses, that the saved solution wins with three stars, that the move and echo limits hold, and that a level marked `needs_echo: true` really can't be won without an echo. The same checks run in C# on GitHub for every push, from `tests/`.

## File format

```
id: w1_l05
name: Two Actions
world: 1
number: 5
echoes: 1          # echo limit for the level
par: 1             # echoes allowed for the third star
moves: 14          # moves allowed in each run
timer: 4           # turns a timed switch stays on (default 4)
needs_echo: true   # the checker proves it can't be won without an echo
boss: false
exit: a            # optional: channels that must all be on to open the exit
hint1: A nudge.
hint2: A direction.
hint3: The full solution in words.
map:
#######
#S.a..#
#..A.X#
#######
solution: RR | RRDRR
```

Lines starting with `//` are comments. The `solution` holds one move list per run, separated by `|`. Every run except the last becomes an echo. Moves are `U`, `D`, `L`, `R` and `W` for wait. The solution must collect the crystal and stay within par, because it is also what hint 3 shows.

## Map characters

| Character | Meaning |
|---|---|
| `#` | Wall |
| `.` | Floor |
| `S` | Start. Exactly one. |
| `X` | Exit. Exactly one. Open unless `exit:` lists channels. |
| `*` | Crystal on floor. At most one. Needed for the second star. |
| `~` | Gap. Nothing can stand on it. |
| `a`–`e` | Pressure plate on channel a–e. On while the player or an echo stands on it. |
| `A`–`E` | Door on channel a–e. Open while that channel is on. Never closes on someone standing in it. |
| `1`–`5` | Switch on channel a–e. Bump into it to toggle. Not walkable. |
| `6`–`9`, `0` | Timed switch on channel a–e. Bump it to turn it on for `timer` turns. Not walkable. |
| `f`–`j` | Bridge on channel a–e, over a gap. Walkable while that channel is on. Never retracts under someone. |
| `^` | Spikes. Stepping on them fails the level. Echoes ignore them. |
| `!` | Spikes that are up on odd turns: 1, 3, 5 and so on |
| `;` | Spikes that are up on even turns: 2, 4, 6 and so on |
| `%` | Crumbling floor. Turns into a gap once something steps off it. |
| `u` `v` `<` `>` | Wind pushing up, down, left or right. Entering it pushes you one extra tile if that tile is free. |
| `=` | Moving platform over a gap, moving left and right. It bounces at the ends of its gap lane and carries whoever stands on it. |
| `\|` | Moving platform over a gap, moving up and down |

A channel is on if any plate or switch with that letter is on. Plates, doors and switches with the same letter share one colour in the game.

## Rules that matter for design

- Each swipe or Wait is one turn. A turn resolves in this order: the player moves, echoes move oldest first, platforms move, devices update, crumbling floor breaks, then spikes, crystal and exit are checked.
- Doors use the state from the end of the previous turn. A plate pressed this turn opens its door for the next turn.
- Echoes repeat moves, not positions. If something blocks an echo, it stays put and carries on with its next move.
- Echoes are ghosts. They pass through the player and each other, ignore spikes, press plates and bump switches. They can't collect crystals or use the exit.
- An echo that runs out of moves stays where it stopped.
- Tapping Echo sends the player and every echo back to the start and resets the room.

## Size limits

- World 1: at most 7 wide and 9 tall.
- Later worlds: at most 9 wide and 12 tall.
- Keep a solution under about 60 moves in total across all runs.
