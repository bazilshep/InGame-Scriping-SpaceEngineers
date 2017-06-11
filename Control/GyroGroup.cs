using System;
using System.Collections.Generic;
using VRageMath; // VRage.Math.dll
using Sandbox.ModAPI.Interfaces; // Sandbox.Common.dll
using Sandbox.ModAPI.Ingame; // Sandbox.Common.dll

namespace SpaceEngineersIngameScript.Control
{
    /// <summary>        
    /// Class for commanding a group of IMyGyro objects.  Note: If any of the        
    /// Gyroscopes are removed/etc then the class instance should be thrown        
    /// away and a new one constructed.        
    /// </summary>        
    public class GyroGroup
    {
        List<IMyGyro> gyroscopes = null;
        string[] gyroYawField = null;
        string[] gyroPitchField = null;
        string[] gyroRollField = null;
        float[] gyroYawFactor = null;
        float[] gyroPitchFactor = null;
        float[] gyroRollFactor = null;

        static readonly string[] gyroAxisLookup = { "Roll", "Roll", "Pitch", "Pitch", "Yaw", "Yaw" };
        static readonly float[] gyroSignLookup = { -1.0f, 1.0f, 1.0f, -1.0f, 1.0f, -1.0f };

        const float RPM2RadPS = (float)(Math.PI / 30);

        /// <summary>        
        /// Constructs a GyroGroup using a list of IMyGyro objects. All gyros must have the same CubeGrid.        
        /// </summary>        
        /// <param name="gyros"></param>        
        public GyroGroup(List<IMyGyro> gyros)
        {
            gyroscopes = gyros;
            InitGyroscopes();
        }
        public void Disable() { SetGyroOverride(false); }
        public void Enable() { SetGyroOverride(true); }
        void InitGyroscopes()
        {
            gyroYawField = new string[gyroscopes.Count];
            gyroPitchField = new string[gyroscopes.Count];
            gyroYawFactor = new float[gyroscopes.Count];
            gyroPitchFactor = new float[gyroscopes.Count];
            gyroRollField = new string[gyroscopes.Count];
            gyroRollFactor = new float[gyroscopes.Count];



            for (int i = 0; i < gyroscopes.Count; i++)
            {
                MatrixD refMatrix = gyroscopes[i].CubeGrid.WorldMatrix;
                int gyroUp = (int)gyroscopes[i].WorldMatrix.GetClosestDirection(refMatrix.Up);
                int gyroLeft = (int)gyroscopes[i].WorldMatrix.GetClosestDirection(refMatrix.Left);
                int gyroForward = (int)gyroscopes[i].WorldMatrix.GetClosestDirection(refMatrix.Forward);

                gyroYawField[i] = gyroAxisLookup[gyroUp];
                gyroYawFactor[i] = gyroSignLookup[gyroUp];
                gyroPitchField[i] = gyroAxisLookup[gyroLeft];
                gyroPitchFactor[i] = gyroSignLookup[gyroLeft];
                gyroRollField[i] = gyroAxisLookup[gyroForward];
                gyroRollFactor[i] = gyroSignLookup[gyroForward];
            }
        }
        void SetGyroOverride(bool bOverride)
        {
            for (int i = 0; i < gyroscopes.Count; i++)
                if ((gyroscopes[i]).GyroOverride != bOverride)
                    gyroscopes[i].ApplyAction("Override");
        }
        public void SetGyroYaw(float yawRate)
        {
            for (int i = 0; i < gyroscopes.Count; i++)
                gyroscopes[i].SetValueFloat(gyroYawField[i], -yawRate * gyroYawFactor[i]);
        }
        public void SetGyroPitch(float pitchRate)
        {
            for (int i = 0; i < gyroscopes.Count; i++)
                gyroscopes[i].SetValueFloat(gyroPitchField[i], pitchRate * gyroPitchFactor[i]);
        }
        public void SetGyroRoll(float rollRate)
        {
            for (int i = 0; i < gyroscopes.Count; i++)
                gyroscopes[i].SetValueFloat(gyroRollField[i], rollRate * gyroRollFactor[i]);
        }
        public void SetAngularVelocity(Vector3 w)
        {
            SetGyroPitch(w.X);
            SetGyroYaw(w.Y);
            SetGyroRoll(w.Z);
        }
        public void ResetGyro()
        {
            for (int i = 0; i < gyroscopes.Count; i++)
            {
                gyroscopes[i].SetValueFloat("Yaw", 0.0f);
                gyroscopes[i].SetValueFloat("Pitch", 0.0f);
                gyroscopes[i].SetValueFloat("Roll", 0.0f);
            }
        }

    }
    
}