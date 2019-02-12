using System.Collections.Generic;
using System.Linq;
using ConsoleApp1.Common;
using Newtonsoft.Json.Linq;

namespace ConsoleApp1.DebugHelpers
{
    public class LineShower: IDebugHelper
    {
        public LineShower(Vector3 point_a, Vector3 point_b, Color color, double width = 0.1)
        {
            x1 = point_a.x;
            y1 = point_a.y;
            z1 = point_a.z;
            x2 = point_b.x;
            y2 = point_b.y;
            z2 = point_b.z;
            this.width = width;
            r = color.r;
            g = color.g;
            b = color.b;
            a = color.a;
        }

        public static List<IDebugHelper> ToLineDebugHelpers(IEnumerable<Vector3> points, Color color, double width = 5)
        {
            return points.Skip(1).Zip(points, (second, first) => (IDebugHelper)new LineShower(first, second, color, width)).ToList();
        }
        
        public double x1;
        public double y1;
        public double z1;
        public double x2;
        public double y2;
        public double z2;
        public double width;
        public double r;
        public double g;
        public double b;
        public double a;

        public List<JObject> MakeJObjects()
        {
            return null;
        }

        public JObject MakeJObject()
        {
            JObject result = new JObject();
            result["Line"] = JObject.FromObject(this);
            return result;
        }
    }
}