using UnityEngine;

// Φτιάχνει σκόνη που αιωρείται στον αέρα μέσω κώδικα 
public class AmbientParticles : MonoBehaviour
{
    [Header("Settings")]
    public int particleCount = 60;
    public float spawnRadius = 6f;
    public float particleSpeed = 0.15f;
    public float particleSize = 0.02f;
    public Color particleColor = new Color(0.8f, 0.9f, 1f, 0.2f);

    private ParticleSystem ps;

    private void Start()
    {
        CreateParticleSystem();
    }

    // Χτίζει το Particle System και του περνάει ρυθμίσεις κατευθείαν
    private void CreateParticleSystem()
    {
        GameObject particleObj = new GameObject("ICU_AmbientDust");
        particleObj.transform.SetParent(transform);
        particleObj.transform.localPosition = Vector3.up * 2f;

        ps = particleObj.AddComponent<ParticleSystem>();
        ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

        var main = ps.main;
        main.maxParticles = particleCount;
        main.startLifetime = 12f;
        main.startSpeed = particleSpeed;
        main.startSize = particleSize;
        main.startColor = particleColor;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.gravityModifier = -0.01f;
        main.loop = true;

        var emission = ps.emission;
        emission.rateOverTime = particleCount / 8f;

        var shape = ps.shape;
        shape.shapeType = ParticleSystemShapeType.Sphere;
        shape.radius = spawnRadius;

        var velocityOverLifetime = ps.velocityOverLifetime;
        velocityOverLifetime.enabled = true;
        velocityOverLifetime.x = new ParticleSystem.MinMaxCurve(-0.05f, 0.05f);
        velocityOverLifetime.y = new ParticleSystem.MinMaxCurve(0.01f, 0.04f);
        velocityOverLifetime.z = new ParticleSystem.MinMaxCurve(-0.05f, 0.05f);

        var colorOverLifetime = ps.colorOverLifetime;
        colorOverLifetime.enabled = true;
        Gradient grad = new Gradient();
        grad.SetKeys(
            new GradientColorKey[] {
                new GradientColorKey(Color.white, 0f),
                new GradientColorKey(Color.white, 1f)
            },
            new GradientAlphaKey[] {
                new GradientAlphaKey(0f, 0f),
                new GradientAlphaKey(0.3f, 0.2f),
                new GradientAlphaKey(0.3f, 0.8f),
                new GradientAlphaKey(0f, 1f)
            }
        );
        colorOverLifetime.color = grad;

        var sizeOverLifetime = ps.sizeOverLifetime;
        sizeOverLifetime.enabled = true;
        sizeOverLifetime.size = new ParticleSystem.MinMaxCurve(1f, new AnimationCurve(
            new Keyframe(0f, 0.5f),
            new Keyframe(0.5f, 1f),
            new Keyframe(1f, 0.3f)
        ));

        var renderer = particleObj.GetComponent<ParticleSystemRenderer>();
        renderer.material = new Material(Shader.Find("Particles/Standard Unlit"));
        renderer.material.color = particleColor;

        ps.Play();
    }

    private void Update()
    {
        // Ακολουθεί την κάμερα για να έχουμε πάντα σκόνη μπροστά μας
        if (ps == null || Camera.main == null) return;
        ps.transform.position = Camera.main.transform.position;
    }
}