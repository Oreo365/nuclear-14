// #Misfits Change /Tweak/ - Toggle heater ambience from active grill or hotplate state and play ignition audio on activation.
using Content.Server.Chemistry.Components;
using Content.Server.Power.Components;
using Content.Server.Temperature.Components;
using Content.Shared.Audio;
using Content.Shared.Sound;
using Content.Shared.Sound.Components;
using Content.Shared.Temperature;
using Content.Shared._Misfits.Sound;
using Robust.Server.Audio;

namespace Content.Server._Misfits.Sound;

/// <summary>
/// Keeps heater audio aligned with whether a grill, stove, or hotplate is
/// actively heating instead of merely being powered.
/// </summary>
public sealed class GrillHeaterSoundSystem : EntitySystem
{
    [Dependency] private readonly SharedAmbientSoundSystem _ambientSound = default!;
    [Dependency] private readonly SharedEmitSoundSystem _emitSound = default!;
    [Dependency] private readonly AudioSystem _audio = default!;

    // Misfits Fix: polling heater state at 20 Hz is wasteful — state changes are
    // user-driven and won't be missed at 2 Hz (every 0.5 s).
    private float _updateAccum;
    private const float UpdateInterval = 0.5f;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<GrillHeaterSoundComponent, ComponentStartup>(OnStartup);
    }

    private void OnStartup(EntityUid uid, GrillHeaterSoundComponent component, ComponentStartup args)
    {
        var active = IsActive(uid);
        component.LastActive = active;

        if (TryComp<AmbientSoundComponent>(uid, out var ambient))
            _ambientSound.SetAmbience(uid, active, ambient);

        if (TryComp<SpamEmitSoundComponent>(uid, out var spam))
            _emitSound.SetEnabled((uid, spam), active);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        // Misfits Fix: gate to 2 Hz — heater state changes are user-triggered, sub-second precision unneeded.
        _updateAccum += frameTime;
        if (_updateAccum < UpdateInterval)
            return;
        _updateAccum -= UpdateInterval;

        var query = EntityQueryEnumerator<GrillHeaterSoundComponent>();
        while (query.MoveNext(out var uid, out var sound))
        {
            var active = IsActive(uid);
            if (active == sound.LastActive)
                continue;

            sound.LastActive = active;

            if (TryComp<AmbientSoundComponent>(uid, out var ambient))
                _ambientSound.SetAmbience(uid, active, ambient);

            if (TryComp<SpamEmitSoundComponent>(uid, out var spam))
                _emitSound.SetEnabled((uid, spam), active);

            if (active)
                _audio.PlayPvs(sound.StartSound, uid);
            else if (sound.StopSound != null)
                _audio.PlayPvs(sound.StopSound, uid);
        }
    }

    private bool IsActive(EntityUid uid)
    {
        if (TryComp<EntityHeaterComponent>(uid, out var heater)
            && TryComp<ApcPowerReceiverComponent>(uid, out var power)
            && power.Powered)
        {
            return heater.Setting != EntityHeaterSetting.Off;
        }

        if (HasComp<ActiveSolutionHeaterComponent>(uid))
            return true;

        return false;
    }
}
