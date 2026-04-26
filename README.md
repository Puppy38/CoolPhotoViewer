# Cool Photo Viewer

A lightweight photo viewer written in C# (.NET Framework 4.8) with support for standard image formats and custom **TXI/TXIA** files.

![Photo Viewer Screenshot](screenshot.png)

## ✨ Features
- Supports: **JPG / JPEG / JFIF, PNG, BMP, TXI, TXIA**
- Built-in TXI/TXIA renderer
- Zoom in / zoom out
- Open images with external editors:
  - MS Paint
  - Photos
  - Notepad (TXI/TXIA)
  - Visual Studio Code (TXI/TXIA)
- Simple and fast single-file executable

---

## ▶️ How to Run
Go to the **Releases** section and download the latest version.

---

## 🛠️ How to Compile

### Requirements
- .NET Framework 4.8
- `csc` (C# compiler)

### Steps
```cmd
csc /target:winexe /out:CoolPhotoViewer.exe CoolPhotoViewer.cs
