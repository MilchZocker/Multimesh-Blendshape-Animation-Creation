using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using System.IO;
using System.Collections.Generic;

public class BlendshapeAnimationCreator : EditorWindow
{
    private string blendshapeName = "example"; // Default blendshape name
    private string savePath = "Assets/Animations";   // Default save path for animations
    private string animationNamePrefix = "BlendshapeAnimation"; // Default animation name prefix

    [MenuItem("Tools/Blendshape Animation Creator")]
    public static void ShowWindow()
    {
        GetWindow<BlendshapeAnimationCreator>("Blendshape Animation Creator");
    }

    private void OnGUI()
    {
        GUILayout.Label("Blendshape Animation Creator", EditorStyles.boldLabel);

        blendshapeName = EditorGUILayout.TextField("Blendshape Name", blendshapeName);
        savePath = EditorGUILayout.TextField("Save Path", savePath);
        animationNamePrefix = EditorGUILayout.TextField("Animation Name Prefix", animationNamePrefix);

        if (GUILayout.Button("Create Animations"))
        {
            CreateAnimations();
        }
    }

    private void CreateAnimations()
    {
        if (string.IsNullOrEmpty(blendshapeName))
        {
            Debug.LogError("Blendshape name cannot be empty.");
            return;
        }

        if (string.IsNullOrEmpty(savePath) || !savePath.StartsWith("Assets"))
        {
            Debug.LogError("Save path must start with 'Assets/' and cannot be empty.");
            return;
        }

        if (string.IsNullOrEmpty(animationNamePrefix))
        {
            Debug.LogError("Animation name prefix cannot be empty.");
            return;
        }

        // Ensure the save path exists
        if (!Directory.Exists(savePath))
        {
            Directory.CreateDirectory(savePath);
            AssetDatabase.Refresh(); // Refresh the AssetDatabase to recognize the new directory
        }

        // Create two animation clips: one for 0 and one for 100
        CreateAnimation(0);   // Animation for weight 0
        CreateAnimation(100); // Animation for weight 100

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log("Animations created successfully.");
    }

    private void CreateAnimation(float weight)
    {
        // Create a new animation clip
        AnimationClip clip = new AnimationClip();

        // Set the clip to be non-legacy (Mecanim-compatible)
        clip.legacy = false;

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
                    // Create animation curve for this blendshape
                    AnimationCurve curve = AnimationCurve.Linear(0, weight, 1, weight); // Constant weight over 1 second
                    string propertyName = $"blendShape.{variation}";

                    // Bind the curve to the SkinnedMeshRenderer
                    EditorCurveBinding binding = new EditorCurveBinding
                    {
                        path = AnimationUtility.CalculateTransformPath(renderer.transform, renderer.rootBone), // Path from root
                        type = typeof(SkinnedMeshRenderer),
                        propertyName = propertyName
                    };

                    AnimationUtility.SetEditorCurve(clip, binding, curve);
                    Debug.Log($"Animation curve set for '{variation}' on '{renderer.name}'");
                }
                else
                {
                    Debug.LogWarning($"Blendshape '{variation}' not found on {renderer.name}.");
                }
            }
        }

        // Save the animation clip
        string animationName = $"{animationNamePrefix}_{weight}.anim";
        string fullPath = $"{savePath}/{animationName}";

        try
        {
            AssetDatabase.CreateAsset(clip, fullPath);
            Debug.Log($"Animation '{animationName}' created successfully at {fullPath}");
        }
        catch (UnityException e)
        {
            Debug.LogError($"Failed to create animation asset at {fullPath}: {e.Message}");
        }
    }

    private List<string> GetBlendshapeVariations(string baseName)
    {
        // Create a list of possible variations for the blendshape name
        List<string> variations = new List<string>
        {
            baseName,
            baseName.Replace("_", " "), // Replace underscores with spaces
            baseName.Replace("_", "").Replace(" ", "").ToLower(), // Remove spaces and underscores, lowercase
            baseName.Replace("_", "").Replace(" ", "").ToUpper(), // Remove spaces and underscores, uppercase
            baseName.Replace("_", " ").ToLower(), // Lowercase with spaces
            baseName.Replace("_", " ").ToUpper() // Uppercase with spaces
        };

        // Add more variations as needed
        variations.Add($"Big {baseName.Replace("_", " ")}");
        variations.Add($"{baseName.Replace("_", " ")} Big");
        variations.Add($"Big {baseName}");
        variations.Add($"{baseName} Big");

        return variations;
    }
}
