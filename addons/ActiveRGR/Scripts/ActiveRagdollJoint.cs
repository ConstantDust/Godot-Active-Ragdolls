using Godot;


public partial class ActiveRagdollJoint : Generic6DofJoint3D
{
    [Export] public Skeleton3D ParentSkeleton;
    [Export] public Skeleton3D TargetSkeleton;
    
    [Export] public RagdollBone BoneA;
    [Export] public RagdollBone BoneB;
    
    [Export] public float Stiffness = 0f;
        
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
        // Steps for a (hopefully) goodd ragdoll
        // X // 1. get the local rotation of the animation skeleton
        // X // 2. get the local rotation of the physics state skeleton
        // 3. find the offset between the two
        // 4. apply forces to rotate the bone into the correct direction
        
        // (Note) i should probably also apply a linear force to help push bones into the correct direction
        // for this use Rigidbody.ApplyForce() because linear motor isn't implemented or some bs
        
        // Gets the b bone relative to a
        Transform3D animationBoneTransform = TargetSkeleton.GetBonePose(BoneBIndex);
        Transform3D physicsBoneTransform = ParentSkeleton.GetBonePose(BoneBIndex);

        Basis rotationOffset = animationBoneTransform.Basis.Inverse() * physicsBoneTransform.Basis;
        
        
        SetParamX(Param.AngularMotorTargetVelocity, rotationOffset.GetEuler().X * Stiffness);
        SetParamY(Param.AngularMotorTargetVelocity, rotationOffset.GetEuler().Y * Stiffness);
        SetParamZ(Param.AngularMotorTargetVelocity, rotationOffset.GetEuler().Z * Stiffness);
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

