using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using ConsoleApp1.Common;

public partial struct Vector3 : IEquatable<Vector3>
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

        // X component of the vector.
        public double x;
        // Y component of the vector.
        public double y;
        // Z component of the vector.
        public double z;
//
//        // Linearly interpolates between two vectors.
//        public static Vector3 Lerp(Vector3 a, Vector3 b, double t)
//        {
//            t = Mathf.Clamp01(t);
//            return new Vector3(
//                a.x + (b.x - a.x) * t,
//                a.y + (b.y - a.y) * t,
//                a.z + (b.z - a.z) * t
//            );
//        }
//
//        // Linearly interpolates between two vectors without clamping the interpolant
//        public static Vector3 LerpUnclamped(Vector3 a, Vector3 b, double t)
//        {
//            return new Vector3(
//                a.x + (b.x - a.x) * t,
//                a.y + (b.y - a.y) * t,
//                a.z + (b.z - a.z) * t
//            );
//        }
//
//        // Moves a point /current/ in a straight line towards a /target/ point.
//        public static Vector3 MoveTowards(Vector3 current, Vector3 target, double maxDistanceDelta)
//        {
//            Vector3 toVector = target - current;
//            double dist = toVector.magnitude;
//            if (dist <= maxDistanceDelta || dist < double.Epsilon)
//                return target;
//            return current + toVector / dist * maxDistanceDelta;
//        }
//
//        // Access the x, y, z components using [0], [1], [2] respectively.
//        public double this[int index]
//        {
//            get
//            {
//                switch (index)
//                {
//                    case 0: return x;
//                    case 1: return y;
//                    case 2: return z;
//                    default:
//                        throw new IndexOutOfRangeException("Invalid Vector3 index!");
//                }
//            }
//
//            set
//            {
//                switch (index)
//                {
//                    case 0: x = value; break;
//                    case 1: y = value; break;
//                    case 2: z = value; break;
//                    default:
//                        throw new IndexOutOfRangeException("Invalid Vector3 index!");
//                }
//            }
//        }

        // Creates a new vector with given x, y, z components.
        public Vector3(double x, double y, double z) { this.x = x; this.y = y; this.z = z; }
        // Creates a new vector with given x, y components and sets /z/ to zero.
        public Vector3(double x, double y) { this.x = x; this.y = y; z = 0D; }

//        // Set x, y and z components of an existing Vector3.
//        public void Set(double newX, double newY, double newZ) { x = newX; y = newY; z = newZ; }
//
//        // Multiplies two vectors component-wise.
//        public static Vector3 Scale(Vector3 a, Vector3 b) { return new Vector3(a.x * b.x, a.y * b.y, a.z * b.z); }
//
//        // Multiplies every component of this vector by the same component of /scale/.
//        public void Scale(Vector3 scale) { x *= scale.x; y *= scale.y; z *= scale.z; }
//
        // Cross Product of two vectors.
        public static Vector3 Cross(Vector3 lhs, Vector3 rhs)
        {
            return new Vector3(
                lhs.y * rhs.z - lhs.z * rhs.y,
                lhs.z * rhs.x - lhs.x * rhs.z,
                lhs.x * rhs.y - lhs.y * rhs.x);
        }
        
        public Vector3 orthogonal
        {
            get
            {
                double x = Math.Abs(this.x);
                double y = Math.Abs(this.y);
                double z = Math.Abs(this.z);

                Vector3 other = x < y ? (x < z ? rightVector : forwardVector) : (y < z ? upVector : forwardVector);
                return Vector3.Cross(this, other);
            }
        }

        // used to allow Vector3s to be used as keys in hash tables
        public override int GetHashCode()
        {
            return x.GetHashCode() ^ (y.GetHashCode() << 2) ^ (z.GetHashCode() >> 2);
        }

        // also required for being able to use Vector3s as keys in hash tables
        public override bool Equals(object other)
        {
            if (!(other is Vector3)) return false;

            return Equals((Vector3)other);
        }

        public bool Equals(Vector3 other)
        {
            return x.Equals(other.x) && y.Equals(other.y) && z.Equals(other.z);
        }
