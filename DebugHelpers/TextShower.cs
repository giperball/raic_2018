using System.Collections.Generic;
using Newtonsoft.Json.Linq;

namespace ConsoleApp1.DebugHelpers
{
    public class TextShower : IDebugHelper
    {
        private readonly string _text;
        
        public TextShower(string text)
        {
            _text = text;
        }

        public string Text
        {
            get { return _text; }
        }

        public List<JObject> MakeJObjects()
        {
            return null;
        }

        public JObject MakeJObject()
        {
            JObject result = new JObject();
            result["Text"] = Text;
            return result;
        }
    }
}