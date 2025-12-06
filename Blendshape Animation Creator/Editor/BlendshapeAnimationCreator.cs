using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;

public class BlendshapeAnimationCreator : EditorWindow
{
    private string blendshapePattern = "Breast_Big*";   // Supports '*' wildcards
    private string savePath = "Assets/Animations";
    private string animationNamePrefix = "BlendshapeAnimation";

    private float startValue = 0f;
    private float endValue = 100f;
    private float duration = 1f;

    private GenerationMode generationMode = GenerationMode.SingleClip;
    private CurveMode curveMode = CurveMode.Linear;

    private bool caseInsensitive = true;
    private bool logPreviewOnly = false;

    private enum GenerationMode
    {
        SingleClip,         // one clip going Start -> End
        TwoConstantClips,   // two clips with constant Start and constant End
        PingPong            // Start -> End -> Start
    }

    private enum CurveMode
    {
        Linear,
        EaseInOut,
        Constant
    }

    [MenuItem("Tools/Blendshape Animation Creator")]
    public static void ShowWindow()
    {
        GetWindow<BlendshapeAnimationCreator>("Blendshape Animation Creator");
    }

    private void OnGUI()
    {
        GUILayout.Label("Blendshape Animation Creator", EditorStyles.boldLabel);

        blendshapePattern = EditorGUILayout.TextField(
            new GUIContent("Blendshape Pattern",
            "Use * as wildcard.\n" +
            "*Name = ends with\n" +
            "Name* = starts with\n" +
            "*Name* = contains\n" +
            "No * = exact match"),
            blendshapePattern);

        caseInsensitive = EditorGUILayout.Toggle(
            new GUIContent("Case Insensitive"), caseInsensitive);

        EditorGUILayout.Space(6);

        savePath = EditorGUILayout.TextField("Save Path", savePath);
        animationNamePrefix = EditorGUILayout.TextField("Animation Name Prefix", animationNamePrefix);

        EditorGUILayout.Space(8);
        GUILayout.Label("Value / Timing", EditorStyles.boldLabel);

        startValue = EditorGUILayout.Slider(
            new GUIContent("Start Value", "Blendshape weight at time 0"),
            startValue, 0f, 100f);

        endValue = EditorGUILayout.Slider(
            new GUIContent("End Value", "Blendshape weight at clip end"),
            endValue, 0f, 100f);

        duration = EditorGUILayout.FloatField(
            new GUIContent("Duration (sec)", "Clip length in seconds"),
            Mathf.Max(0.01f, duration));

        EditorGUILayout.Space(8);
        GUILayout.Label("Generation Options", EditorStyles.boldLabel);

        generationMode = (GenerationMode)EditorGUILayout.EnumPopup(
            new GUIContent("Generation Mode"), generationMode);

        curveMode = (CurveMode)EditorGUILayout.EnumPopup(
            new GUIContent("Curve Mode"), curveMode);

        logPreviewOnly = EditorGUILayout.Toggle(
            new GUIContent("Preview Matches Only", "If enabled, no clips are created; matches are logged."),
            logPreviewOnly);

        EditorGUILayout.Space(10);

        if (GUILayout.Button(logPreviewOnly ? "Preview Matches" : "Create Animations"))
        {
            CreateAnimations();
        }
    }

