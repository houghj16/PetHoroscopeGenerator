function safeJson(value) {
  return JSON.stringify(value)
    .replaceAll("<", "\\u003c")
    .replaceAll(">", "\\u003e")
    .replaceAll("&", "\\u0026")
    .replaceAll("\u2028", "\\u2028")
    .replaceAll("\u2029", "\\u2029");
}

export function renderPage(game) {
  return `<!doctype html>
<html lang="en">
<head>
  <meta charset="utf-8">
  <meta name="viewport" content="width=device-width, initial-scale=1">
  <meta name="color-scheme" content="dark light">
  <title>Tic-tac-toe</title>
  <style>
    :root {
      font-family: ui-rounded, "Segoe UI", system-ui, sans-serif;
      color: #f7f5ff;
      background: #17142b;
    }
    * { box-sizing: border-box; }
    body {
      min-height: 100vh;
      margin: 0;
      display: grid;
      place-items: center;
      padding: 24px;
      background:
        radial-gradient(circle at 15% 15%, #533a8b 0, transparent 34%),
        radial-gradient(circle at 85% 80%, #174f65 0, transparent 38%),
        #17142b;
    }
    main {
      width: min(100%, 520px);
      padding: clamp(22px, 5vw, 38px);
      border: 1px solid rgb(255 255 255 / 16%);
      border-radius: 28px;
      background: rgb(20 18 42 / 82%);
      box-shadow: 0 28px 80px rgb(0 0 0 / 40%);
      backdrop-filter: blur(18px);
    }
    header { text-align: center; }
    h1 {
      margin: 0;
      font-size: clamp(2rem, 8vw, 3.8rem);
      letter-spacing: -0.06em;
    }
    .subtitle { margin: 8px 0 24px; color: #c9c2e8; }
    #status {
      min-height: 30px;
      margin: 0 0 18px;
      text-align: center;
      font-size: 1.15rem;
      font-weight: 700;
    }
    .board {
      display: grid;
      grid-template-columns: repeat(3, 1fr);
      gap: 10px;
      width: min(100%, 390px);
      margin-inline: auto;
    }
    .cell {
      aspect-ratio: 1;
      border: 1px solid rgb(255 255 255 / 14%);
      border-radius: 18px;
      color: #fff;
      background: rgb(255 255 255 / 8%);
      font: inherit;
      font-size: clamp(2.2rem, 13vw, 4.6rem);
      font-weight: 800;
      line-height: 1;
      cursor: pointer;
      transition: transform 120ms ease, background 120ms ease, border-color 120ms ease;
    }
    .cell:hover:not(:disabled) {
      transform: translateY(-2px);
      border-color: #8ae2ff;
      background: rgb(138 226 255 / 12%);
    }
    .cell:focus-visible, #reset:focus-visible {
      outline: 3px solid #ffd166;
      outline-offset: 4px;
    }
    .cell[data-mark="X"] { color: #8ae2ff; text-shadow: 0 0 24px rgb(138 226 255 / 30%); }
    .cell[data-mark="O"] { color: #ff9ec4; text-shadow: 0 0 24px rgb(255 158 196 / 30%); }
    .cell:disabled { cursor: default; }
    footer { display: flex; justify-content: center; margin-top: 24px; }
    #reset {
      border: 0;
      border-radius: 999px;
      padding: 11px 20px;
      color: #17142b;
      background: #ffd166;
      font: inherit;
      font-weight: 800;
      cursor: pointer;
    }
    #reset:hover { background: #ffe098; }
    #error {
      min-height: 22px;
      margin: 16px 0 0;
      color: #ffb4b4;
      text-align: center;
    }
    @media (prefers-reduced-motion: reduce) {
      .cell { transition: none; }
    }
  </style>
</head>
<body>
  <main>
    <header>
      <h1>Tic-tac-toe</h1>
      <p class="subtitle">Take turns on the same board.</p>
    </header>
    <p id="status" role="status" aria-live="polite"></p>
    <div id="board" class="board" role="group" aria-label="Tic-tac-toe board"></div>
    <footer><button id="reset" type="button">New game</button></footer>
    <p id="error" role="alert" aria-live="assertive"></p>
  </main>
  <script>
    let game = ${safeJson(game)};
    const board = document.querySelector("#board");
    const status = document.querySelector("#status");
    const error = document.querySelector("#error");
    const reset = document.querySelector("#reset");

    function statusText() {
      if (game.status === "draw") return "Draw game.";
      if (game.status.endsWith("-won")) return game.status[0] + " wins!";
      return game.currentPlayer + "'s turn";
    }

    function render() {
      status.textContent = statusText();
      board.replaceChildren();
      game.board.forEach((mark, index) => {
        const button = document.createElement("button");
        button.type = "button";
        button.className = "cell";
        button.dataset.mark = mark || "";
        button.textContent = mark || "";
        button.disabled = mark !== null || game.status !== "in-progress";
        button.setAttribute("aria-label", mark ? "Cell " + (index + 1) + ", " + mark : "Cell " + (index + 1) + ", empty");
        button.addEventListener("click", () => play(index));
        board.append(button);
      });
    }

    async function request(url, options) {
      error.textContent = "";
      const response = await fetch(url, options);
      const payload = await response.json();
      if (!response.ok) throw new Error(payload.error || "The request failed.");
      game = payload;
      render();
    }

    async function play(index) {
      try {
        await request("/api/move", {
          method: "POST",
          headers: { "content-type": "application/json" },
          body: JSON.stringify({ index }),
        });
      } catch (requestError) {
        error.textContent = requestError.message;
      }
    }

    reset.addEventListener("click", async () => {
      try {
        await request("/api/reset", { method: "POST" });
      } catch (requestError) {
        error.textContent = requestError.message;
      }
    });

    const events = new EventSource("/api/events");
    events.addEventListener("game", (event) => {
      game = JSON.parse(event.data);
      error.textContent = "";
      render();
    });
    events.addEventListener("error", () => {
      error.textContent = "Live updates disconnected. Reopen the canvas to reconnect.";
    });

    render();
  </script>
</body>
</html>`;
}

