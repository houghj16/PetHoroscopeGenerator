import crypto from "node:crypto";
import fs from "node:fs/promises";
import os from "node:os";
import path from "node:path";

const FORMAT = "pet-horoscope-tic-tac-toe";
const VERSION = 1;
const LOCK_TIMEOUT_MS = 4_000;
const STALE_LOCK_MS = 15_000;

export class GameStoreError extends Error {
  constructor(code, message) {
    super(message);
    this.name = "GameStoreError";
    this.code = code;
  }
}

function newGame() {
  return {
    board: Array(9).fill(null),
    currentPlayer: "X",
    status: "in-progress",
    moves: 0,
    updatedAt: new Date().toISOString(),
  };
}

function winningPlayer(board) {
  const lines = [
    [0, 1, 2],
    [3, 4, 5],
    [6, 7, 8],
    [0, 3, 6],
    [1, 4, 7],
    [2, 5, 8],
    [0, 4, 8],
    [2, 4, 6],
  ];

  for (const [a, b, c] of lines) {
    if (board[a] && board[a] === board[b] && board[a] === board[c]) {
      return board[a];
    }
  }
  return null;
}

function validateGame(value) {
  if (!value || typeof value !== "object" || Array.isArray(value)) return false;
  if (!Array.isArray(value.board) || value.board.length !== 9) return false;
  if (!value.board.every((cell) => cell === null || cell === "X" || cell === "O")) return false;
  if (value.currentPlayer !== "X" && value.currentPlayer !== "O") return false;
  if (!["in-progress", "X-won", "O-won", "draw"].includes(value.status)) return false;
  if (!Number.isInteger(value.moves) || value.moves < 0 || value.moves > 9) return false;
  if (typeof value.updatedAt !== "string" || Number.isNaN(Date.parse(value.updatedAt))) return false;

  const xCount = value.board.filter((cell) => cell === "X").length;
  const oCount = value.board.filter((cell) => cell === "O").length;
  if (xCount + oCount !== value.moves || xCount < oCount || xCount > oCount + 1) return false;

  const winner = winningPlayer(value.board);
  const expectedStatus = winner ? `${winner}-won` : value.moves === 9 ? "draw" : "in-progress";
  const expectedPlayer = value.moves % 2 === 0 ? "X" : "O";
  return value.status === expectedStatus && value.currentPlayer === expectedPlayer;
}

function validateDocument(value) {
  return value?.format === FORMAT && value?.version === VERSION && validateGame(value.game);
}

async function delay(milliseconds) {
  await new Promise((resolve) => setTimeout(resolve, milliseconds));
}

export class GameStore {
  constructor(workspacePath) {
    const copilotHome =
      typeof process.env.COPILOT_HOME === "string" && process.env.COPILOT_HOME.length > 0
        ? process.env.COPILOT_HOME
        : path.join(os.homedir(), ".copilot");
    const identity = path.resolve(workspacePath).toLowerCase();
    const key = crypto.createHash("sha256").update(identity).digest("hex");
    const directory = path.join(copilotHome, "extension-data", FORMAT);

    // The workspace hash keeps private game state stable without exposing local paths.
    this.filePath = path.join(directory, `${key}.json`);
    this.lockPath = `${this.filePath}.lock`;
    this.directory = directory;
  }

  async read() {
    await fs.mkdir(this.directory, { recursive: true });

    let text;
    try {
      text = await fs.readFile(this.filePath, "utf8");
    } catch (error) {
      if (error?.code === "ENOENT") return newGame();
      throw new GameStoreError("storage_read_failed", `Could not read the saved game: ${error.message}`);
    }

    let document;
    try {
      document = JSON.parse(text);
    } catch {
      throw new GameStoreError("storage_corrupt", "The saved tic-tac-toe game is not valid JSON.");
    }

    if (!validateDocument(document)) {
      throw new GameStoreError(
        "storage_unsupported",
        "The saved tic-tac-toe game has an unsupported format or invalid data.",
      );
    }
    return structuredClone(document.game);
  }

  async update(change) {
    const release = await this.acquireLock();
    try {
      const game = await this.read();
      const next = change(structuredClone(game));
      if (!validateGame(next)) {
        throw new GameStoreError("invalid_game_state", "The requested change produced an invalid game.");
      }
      await this.write(next);
      return structuredClone(next);
    } finally {
      await release();
    }
  }

  async reset() {
    return this.update(() => newGame());
  }

  async play(index) {
    return this.update((game) => {
      if (game.status !== "in-progress") {
        throw new GameStoreError("game_finished", "The game is finished. Reset it before playing again.");
      }
      if (!Number.isInteger(index) || index < 0 || index > 8) {
        throw new GameStoreError("invalid_cell", "Cell must be an integer from 0 through 8.");
      }
      if (game.board[index] !== null) {
        throw new GameStoreError("cell_occupied", `Cell ${index} is already occupied.`);
      }

      game.board[index] = game.currentPlayer;
      game.moves += 1;
      const winner = winningPlayer(game.board);
      game.status = winner ? `${winner}-won` : game.moves === 9 ? "draw" : "in-progress";
      game.currentPlayer = game.moves % 2 === 0 ? "X" : "O";
      game.updatedAt = new Date().toISOString();
      return game;
    });
  }

  async write(game) {
    const document = JSON.stringify({ format: FORMAT, version: VERSION, game }, null, 2);
    const temporaryPath = `${this.filePath}.${process.pid}.${crypto.randomUUID()}.tmp`;

    try {
      await fs.writeFile(temporaryPath, document, { encoding: "utf8", flag: "wx" });
      await fs.rename(temporaryPath, this.filePath);
    } catch (error) {
      await fs.rm(temporaryPath, { force: true }).catch(() => {});
      throw new GameStoreError("storage_write_failed", `Could not save the game: ${error.message}`);
    }
  }

  async acquireLock() {
    await fs.mkdir(this.directory, { recursive: true });
    const deadline = Date.now() + LOCK_TIMEOUT_MS;

    while (Date.now() < deadline) {
      try {
        const handle = await fs.open(this.lockPath, "wx");
        await handle.writeFile(`${process.pid}\n${new Date().toISOString()}\n`);
        return async () => {
          await handle.close();
          await fs.rm(this.lockPath, { force: true });
        };
      } catch (error) {
        if (error?.code !== "EEXIST") {
          throw new GameStoreError("storage_lock_failed", `Could not lock the saved game: ${error.message}`);
        }

        try {
          const stats = await fs.stat(this.lockPath);
          if (Date.now() - stats.mtimeMs > STALE_LOCK_MS) {
            await fs.rm(this.lockPath, { force: true });
            continue;
          }
        } catch (statError) {
          if (statError?.code === "ENOENT") continue;
          throw new GameStoreError("storage_lock_failed", `Could not inspect the game lock: ${statError.message}`);
        }
        await delay(50);
      }
    }

    throw new GameStoreError("storage_busy", "The saved game is busy. Please try again.");
  }
}

