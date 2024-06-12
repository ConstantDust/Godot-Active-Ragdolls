using Godot;

[Tool]
public partial class ActiveRagdollJoint : Generic6DofJoint3D
{
    [Export] public Skeleton3D ParentSkeleton;
    [Export] public Skeleton3D TargetSkeleton;
    [Export] public int BoneAIndex = -1;
    [Export] public int BoneBIndex = -1;
    [Export] public float MatchingVelocityMultiplier = 1f;

    public override void _Ready()
    {
        DeclareFlagForAllAxis(Flag.EnableAngularLimit, false);
        DeclareFlagForAllAxis(Flag.EnableMotor, true);
        
        if (!Engine.IsEditorHint())
        {
            SetParamX(Param.AngularMotorForceLimit, 1);
            SetParamY(Param.AngularMotorForceLimit, 1);
            SetParamZ(Param.AngularMotorForceLimit, 1);
            
            SetPhysicsProcess(TargetSkeleton != null);
            
            if (BoneAIndex < 0)
            {
                var nodeA = TargetSkeleton!.GetNode("node_A") as RagdollBone;
                BoneAIndex = nodeA!.BoneIndex;
            }

            if (BoneBIndex < 0)
            {
                var nodeB = TargetSkeleton!.GetNode("node_b") as RagdollBone;
                BoneBIndex = nodeB!.BoneIndex;
            }
        }
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

