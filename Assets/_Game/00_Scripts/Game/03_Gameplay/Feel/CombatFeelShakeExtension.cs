using Cinemachine;
using UnityEngine;

/// <summary>
/// Added at runtime to every Cinemachine virtual camera by
/// <see cref="CombatFeelRunner"/>; adds the current shake offset after the
/// noise stage so it composes with whatever the vcam is doing.
/// </summary>
[AddComponentMenu("")]
public class CombatFeelShakeExtension : CinemachineExtension
{
    protected override void PostPipelineStageCallback(
        CinemachineVirtualCameraBase vcam,
        CinemachineCore.Stage stage,
        ref CameraState state,
        float deltaTime)
    {
        if (stage != CinemachineCore.Stage.Noise)
            return;

        CombatFeelRunner runner = CombatFeel.Current;

        if (runner != null)
            state.PositionCorrection += runner.ShakeOffset;
    }
}
