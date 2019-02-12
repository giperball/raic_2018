
using System.Collections.Generic;
using ConsoleApp1.DebugHelpers;
using Newtonsoft.Json.Linq;

namespace ConsoleApp1.Common
{
    public class SphereShower : IDebugHelper
    {
        public SphereShower(Vector3 pos, Color color, double radius = 2)
        {
            x = pos.x;
            y = pos.y;
            z = pos.z;
            r = color.r;
            g = color.g;
            b = color.b;
            a = color.a;
            this.radius = radius;
        }
        
        public double x;
        public double y;
        public double z;
        public double radius = 0.1;
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
            result["Sphere"] = JObject.FromObject(this);
            return result;
        }
    }
}