    private void CreateAnimations()
    {
        if (string.IsNullOrEmpty(blendshapePattern))
        {
            Debug.LogError("Blendshape pattern cannot be empty.");
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

        // Ensure folder exists
        if (!Directory.Exists(savePath))
        {
            Directory.CreateDirectory(savePath);
            AssetDatabase.Refresh();
        }

        // Find matches once (across all renderers)
        var renderers = FindObjectsOfType<SkinnedMeshRenderer>();
        if (renderers == null || renderers.Length == 0)
        {
            Debug.LogWarning("No SkinnedMeshRenderers found in the scene.");
            return;
        }

        int totalMatches = 0;
        foreach (var r in renderers)
        {
            totalMatches += GetMatchingBlendshapes(r, blendshapePattern).Count;
        }

        if (totalMatches == 0)
        {
            Debug.LogWarning($"No blendshapes matched pattern '{blendshapePattern}'.");
            return;
        }

        Debug.Log($"Pattern '{blendshapePattern}' matched {totalMatches} blendshape(s).");

        if (logPreviewOnly)
        {
            foreach (var r in renderers)
            {
                var matches = GetMatchingBlendshapes(r, blendshapePattern);
                foreach (var m in matches)
                    Debug.Log($"[Preview] {r.name} -> {m}");
            }
            return;
        }

        switch (generationMode)
        {
            case GenerationMode.SingleClip:
                CreateAnimationClip(startValue, endValue, duration, false);
                break;

            case GenerationMode.TwoConstantClips:
                CreateConstantClip(startValue);
                CreateConstantClip(endValue);
                break;

            case GenerationMode.PingPong:
                CreateAnimationClip(startValue, endValue, duration, true);
                break;
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("Animations created successfully.");
    }

    private void CreateConstantClip(float value)
    {
        AnimationClip clip = new AnimationClip { legacy = false };

        foreach (var r in FindObjectsOfType<SkinnedMeshRenderer>())
        {
            foreach (var blendshape in GetMatchingBlendshapes(r, blendshapePattern))
            {
                var curve = AnimationCurve.Linear(0f, value, duration, value);

                var binding = new EditorCurveBinding
                {
                    path = GetRendererPath(r),
                    type = typeof(SkinnedMeshRenderer),
                    propertyName = $"blendShape.{blendshape}"
                };

                AnimationUtility.SetEditorCurve(clip, binding, curve);
            }
        }

        SaveClip(clip, $"{animationNamePrefix}_{value:0.##}");
    }

    private void CreateAnimationClip(float start, float end, float time, bool pingPong)
    {
        AnimationClip clip = new AnimationClip { legacy = false };

        foreach (var r in FindObjectsOfType<SkinnedMeshRenderer>())
        {
            foreach (var blendshape in GetMatchingBlendshapes(r, blendshapePattern))
            {
                AnimationCurve curve;

                if (!pingPong)
                {
                    curve = BuildCurve(start, end, time);
                }
                else
                {
                    // Start -> End -> Start over full duration
                    float midT = time * 0.5f;
                    curve = new AnimationCurve(
                        new Keyframe(0f, start),
                        new Keyframe(midT, end),
                        new Keyframe(time, start)
                    );
                    ApplyCurveModeTangents(curve);
                }

                var binding = new EditorCurveBinding
                {
                    path = GetRendererPath(r),
                    type = typeof(SkinnedMeshRenderer),
                    propertyName = $"blendShape.{blendshape}"
                };

                AnimationUtility.SetEditorCurve(clip, binding, curve);
            }
        }

        string modeSuffix = pingPong ? "PingPong" : "Range";
        SaveClip(clip, $"{animationNamePrefix}_{modeSuffix}_{start:0.##}to{end:0.##}");
    }

    private AnimationCurve BuildCurve(float start, float end, float time)
    {
        AnimationCurve curve;

        switch (curveMode)
        {
            case CurveMode.Constant:
                curve = AnimationCurve.Linear(0f, start, time, start);
                break;

            case CurveMode.EaseInOut:
                curve = AnimationCurve.EaseInOut(0f, start, time, end);
                break;

            default:
                curve = AnimationCurve.Linear(0f, start, time, end);
                break;
        }

        ApplyCurveModeTangents(curve);
        return curve;
    }

    private void ApplyCurveModeTangents(AnimationCurve curve)
    {
        if (curveMode == CurveMode.EaseInOut || curve.length < 2)
            return;

        for (int i = 0; i < curve.length; i++)
        {
            AnimationUtility.SetKeyLeftTangentMode(curve, i, AnimationUtility.TangentMode.Linear);
            AnimationUtility.SetKeyRightTangentMode(curve, i, AnimationUtility.TangentMode.Linear);
        }
    }

    private void SaveClip(AnimationClip clip, string fileNameNoExt)
    {
        string fullPath = $"{savePath}/{fileNameNoExt}.anim";
        fullPath = AssetDatabase.GenerateUniqueAssetPath(fullPath);

        AssetDatabase.CreateAsset(clip, fullPath);
        Debug.Log($"Animation '{fileNameNoExt}' created at {fullPath}");
    }

    private string GetRendererPath(SkinnedMeshRenderer renderer)
    {
        // Use rootBone if present, otherwise fallback to transform root.
        Transform root = renderer.rootBone != null ? renderer.rootBone : renderer.transform.root;
        return AnimationUtility.CalculateTransformPath(renderer.transform, root);
    }

    private List<string> GetMatchingBlendshapes(SkinnedMeshRenderer renderer, string pattern)
    {
        var results = new List<string>();
        var mesh = renderer.sharedMesh;
        if (mesh == null) return results;

        Regex regex = WildcardToRegex(pattern, caseInsensitive);

        int count = mesh.blendShapeCount;
        for (int i = 0; i < count; i++)
        {
            string name = mesh.GetBlendShapeName(i);
            if (regex.IsMatch(name))
                results.Add(name);
        }

        return results;
    }

    private Regex WildcardToRegex(string pattern, bool insensitive)
    {
        // Escape regex special chars except '*'
        string escaped = Regex.Escape(pattern).Replace("\\*", ".*");
        string rx = $"^{escaped}$";  // whole-string match

        var opts = RegexOptions.Compiled;
        if (insensitive) opts |= RegexOptions.IgnoreCase;

        return new Regex(rx, opts);
    }
}
