using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Godot;

namespace Druidmorph.Combat;

[GlobalClass]
public abstract partial class CombatAction : Node
{
    public virtual bool IsAvailable => true;

    protected CombatEntity owner;

    protected PackedScene bloodEffect = GD.Load<PackedScene>("res://scenes/Effects/oneshot_particle_cast_blood.tscn");
    

    [Export]
    public virtual PackedScene ActionHitEffect { get; protected set; } = GD.Load<PackedScene>("res://scenes/Effects/oneshot_particle_hit_default.tscn");
    [Export]
    public virtual PackedScene ActionCastEffect { get; protected set; } = GD.Load<PackedScene>("res://scenes/Effects/oneshot_particle_cast_default.tscn");

    [Export] public Marker2D[] originPoints;

    // To make debugging specific actions easier
    [Export] public bool actionIsEnabled = true;
    [Export(PropertyHint.MultilineText)] protected string customActionDescription = "";

    public virtual void Perform(CombatController combatController)
    {
        _ = owner.animPlayer.PlayAnim(Name);
        LogAction();
    }
    
    public virtual void SpawnCastEffect()
    {
        PlayActionSound();
        if (originPoints.Length == 0) return;
        
        
        foreach(var point in originPoints)
        {
            
            var pos = owner.Body.BodyParts.FirstOrDefault(e => e.effectPosition == point);
            if(pos is not null && pos.IsAlive)
            {
                var cast = ActionCastEffect.Instantiate<Node2D>();
                pos.effectPosition.AddChild(cast);
            }

        }
        
    }

    protected void CreateParticlesAtBodyPart(PackedScene particles, BodyPart target)
    {
        if (target is null || target.effectPosition is null || particles is null)
        {
            return;
        }
        if (!GodotObject.IsInstanceValid(target.effectPosition))
            return;
        
        var effect = particles.Instantiate<Node2D>();
        target.effectPosition.AddChild(effect);
    }

    public virtual void LogAction()
    {
        var log = $"{owner.Name} uses {Name} -Action!";
        if(customActionDescription != "")
        {
            log = customActionDescription;
        }
        CombatScene.Instance.WriteLogs(log);
    }

    
    public virtual void OnAnimationSequenceFinished(string animName)
    {
        if(Name == animName)
        {
           _= owner.SignalActionFinished();
        }
    }

    public void SetOwner(CombatEntity newOwner)
    {
        owner = newOwner;
    }
    protected void PlayActionSound()
    {
        if(owner.SoundProfile is null) return;

        // Random choise of audio from correct keys
        var matches = new List<AudioEntry>();
        foreach(var entry in owner.SoundProfile.Entries)
        {
            if(entry is null) continue;
            if(entry.Audio is null) continue;

            if(entry.Key == Name) matches.Add(entry);
        }
        if(matches.Count == 0) return;
        int index = (int)GD.RandRange(0, matches.Count - 1);
        SoundManager.PlaySound(matches[index]);
    }
}
