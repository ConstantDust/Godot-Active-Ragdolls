using Godot;


public partial class ActiveRagdollJoint : Generic6DofJoint3D
{
    [Export] public Skeleton3D ParentSkeleton;
    [Export] public Skeleton3D TargetSkeleton;
    
    [Export] public RagdollBone BoneA;
    [Export] public RagdollBone BoneB;
    
    [Export] public int BoneAIndex = -1;
    [Export] public int BoneBIndex = -1;
    
    [Export] public float MatchingVelocityMultiplier = 1f;

    public override void _Ready()
    {
        // DeclareFlagForAllAxis(Flag.EnableAngularLimit, true);
        // DeclareParamForAllAxis(Param.AngularForceLimit, 25000f);
        
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
        BoneA.UpdateBonePhysics(TargetSkeleton, (float)delta, BoneA);
        
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

