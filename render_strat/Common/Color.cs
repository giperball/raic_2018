using System;

public struct Color
{
    // Red component of the color.
    public double r;

    // Green component of the color.
    public double g;

    // Blue component of the color.
    public double b;

    // Alpha component of the color.
    public double a;


    // Constructs a new Color with given r,g,b,a components.
    public Color(double r, double g, double b, double a)
    {
        this.r = r; this.g = g; this.b = b; this.a = a;
    }

    // Constructs a new Color with given r,g,b components and sets /a/ to 1.
    public Color(double r, double g, double b)
    {
        this.r = r; this.g = g; this.b = b; this.a = 1.0F;
    }

    override public string ToString()
    {
        return String.Format("RGBA({0:F3}, {1:F3}, {2:F3}, {3:F3})", r, g, b, a);
    }

    public override bool Equals(object other)
    {
        if (!(other is Color)) return false;

        return Equals((Color)other);
    }

    public bool Equals(Color other)
    {
        return r.Equals(other.r) && g.Equals(other.g) && b.Equals(other.b) && a.Equals(other.a);
    }

    public static Color operator+(Color a, Color b) { return new Color(a.r + b.r, a.g + b.g, a.b + b.b, a.a + b.a); }

    // Subtracts color /b/ from color /a/. Each component is subtracted separately.
    public static Color operator-(Color a, Color b) { return new Color(a.r - b.r, a.g - b.g, a.b - b.b, a.a - b.a); }

    // Multiplies two colors together. Each component is multiplied separately.
    public static Color operator*(Color a, Color b) { return new Color(a.r * b.r, a.g * b.g, a.b * b.b, a.a * b.a); }

    // Multiplies color /a/ by the double /b/. Each color component is scaled separately.
    public static Color operator*(Color a, double b) { return new Color(a.r * b, a.g * b, a.b * b, a.a * b); }

    // Multiplies color /a/ by the double /b/. Each color component is scaled separately.
    public static Color operator*(double b, Color a) { return new Color(a.r * b, a.g * b, a.b * b, a.a * b); }

    // Divides color /a/ by the double /b/. Each color component is scaled separately.
    public static Color operator/(Color a, double b) { return new Color(a.r / b, a.g / b, a.b / b, a.a / b); }



    // Interpolates between colors /a/ and /b/ by /t/ without clamping the interpolant
    public static Color LerpUnclamped(Color a, Color b, double t)
    {
        return new Color(
            a.r + (b.r - a.r) * t,
            a.g + (b.g - a.g) * t,
            a.b + (b.b - a.b) * t,
            a.a + (b.a - a.a) * t
        );
    }

    // Returns new color that has RGB components multiplied, but leaving alpha untouched.
    internal Color RGBMultiplied(double multiplier) { return new Color(r * multiplier, g * multiplier, b * multiplier, a); }
    // Returns new color that has RGB components multiplied, but leaving alpha untouched.
    internal Color AlphaMultiplied(double multiplier) { return new Color(r, g, b, a * multiplier); }
    // Returns new color that has RGB components multiplied, but leaving alpha untouched.
    internal Color RGBMultiplied(Color multiplier) { return new Color(r * multiplier.r, g * multiplier.g, b * multiplier.b, a); }

    // Solid red. RGBA is (1, 0, 0, 1).
    public static Color red { get { return new Color(1F, 0F, 0F, 0.5F); } }
    // Solid green. RGBA is (0, 1, 0, 1).
    public static Color green { get { return new Color(0F, 1F, 0F, 0.5F); } }
    // Solid blue. RGBA is (0, 0, 1, 1).
    public static Color blue { get { return new Color(0F, 0F, 1F, 0.5F); } }
    // Solid white. RGBA is (1, 1, 1, 1).
    public static Color white { get { return new Color(1F, 1F, 1F, 1F); } }
    // Solid black. RGBA is (0, 0, 0, 1).
    public static Color black { get { return new Color(0F, 0F, 0F, 0.7F); } }
    public static Color orange { get { return new Color(204.0 / 256.0, 51.0 / 256.0, 0F, 0.5); } }
    // Yellow. RGBA is (1, 0.92, 0.016, 1), but the color is nice to look at!
    public static Color yellow { get { return new Color(1F, 235F / 255F, 4F / 255F, 0.5F); } }
    // Cyan. RGBA is (0, 1, 1, 1).
    public static Color cyan { get { return new Color(0F, 1F, 1F, 0.5F); } }
    // Magenta. RGBA is (1, 0, 1, 1).
    public static Color magenta { get { return new Color(1F, 0F, 1F, 0.5F); } }
    // Gray. RGBA is (0.5, 0.5, 0.5, 1).
    public static Color gray { get { return new Color(.5F, .5F, .5F, 0.5F); } }
    // English spelling for ::ref::gray. RGBA is the same (0.5, 0.5, 0.5, 1).
    public static Color grey { get { return new Color(.5F, .5F, .5F, 0.5F); } }
    // Completely transparent. RGBA is (0, 0, 0, 0).
    public static Color clear { get { return new Color(0F, 0F, 0F, 0F); } }

    // The grayscale value of the color (RO)
    public double grayscale { get { return 0.299F * r + 0.587F * g + 0.114F * b; } }


    // Access the r, g, b,a components using [0], [1], [2], [3] respectively.
    public double this[int index]
    {
        get
        {
            switch (index)
            {
                case 0: return r;
                case 1: return g;
                case 2: return b;
                case 3: return a;
                default:
                    throw new IndexOutOfRangeException("Invalid Vector3 index!");
            }
        }

        set
        {
            switch (index)
            {
                case 0: r = value; break;
                case 1: g = value; break;
                case 2: b = value; break;
                case 3: a = value; break;
                default:
                    throw new IndexOutOfRangeException("Invalid Vector3 index!");
            }
        }
    }

    // Convert a color from RGB to HSV color space.
    public static void RGBToHSV(Color rgbColor, out double H, out double S, out double V)
    {
        // when blue is highest valued
        if ((rgbColor.b > rgbColor.g) && (rgbColor.b > rgbColor.r))
            RGBToHSVHelper((double)4, rgbColor.b, rgbColor.r, rgbColor.g, out H, out S, out V);
        //when green is highest valued
        else if (rgbColor.g > rgbColor.r)
            RGBToHSVHelper((double)2, rgbColor.g, rgbColor.b, rgbColor.r, out H, out S, out V);
        //when red is highest valued
        else
            RGBToHSVHelper((double)0, rgbColor.r, rgbColor.g, rgbColor.b, out H, out S, out V);
    }

    static void RGBToHSVHelper(double offset, double dominantcolor, double colorone, double colortwo, out double H, out double S, out double V)
    {
        V = dominantcolor;
        //we need to find out which is the minimum color
        if (V != 0)
        {
            //we check which color is smallest
            double small = 0;
            if (colorone > colortwo) small = colortwo;
            else small = colorone;

            double diff = V - small;

            //if the two values are not the same, we compute the like this
            if (diff != 0)
            {
                //S = max-min/max
                S = diff / V;
                //H = hue is offset by X, and is the difference between the two smallest colors
                H = offset + ((colorone - colortwo) / diff);
            }
            else
            {
                //S = 0 when the difference is zero
                S = 0;
                //H = 4 + (R-G) hue is offset by 4 when blue, and is the difference between the two smallest colors
                H = offset + (colorone - colortwo);
            }

            H /= 6;

            //conversion values
            if (H < 0)
                H += 1.0f;
        }
        else
        {
            S = 0;
            H = 0;
        }
    }

}