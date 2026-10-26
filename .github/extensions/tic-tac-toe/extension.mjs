import path from "node:path";
import { CanvasError, createCanvas, joinSession } from "@github/copilot-sdk/extension";
import { GameStore, GameStoreError } from "./game-store.mjs";
import { GameServer } from "./server.mjs";

let session;
let store;
const servers = new Map();

function canvasError(error) {
  if (error instanceof CanvasError) return error;
  if (error instanceof GameStoreError) return new CanvasError(error.code, error.message);
  return new CanvasError("tic_tac_toe_failed", error?.message || "The tic-tac-toe operation failed.");
}

function getStore() {
  if (!store) {
    const workspacePath = session?.workspacePath || process.cwd();
    store = new GameStore(path.resolve(workspacePath));
  }
  return store;
}

function publish(game) {
  for (const server of servers.values()) server.publish(game);
}

async function ensureServer(instanceId) {
  const current = servers.get(instanceId);
  if (current) return current;

  const server = new GameServer(getStore(), publish);
  await server.start();
  servers.set(instanceId, server);
  return server;
}

async function withGameOperation(operation) {
  try {
    return await operation(getStore());
  } catch (error) {
    throw canvasError(error);
  }
}

const canvas = createCanvas({
  id: "pet-horoscope-tic-tac-toe",
  displayName: "Tic-tac-toe",
  description: "Play a shared, persistent game of tic-tac-toe in an accessible board.",
  actions: [
    {
      name: "get-game",
      description: "Get the current tic-tac-toe board, turn, and result.",
      handler: async () => withGameOperation((gameStore) => gameStore.read()),
    },
    {
      name: "play-move",
      description: "Place the current player's mark in a cell numbered 0 through 8.",
      inputSchema: {
        type: "object",
        properties: {
          index: {
            type: "integer",
            minimum: 0,
            maximum: 8,
            description: "Zero-based cell index, ordered left-to-right and top-to-bottom.",
          },
        },
        required: ["index"],
        additionalProperties: false,
      },
      handler: async (ctx) =>
        withGameOperation(async (gameStore) => {
          const game = await gameStore.play(ctx.input.index);
          publish(game);
          return game;
        }),
    },
    {
      name: "reset-game",
      description: "Clear the board and start a new game with X to move.",
      handler: async () =>
        withGameOperation(async (gameStore) => {
          const game = await gameStore.reset();
          publish(game);
          return game;
        }),
    },
  ],
  open: async (ctx) => {
    try {
      await getStore().read();
      const server = await ensureServer(ctx.instanceId);
      return { title: "Tic-tac-toe", url: server.url };
    } catch (error) {
      throw canvasError(error);
    }
  },
  onClose: async (ctx) => {
    // Servers are instance-owned so a closed canvas cannot leave a listener behind.
    if (ctx?.instanceId) {
      const server = servers.get(ctx.instanceId);
      if (!server) return;
      servers.delete(ctx.instanceId);
      await server.close();
      return;
    }

    const closing = [...servers.values()].map((server) => server.close());
    servers.clear();
    await Promise.all(closing);
  },
});

try {
  await new Promise((resolve) => setTimeout(resolve, 900));
  session = await joinSession({ canvases: [canvas] });
  await session.log("Tic-tac-toe canvas ready.", { ephemeral: true });
} catch (error) {
  const message = error?.message ? String(error.message) : String(error);
  if (/session not found/i.test(message)) process.exit(0);
  throw error;
}
