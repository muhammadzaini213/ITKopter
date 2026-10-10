# Sourdough-Starter
Unity 6.3 LTS URP starter for Slafurry Studios — GameSystem base classes, Drive asset sync, and itch.io deploy included.

<p align="center">
  <img src="https://github.com/user-attachments/assets/9dee6058-b82a-43b4-ae26-eab6ffe699da" width="100%" alt="Sourdough-Starter banner" />
</p>

<br>
<p align="left">
  <img src="https://img.shields.io/badge/UNITY-ffffff?style=for-the-badge&logo=unity&logoColor=000000" />
  <img src="https://img.shields.io/badge/STATUS • IN DEVELOPMENT-000000?style=for-the-badge&logo=git&logoColor=ffffff" />
  <img src="https://img.shields.io/badge/MADE BY SLAFURRY STUDIOS-ffffff?style=for-the-badge&logo=gamepad&logoColor=000000" />
</p>

---

## ABOUT
Short paragraph explaining the project's purpose, scope, and what makes it worth using or contributing to. 2-4 sentences max.

---

## PLAY IT ON
<p align="left">
  <a href="https://lordzaini.itch.io/"><img src="https://img.shields.io/badge/ITCH.IO-ffffff?style=for-the-badge&logo=itch.io&logoColor=000000" /></a>
</p>

---

## FEATURES
- Feature one, described in a single clear line
- Feature two
- Feature three
- Feature four

---

## ARCHITECTURE
Brief summary of the core architecture pattern used, with a link to the full doc for anyone who wants details.

```
Assets/_Game/
├── 00_Scripts/
│   ├── Core/       — base classes & interfaces (Singleton, GameSystem, Manager)
│   ├── Systems/    — persistent cross-scene services (Audio, Save, Scene, Localization)
│   ├── Manager/    — per-session gameplay coordinators
│   ├── Game/       — per-instance controllers & entities (+ Triggers/)
│   ├── UI/         — reactive observers & screen coordination
│   └── Utils/      — generic reusable helpers (GameFeel, extensions)
├── 01_Objects/     — prefabs & data, grouped by domain
├── 03_Audio/       — GameAudioMixer + Music/SFX (Drive-synced clips)
├── 04_Scenes/      — Boot.unity (in Build Settings)
└── 05_Settings/    — URP, Input
```

See [`ARCHITECTURE.md`](./ARCHITECTURE.md) for the full breakdown of base classes, naming conventions, and design decisions.

---

## GETTING STARTED

### Prerequisites
- Unity `6000.3.25f1` (see `ProjectSettings/ProjectVersion.txt`)
- Git (no Git LFS — binaries are stored as plain blobs)

### Setup
```bash
git clone https://github.com/muhammadzaini213/Sourdough-Starter.git
cd Sourdough-Starter
```
Open the project in Unity Hub, let it import, then open `Assets/_Game/04_Scenes/Boot.unity`.

---

## GITHUB WORKFLOWS

CI/CD covers Google Drive asset sync (`track`/`retrieve`), a **PR compile gate** (`unity-compile`), and Unity builds deployed to itch.io. Setup (secrets/variables) and usage are documented in [`GITHUB_WORKFLOWS.md`](./GITHUB_WORKFLOWS.md).

---

## TECH STACK
![Unity](https://img.shields.io/badge/Unity-000000?style=flat-square&logo=unity&logoColor=ffffff)
![C#](https://img.shields.io/badge/C%23-000000?style=flat-square&logo=csharp&logoColor=ffffff)
![Cinemachine](https://img.shields.io/badge/Cinemachine-000000?style=flat-square&logo=unity&logoColor=ffffff)
![Newtonsoft.Json](https://img.shields.io/badge/Newtonsoft.Json-000000?style=flat-square&logo=json&logoColor=ffffff)

---

## TEAM
| Role | Name |
|---|---|
| [Role] | [Name] |
| [Role] | [Name] |

---

## CONTRIBUTING

Contributions are welcome — see [`CONTRIBUTING.md`](./CONTRIBUTING.md) for setup, architecture rules, and PR guidelines.

To report a vulnerability, please email **slafurrystudios@gmail.com** instead of opening a public issue — see [`SECURITY.md`](./SECURITY.md).

---

## CONNECT
<p align="left">
  <a href="https://github.com/Slafurry-Studios"><img src="https://img.shields.io/badge/GITHUB-ffffff?style=for-the-badge&logo=github&logoColor=000000" /></a>
  <a href="https://www.linkedin.com/company/slafurry-studios/"><img src="https://img.shields.io/badge/LINKEDIN-000000?style=for-the-badge&logo=linkedin&logoColor=ffffff" /></a>
  <a href="https://slafurrystudios.itch.io/"><img src="https://img.shields.io/badge/ITCH.IO-ffffff?style=for-the-badge&logo=itch.io&logoColor=000000" /></a>
</p>
