using Godot;

public partial class RagdollBone : RigidBody3D
{
    [Export] public string BoneName;
    [Export] public Skeleton3D ParentSkeleton;
    [Export] public Skeleton3D TargetSkeleton;
    public int BoneIndex = -1;

    public override void _Ready()
    {
        if (Engine.IsEditorHint())
        {
            SetPhysicsProcess(false);
        }
        else
        {
            if (ParentSkeleton != null)
            {
                if (!string.IsNullOrEmpty(BoneName))
                {
                    BoneIndex = ParentSkeleton.FindBone(BoneName);
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
        if (ParentSkeleton != null)
        {
            // Get the global transform of the bone in the skeleton
            Transform3D boneGlobalTransform = ParentSkeleton.GetBoneGlobalPose(BoneIndex);

            // Calculate the transform difference between the current global transform of the ragdoll bone and the skeleton bone's global transform
            Transform3D transformDifference = boneGlobalTransform.AffineInverse() * GlobalTransform;

            // Apply the transform difference to the bone's local pose
            Transform3D newBonePose = ParentSkeleton.GetBonePose(BoneIndex) * transformDifference;

            // Set the bone's new pose in the skeleton
            ParentSkeleton.SetBonePosePosition(BoneIndex, newBonePose.Origin);
            ParentSkeleton.SetBonePoseRotation(BoneIndex, newBonePose.Basis.GetRotationQuaternion());
            ParentSkeleton.SetBonePoseScale(BoneIndex, newBonePose.Basis.Scale);
        }
    }
    
    // function for hookes law with damping
    Vector3 HookesLaw(Vector3 offset, Vector3 velocity, float stiffness, float damping) => (stiffness * offset) - (damping * velocity);
}