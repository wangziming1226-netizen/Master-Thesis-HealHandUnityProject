# HealHand Unity Project

A privacy-preserving, adaptive hand rehabilitation training prototype developed in Unity for an MSc thesis at DTU Compute.

HealHand uses MediaPipe Hands for touchless hand sensing and provides a rigged hand/avatar interface instead of showing a direct camera preview during the formal training workflow.

> This repository contains a late-stage research prototype for technical evaluation.  
> It is not a certified clinical rehabilitation product.

---

## Overview

HealHand explores how an iPad-based hand rehabilitation system can combine:

- Privacy-preserving camera-based hand sensing
- A rigged hand/avatar interface
- Interpretable movement-quality feedback
- Adaptive task difficulty
- Structured task-level logging

The user performs hand gestures in front of the device camera. MediaPipe Hands estimates hand landmarks, the project recognises the intended gesture, and the system provides visual feedback through the rigged hand/avatar interface.

The formal thesis workflow does not display or store identifiable raw camera video.

---

## Research Goals

HealHand focuses on three main research directions.

### 1. Privacy-preserving interaction

The system uses camera-based hand sensing while avoiding identifiable video display and storage in the normal formal workflow.

### 2. Interpretable movement-quality feedback

The system provides more than a simple success/failure result. Task feedback can include:

- Movement-quality score
- Hold performance
- Gesture confidence
- Tracking status
- Tracking-loss information
- Error type
- Completion time

### 3. Adaptive task difficulty

The system compares a fixed baseline with an adaptive mode.

The final adaptive mechanism follows an **increase-or-keep** strategy:

- Difficulty increases after sufficiently strong recent performance.
- Difficulty remains unchanged when performance does not meet the required criteria.
- The evaluated design does not automatically lower difficulty.

---

## Main Features

- MediaPipe Hands-based hand tracking
- Touchless gesture interaction
- Rigged hand/avatar interface
- Open Hand, Fist, and OK gesture tasks
- Practice, Assessment, Daily, and Story modules
- Fixed and adaptive task modes
- QR-card task triggering
- Movement-quality scoring and feedback
- Tracking-loss awareness
- Structured task and session logging
- iPad-oriented interaction design
- Privacy-aware formal evaluation workflow

---

## Main Modules

### Practice

Practice is the most mature module and the main module used for the fixed-versus-adaptive evaluation.

It supports:

- Gesture-specific tasks
- Fixed and adaptive difficulty modes
- Movement-quality scoring
- Task feedback
- Tracking-aware status information
- Structured task-level logging

### Assessment

Assessment provides a more structured task flow for supplementary validation.

It is designed to support repeatable gesture-based tasks and comparable performance outputs.

### Daily

Daily provides a lighter routine-based interaction mode for repeated everyday practice.

### Story

Story is a lightweight recreational interaction mode.

It is not part of the main formal evaluation, but demonstrates that the same gesture-input pipeline can also support a more playful interaction format.

---

## System Pipeline

```text
Camera Input
    ↓
MediaPipe Hand Tracking
    ↓
Hand Landmark Processing
    ↓
Gesture Recognition and Confidence Estimation
    ↓
Task Engine and Quality Scoring
    ↓
Fixed or Adaptive Difficulty Control
    ↓
Rigged Hand Avatar and User Feedback
    ↓
Task-Level Logging and Export
```

The project uses one primary hand-tracking pipeline to reduce duplicated camera ownership and avoid unstable concurrent tracking behaviour.

---

## Privacy and Data Principles

The formal thesis workflow follows these principles:

- No identifiable raw camera video is displayed in the normal user-facing workflow.
- No raw camera video is stored for formal evaluation.
- No per-frame hand landmark trajectories are exported for formal analysis.
- Exported outputs are limited to anonymised task-level and session-level summaries.
- Participant consent forms, exported formal logs, and identifiable administrative material are excluded from this repository.
- Restricted experiment and interview videos are stored separately under `Study_Materials/Videos/` in this private repository for thesis supervision and examination only.

