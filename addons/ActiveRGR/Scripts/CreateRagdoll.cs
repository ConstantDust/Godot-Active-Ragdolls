using Godot;
using Godot.Collections;

[Tool]
public partial class CreateRagdoll : Skeleton3D
{
    [Export] public bool CreateRigidbodies;
    [Export] public bool CreateJoints;
    [Export] public bool HaveDebugMeshes;

    [Export] public float BoneThickness = 0.1f;
    [Export(PropertyHint.Enum, "Skeleton,NodeA,NodeB")] 
    public ParentingType ParentingType = ParentingType.Skeleton;
    
    [Export] public Skeleton3D AnimationSkeleton;
    [Export] public string BoneWhitelist = "";
    private Array<int> _whitelist = new();

    [Signal] public delegate void TraceAnimationSkeletonEventHandler(bool value);

    public override void _Process(double delta)
    {
        if (CreateRigidbodies)
        {
            CreateRigidbodies = false;
            SetCreateRagdoll();
        }

        if (CreateJoints)
        {
            CreateJoints = false;
            SetCreateJoints();
        }

        if (HaveDebugMeshes)
        {
            SetHaveDebugMeshes(true);
        }
    }

    private void SetCreateRagdoll()
    {
        if (!string.IsNullOrEmpty(BoneWhitelist))
        {
            _whitelist.Clear();
            if (InterpretWhitelist())
            {
                foreach (var boneId in _whitelist)
                {
                    AddRagdollBone(boneId);
                }
            }
        }
        else
        {
            for (int i = 0; i < GetBoneCount(); i++)
            {
                AddRagdollBone(i);
            }
        }
    }

    private void AddRagdollBone(int boneId)
    {
        var bone = new RagdollBone();
        bone.BoneName = GetBoneName(boneId);
        bone.Name = GetCleanBoneName(boneId);
        bone.Transform = GetBoneGlobalPose(boneId);

        var collision = new CollisionShape3D();
        collision.Name = (bone.BoneName+"Collider");

        if (GetBoneChildren(boneId).Length > 0)
        {
            Vector3 boneEnd = GetBoneChildrenAveragePosition(boneId);
            float boneLength = (boneEnd - GetBoneGlobalPose(boneId).Origin).Length();
            collision.Shape = new CapsuleShape3D { Radius = boneLength*BoneThickness, Height = boneLength};
            collision.Position = Position + Vector3.Up * (boneLength * 0.5f);
        }
        else
        {
            collision.Shape = new SphereShape3D { Radius = 0.1f};
            collision.Position = Position;
        }
        
        bone.AddChild(collision);
        AddChild(bone);
        bone.Owner = GetOwner<Node>();
        collision.Owner = GetOwner<Node>();
    }

    private void SetCreateJoints()
    {
        if (!string.IsNullOrEmpty(BoneWhitelist))
        {
            _whitelist.Clear();
            if (InterpretWhitelist())
            {
                if (_whitelist.Count > 1)
                {
                    foreach (var boneId in _whitelist)
                    {
                        AddJointFor(boneId);
                    }
                }
                else
                {
                    GD.PushError("Too few bones whitelisted. Need at least two.");
                }
            }
        }
        else
        {
            for (int i = 1; i < GetBoneCount(); i++)
            {
                AddJointFor(i);
            }
        }
    }

