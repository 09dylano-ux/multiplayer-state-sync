
```markdown
# Multiplayer State Sync & Command Queue Engine

A client-server authoritative networking architecture built in C#. Demonstrates client-side prediction, server reconciliation, entity interpolation, and deterministic command queuing for real-time multiplayer games.

---

## The Problem It Solves

Over high-latency internet connections, sending input to a server and waiting for a response creates noticeable input lag for the player. Conversely, letting the client dictate its position enables game client hacking and cheating.

## The Solution

This architecture runs an **Authoritative Server** paired with **Client-Side Prediction**:
1. The local client immediately applies user movement inputs locally for zero visual latency.
2. Inputs are packaged as sequence-numbered commands and sent to the server command queue.
3. The server processes inputs sequentially, validates authority, and sends back official state updates.
4. If the server disagrees with the client's past prediction, the client silently **reconciles** (rewinds and re-simulates) its state.

---

## Architecture Diagram
Client Input  --->  [ Local Prediction ]  ---> Instant Visual Feedback
│
(Sequence-Numbered Input)
│
▼
[ Server Queue ]
│
(Authoritative Tick)
│
▼
[ State Snapshot ]  ---> Client Reconciliation Check
## Features

- **Client-Side Prediction:** Eliminates perceived input latency on movement commands.
- **Server Reconciliation:** Corrects client drift without jitter or snapping when packet loss occurs.
- **Entity Interpolation:** Smooths visual positions of remote entities using a configurable interpolation buffer delay.
- **Packet Loss Simulation:** Built-in network simulator to test stability under lag, jitter, and dropped packets.

---

## How to Run

### Running in Visual Studio:
1. Open `MultiplayerSync.sln`.
2. Right-click the Solution in Solution Explorer > **Set Startup Projects...**
3. Select **Multiple startup projects** and set both `ServerApp` and `ClientApp` to **Start**.
4. Press `F5`.

### Running in VS Code / Terminal:
1. Open two terminal windows (`Ctrl + ~`).
2. In Terminal 1 (Run the Server):
   ```bash
   cd ServerApp
   dotnet run
