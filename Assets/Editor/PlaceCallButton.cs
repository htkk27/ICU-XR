using UnityEngine;
using UnityEditor;

public class PlaceCallButton : EditorWindow
{
    [MenuItem("Tools/Τοποθέτηση Call Button")]
    static void Place()
    {
        GameObject old = GameObject.Find("CallButton");
        if (old != null)
        {
            DestroyImmediate(old);
        }

        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/OT Room/redcallbuttom/export3dcoat.obj");
        if (prefab == null)
        {
            EditorUtility.DisplayDialog("Σφάλμα", "Δεν βρέθηκε το μοντέλο στο Assets/OT Room/redcallbuttom/export3dcoat.obj", "OK");
            return;
        }

        GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
        instance.name = "CallButton";

        instance.transform.localScale = Vector3.one * 0.0012f;

        GameObject surgicalTool = GameObject.Find("SuricalTool Variant");
        if (surgicalTool != null)
            instance.transform.position = surgicalTool.transform.position + new Vector3(0f, 0.35f, 0f);
        else
            instance.transform.position = new Vector3(0.103f, 1.1f, 1.058f);

        // Εφαρμογή materials με textures
        ApplyMaterials(instance);

        // Collider
        BoxCollider col = instance.AddComponent<BoxCollider>();
        Renderer r = instance.GetComponentInChildren<Renderer>();
        if (r != null)
        {
            Bounds b = r.bounds;
            col.center = instance.transform.InverseTransformPoint(b.center);
            Vector3 s = instance.transform.InverseTransformVector(b.size);
            col.size = new Vector3(Mathf.Abs(s.x), Mathf.Abs(s.y), Mathf.Abs(s.z));
        }

        Selection.activeGameObject = instance;
        EditorUtility.SetDirty(instance);
        EditorUtility.DisplayDialog("Επιτυχία",
            "Το CallButton τοποθετήθηκε!\n\n" +
            "1. Add Component → CallButtonHotspot\n" +
            "2. Αν είναι μεγάλο/μικρό, άλλαξε Scale\n" +
            "3. Ctrl+S για αποθήκευση", "OK");
    }

    static void ApplyMaterials(GameObject obj)
    {
        string basePath = "Assets/OT Room/redcallbuttom/";

        Texture2D colorTex = AssetDatabase.LoadAssetAtPath<Texture2D>(basePath + "export3dcoat__color.tga");
        Texture2D metalTex = AssetDatabase.LoadAssetAtPath<Texture2D>(basePath + "export3dcoat__metalness.tga");
        Texture2D normalTex = AssetDatabase.LoadAssetAtPath<Texture2D>(basePath + "export3dcoat__nmap.tga");
        Texture2D glossTex = AssetDatabase.LoadAssetAtPath<Texture2D>(basePath + "export3dcoat__gloss.tga");

        Renderer[] renderers = obj.GetComponentsInChildren<Renderer>();
        foreach (Renderer rend in renderers)
        {
            Material mat = new Material(Shader.Find("Standard"));

            if (colorTex != null)
                mat.SetTexture("_MainTex", colorTex);

            if (metalTex != null)
            {
                mat.SetTexture("_MetallicGlossMap", metalTex);
                mat.SetFloat("_Metallic", 1f);
            }

            if (normalTex != null)
            {
                mat.SetTexture("_BumpMap", normalTex);
                mat.EnableKeyword("_NORMALMAP");
            }

            if (glossTex != null)
            {
                mat.SetFloat("_Glossiness", 0.7f);
                mat.SetFloat("_GlossMapScale", 0.7f);
            }

            rend.sharedMaterial = mat;
        }
    }
}