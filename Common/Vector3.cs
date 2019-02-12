using System;

namespace ConsoleApp1.Common
{
    public struct Vector3
    {        
        public bool IsPointBetween(Vector3 point_b, Vector3 point_c, double eps = 1e-12)
        {                     
            var projectedPoint = Project((this-point_b),(point_c-point_b))+point_b;

            return ((projectedPoint.DistanceTo(point_b) + projectedPoint.DistanceTo(point_c)).AlmostEqualTo(
                point_b.DistanceTo(point_c),
                eps));
        }
        
        public Vector3 DropY(double new_y = 0)
        {
            return new Vector3(x, new_y, z);
        }
        
        public const double kEpsilon = 1e-30D;

        public double x;
        public double y;
        public double z;

        public Vector3(double x, double y, double z) { this.x = x; this.y = y; this.z = z; }
        public Vector3(double x, double y) { this.x = x; this.y = y; z = 0D; }

        public override int GetHashCode()
        {
            return x.GetHashCode() ^ (y.GetHashCode() << 2) ^ (z.GetHashCode() >> 2);
        }

        public override bool Equals(object other)
        {
            if (!(other is Vector3)) return false;

            return Equals((Vector3)other);
        }

        public bool Equals(Vector3 other)
        {
            return x.Equals(other.x) && y.Equals(other.y) && z.Equals(other.z);
        }
        
        public double DistanceTo(Vector3 other)
        {
            return (this - other).magnitude;
        }

        public Vector3 normalized
        {
            get
            {
                double mag = Magnitude(this);
                if (mag > kEpsilon)
                    return this / mag;
                else
                    return zero;
            }
        }

        public static double Dot(Vector3 lhs, Vector3 rhs) { return lhs.x * rhs.x + lhs.y * rhs.y + lhs.z * rhs.z; }

        public static Vector3 Project(Vector3 vector, Vector3 onNormal)
        {
            double sqrMag = Dot(onNormal, onNormal);
            if (sqrMag < kEpsilon)
                return zero;
            else
                return onNormal * Dot(vector, onNormal) / sqrMag;
        }

        public static Vector3 ClampMagnitude(Vector3 vector, double maxLength)
        {
            if (vector.sqrMagnitude > maxLength * maxLength)
                return vector.normalized * maxLength;
            return vector;
        }

        public static double Magnitude(Vector3 vector) { return Math.Sqrt(vector.x * vector.x + vector.y * vector.y + vector.z * vector.z); }

        public double magnitude => Math.Sqrt(x * x + y * y + z * z);

        public static double SqrMagnitude(Vector3 vector) { return vector.x * vector.x + vector.y * vector.y + vector.z * vector.z; }

        public double sqrMagnitude => x * x + y * y + z * z;

        static readonly Vector3 zeroVector = new Vector3(0D, 0D, 0D);
        static readonly Vector3 oneVector = new Vector3(1D, 1D, 1D);
        static readonly Vector3 upVector = new Vector3(0D, 1D, 0D);
        static readonly Vector3 downVector = new Vector3(0D, -1D, 0D);
        static readonly Vector3 leftVector = new Vector3(-1D, 0D, 0D);
        static readonly Vector3 rightVector = new Vector3(1D, 0D, 0D);
        static readonly Vector3 forwardVector = new Vector3(0D, 0D, 1D);
        static readonly Vector3 backVector = new Vector3(0D, 0D, -1D);

        public static Vector3 zero { get { return zeroVector; } }
        public static Vector3 one { get { return oneVector; } }
        public static Vector3 forward { get { return forwardVector; } }
        public static Vector3 back { get { return backVector; } }
        public static Vector3 up { get { return upVector; } }
        public static Vector3 down { get { return downVector; } }
        public static Vector3 left { get { return leftVector; } }
        public static Vector3 right { get { return rightVector; } }

        public static Vector3 operator+(Vector3 a, Vector3 b) { return new Vector3(a.x + b.x, a.y + b.y, a.z + b.z); }
        public static Vector3 operator-(Vector3 a, Vector3 b) { return new Vector3(a.x - b.x, a.y - b.y, a.z - b.z); }
        public static Vector3 operator-(Vector3 a) { return new Vector3(-a.x, -a.y, -a.z); }
        public static Vector3 operator*(Vector3 a, double d) { return new Vector3(a.x * d, a.y * d, a.z * d); }
        public static Vector3 operator*(double d, Vector3 a) { return new Vector3(a.x * d, a.y * d, a.z * d); }
        public static Vector3 operator/(Vector3 a, double d) { return new Vector3(a.x / d, a.y / d, a.z / d); }

        public bool AlmostEqual(Vector3 rhs, double eps)
        {
            return SqrMagnitude(this - rhs) < eps * eps;
        }
        
        public static bool operator==(Vector3 lhs, Vector3 rhs)
        {
            return SqrMagnitude(lhs - rhs) < kEpsilon * kEpsilon;
        }

        public static bool operator!=(Vector3 lhs, Vector3 rhs)
        {
            return !(lhs == rhs);
        }

        public override string ToString()
        {
            return string.Format("({0}, {1}, {2})", x, y, z);
        }
    }
}
