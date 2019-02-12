using ConsoleApp1.Common;

namespace ConsoleApp1.Structs
{
    public struct Control
    {
        public Vector3 TargetVelocity;
        public double JumpSpeed;
        public bool Nitro;

        public Control(Vector3 targetVelocity, double jumpSpeed = 0, bool nitro = false)
        {
            TargetVelocity = targetVelocity;
            JumpSpeed = jumpSpeed;
            Nitro = nitro;
        }
    }
}