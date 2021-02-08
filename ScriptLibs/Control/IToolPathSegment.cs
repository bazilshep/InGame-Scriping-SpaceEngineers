using VRageMath; // VRage.Math.dll

namespace IngameScript.Control
{

    public interface IToolPathSegment : IPathSegment<Vector3D, Vector3D, PositionWaypoint>, IPathSegment<Quaternion, Vector3, AttitudeWaypoint>
    {
        void InterpolatedWaypoint(double t, out AttitudeWaypoint att, out PositionWaypoint pos);
    }
    
}