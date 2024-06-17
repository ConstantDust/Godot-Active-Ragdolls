using System.Drawing;
using Godot;
using Godot.Collections;

[Tool]
public partial class RigidbodyRagdoll : Skeleton3D
{
    [Export] public bool CreateRigidbodies;
    [Export] public bool CreateJoints;
    [Export] public bool CreateCollisionMesh;

    [Export] public float BoneThickness = 0.1f;
    [Export(PropertyHint.Enum, "Parent,Child")] 
    public ParentingType ParentingType = ParentingType.Parent;
    
    [Export] public Skeleton3D AnimationSkeleton;
    [Export] public Array<string> Blacklist = new();

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
    }

    private void SetCreateRagdoll()
    {
        for (int i = 0; i < GetBoneCount(); i++)
        {
            bool blacklistFound = false;
            foreach (var value in Blacklist)
            {
                if (GetCleanBoneName(i).Contains(value)) blacklistFound = true;
            }
            if(blacklistFound) continue;
            AddRagdollBone(i);
        }
    }

    private void AddRagdollBone(int boneId)
    {
        var bone = new RagdollBone();
        bone.BoneName = GetBoneName(boneId);
        bone.Name = GetCleanBoneName(boneId);
        bone.Transform = GetBoneGlobalPose(boneId);

        bone.CanSleep = false;
        bone.FreezeMode = RigidBody3D.FreezeModeEnum.Kinematic;
        bone.ContinuousCd = true;
        
        // TESTING
        var mesh = new MeshInstance3D();
        
        var collision = new CollisionShape3D();
        collision.Name = (bone.BoneName+"_collider");

        if (GetBoneChildren(boneId).Length == 1)
        {
            Vector3 boneEnd = GetBoneChildrenAveragePosition(boneId);
            float boneLength = (boneEnd - GetBoneGlobalPose(boneId).Origin).Length();
            collision.Shape = new CapsuleShape3D { Radius = boneLength*BoneThickness, Height = boneLength};
            collision.Position = Position + Vector3.Up * (boneLength * 0.5f);

            if (CreateCollisionMesh)
            {
                mesh.Mesh = new CapsuleMesh { Radius = boneLength*BoneThickness, Height = boneLength};
                            mesh.Position = Position + Vector3.Up * (boneLength * 0.5f);
            }
            
        }
        else if (GetBoneChildren(boneId).Length > 1)
        {
            Vector3 boneEnd = GetBoneChildrenAveragePosition(boneId);
            float boneLength = (boneEnd - GetBoneGlobalPose(boneId).Origin).Length();
            Vector3 size = Vector3.Up * boneLength;

            size.X = BoneThickness * 0.25f;
            size.Z = BoneThickness * 0.25f;
            
            collision.Shape = new BoxShape3D { Size = size };
            collision.Position = Position + Vector3.Up * (boneLength * 0.5f);
            
            if(CreateCollisionMesh)
            {
                mesh.Mesh = new BoxMesh { Size = size };
                mesh.Position = Position + Vector3.Up * (boneLength * 0.5f);
            }
        }
        else
        {
            collision.Shape = new SphereShape3D { Radius = BoneThickness * 0.25f };
            collision.Position = Position;
            
            if(CreateCollisionMesh)
            {
                mesh.Mesh = new SphereMesh { Radius = BoneThickness * 0.25f, Height = BoneThickness * 0.5f };
                mesh.Position = Position;
            }
        }
        
        AddChild(bone);
        
        if(CreateCollisionMesh)
        {
            bone.AddChild(mesh);
            mesh.Owner = GetOwner<Node>();
        }
        
        bone.AddChild(collision);
        collision.Owner = GetOwner<Node>();
        
        bone.Owner = GetOwner<Node>();
        bone.ParentSkeleton = this;
        bone.TargetSkeleton = AnimationSkeleton;
    }

    private void SetCreateJoints()
    {
        for (int i = 0; i < GetBoneCount(); i++)
        {
            bool blacklistFound = false;
            foreach (var value in Blacklist)
            {
                if (GetCleanBoneName(i).Contains(value)) blacklistFound = true;
            }
            if(blacklistFound) continue;
            AddJointFor(i);
        }
    }

    private void AddJointFor(int boneId)
    {
        if (boneId >= 0 && GetBoneParent(boneId) >= 0)
        {
            var parentNode = GetNodeOrNull<RagdollBone>(GetCleanBoneName(GetBoneParent(boneId)));
            var thisNode = GetNodeOrNull<RagdollBone>(GetCleanBoneName(boneId));
            
            if (parentNode != null && thisNode != null)
            {
                if(Blacklist.Contains(parentNode.BoneName)) return;
                if(Blacklist.Contains(thisNode.BoneName)) return;
                
                // Create the joint object
                var joint = new ActiveRagdollJoint();
                joint.Transform = GetBoneGlobalPose(boneId);
                joint.Name = $"{parentNode.Name}_to_{thisNode.Name}";
                joint.BoneAIndex = FindBone(parentNode.BoneName);
                joint.BoneBIndex = FindBone(thisNode.BoneName);

                // parent so transforms and such are moved automatically
                AddChild(joint);
                switch (ParentingType)
                {
                    case ParentingType.Parent:
                        joint.Reparent(parentNode);
                        break;
                    case ParentingType.Child:
                        joint.Reparent(thisNode);
                        break;
                }
                joint.Owner = GetOwner<Node>();
                
                // set the nodes
                joint.Set("node_a", joint.GetPathTo(parentNode));
                joint.Set("node_b", joint.GetPathTo(thisNode));

                joint.BoneA = parentNode;
                joint.BoneB = thisNode;
                
                joint.ParentSkeleton = this;
                joint.TargetSkeleton = AnimationSkeleton;
            }
        }
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

}

public enum ParentingType
{
    Parent,
    Child
}