//
//        // Reflects a vector off the plane defined by a normal.
//        public static Vector3 Reflect(Vector3 inDirection, Vector3 inNormal)
//        {
//            return -2D * Dot(inNormal, inDirection) * inNormal + inDirection;
//        }

        // *undoc* --- we have normalized property now
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector3 Normalize(Vector3 value)
        {
            double mag = Magnitude(value);
            if (mag > kEpsilon)
                return value / mag;
            else
                return zero;
        }

        
        public double DistanceTo(Vector3 other)
        {
            return (this - other).magnitude;
        }

        // Returns this vector with a ::ref::magnitude of 1 (RO).
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

        // Dot Product of two vectors.
        public static double Dot(Vector3 lhs, Vector3 rhs) { return lhs.x * rhs.x + lhs.y * rhs.y + lhs.z * rhs.z; }

        // Projects a vector onto another vector.
        public static Vector3 Project(Vector3 vector, Vector3 onNormal)
        {
            double sqrMag = Dot(onNormal, onNormal);
            if (sqrMag < kEpsilon)
                return zero;
            else
                return onNormal * Dot(vector, onNormal) / sqrMag;
        }

//        // Projects a vector onto a plane defined by a normal orthogonal to the plane.
//        public static Vector3 ProjectOnPlane(Vector3 vector, Vector3 planeNormal)
//        {
//            return vector - Project(vector, planeNormal);
//        }
//
//        // Returns the angle in degrees between /from/ and /to/. This is always the smallest
//        public static double Angle(Vector3 from, Vector3 to)
//        {
//            // sqrt(a) * sqrt(b) = sqrt(a * b) -- valid for real numbers
//            double denominator = Mathf.Sqrt(from.sqrMagnitude * to.sqrMagnitude);
//            if (denominator < kEpsilonNormalSqrt)
//                return 0D;
//
//            double dot = Mathf.Clamp(Dot(from, to) / denominator, -1D, 1D);
//            return Mathf.Acos(dot) * Mathf.Rad2Deg;
//        }
//
//        // The smaller of the two possible angles between the two vectors is returned, therefore the result will never be greater than 180 degrees or smaller than -180 degrees.
//        // If you imagine the from and to vectors as lines on a piece of paper, both originating from the same point, then the /axis/ vector would point up out of the paper.
//        // The measured angle between the two vectors would be positive in a clockwise direction and negative in an anti-clockwise direction.
//        public static double SignedAngle(Vector3 from, Vector3 to, Vector3 axis)
//        {
//            double unsignedAngle = Angle(from, to);
//            double sign = Mathf.Sign(Dot(axis, Cross(from, to)));
//            return unsignedAngle * sign;
//        }
//
//        // Returns the distance between /a/ and /b/.
//        public static double Distance(Vector3 a, Vector3 b)
//        {
//            Vector3 vec = new Vector3(a.x - b.x, a.y - b.y, a.z - b.z);
//            return Mathf.Sqrt(vec.x * vec.x + vec.y * vec.y + vec.z * vec.z);
//        }

        // Returns a copy of /vector/ with its magnitude clamped to /maxLength/.
        public static Vector3 ClampMagnitude(Vector3 vector, double maxLength)
        {
            if (vector.sqrMagnitude > maxLength * maxLength)
                return vector.normalized * maxLength;
            return vector;
        }

        // *undoc* --- there's a property now
        public static double Magnitude(Vector3 vector) { return Math.Sqrt(vector.x * vector.x + vector.y * vector.y + vector.z * vector.z); }

        // Returns the length of this vector (RO).
        public double magnitude { get { return Math.Sqrt(x * x + y * y + z * z); } }

        // *undoc* --- there's a property now
        public static double SqrMagnitude(Vector3 vector) { return vector.x * vector.x + vector.y * vector.y + vector.z * vector.z; }

        // Returns the squared length of this vector (RO).
        public double sqrMagnitude { get { return x * x + y * y + z * z; } }
