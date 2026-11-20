#!/usr/bin/env node
// Quick level checker for Echo Shift.
//
// A JavaScript copy of the rules in Assets/Scripts/Simulation. The C# code is the
// source of truth and the C# tests in tests/ run the same checks on every push.
// This tool exists so levels can be checked on any machine that has Node, without Unity.
//
// Usage:
//   node tools/levelcheck.mjs                 check every level
//   node tools/levelcheck.mjs w1_l05          check one level by id or file name
//   node tools/levelcheck.mjs --show w1_l05   also print the board after every turn

import { readFileSync, readdirSync } from "node:fs";
import { join, dirname, basename } from "node:path";
import { fileURLToPath } from "node:url";

const ROOT = join(dirname(fileURLToPath(import.meta.url)), "..");
const LEVEL_DIR = join(ROOT, "Assets", "Resources", "Levels");

// ------------------------------------------------------------------ parsing

const KEYS = new Set(["id", "name", "world", "number", "echoes", "par", "moves", "timer", "needs_echo", "boss",
  "exit", "hint1", "hint2", "hint3", "solution", "map"]);

function isKeyLine(line) {
  const colon = line.indexOf(":");
  if (colon <= 0) return false;
  if (!/^[a-z0-9_ ]+$/.test(line.slice(0, colon))) return false;
  return /[a-zA-Z]/.test(line.trimStart()[0]);
}

export function parseLevel(text) {
  const L = {
    id: "", name: "", world: 1, number: 1, echoLimit: 1, par: 1, moves: 12, timer: 4,
    needsEcho: true, boss: false, exitChannels: [], hints: ["", "", ""], solution: [],
    width: 0, height: 0, tiles: [], start: null, exit: null, hasCrystal: false, crystal: null,
    devices: [], platforms: [],
  };
  const rows = [];
  let inMap = false, sawEchoes = false, sawPar = false;
  const lines = text.replace(/\r\n?/g, "\n").split("\n");
  for (let i = 0; i < lines.length; i++) {
    const line = lines[i].replace(/\s+$/, "");
    if (line.trimStart().startsWith("//")) continue;
    if (inMap) {
      if (line.length === 0 || isKeyLine(line)) inMap = false;
      else { rows.push(line.trim()); continue; }
    }
    if (line.trim().length === 0) continue;
    const colon = line.indexOf(":");
    if (colon <= 0) throw new Error(`${L.id || "level"} line ${i + 1}: expected 'key: value'`);
    const key = line.slice(0, colon).trim().toLowerCase();
    const value = line.slice(colon + 1).trim();
    if (!KEYS.has(key)) throw new Error(`${L.id} line ${i + 1}: unknown key '${key}'`);
    switch (key) {
      case "id": L.id = value; break;
      case "name": L.name = value; break;
      case "world": L.world = +value; break;
      case "number": L.number = +value; break;
      case "echoes": L.echoLimit = +value; sawEchoes = true; break;
      case "par": L.par = +value; sawPar = true; break;
      case "moves": L.moves = +value; break;
      case "timer": L.timer = +value; break;
      case "needs_echo": L.needsEcho = /^(true|yes|1)$/i.test(value); break;
      case "boss": L.boss = /^(true|yes|1)$/i.test(value); break;
      case "exit": L.exitChannels = value.split(/[ ,]+/).filter(Boolean).map((c) => c.charCodeAt(0) - 97); break;
      case "hint1": L.hints[0] = value; break;
      case "hint2": L.hints[1] = value; break;
      case "hint3": L.hints[2] = value; break;
      case "solution":
        L.solution = value.split("|").map((run) => [...run.replace(/\s+/g, "")].map((c) => {
          if (!"UDLRW".includes(c.toUpperCase())) throw new Error(`${L.id}: bad move '${c}'`);
          return c.toUpperCase();
        }));
        break;
      case "map": inMap = true; break;
    }
  }
  if (!sawEchoes) L.echoLimit = L.needsEcho ? 1 : 0;
  if (!sawPar) L.par = L.echoLimit;
  if (!rows.length) throw new Error(`${L.id}: no map`);
  L.height = rows.length;
  L.width = rows[0].length;
  for (let y = 0; y < rows.length; y++) {
    if (rows[y].length !== L.width) throw new Error(`${L.id}: map row ${y + 1} has width ${rows[y].length}, expected ${L.width}`);
    for (let x = 0; x < L.width; x++) {
      const c = rows[y][x];
      let tile = "floor";
      const p = { x, y };
      switch (c) {
        case ".": break;
        case "#": tile = "wall"; break;
        case "~": tile = "gap"; break;
        case "^": tile = "spikes"; break;
        case "!": tile = "spikesOdd"; break;
        case ";": tile = "spikesEven"; break;
        case "%": tile = "crumble"; break;
        case "u": tile = "windUp"; break;
        case "v": tile = "windDown"; break;
        case "<": tile = "windLeft"; break;
        case ">": tile = "windRight"; break;
        case "S": if (L.start) throw new Error(`${L.id}: two starts`); L.start = p; break;
        case "X": if (L.exit) throw new Error(`${L.id}: two exits`); L.exit = p; break;
        case "*": if (L.hasCrystal) throw new Error(`${L.id}: two crystals`); L.hasCrystal = true; L.crystal = p; break;
        case "=": tile = "gap"; L.platforms.push({ pos: p, horizontal: true }); break;
        case "|": tile = "gap"; L.platforms.push({ pos: p, horizontal: false }); break;
        default:
          if (c >= "a" && c <= "e") L.devices.push({ kind: "plate", pos: p, ch: c.charCodeAt(0) - 97 });
          else if (c >= "A" && c <= "E") L.devices.push({ kind: "door", pos: p, ch: c.charCodeAt(0) - 65 });
          else if (c >= "1" && c <= "5") L.devices.push({ kind: "switch", pos: p, ch: c.charCodeAt(0) - 49 });
          else if (c >= "6" && c <= "9") L.devices.push({ kind: "timed", pos: p, ch: c.charCodeAt(0) - 54 });
          else if (c === "0") L.devices.push({ kind: "timed", pos: p, ch: 4 });
          else if (c >= "f" && c <= "j") { tile = "gap"; L.devices.push({ kind: "bridge", pos: p, ch: c.charCodeAt(0) - 102 }); }
          else throw new Error(`${L.id}: unknown map character '${c}' at row ${y + 1}, column ${x + 1}`);
      }
      L.tiles.push(tile);
    }
  }
  if (!L.start) throw new Error(`${L.id}: no start`);
  if (!L.exit) throw new Error(`${L.id}: no exit`);
  return L;
}