    private void AddJointFor(int boneId)
    {
        if (GetBoneParent(boneId) >= 0)
        {
            var nodeA = GetNodeOrNull<RagdollBone>(GetCleanBoneName(GetBoneParent(boneId)));
            var nodeB = GetNodeOrNull<RagdollBone>(GetCleanBoneName(boneId));
            if (nodeA != null && nodeB != null)
            {
                var joint = new ActiveRagdollJoint();
                joint.Transform = GetBoneGlobalPose(boneId);
                joint.Name = $"JOINT_{nodeA.Name}_{nodeB.Name}";
                joint.BoneAIndex = FindBone(nodeA.BoneName);
                joint.BoneBIndex = FindBone(nodeB.BoneName);

                AddChild(joint);
                switch (ParentingType)
                {
                    case ParentingType.NodeA:
                        joint.Reparent(nodeA);
                        break;
                    case ParentingType.NodeB:
                        joint.Reparent(nodeB);
                        break;
                }
                joint.Owner = GetOwner<Node>();
                
                if (AnimationSkeleton != null) joint.AnimationSkeletonPath = joint.GetPathTo(AnimationSkeleton); 
                
                joint.Set("node_a", joint.GetPathTo(nodeA));
                joint.Set("node_b", joint.GetPathTo(nodeB));

                TraceAnimationSkeleton += joint.TraceSkeleton;
                
            }
        }
    }

    private void SetHaveDebugMeshes(bool value)
    {
        HaveDebugMeshes = value;
        if (HaveDebugMeshes)
        {
            for (int i = 0; i < GetBoneCount(); i++)
            {
                var ragdollBone = GetNodeOrNull<RagdollBone>(GetCleanBoneName(i));
                if (ragdollBone != null)
                {
                    var collision = ragdollBone.GetNodeOrNull<CollisionShape3D>("CollisionShape3D");
                    if (collision != null && !collision.HasNode("DEBUG_MESH"))
                    {
                        if (collision.Shape is BoxShape3D boxShape)
                        {
                            var box = new MeshInstance3D { Name = "DEBUG_MESH", Mesh = new BoxMesh { Size = boxShape.Size * 2 } };
                            collision.AddChild(box);
                            box.Owner = GetOwner<Node>();
                        }
                        else if (collision.Shape is CapsuleShape3D capsuleShape)
                        {
                            var capsule = new MeshInstance3D { Name = "DEBUG_MESH", Mesh = new CapsuleMesh { Radius = capsuleShape.Radius, Height = capsuleShape.Height } };
                            collision.AddChild(capsule);
                            capsule.Owner = GetOwner<Node>();
                        }
                    }
                }
            }
        }
        else
        {
            for (int i = 0; i < GetBoneCount(); i++)
            {
                var ragdollBone = GetNodeOrNull<RagdollBone>(GetCleanBoneName(i));
                if (ragdollBone != null)
                {
                    var collision = ragdollBone.GetNodeOrNull<CollisionShape3D>("CollisionShape3D");
                    if (collision != null && collision.HasNode("DEBUG_MESH"))
                    {
                        collision.GetNode("DEBUG_MESH").QueueFree();
                    }
                }
            }
        }
    }

    private bool InterpretWhitelist()
    {
        var ranges = BoneWhitelist.Split(",");
        foreach (var range in ranges)
        {
            var num = range.Split("-");
            if (num.Length == 1)
            {
                _whitelist.Add(int.Parse(num[0]));
            }
            else if (num.Length == 2)
            {
                for (int i = int.Parse(num[0]); i <= int.Parse(num[1]); i++)
                {
                    _whitelist.Add(i);
                }
            }
            else
            {
                GD.PushError("Incorrect entry in whitelist");
                return false;
            }
        }
        return true;
    }

    private string GetCleanBoneName(int boneId)
    {
        return GetBoneName(boneId).Replace(".", "_");
    }
    
    private Vector3 GetBoneChildrenAveragePosition(int boneId)
    {
        Vector3 averagePosition = Vector3.Zero;
        int childrenCount = 0;

        foreach (int childBoneId in GetBoneChildren(boneId))
        {
            averagePosition += GetBoneGlobalPose(childBoneId).Origin;
            childrenCount++;
        }

        if (childrenCount > 0)
        {
            averagePosition /= childrenCount;
        }

        return averagePosition;
    }

    public void StartTracing()
    {
        CallDeferred("emit_signal", "TraceAnimationSkeleton", true);
    }

    public void StopTracing()
    {
        CallDeferred("emit_signal", "TraceAnimationSkeleton", false);
    }
}

public enum ParentingType
{
    Skeleton,
    NodeA,
    NodeB
}