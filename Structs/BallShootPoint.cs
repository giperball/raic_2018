using ConsoleApp1.Common;

namespace Com.CodeGame.CodeBall2018.DevKit.CSharpCgdk.Structs
{
    public struct BallShootPoint
    {
        public int Index;
        public Vector3 BallPoint;
        public Polar3 PolarShift;

        public BallShootPoint(int index, Vector3 ballPoint, Polar3 polarShift)
        {
            Index = index;
            PolarShift = polarShift;
            BallPoint = ballPoint;
        }

        public Vector3 GlobalPoint
        {
            get { return BallPoint + PolarShift.ToDecart(); }
        }
    }
}