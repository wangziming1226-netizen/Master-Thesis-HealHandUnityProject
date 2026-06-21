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
- Participant consent forms, interview recordings, formal study data, and identifiable material are excluded from this repository.

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

- Unity 6.x
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
4. Open the project using a compatible Unity 6.x version.
5. Allow Unity to restore the dependencies defined in `Packages/manifest.json`.

### 3. Run in the Unity Editor

1. Open the main project scene from Unity Build Settings or from:

```text
Assets/ASET/_MyGameScenes/
```

2. Confirm that MediaPipe hand tracking is available.
3. Confirm that camera access is enabled.
4. Press **Play** in the Unity Editor.
5. Select one of the available modules: Practice, Assessment, Daily, or Story.

### 4. Build for iPad

1. In Unity, open **File → Build Settings**.
2. Select **iOS**.
3. Click **Switch Platform**.
4. Confirm that the intended main scene is included in **Scenes In Build**.
5. Click **Build**.
6. Open the generated Xcode project.
7. Configure signing for the target iPad.
8. Deploy the project to the device.

For iPad testing, camera permission must be enabled and the device should be used under stable lighting conditions.

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

## Repository Exclusions

The repository intentionally excludes:

- Participant CSV data
- Exported formal logs
- Interview audio or video
- Consent forms
- Raw camera recordings
- Build folders
- Unity cache folders
- Local crash logs
- Private study material

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
