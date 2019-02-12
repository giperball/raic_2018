using System.Collections.Generic;
using Newtonsoft.Json.Linq;

namespace ConsoleApp1.DebugHelpers
{
    public interface IDebugHelper
    {
        List<JObject> MakeJObjects();
        JObject MakeJObject();
    }
}