// ------------------------------------------------------------------ simulation

const DELTA = { U: [0, -1], D: [0, 1], L: [-1, 0], R: [1, 0], W: [0, 0] };
const WIND = { windUp: [0, -1], windDown: [0, 1], windLeft: [-1, 0], windRight: [1, 0] };
const eq = (a, b) => a.x === b.x && a.y === b.y;
const add = (p, d) => ({ x: p.x + d[0], y: p.y + d[1] });

export class Sim {
  constructor(level) {
    this.L = level;
    this.echoes = [];
    this.run = [];
    this.undo = [];
    this.reset();
  }

  inBounds(p) { return p.x >= 0 && p.y >= 0 && p.x < this.L.width && p.y < this.L.height; }
  tile(p) { return this.inBounds(p) ? this.L.tiles[p.y * this.L.width + p.x] : "wall"; }
  deviceAt(p) { return this.L.devices.findIndex((d) => eq(d.pos, p)); }
  platformAt(p) { return this.s.platPos.findIndex((q) => eq(q, p)); }
  occupied(p) { return eq(this.s.player, p) || this.s.echoes.some((e) => eq(e, p)); }

  reset() {
    const L = this.L;
    this.s = {
      turn: 0, player: { ...L.start }, echoes: this.echoes.map(() => ({ ...L.start })),
      active: L.devices.map(() => false), timer: L.devices.map(() => 0), channel: [false, false, false, false, false],
      crumble: L.tiles.map(() => 0), platPos: L.platforms.map((p) => ({ ...p.pos })), platDir: L.platforms.map(() => 1),
      exitOpen: false, crystal: false, status: "playing", cause: "",
    };
    this.updateDevices();
  }

  clone() { return JSON.parse(JSON.stringify(this.s)); }

  get canEcho() { return this.s.status === "playing" && this.run.length > 0 && this.echoes.length < this.L.echoLimit; }

  step(m) {
    if (this.s.status !== "playing") return "notPlaying";
    if (this.run.length >= this.L.moves) return "limit";
    this.undo.push(this.clone());
    this.run.push(m);
    this.resolve(m);
    return "ok";
  }

  doUndo() { if (!this.undo.length) return false; this.s = this.undo.pop(); this.run.pop(); return true; }

