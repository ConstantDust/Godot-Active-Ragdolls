using Godot;
using System;

public partial class AnimationPlayer : Godot.AnimationPlayer
{
    public override void _Process(double delta)
    {
        CurrentAnimation = "run";
    }
}
