using Godot;

[Tool]
public partial class ActiveRagdollJoint : Generic6DofJoint3D
{
    public Skeleton3D ParentSkeleton;
    public Skeleton3D TargetSkeleton;
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

            // TargetSkeleton = GetNode<Skeleton3D>(AnimationSkeletonPath);
            if (TargetSkeleton != null)
            {
                TraceSkeleton(true);
            }
            else
            {
                TraceSkeleton(false);
            }

            if (BoneAIndex < 0)
            {
                var nodeA = TargetSkeleton!.GetNode("node_A") as RagdollBone;
                BoneAIndex = nodeA.BoneIndex;
            }

            if (BoneBIndex < 0)
            {
                var nodeB = TargetSkeleton!.GetNode("node_b") as RagdollBone;
                BoneBIndex = nodeB.BoneIndex;
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
            var targetRotation = TargetSkeleton.GetBoneGlobalPose(BoneBIndex).Basis.Inverse() * ParentSkeleton.GetBoneGlobalPose(BoneBIndex).Basis;
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