  makeEcho() {
    if (!this.canEcho) return false;
    this.echoes.push([...this.run]);
    this.run = [];
    this.undo = [];
    this.reset();
    return true;
  }

  restart() { this.echoes = []; this.run = []; this.undo = []; this.reset(); }

  stars() {
    let st = 1;
    if (!this.L.hasCrystal || this.s.crystal) st = 2;
    if (st === 2 && this.echoes.length <= this.L.par) st = 3;
    return st;
  }

  resolve(m) {
    const s = this.s;
    s.turn++;
    const idx = s.turn - 1;
    this.moveEntity(-1, m);
    this.echoes.forEach((run, i) => this.moveEntity(i, idx < run.length ? run[idx] : "W"));
    this.advancePlatforms();
    this.updateDevices();
    this.updateCrumble();
    if (this.spikeUp(s.player, s.turn)) { s.status = "failed"; s.cause = "spikes"; return; }
    if (!this.ground(s.player)) { s.status = "failed"; s.cause = "fell"; return; }
    if (this.L.hasCrystal && !s.crystal && eq(s.player, this.L.crystal)) s.crystal = true;
    if (eq(s.player, this.L.exit) && s.exitOpen) s.status = "won";
  }

  getPos(e) { return e === -1 ? this.s.player : this.s.echoes[e]; }
  setPos(e, p) { if (e === -1) this.s.player = p; else this.s.echoes[e] = p; }
  isSwitch(i) { return i >= 0 && (this.L.devices[i].kind === "switch" || this.L.devices[i].kind === "timed"); }

  moveEntity(e, m) {
    if (m === "W") return;
    const from = this.getPos(e);
    const target = add(from, DELTA[m]);
    const dev = this.deviceAt(target);
    if (this.isSwitch(dev)) { this.bump(dev); return; }
    if (!this.walkable(target)) return;
    this.setPos(e, target);
    if (this.tile(from) === "crumble") {
      const i = from.y * this.L.width + from.x;
      if (this.s.crumble[i] === 0) this.s.crumble[i] = 1;
    }
    const t = this.tile(target);
    if (WIND[t]) {
      const to = add(target, WIND[t]);
      if (!this.isSwitch(this.deviceAt(to)) && this.walkable(to)) this.setPos(e, to);
    }
  }

  bump(i) {
    if (this.L.devices[i].kind === "switch") this.s.active[i] = !this.s.active[i];
    else this.s.timer[i] = this.L.timer + 1;
  }

  canPlatformEnter(p, self) {
    if (!this.inBounds(p) || this.tile(p) !== "gap") return false;
    if (this.deviceAt(p) >= 0) return false;
    return !this.s.platPos.some((q, k) => k !== self && eq(q, p));
  }

  advancePlatforms() {
    const s = this.s;
    this.L.platforms.forEach((def, k) => {
      const pos = s.platPos[k];
      const step = def.horizontal ? [s.platDir[k], 0] : [0, s.platDir[k]];
      let next = add(pos, step);
      if (!this.canPlatformEnter(next, k)) {
        s.platDir[k] = -s.platDir[k];
        next = add(pos, [-step[0], -step[1]]);
        if (!this.canPlatformEnter(next, k)) return;
      }
      if (eq(s.player, pos)) s.player = { ...next };
      s.echoes.forEach((ep, ei) => { if (eq(ep, pos)) s.echoes[ei] = { ...next }; });
      s.platPos[k] = next;
    });
  }

  updateDevices() {
    const s = this.s, D = this.L.devices;
    D.forEach((d, i) => {
      if (d.kind === "plate") s.active[i] = this.occupied(d.pos);
      else if (d.kind === "timed") { if (s.timer[i] > 0) s.timer[i]--; s.active[i] = s.timer[i] > 0; }
    });
    s.channel = [false, false, false, false, false];
    D.forEach((d, i) => {
      if ((d.kind === "plate" || d.kind === "switch" || d.kind === "timed") && s.active[i]) s.channel[d.ch] = true;
    });
    D.forEach((d, i) => {
      if (d.kind !== "door" && d.kind !== "bridge") return;
      let want = s.channel[d.ch];
      if (!want && s.active[i] && this.occupied(d.pos)) want = true;
      s.active[i] = want;
    });
    let open = this.L.exitChannels.every((c) => s.channel[c]);
    if (!open && s.exitOpen && this.occupied(this.L.exit)) open = true;
    s.exitOpen = open;
  }

