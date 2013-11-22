using System;
using System.Collections.Generic;
using System.Linq;

namespace MultipartDataMediaFormatter.Infrastructure
{
    public class FormData
    {
        private List<ValueFile> _Files;
        private List<ValueString> _Fields;

        public List<ValueFile> Files
        {
            get
            {
                if(_Files == null)
                    _Files = new List<ValueFile>();
                return _Files;
            }
            set
            {
                _Files = value;
            }
        }

        public List<ValueString> Fields
        {
            get
            {
                if(_Fields == null)
                    _Fields = new List<ValueString>();
                return _Fields;
            }
            set
            {
                _Fields = value;
            }
        }

        public void Add(string name, string value)
        {
            Fields.Add(new ValueString() { Name = name, Value = value});
        }

        public void Add(string name, HttpFile value)
        {
            Files.Add(new ValueFile() { Name = name, Value = value });
        }

        public bool TryGetValue(string name, out string value)
        {
            var field = Fields.FirstOrDefault(m => String.Equals(m.Name, name, StringComparison.CurrentCultureIgnoreCase));
            if (field != null)
            {
                value = field.Value;
                return true;
            }
            value = null;
            return false;
        }

        public bool TryGetValue(string name, out HttpFile value)
        {
            var field = Files.FirstOrDefault(m => String.Equals(m.Name, name, StringComparison.CurrentCultureIgnoreCase));
            if (field != null)
            {
                value = field.Value;
                return true;
            }
            value = null;
            return false;
        }

        public class ValueString
        {
            public string Name { get; set; }
            public string Value { get; set; }
        }

        public class ValueFile
        {
            public string Name { get; set; }
            public HttpFile Value { get; set; }
        }
    }
}
