# AR Survival Shooter

A mobile Augmented Reality (AR) first-person survival shooter built with Unity and AR Foundation. Point your phone at a real-world surface, place a virtual barn arena on it, then survive waves of enemies using your phone as your eyes.

---

## How It Works

### AR Placement
On launch the app scans the environment using ARCore (Android) or ARKit (iOS). Once a flat surface is detected, the start menu appears. Pressing **Start** anchors a virtual barn arena onto the detected surface via an `ARAnchor`, so it stays locked to the real world as you move around.

### Gameplay Loop
The game has four states managed by `GameManager`:

| State | Description |
|-------|-------------|
| **Start** | Scanning for surfaces; start menu shown |
| **Placing** | Barn is being positioned on the AR plane |
| **Playing** | Active round — timer counting down, enemies spawning |
| **End** | Round over — win/loss screen shown |

**Win condition:** Defeat 12 enemies **or** survive 60 seconds.  
**Lose condition:** Lose all 3 lives.

### Player
- Your phone's camera is your first-person view — physically moving and tilting the phone looks around the arena.
- A virtual joystick (MOVE pad) lets you walk without physically moving.
- **Turn Left / Turn Right** buttons rotate the camera around the arena.
- Tap the screen or hold the **SHOOT** button to fire. Auto-aim snaps to enemies within 25° of the crosshair.
- The player has **100 HP** and **3 lives**. Losing a life restores full HP with a brief invulnerability window.

### Enemies
Two enemy types alternate each spawn:
- **Melee Enemy** — walks directly toward the player and attacks on contact.
- **Shooter Enemy** — keeps its distance and fires projectiles at the player.

Enemies spawn on a ring around the player (4–8 arena units away) with a 1.5-second grace period at round start. Up to 8 enemies can be alive at once.

### Difficulty
| Mode | Spawn Interval | Enemy HP |
|------|---------------|----------|
| Easy | 3.5 s | ×1 |
| Hard | 1.8 s | ×2 |

### Scoring & Leaderboard
Each enemy defeated adds points to the score. Session results (score, kills, time survived, win/loss, difficulty) are saved locally via `PlayerPrefs` and displayed in the in-game leaderboard (top 10 by score).

---

## Requirements

- **Unity** 6000.0.58f1 (Unity 6)
- **Android** device with ARCore support **or** **iOS** device with ARKit support
- Unity modules: **Android Build Support** or **iOS Build Support**
- Packages (already in `Packages/manifest.json`):
  - AR Foundation
  - ARCore XR Plugin (Android) / ARKit XR Plugin (iOS)
  - XR Interaction Toolkit
  - Universal Render Pipeline (URP)
  - Input System
  - TextMeshPro

---

## How to Run

### In the Unity Editor (Desktop Preview)
1. Open the project folder in **Unity Hub** and launch with Unity 6000.0.58f1.
2. Open the scene: `Assets/Scenes/ARMain.unity`.
3. Press **Play**.
   - Because there is no real AR device, the app automatically places the barn on a virtual floor after ~2.5 seconds.
   - Hold **right-click + drag** to look around with the mouse.
   - Use **WASD** to move, **Q/E** to rotate the camera, and **Space** to shoot.

### Build & Deploy to Android
1. Go to **File → Build Settings**, select **Android**, and click **Switch Platform**.
2. Under **Player Settings → Other Settings**, ensure:
   - Minimum API Level ≥ 24
   - Scripting Backend: **IL2CPP**
   - Target Architectures: **ARM64**
3. Connect an ARCore-compatible Android device with USB debugging enabled.
4. Click **Build and Run** (or use the pre-built APK at `Survival-Shooter apk/AR-survivalShooter.apk`).

### Build & Deploy to iOS
1. Go to **File → Build Settings**, select **iOS**, and click **Switch Platform**.
2. Click **Build** to generate an Xcode project, then open it in Xcode and deploy to a device.

### Using the Pre-built APK
A ready-to-install APK is included:
```
Survival-Shooter apk/AR-survivalShooter.apk
```
Transfer it to an ARCore-supported Android device and install it (enable *Install from unknown sources* in device settings if prompted).

---

## Project Structure

```
Assets/
├── Scenes/         # ARMain.unity — the single game scene
├── Scripts/        # All C# game logic
│   ├── GameManager.cs          # Game state, scoring, win/lose
│   ├── ARPlacementController.cs# AR plane detection & barn anchoring
│   ├── PlayerController.cs     # First-person movement, shooting, health
│   ├── EnemyBase.cs            # Shared enemy logic (health, death, movement)
│   ├── MeleeEnemy.cs           # Melee attack behaviour
│   ├── ShooterEnemy.cs         # Ranged attack behaviour
│   ├── EnemySpawner.cs         # Wave spawning logic
│   ├── UIManager.cs            # All UI panels and HUD
│   ├── LeaderboardManager.cs   # Local score persistence
│   ├── AudioManager.cs         # Sound effects
│   ├── ObjectPool.cs           # Projectile pooling
│   └── Projectile.cs           # Bullet behaviour
├── Prefabs/        # Enemy models, projectile, barn world root
├── Audio/          # Sound effect assets
└── Settings/       # URP render pipeline settings
```
