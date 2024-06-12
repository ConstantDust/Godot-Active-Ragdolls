using Godot;
using Godot.Collections;

[Tool]
public partial class CreateRagdoll : Skeleton3D
{
    [Export] public bool createRagdoll;
    [Export] public bool createJoints;
    [Export] public bool haveDebugMeshes;
    [Export] public string boneWhitelist = "";
    private Array<int> _whitelist = new();

    [Signal] public delegate void TraceAnimationSkeletonEventHandler(bool value);

    public override void _Process(double delta)
    {
        if (createRagdoll)
        {
            GD.Print("HMM");
            createRagdoll = false;
            SetCreateRagdoll(true);
        }

        if (createJoints)
        {
            createJoints = false;
            SetCreateJoints(true);
        }

        if (haveDebugMeshes)
        {
            SetHaveDebugMeshes(true);
        }
    }

    private void SetCreateRagdoll(bool value)
    {
        if (value)
        {
            if (!string.IsNullOrEmpty(boneWhitelist))
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
    }

    private void AddRagdollBone(int boneId)
    {
        var bone = new RagdollBone();
        bone.BoneName = GetBoneName(boneId);
        bone.Name = GetCleanBoneName(boneId);
        bone.Transform = GetBoneGlobalPose(boneId);

        var collision = new CollisionShape3D();
        collision.Shape = new CapsuleShape3D { Radius = 0.1f, Height = 0.1f };
        collision.RotateX(Mathf.Pi / 2);
        bone.AddChild(collision);

        AddChild(bone);
        bone.Owner = GetOwner<Node>();
        collision.Owner = GetOwner<Node>();
    }

    private void SetCreateJoints(bool value)
    {
        if (value)
        {
            if (!string.IsNullOrEmpty(boneWhitelist))
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
                joint.boneAIndex = FindBone(nodeA.BoneName);
                joint.boneBIndex = FindBone(nodeB.BoneName);

                AddChild(joint);
                joint.Owner = GetOwner<Node>();

                joint.Set("nodes/node_a", joint.GetPathTo(nodeA));
                joint.Set("nodes/node_b", joint.GetPathTo(nodeB));

                TraceAnimationSkeleton += joint.TraceSkeleton;
            }
        }
    }

    private void SetHaveDebugMeshes(bool value)
    {
        haveDebugMeshes = value;
        if (haveDebugMeshes)
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
        var ranges = boneWhitelist.Split(",");
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

    public void StartTracing()
    {
        CallDeferred("emit_signal", "TraceAnimationSkeleton", true);
    }

    public void StopTracing()
    {
        CallDeferred("emit_signal", "TraceAnimationSkeleton", false);
    }
}