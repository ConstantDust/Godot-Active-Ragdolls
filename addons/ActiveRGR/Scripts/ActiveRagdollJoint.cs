using Godot;


public partial class ActiveRagdollJoint : Generic6DofJoint3D
{
    [Export] public Skeleton3D ParentSkeleton;
    [Export] public Skeleton3D TargetSkeleton;
    
    [Export] public RagdollBone BoneA;
    [Export] public RagdollBone BoneB;
    
    [Export] public float Stiffness = 2f;
        
    [Export] public int BoneAIndex = -1;
    [Export] public int BoneBIndex = -1;
    
    [Export] public float MatchingVelocityMultiplier = 1f;

    public override void _Ready()
    {
        DeclareFlagForAllAxis(Flag.EnableAngularLimit, false);
        DeclareParamForAllAxis(Param.AngularMotorForceLimit, 9999999);
        DeclareFlagForAllAxis(Flag.EnableMotor, true);
        
        if (!Engine.IsEditorHint())
        {
            
            if (BoneAIndex < 0)
            {
                var boneA = TargetSkeleton!.GetNode(NodeA) as RagdollBone;
                BoneAIndex = boneA!.BoneIndex;
            }

            if (BoneBIndex < 0)
            {
                var boneB = TargetSkeleton!.GetNode(NodeB) as RagdollBone;
                BoneBIndex = boneB!.BoneIndex;
            }
            
            SetPhysicsProcess(TargetSkeleton != null && BoneA != null && BoneB != null);
        }
    }

    public override void _PhysicsProcess(double delta)
    {
        if (Engine.IsEditorHint()) return;
        
        Basis targetVelocity = TargetSkeleton.GetBoneGlobalPose(BoneBIndex).Basis.Inverse() * ParentSkeleton.GetBoneGlobalPose(BoneBIndex).Basis;
        
        // Steps for a (hopefully) goodd ragdoll
        // 1. get the local rotation of the animation skeleton
        // 2. get the local rotation of the physics state skeleton
        // 3. find the offset between the two
        // 4. apply forces to rotate the bone into the correct direction
        
        // (Note) i should probably also apply a linear force to help push bones into the correct direction
        // for this use Rigidbody.ApplyForce() because linear motor isn't implemented or some bs
        
        SetParamX(Param.AngularMotorTargetVelocity, targetVelocity.GetEuler().X * Stiffness * BoneA.Mass);
        SetParamY(Param.AngularMotorTargetVelocity, targetVelocity.GetEuler().Y * Stiffness * BoneA.Mass);
        SetParamZ(Param.AngularMotorTargetVelocity, targetVelocity.GetEuler().Z * Stiffness * BoneA.Mass);
    }

    private void DeclareFlagForAllAxis(Flag param, bool value)
    {
        SetFlagX(param, value);
        SetFlagY(param, value);
        SetFlagZ(param, value);
    }

    private void DeclareParamForAllAxis(Param param, float value)
    {
        SetParamX(param, value);
        SetParamY(param, value);
        SetParamZ(param, value);
    }
}

