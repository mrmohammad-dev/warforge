using UnityEngine;
using WarForge.Vehicles;

namespace WarForge.Combat
{
    /// <summary>
    /// Attached to every armor collider. Thickness either uniform (a sloped plate, mantlet, track) or chosen per box face
    /// from the local hit normal (hull and turret boxes). Real slope comes from the collider geometry itself.
    /// </summary>
    public class ArmorPlate : MonoBehaviour
    {
        public Vehicle Vehicle;
        public string PlateName = "Hull";
        public bool Uniform;
        public float UniformThickness;
        public float Front, Side, Rear, Top, Bottom;
        public bool IsTrack;
        public ModuleType TrackModule;

        public void SetFaces(float front, float side, float rear, float top, float bottom)
        {
            Uniform = false; Front = front; Side = side; Rear = rear; Top = top; Bottom = bottom;
        }

        public void SetUniform(float thickness)
        {
            Uniform = true; UniformThickness = thickness;
        }

        public float ThicknessFor(Vector3 worldNormal, out string faceName)
        {
            if (Uniform)
            {
                faceName = PlateName;
                return UniformThickness;
            }
            Vector3 n = transform.InverseTransformDirection(worldNormal);
            float ax = Mathf.Abs(n.x), ay = Mathf.Abs(n.y), az = Mathf.Abs(n.z);
            if (ay >= ax && ay >= az)
            {
                faceName = PlateName + (n.y > 0 ? " roof" : " floor");
                return n.y > 0 ? Top : Bottom;
            }
            if (az >= ax)
            {
                faceName = PlateName + (n.z > 0 ? " front" : " rear");
                return n.z > 0 ? Front : Rear;
            }
            faceName = PlateName + " side";
            return Side;
        }
    }
}
