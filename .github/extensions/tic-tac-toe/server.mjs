import http from "node:http";
import { renderPage } from "./page.mjs";

const MAX_BODY_BYTES = 1_024;

function sendJson(response, statusCode, value) {
  const body = JSON.stringify(value);
  response.writeHead(statusCode, {
    "content-type": "application/json; charset=utf-8",
    "content-length": Buffer.byteLength(body),
    "cache-control": "no-store",
  });
  response.end(body);
}

async function readJson(request) {
  let body = "";
  for await (const chunk of request) {
    body += chunk;
    if (Buffer.byteLength(body) > MAX_BODY_BYTES) {
      throw new Error("Request body is too large.");
    }
  }
  if (!body) return {};
  try {
    return JSON.parse(body);
  } catch {
    throw new Error("Request body must be valid JSON.");
  }
}

export class GameServer {
  constructor(store, onChanged) {
    this.store = store;
    this.onChanged = onChanged;
    this.clients = new Set();
    this.server = http.createServer((request, response) => {
      this.handle(request, response).catch((error) => {
        sendJson(response, 500, { error: error.message });
      });
    });
  }

  async start() {
    await new Promise((resolve, reject) => {
      this.server.once("error", reject);
      this.server.listen(0, "127.0.0.1", resolve);
    });
    const address = this.server.address();
    this.url = `http://127.0.0.1:${address.port}/`;
  }

  async close() {
    for (const response of this.clients) response.end();
    this.clients.clear();
    if (!this.server.listening) return;
    await new Promise((resolve, reject) => {
      this.server.close((error) => (error ? reject(error) : resolve()));
    });
  }

  publish(game) {
    const event = `event: game\ndata: ${JSON.stringify(game)}\n\n`;
    for (const response of this.clients) response.write(event);
  }

  async handle(request, response) {
    const url = new URL(request.url, "http://127.0.0.1");
    response.setHeader("x-content-type-options", "nosniff");
    response.setHeader("referrer-policy", "no-referrer");
    response.setHeader(
      "content-security-policy",
      "default-src 'self'; script-src 'unsafe-inline'; style-src 'unsafe-inline'; connect-src 'self'; frame-ancestors 'self'",
    );

    if (request.method === "GET" && url.pathname === "/") {
      const game = await this.store.read();
      const body = renderPage(game);
      response.writeHead(200, {
        "content-type": "text/html; charset=utf-8",
        "content-length": Buffer.byteLength(body),
        "cache-control": "no-store",
      });
      response.end(body);
      return;
    }

    if (request.method === "GET" && url.pathname === "/api/events") {
      response.writeHead(200, {
        "content-type": "text/event-stream; charset=utf-8",
        "cache-control": "no-cache",
        connection: "keep-alive",
      });
      response.write(": connected\n\n");
      this.clients.add(response);
      request.once("close", () => this.clients.delete(response));
      return;
    }

    if (request.method === "POST" && url.pathname === "/api/move") {
      const input = await readJson(request);
      const game = await this.store.play(input.index);
      this.onChanged(game);
      sendJson(response, 200, game);
      return;
    }

    if (request.method === "POST" && url.pathname === "/api/reset") {
      const game = await this.store.reset();
      this.onChanged(game);
      sendJson(response, 200, game);
      return;
    }

    sendJson(response, 404, { error: "Not found." });
  }
}

