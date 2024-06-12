using Godot;

[Tool]
public partial class ActiveRagdollJoint : Generic6DofJoint3D
{
    [Export] public NodePath animationSkeletonPath;
    private Skeleton3D targetSkeleton;
    [Export] public int boneAIndex = -1;
    [Export] public int boneBIndex = -1;
    [Export] public float matchingVelocityMultiplier = 1;

    public override void _Ready()
    {
        if (!Engine.IsEditorHint())
        {
            SetParamX(Param.AngularMotorForceLimit, 9999999);
            SetParamY(Param.AngularMotorForceLimit, 9999999);
            SetParamZ(Param.AngularMotorForceLimit, 9999999);

            var parent = GetParent();
            if (parent is Skeleton3D skeleton)
            {
                targetSkeleton = GetNode<Skeleton3D>(animationSkeletonPath);
                if (targetSkeleton != null)
                {
                    TraceSkeleton(true);
                }
                else
                {
                    TraceSkeleton(false);
                }

                if (boneAIndex < 0)
                {
                    var nodeA = GetNode("nodes/node_A") as RagdollBone;
                    boneAIndex = nodeA.BoneIndex;
                }

                if (boneBIndex < 0)
                {
                    var nodeB = GetNode("nodes/node_b") as RagdollBone;
                    boneBIndex = nodeB.BoneIndex;
                }
            }
            else
            {
                GD.PrintErr($"The Ragdoll Bone [{Name}] is supposed to be a child of a Skeleton3D");
            }
        }
    }

    public void TraceSkeleton(bool value)
    {
        SetPhysicsProcess(value);
        DeclareFlagForAllAxis(Flag.EnableAngularLimit, !value);
        DeclareFlagForAllAxis(Flag.EnableMotor, value);
    }

    public override void _PhysicsProcess(double delta)
    {
        if (!Engine.IsEditorHint())
        {
            var targetRotation = targetSkeleton.GetBoneGlobalPose(boneBIndex).Basis.Inverse() * GetParent<Skeleton3D>().GetBoneGlobalPose(boneBIndex).Basis;
            var targetVelocity = targetRotation.GetEuler() * matchingVelocityMultiplier;

            SetParamX(Param.AngularMotorTargetVelocity, targetVelocity.X);
            SetParamY(Param.AngularMotorTargetVelocity, targetVelocity.Y);
            SetParamZ(Param.AngularMotorTargetVelocity, targetVelocity.Z);
        }
    }

    private void DeclareFlagForAllAxis(Flag param, bool value)
    {
        SetFlagX(param, value);
        SetFlagY(param, value);
        SetFlagZ(param, value);
    }
}

