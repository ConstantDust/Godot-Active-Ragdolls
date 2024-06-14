using Godot;
using System;

public partial class AnimationPlayer : Godot.AnimationPlayer
{
    public override void _Ready()
    {
        CurrentAnimation = "idle";
    }

    public override void _Process(double delta)
    {
        if (!IsPlaying())
        {
            Play();
        }
    }
}