  updateCrumble() {
    const s = this.s;
    s.crumble.forEach((v, i) => {
      if (v !== 1) return;
      const p = { x: i % this.L.width, y: Math.floor(i / this.L.width) };
      if (!this.occupied(p)) s.crumble[i] = 2;
    });
  }

  ground(p) {
    if (!this.inBounds(p)) return false;
    const t = this.tile(p);
    if (t === "wall") return false;
    if (t === "gap") {
      if (this.platformAt(p) >= 0) return true;
      const d = this.deviceAt(p);
      return d >= 0 && this.L.devices[d].kind === "bridge" && this.s.active[d];
    }
    if (t === "crumble" && this.s.crumble[p.y * this.L.width + p.x] === 2) return this.platformAt(p) >= 0;
    return true;
  }

  walkable(p) {
    if (!this.ground(p)) return false;
    const d = this.deviceAt(p);
    if (d >= 0) {
      const k = this.L.devices[d].kind;
      if (k === "switch" || k === "timed") return false;
      if (k === "door" && !this.s.active[d]) return false;
    }
    if (eq(p, this.L.exit) && !this.s.exitOpen) return false;
    return true;
  }

  spikeUp(p, turn) {
    const t = this.tile(p);
    return t === "spikes" || (t === "spikesOdd" && turn % 2 === 1) || (t === "spikesEven" && turn % 2 === 0);
  }

  render() {
    const L = this.L, s = this.s;
    const out = [];
    for (let y = 0; y < L.height; y++) {
      let row = "";
      for (let x = 0; x < L.width; x++) {
        const p = { x, y };
        if (eq(s.player, p)) { row += "@"; continue; }
        const ei = s.echoes.findIndex((e) => eq(e, p));
        if (ei >= 0) { row += String(ei + 1); continue; }
        if (this.platformAt(p) >= 0) { row += "="; continue; }
        const d = this.deviceAt(p);
        if (d >= 0) {
          const dv = L.devices[d], on = s.active[d], ch = "abcde"[dv.ch];
          row += dv.kind === "plate" ? (on ? ch.toUpperCase() : ch)
            : dv.kind === "door" ? (on ? "'" : "D")
            : dv.kind === "bridge" ? (on ? "_" : "~")
            : (on ? "+" : "o");
          continue;
        }
        if (eq(p, L.exit)) { row += s.exitOpen ? "X" : "x"; continue; }
        if (L.hasCrystal && !s.crystal && eq(p, L.crystal)) { row += "*"; continue; }
        const t = this.tile(p);
        row += t === "wall" ? "#" : t === "gap" ? "~" : t === "spikes" ? "^" : t === "spikesOdd" || t === "spikesEven"
          ? (this.spikeUp(p, s.turn + 1) ? "^" : ",") : t === "crumble" ? (s.crumble[y * L.width + x] === 2 ? "~" : "%")
          : t.startsWith("wind") ? { windUp: "u", windDown: "v", windLeft: "<", windRight: ">" }[t] : ".";
      }
      out.push(row);
    }
    return out.join("\n");
  }
}

// ------------------------------------------------------------------ checks

export function checkSolution(L, show = false) {
  const errors = [];
  const sim = new Sim(L);
  if (!L.solution.length) return ["no solution"];
  if (L.solution.length - 1 > L.echoLimit) errors.push(`solution uses ${L.solution.length - 1} echoes but the limit is ${L.echoLimit}`);
  for (let r = 0; r < L.solution.length; r++) {
    const run = L.solution[r];
    if (run.length > L.moves) errors.push(`run ${r + 1} has ${run.length} moves but the limit is ${L.moves}`);
    const last = r === L.solution.length - 1;
    for (const m of run) {
      const res = sim.step(m);
      if (show) console.log(`run ${r + 1} turn ${sim.s.turn} ${m}\n${sim.render()}\n`);
      if (res !== "ok") { errors.push(`run ${r + 1}: step returned ${res}`); return errors; }
      if (sim.s.status === "failed") { errors.push(`run ${r + 1} turn ${sim.s.turn}: player failed (${sim.s.cause})`); return errors; }
      if (sim.s.status === "won") {
        if (!last) errors.push(`run ${r + 1} already wins; drop the later runs`);
        break;
      }
    }
    if (!last && !sim.makeEcho()) { errors.push(`run ${r + 1}: could not make an echo`); return errors; }
  }
  if (sim.s.status !== "won") errors.push("solution does not reach the open exit");
  else if (sim.stars() !== 3) errors.push(`solution earns ${sim.stars()} stars, expected 3 (crystal ${sim.s.crystal ? "taken" : "missed"}, echoes ${sim.echoes.length}, par ${L.par})`);
  return errors;
}

