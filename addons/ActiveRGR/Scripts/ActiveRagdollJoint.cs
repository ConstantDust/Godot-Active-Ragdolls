using Godot;

[Tool]
public partial class ActiveRagdollJoint : Generic6DofJoint3D
{
    [Export] public NodePath AnimationSkeletonPath;
    private Skeleton3D _targetSkeleton;
    [Export] public int BoneAIndex = -1;
    [Export] public int BoneBIndex = -1;
    [Export] public float MatchingVelocityMultiplier = 1;

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
                _targetSkeleton = GetNode<Skeleton3D>(AnimationSkeletonPath);
                if (_targetSkeleton != null)
                {
                    TraceSkeleton(true);
                }
                else
                {
                    TraceSkeleton(false);
                }

                if (BoneAIndex < 0)
                {
                    var nodeA = GetNode("nodes/node_A") as RagdollBone;
                    BoneAIndex = nodeA.BoneIndex;
                }

                if (BoneBIndex < 0)
                {
                    var nodeB = GetNode("nodes/node_b") as RagdollBone;
                    BoneBIndex = nodeB.BoneIndex;
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
            var targetRotation = _targetSkeleton.GetBoneGlobalPose(BoneBIndex).Basis.Inverse() * GetParent<Skeleton3D>().GetBoneGlobalPose(BoneBIndex).Basis;
            var targetVelocity = targetRotation.GetEuler() * MatchingVelocityMultiplier;

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