Examples of task-level outputs include:

```text
success
score
completion_time_sec
error_type
difficulty_mode
difficulty_level
tracking_ratio
tracking_loss_count
gesture confidence values
```

---

## Requirements

The project was developed and tested with:

- Unity 6000.0.58f1 (Unity 6)
- Universal Render Pipeline (URP)
- MediaPipe Hands Unity integration
- macOS and Xcode for iOS/iPadOS builds
- An iPad with a front-facing camera
- Camera permission enabled for the selected build target

Unity package dependencies are defined in:

```text
Packages/manifest.json
```

---

## Getting Started

### 1. Clone the repository

```bash
git clone https://github.com/wangziming1226-netizen/Master-Thesis-HealHandUnityProject.git
cd Master-Thesis-HealHandUnityProject
```

### 2. Open the project in Unity

1. Open Unity Hub.
2. Select **Add**.
3. Choose the project root folder.
4. Open the project using Unity 6000.0.58f1 (Unity 6). Using a different Unity version may trigger package, scene, or project-setting upgrades.
5. Allow Unity to restore the dependencies defined in `Packages/manifest.json`.

### 3. Open the main scene

The main thesis prototype entry scene is:

```text
ThesisHub
```

Open `ThesisHub` from:

```text
Assets/ASET/_MyGameScenes/
```

`ThesisHub` is the host scene for the user-facing interface. The tracking services are loaded through the project workflow, so the tracking scene normally does not need to be opened and run manually as a separate entry scene.

Before running or building, confirm that `ThesisHub` is included in **Scenes In Build** and appears first in the build order.

### 4. Run the thesis prototype in the Unity Editor

1. Open `ThesisHub`.
2. Confirm that the camera is available and that MediaPipe hand tracking is configured.
3. Press **Play** in the Unity Editor.
4. Allow camera access when requested.
5. Wait for the home screen and the rigged hand/avatar view to appear.
6. Move one hand in front of the camera and confirm that the avatar follows the hand.

From the home screen, select one of the available modules:

- **Practice** — the main formal module for fixed-versus-adaptive task evaluation.
- **Assessment** — a structured supporting task flow for repeatable assessment-oriented trials.
- **Daily** — a lighter routine-based practice flow.
- **Story** — a lightweight recreational interaction mode.

### 5. Basic interaction flow

For the main thesis workflow, use the following sequence:

1. Start from `ThesisHub`.
2. Select **Practice**.
3. Select **Fixed** or **Adaptive** mode.
4. When prompted, scan a supported QR card to set the target gesture and initial task parameters.
5. Follow the displayed target gesture with your hand.
6. Keep the gesture stable for the required hold duration.
7. Review the task feedback, including success status, score, completion time, and tracking-related information.
8. Continue with the next task or return to the home screen.

The main formal evaluation focuses on Practice. Assessment and Daily are supplementary modules, while Story is a lightweight extension and is not part of the main fixed-versus-adaptive comparison.

### 5.1 Restricted study videos

Restricted study recordings are available in:

```text
Study_Materials/Videos/
```

This folder contains pseudonymised experiment recordings and post-task interview videos associated with the thesis evaluation. The materials are included only for supervision and examination in this private repository.

Files use participant-style identifiers such as `Person1`, `Person2`, and `Person3`. They must not be redistributed, made public, or used outside the thesis review context.

### 6. Build for iPad

1. In Unity, open **File → Build Settings**.
2. Select **iOS**.
3. Click **Switch Platform**.
4. Confirm that `ThesisHub` is included in **Scenes In Build** and is first in the list.
5. Click **Build**.
6. Open the generated Xcode project.
7. Configure signing for the target iPad.
8. Confirm that the iOS camera usage description is present in the generated project settings.
9. Deploy the project to the iPad.
10. On first launch, allow camera permission.

For iPad testing, use stable lighting and keep one hand clearly within the front-camera view. The avatar mapping may need the Inspector calibration described later in this README.

---

## Project Structure