/** Breadth-first search with no echoes. Returns the shortest winning move list, or null. */
export function solveWithoutEcho(L) {
  const sim = new Sim({ ...L, echoLimit: 0 });
  const key = (s) => JSON.stringify([s.player, s.active, s.timer, s.crumble, s.platPos, s.platDir, L.tiles.some((t) => t.startsWith("spikes") && t !== "spikes") ? s.turn % 2 : 0]);
  let frontier = [{ s: sim.clone(), path: "" }];
  const seen = new Set([key(sim.s)]);
  for (let depth = 0; depth < L.moves && frontier.length; depth++) {
    const next = [];
    for (const node of frontier) {
      for (const m of "UDLRW") {
        sim.s = JSON.parse(JSON.stringify(node.s));
        sim.run = new Array(depth).fill("W");
        sim.step(m);
        if (sim.s.status === "won") return node.path + m;
        if (sim.s.status === "failed") continue;
        const k = key(sim.s);
        if (seen.has(k)) continue;
        seen.add(k);
        next.push({ s: sim.clone(), path: node.path + m });
      }
    }
    frontier = next;
  }
  return null;
}

export function lint(L) {
  const errors = [];
  if (L.width > 9 || L.height > 12) errors.push(`map is ${L.width}x${L.height}; the maximum is 9x12`);
  if (L.world === 1 && (L.width > 7 || L.height > 9)) errors.push(`World 1 maps must fit 7x9, this is ${L.width}x${L.height}`);
  for (const d of L.devices.filter((d) => d.kind === "door" || d.kind === "bridge")) {
    if (!L.devices.some((s) => s.ch === d.ch && (s.kind === "plate" || s.kind === "switch" || s.kind === "timed")))
      errors.push(`${d.kind} at row ${d.pos.y + 1}, column ${d.pos.x + 1} listens to channel ${"abcde"[d.ch]} but nothing sends it`);
  }
  for (const c of L.exitChannels)
    if (!L.devices.some((s) => s.ch === c && s.kind !== "door" && s.kind !== "bridge")) errors.push(`exit needs channel ${"abcde"[c]} but nothing sends it`);
  if (!L.hints.every((h) => h.length > 0)) errors.push("needs three hints");
  if (!L.name) errors.push("needs a name");
  if (L.par > L.echoLimit) errors.push("par is higher than the echo limit");
  return errors;
}

// ------------------------------------------------------------------ CLI

if (process.argv[1] && basename(process.argv[1]) === "levelcheck.mjs") {
  const args = process.argv.slice(2);
  const show = args.includes("--show");
  const wanted = args.filter((a) => !a.startsWith("--"));
  const files = readdirSync(LEVEL_DIR).filter((f) => f.endsWith(".txt")).sort();
  let failed = 0, checked = 0;
  const ids = new Set(), numbers = new Set();
  for (const f of files) {
    const id = f.replace(/\.txt$/, "");
    if (wanted.length && !wanted.includes(id) && !wanted.includes(f)) continue;
    checked++;
    let errs = [];
    let L;
    try {
      L = parseLevel(readFileSync(join(LEVEL_DIR, f), "utf8"));
      if (L.id !== id) errs.push(`id '${L.id}' does not match file name '${id}'`);
      if (ids.has(L.id)) errs.push("duplicate id");
      if (numbers.has(L.number)) errs.push(`duplicate number ${L.number}`);
      ids.add(L.id); numbers.add(L.number);
      errs = errs.concat(lint(L), checkSolution(L, show));
      if (L.needsEcho) {
        const cheat = solveWithoutEcho(L);
        if (cheat) errs.push(`marked needs_echo but can be won with no echo: ${cheat}`);
      }
    } catch (e) {
      errs.push(e.message);
    }
    const label = L ? `${String(L.number).padStart(3)} ${L.id} ${L.name}` : id;
    if (errs.length) { failed++; console.log(`FAIL ${label}`); errs.forEach((e) => console.log(`     - ${e}`)); }
    else console.log(`ok   ${label}`);
  }
  console.log(`\n${checked - failed}/${checked} levels passed`);
  process.exit(failed ? 1 : 0);
}
