
using UnityEngine;

// Straight projectile with optional lateral sine oscillation (SineBullet pattern).
// Amplitude/frequency are (re)applied by the spawner on every spawn via SetSine.
public class SineProjectile : Projectile
{
    private float amplitude;
    private float frequency;
    private float phase;
    private float currentOffset;

    public void SetSine(float newAmplitude, float newFrequency)
    {
        amplitude = Mathf.Max(0f, newAmplitude);
        frequency = Mathf.Max(0f, newFrequency);
        phase = 0f;
        currentOffset = 0f;
    }

    protected override void FixedUpdate()
    {
        float previousOffset = currentOffset;

        if (amplitude > 0f && frequency > 0f)
        {
            phase += frequency * 2f * Mathf.PI * Time.fixedDeltaTime;
            currentOffset = Mathf.Sin(phase) * amplitude;
        }
        else
        {
            currentOffset = 0f;
        }

        base.FixedUpdate();

        // Apply only this frame's delta so base movement stays authoritative
        // and the offset never double-counts across frames.
        transform.position += transform.right * (currentOffset - previousOffset);
    }
}
