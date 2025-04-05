# Multimesh-Blendshape-Animation-Creation

## Overview
The **Blendshape Animation Creator** is a Unity Editor tool that allows developers to create animation clips for blendshapes in 3D models. This tool simplifies the process of animating blendshapes by generating two separate animation clips: one for setting the blendshape weight to 0 and another for setting it to 100. The tool also supports searching for blendshape names with various naming conventions.

## Features
- **Create Animation Clips**: Automatically generates animation clips for specified blendshapes.
- **Support for Variations**: Searches for blendshapes with different naming conventions (e.g., "Breast_big", "Breast Big", "Big Breast").
- **Non-Legacy Clips**: Creates non-legacy animation clips compatible with Unity's Animator and Blend Trees.

## Installation
1. **Clone the Repository**:
   ```bash
   git clone https://github.com/yourusername/blendshape-animation-creator.git
   cd blendshape-animation-creator
   ```

2. **Add to Unity Project**:
   - Copy the `BlendshapeAnimationCreator.cs` script into the `Assets/Editor` folder of your Unity project.

## Usage
1. Open Unity and navigate to the menu: **Tools > Blendshape Animation Creator**.
2. Enter the desired blendshape name (e.g., `Breast_big`).
3. Specify the save path for the animations (e.g., `Assets/Animations`).
4. Set the animation name prefix (e.g., `BlendshapeAnimation`).
5. Click **Create Animations** to generate the animation clips.

## Example
If you enter `Breast_big` as the blendshape name, the tool will create:
- `BlendshapeAnimation_0.anim`: Sets the blendshape weight to 0.
- `BlendshapeAnimation_100.anim`: Sets the blendshape weight to 100.

## Code Snippet
Here’s a brief look at the core functionality of the script:

```csharp
private void CreateAnimation(float weight)
{
    AnimationClip clip = new AnimationClip();
    clip.legacy = false; // Ensure the clip is non-legacy

    // Find all SkinnedMeshRenderers in the scene
    SkinnedMeshRenderer[] skinnedMeshRenderers = FindObjectsOfType<SkinnedMeshRenderer>();
    
    foreach (var renderer in skinnedMeshRenderers)
    {
        // Check for blendshape variations
        List<string> blendshapeVariations = GetBlendshapeVariations(blendshapeName);
        foreach (var variation in blendshapeVariations)
        {
            int blendShapeIndex = renderer.sharedMesh.GetBlendShapeIndex(variation);
            if (blendShapeIndex != -1)
            {
                AnimationCurve curve = AnimationCurve.Linear(0, weight, 1, weight);
                // Bind the curve to the SkinnedMeshRenderer
                EditorCurveBinding binding = new EditorCurveBinding { /* binding setup */ };
                AnimationUtility.SetEditorCurve(clip, binding, curve);
            }
        }
    }
}
```

## Contributing
Contributions are welcome! If you have suggestions for improvements or new features, please open an issue or submit a pull request.

## License
This project is licensed under the MIT License. See the LICENSE file for details.
