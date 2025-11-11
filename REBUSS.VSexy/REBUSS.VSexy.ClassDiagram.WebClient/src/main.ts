

import { Engine } from "@babylonjs/core/Engines/engine";
import { Scene } from "@babylonjs/core/scene";
import { ArcRotateCamera } from "@babylonjs/core/Cameras/arcRotateCamera";
import { Vector3 } from "@babylonjs/core/Maths/math.vector";
// import { HemisphericLight } from "@babylonjs/core/Lights/hemisphericLight";
import { MeshBuilder } from "@babylonjs/core/Meshes/meshBuilder";

// Importuj style, jeśli są potrzebne
import "./style.css";

declare global {
  interface Window {
    chrome: any;
  }
}

// Utwórz element do wyświetlania wiadomości
const messageDisplay = document.createElement("div");
messageDisplay.id = "messageDisplay";
messageDisplay.style.position = "fixed";
messageDisplay.style.top = "10px";
messageDisplay.style.left = "10px";
messageDisplay.style.right = "10px";
messageDisplay.style.padding = "15px";
messageDisplay.style.backgroundColor = "rgba(0, 0, 0, 0.8)";
messageDisplay.style.color = "#00ff00";
messageDisplay.style.fontFamily = "monospace";
messageDisplay.style.fontSize = "14px";
messageDisplay.style.borderRadius = "5px";
messageDisplay.style.whiteSpace = "pre-wrap";
messageDisplay.style.wordWrap = "break-word";
messageDisplay.style.maxHeight = "300px";
messageDisplay.style.overflowY = "auto";
messageDisplay.style.zIndex = "1000";
messageDisplay.style.display = "none";
document.body.appendChild(messageDisplay);

window.chrome?.webview?.addEventListener("message", (event: any) => {
  // Pobierz dane z eventu
  const message = event.data || event;
  
  // Loguj też do konsoli, żeby zobaczyć co dokładnie przychodzi
  console.log("Received message from host:", message);
  console.log("Message type:", typeof message);
  console.log("Message length:", JSON.stringify(message).length);
  
  // Wyświetl wiadomość na ekranie
  messageDisplay.style.display = "block";
  
  try {
    // Jeśli message to string, spróbuj go sparsować
    const data = typeof message === 'string' ? JSON.parse(message) : message;
    messageDisplay.textContent = JSON.stringify(data, null, 2);
  } catch (e) {
    // Jeśli nie można sparsować, wyświetl jako string
    messageDisplay.textContent = String(message);
  }
});

window.chrome?.webview?.postMessage({ type: "initialized" });

// Utwórz element canvas w HTML, jeśli go nie ma
let canvas = document.getElementById("renderCanvas") as HTMLCanvasElement;
if (!canvas) {
  canvas = document.createElement("canvas");
  canvas.id = "renderCanvas";
  canvas.style.width = "100%";
  canvas.style.height = "100%";
  canvas.style.display = "block";
  document.body.appendChild(canvas);
  console.log("Canvas created and appended to body");
}

// Ustaw rzeczywiste wymiary canvas
canvas.width = window.innerWidth;
canvas.height = window.innerHeight;

console.log("Canvas dimensions:", canvas.width, canvas.height);
console.log("Canvas client dimensions:", canvas.clientWidth, canvas.clientHeight);

// Utwórz silnik Babylon.js z obsługą WebGPU (jeśli dostępne)
const engine = new Engine(canvas, true);
const scene = new Scene(engine);

console.log("Engine created:", engine);
console.log("Scene created:", scene);

// Kamera
const camera = new ArcRotateCamera("camera", Math.PI / 2, Math.PI / 2.5, 4, Vector3.Zero(), scene);
camera.attachControl(canvas, true);

// Światło
// const light = new HemisphericLight("light", new Vector3(1, 1, 0), scene);

// Prosta siatka (kula)
const sphere = MeshBuilder.CreateSphere("sphere", { diameter: 1 }, scene);

console.log("Sphere created:", sphere);
console.log("Active camera:", scene.activeCamera);
console.log("Scene meshes count:", scene.meshes.length);

// Renderuj scenę
engine.runRenderLoop(() => {
  scene.render();
});

console.log("Render loop started");

// Obsługa zmiany rozmiaru okna
window.addEventListener("resize", () => {
  canvas.width = window.innerWidth;
  canvas.height = window.innerHeight;
  engine.resize();
});


