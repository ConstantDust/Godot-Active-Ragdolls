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
    
    public void UpdateBonePhysics(Skeleton3D targetSkeleton, float delta, RagdollBone parent)
    {
        //TODO: make this work with the local target
        
        // Gets the true global transform of the bone
        Transform3D target = targetSkeleton.GlobalTransform * targetSkeleton.GetBoneGlobalPose(BoneIndex);
        Transform3D current = GlobalTransform;
        
        // Set linear movement
        Vector3 positionOffset = target.Origin - current.Origin;
        Vector3 force = HookesLaw(positionOffset, LinearVelocity, 30f, 2f);
        LinearVelocity += force * delta;
            
        // Set angular movement
        Basis rotationOffset = (target.Basis * current.Basis.Inverse());
        Vector3 torque = HookesLaw(rotationOffset.GetEuler(), AngularVelocity, 500f, 5f);
        AngularVelocity += torque * delta;
        
        // GlobalTransform = target;
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

        // check is this is a root bone
        if (TargetSkeleton != null && ParentSkeleton.GetBoneParent(BoneIndex) < 0)
        {
            // Update here because root bones dont have a joint
            UpdateBonePhysics(TargetSkeleton, (float)delta, null);
        }
    }
    
    // function for hookes law with damping
    Vector3 HookesLaw(Vector3 offset, Vector3 velocity, float stiffness, float damping) => (stiffness * offset) - (damping * velocity);
}