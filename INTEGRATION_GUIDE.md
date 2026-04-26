# MNET CampusGuessr – Setup Guide

This guide covers everything you need to get the project running: Unity version, Node.js, the web package, Vite, and the development workflow.

---

## Prerequisites

### Unity

- **Unity 2022.3 or later** (LTS recommended)
- **WebGL Build Support** module (install via Unity Hub when adding the editor)
- Monash University network access (or general internet) for the MazeMaps API

### Web (Node.js)

- **Node.js 18 LTS or later** (20 LTS recommended)
- **npm** (comes with Node.js)

Check versions:

```bash
node -v   # e.g. v20.x.x
npm -v    # e.g. 10.x.x
```

---

## 1. Unity project setup

1. **Open the project** in Unity (open the root folder that contains `Assets` and `ProjectSettings`).

2. **WebGL build settings** (recommended for development):
   - Go to **Edit → Project Settings → Player → WebGL**.
   - **Publishing Settings**: Compression Format **Disabled** (easier debugging).
   - **Resolution and Presentation**: set default canvas size and **Run In Background** as needed.

---

## 2. Web package and Vite

The web UI (MazeMaps, map controls, Unity–JS bridge) lives in the **`web/`** folder and is built and served with **Vite**.

### Install dependencies

From the project root:

```bash
cd web
npm install
```

### What’s in `web/`

| Path                      | Purpose                                                                                   |
| ------------------------- | ----------------------------------------------------------------------------------------- |
| `web/`                    | Vite project root (package.json, index.html).                                             |
| `web/src/`                | JavaScript source (app.js and modules: state, mapCore, markersAndLines, unityBridge, ui). |
| `web/public/`             | Static assets; Unity WebGL build output goes here so the app can load it.                 |
| `web/public/unity/Build/` | Unity loader, .data, .framework.js, .wasm (see workflow below).                           |

### Vite configuration

Vite is already set up in `web/`:

- **Config**: `web/src/vite.config.js` (root `"."`, `publicDir: "public"`, dev server on port **5173**).
- **Entry**: `web/index.html` loads the app and points to `/src/app.js` as the module entry.
- **Dev server**: Serves the app and copies `public/` (including Unity build files) as-is.

No extra Vite setup is required beyond `npm install` and the steps below.

---

## 3. Development workflow

You need both a **Unity WebGL build** and the **web app** running.

### Step 1: Build Unity for WebGL

1. In Unity: **File → Build Settings**.
2. Choose **WebGL**, add your scene(s), click **Build**.
3. Select an **output folder**.  
   For the Vite workflow, either:
   - Build **directly into** `web/public/unity/`, so that the Build folder ends up as `web/public/unity/Build/`, **or**
   - Build somewhere else (e.g. `geoguessr_build/`) and then **copy** the contents of that build folder into `web/public/unity/` so that:
     - `web/public/unity/Build/unity.loader.js`
     - `web/public/unity/Build/unity.framework.js`
     - `web/public/unity/Build/unity.data`
     - `web/public/unity/Build/unity.wasm`
       (and any other files your template produces) are present.

The `web/index.html` expects the Unity loader at `/unity/Build/unity.loader.js` and the rest of the paths in the `createUnityInstance` config; those map to `web/public/unity/Build/`.

### Step 2: Run the web app with Vite

From the **`web/`** directory:

```bash
npm run dev
```

Then open **http://localhost:5173** in a browser. You should see the Unity canvas and, when the game shows the map, the MazeMaps UI.

### Step 3: Iterating

- **Web/JS changes**: Edit files under `web/src/`. Vite will hot-reload; refresh the page if needed. No need to rebuild Unity for JS-only changes.
- **Unity changes**: After changing scenes, scripts, or settings, do a new WebGL build and replace (or rebuild into) `web/public/unity/` as in Step 1, then refresh the browser.
- **Hard refresh** (Ctrl+Shift+R / Cmd+Shift+R) if the game or map doesn’t update as expected.

---

## 4. Building for production

1. **Unity**: Build WebGL as usual. Put the build output into `web/public/unity/` (same as development) so the production bundle includes the latest game.
2. **Web**: From `web/` run:

   ```bash
   npm run build
   ```

   Vite writes the built site (HTML, JS, CSS) and a copy of `public/` (including `unity/Build/`) into **`web/dist/`**.

3. **Deploy** the contents of `web/dist/` to your web server (or host the folder with any static server). Ensure the server is configured for the routes and MIME types required by WebGL (see Unity/WebGL docs if you hit loading issues).

---

## 5. Quick reference

| Task              | Command / action                                                      |
| ----------------- | --------------------------------------------------------------------- |
| Install web deps  | `cd web && npm install`                                               |
| Dev server        | `cd web && npm run dev` → http://localhost:5173                       |
| Production build  | `cd web && npm run build` → output in `web/dist/`                     |
| Unity WebGL build | File → Build Settings → WebGL → Build → output to `web/public/unity/` |

---

## 6. Troubleshooting

- **Map or MazeMaps not loading**: Check network and browser console; ensure MazeMaps script and CSS URLs are reachable.
- **Unity not loading**: Confirm `web/public/unity/Build/` contains the loader and other files, and that paths in `index.html` match (e.g. `/unity/Build/unity.loader.js`).
- **Unity–JS calls failing**: Use a WebGL build (not Editor); check browser console and that global function names match what Unity’s `SendMessage` expects (e.g. `showMapFromUnity`, `addActualLocationFromUnity`).
- **Changes not visible**: Hard refresh (Ctrl+Shift+R); if you moved Unity build output, ensure Vite is serving the updated `public/` (restart `npm run dev` if needed).

For game logic, scene setup, and configuration details, see the main **README.md**.
