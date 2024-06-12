using Godot;

[Tool]
public partial class RagdollBone : RigidBody3D
{
    [Export] public string BoneName;
    public int BoneIndex = -1;

    public override void _Ready()
    {
        if (Engine.IsEditorHint())
        {
            SetPhysicsProcess(false);
        }
        else
        {
            var parent = GetParent();
            if (parent is Skeleton3D skeleton)
            {
                if (!string.IsNullOrEmpty(BoneName))
                {
                    BoneIndex = skeleton.FindBone(BoneName);
                    if (BoneIndex < 0)
                    {
                        GD.PrintErr($"The Ragdoll Bone [{Name}] bone name [{BoneName}] does not match any bone in the skeleton");
                    }
                }
                else
                {
                    GD.PrintErr($"The Ragdoll Bone [{Name}] needs to have its bone name defined");
                }
            }
            else
            {
                GD.PrintErr($"The Ragdoll Bone [{Name}] is supposed to be a child of a Skeleton3D");
            }
        }
    }

    public override void _PhysicsProcess(double delta)
    {
        var parent = GetParent<Skeleton3D>();
        if (parent != null)
        {
            var boneGlobalRotation = parent.GlobalTransform.Basis * parent.GetBoneGlobalPose(BoneIndex).Basis;
            var b2TRotation = boneGlobalRotation.Inverse() * Transform.Basis;
            Basis transform = parent.GetBonePose(BoneIndex).Basis * b2TRotation;
            parent.SetBonePoseRotation(BoneIndex, transform.GetRotationQuaternion());
            parent.SetBonePoseScale(BoneIndex, transform.Scale);
        }
    }
}