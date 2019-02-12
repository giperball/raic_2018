namespace ConsoleApp1.Common
{
    public struct Color
    {
        public double r;
        public double g;
        public double b;
        public double a;

        public Color(double r, double g, double b, double a)
        {
            this.r = r; this.g = g; this.b = b; this.a = a;
        }
    
        public Color(double r, double g, double b)
        {
            this.r = r; this.g = g; this.b = b; this.a = 1.0;
        }

        public override string ToString()
        {
            return $"RGBA({r}, {g}, {b}, {a})";
        }

        public override bool Equals(object other)
        {
            if (!(other is Color)) return false;

            return Equals((Color)other);
        }

        private bool Equals(Color other)
        {
            return r.Equals(other.r) && g.Equals(other.g) && b.Equals(other.b) && a.Equals(other.a);
        }

        public static Color red => new Color(1, 0, 0, 0.5);
        public static Color green => new Color(0, 1, 0, 0.5);
        public static Color blue => new Color(0, 0, 1, 0.5);
        public static Color black => new Color(0, 0, 0, 0.7);
        public static Color cyan => new Color(0, 1, 1, 0.5);
    }
}