```text
Assets/
├── ASET/
│   ├── _MyGameScenes/
│   ├── _Script/
│   └── data/
├── Scripts/
├── Thesis/
├── MediaPipeUnity/
├── CardModeScene.unity
└── other Unity assets

Study_Materials/
└── Videos/
    ├── experiment recordings
    └── post-task interview recordings

Packages/
├── manifest.json
└── packages-lock.json

ProjectSettings/
└── Unity project settings
```

Important implementation areas include:

```text
Assets/Scripts/
Assets/Thesis/
Assets/ASET/_MyGameScenes/
Assets/ASET/_Script/
```

## Thesis Materials

The core MSc thesis-specific content is organised under:

```text
Assets/Thesis/
```

This is the main starting point for reviewing the thesis implementation. It contains the thesis-oriented project materials, including the core prototype configuration, module-specific resources, supporting documentation, implementation notes, and other assets used by the current HealHand thesis prototype.

The broader Unity project also contains shared scripts, MediaPipe integration, scenes, and legacy prototype components outside this folder. However, readers who want to understand the thesis-specific work should begin with `Assets/Thesis/`.

---

## Key Scripts

Examples of central scripts include:

```text
HandGestureRecognizer.cs
HandResultBinder.cs
HandOverlayController.cs
HandRigController.cs
PalmAndIndexEndpointRetarget.cs
QRScanner.cs
PracticeModeController.cs
AssessmentModeController.cs
DailyModeController.cs
AdaptiveDifficultyController.cs
```

### Hand Avatar Mapping

The rigged hand/avatar position is controlled through:

```text
HandOverlayController.cs
```

The avatar mapping may require manual Inspector calibration when switching between desktop and iPad builds because camera orientation and display mapping can differ across platforms.

For the current iPad build, the tested working settings are:

```text
Invert Position X: true
Invert Position Y: false
Invert Rotation: true
```

---

## Adaptive Difficulty

The adaptive controller uses recent task performance to decide whether the next task should become more demanding.

Difficulty may influence parameters such as:

- Required hold duration
- Gesture confidence thresholds
- Matching tolerance
- Assistance intensity
- Task sequence demand

The final evaluated version only supports:

```text
Increase difficulty
or
Keep current difficulty
```

It does not automatically reduce difficulty.

---

## Legacy Components

The project evolved from an earlier QR-card-driven prototype.

Some legacy Card Mode or Random Mode components remain in the repository for compatibility and development history, but the main thesis workflow focuses on:

```text
Practice
Assessment
Daily
Story
```

The Practice module is the primary module used for the formal fixed-versus-adaptive evaluation.

---

## Known Limitations

- Hand tracking is sensitive to lighting, camera placement, and partial finger occlusion.
- The sensing pipeline is based mainly on 2D hand landmarks, so some depth-dependent hand poses can be harder to distinguish reliably.
- The current prototype is designed for one active hand and one user at a time.
- Gesture recognition may vary across users and device orientations.
- The project is a research prototype for technical evaluation and should not be interpreted as a certified clinical rehabilitation system.

---

## Restricted Study Materials and Repository Exclusions

### Restricted materials included in this private repository

The following thesis review materials are stored under:

```text
Study_Materials/Videos/
```

- Experiment recordings
- Post-task interview recordings

These files are restricted to thesis supervision and examination. They must not be redistributed or made publicly available.

### Materials intentionally excluded

The repository intentionally excludes:

- Participant CSV data and exported formal logs
- Consent forms and participant contact details
- Raw camera recordings outside the selected thesis-review videos
- Build folders
- Unity cache folders
- Local crash logs
- Other private administrative study material

---

## Thesis Context

This project supports an MSc thesis focused on:

```text
Privacy-preserving avatar-based hand rehabilitation interaction
Interpretable movement-quality feedback
Adaptive task difficulty compared with a fixed baseline
```

The main formal evaluation focuses on the Practice module.

---

## Author

Ziming Wang  
MSc Thesis, DTU Compute  
2026