//
//        // Returns a vector that is made from the smallest components of two vectors.
//        public static Vector3 Min(Vector3 lhs, Vector3 rhs)
//        {
//            return new Vector3(Mathf.Min(lhs.x, rhs.x), Mathf.Min(lhs.y, rhs.y), Mathf.Min(lhs.z, rhs.z));
//        }
//
//        // Returns a vector that is made from the largest components of two vectors.
//        public static Vector3 Max(Vector3 lhs, Vector3 rhs)
//        {
//            return new Vector3(Mathf.Max(lhs.x, rhs.x), Mathf.Max(lhs.y, rhs.y), Mathf.Max(lhs.z, rhs.z));
//        }

        static readonly Vector3 zeroVector = new Vector3(0D, 0D, 0D);
        static readonly Vector3 oneVector = new Vector3(1D, 1D, 1D);
        static readonly Vector3 upVector = new Vector3(0D, 1D, 0D);
        static readonly Vector3 downVector = new Vector3(0D, -1D, 0D);
        static readonly Vector3 leftVector = new Vector3(-1D, 0D, 0D);
        static readonly Vector3 rightVector = new Vector3(1D, 0D, 0D);
        static readonly Vector3 forwardVector = new Vector3(0D, 0D, 1D);
        static readonly Vector3 backVector = new Vector3(0D, 0D, -1D);
        static readonly Vector3 positiveInfinityVector = new Vector3(double.PositiveInfinity, double.PositiveInfinity, double.PositiveInfinity);
        static readonly Vector3 negativeInfinityVector = new Vector3(double.NegativeInfinity, double.NegativeInfinity, double.NegativeInfinity);

        // Shorthand for writing @@Vector3(0, 0, 0)@@
        public static Vector3 zero { get { return zeroVector; } }
        // Shorthand for writing @@Vector3(1, 1, 1)@@
        public static Vector3 one { get { return oneVector; } }
        // Shorthand for writing @@Vector3(0, 0, 1)@@
        public static Vector3 forward { get { return forwardVector; } }
        public static Vector3 back { get { return backVector; } }
        // Shorthand for writing @@Vector3(0, 1, 0)@@
        public static Vector3 up { get { return upVector; } }
        public static Vector3 down { get { return downVector; } }
        public static Vector3 left { get { return leftVector; } }
        // Shorthand for writing @@Vector3(1, 0, 0)@@
        public static Vector3 right { get { return rightVector; } }
        // Shorthand for writing @@Vector3(double.PositiveInfinity, double.PositiveInfinity, double.PositiveInfinity)@@
        public static Vector3 positiveInfinity { get { return positiveInfinityVector; } }
        // Shorthand for writing @@Vector3(double.NegativeInfinity, double.NegativeInfinity, double.NegativeInfinity)@@
        public static Vector3 negativeInfinity { get { return negativeInfinityVector; } }

        // Adds two vectors.
        public static Vector3 operator+(Vector3 a, Vector3 b) { return new Vector3(a.x + b.x, a.y + b.y, a.z + b.z); }
        // Subtracts one vector from another.
        public static Vector3 operator-(Vector3 a, Vector3 b) { return new Vector3(a.x - b.x, a.y - b.y, a.z - b.z); }
        // Negates a vector.
        public static Vector3 operator-(Vector3 a) { return new Vector3(-a.x, -a.y, -a.z); }
        // Multiplies a vector by a number.
        public static Vector3 operator*(Vector3 a, double d) { return new Vector3(a.x * d, a.y * d, a.z * d); }
        // Multiplies a vector by a number.
        public static Vector3 operator*(double d, Vector3 a) { return new Vector3(a.x * d, a.y * d, a.z * d); }
        // Divides a vector by a number.
        public static Vector3 operator/(Vector3 a, double d) { return new Vector3(a.x / d, a.y / d, a.z / d); }

        // Returns true if the vectors are equal.
        public bool AlmostEqual(Vector3 rhs, double eps)
        {
            // Returns false in the presence of NaN values.
            return SqrMagnitude(this - rhs) < eps * eps;
        }
        
        // Returns true if the vectors are equal.
        public static bool operator==(Vector3 lhs, Vector3 rhs)
        {
            // Returns false in the presence of NaN values.
            return SqrMagnitude(lhs - rhs) < kEpsilon * kEpsilon;
        }

        // Returns true if vectors are different.
        public static bool operator!=(Vector3 lhs, Vector3 rhs)
        {
            // Returns true in the presence of NaN values.
            return !(lhs == rhs);
        }

        public override string ToString()
        {
            return String.Format("({0}, {1}, {2})", x, y, z);
        }

        public string ToString(string format)
        {
            return String.Format("({0}, {1}, {2})", x.ToString(format), y.ToString(format), z.ToString(format));
        }
    }
