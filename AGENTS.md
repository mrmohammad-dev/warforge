# WarForge — Instructions for AI Agents

## Mission
Help build WarForge, an original vehicle-combat game, in small, reliable, testable steps. The first target is a playable Android tank prototype.

## Before changing code
1. Read this file, README.md, and the relevant documents in docs/.
2. Inspect the existing project and current Git status.
3. Understand the current milestone before proposing changes.
4. Avoid overwriting existing work without inspecting it first.

## Technical direction
- Target: Android, landscape orientation.
- Engine: Unity 6.
- Rendering: Universal Render Pipeline (URP).
- Gameplay code: C#.
- Initial scope: one controllable tank, camera, turret, shooting, test arena, and one target.
- Keep gameplay systems modular and understandable.

## Collaboration rules
- Each task must have a clear scope and acceptance criteria.
- Do not have multiple agents edit the same files simultaneously.
- Prefer focused changes over large rewrites.
- Explain which files changed and why.
- Include setup and testing instructions.
- Report untested behavior honestly.
- Do not invent test results, build results, or completed features.
- Do not add dependencies without explaining why they are needed.
- Do not commit generated build output, temporary files, or secrets.

## Quality rules
- Use clear, consistent C# naming and formatting.
- Avoid unnecessary complexity and premature multiplayer architecture.
- Handle invalid states and missing references safely.
- Keep mobile performance in mind.
- Test changes in Unity when an appropriate environment is available.
- If a build or test cannot be run, state that limitation explicitly.

## Definition of done
A task is complete only when its acceptance criteria are addressed, the changed files are identified, and available tests have been run or the missing test environment is clearly reported.
