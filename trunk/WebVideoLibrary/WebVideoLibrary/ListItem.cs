using System;
using System.Collections.Generic;
using System.Text;

namespace WebVideoLibrary
{
    /// <summary>
    /// A simple class for representing a name value pair in a combo box.
    /// </summary>
    class ListItem
    {
        public string Name { get; set; }
        public object Value { get; set; }

        public ListItem(string name, object value)
        {
            Name = name;
            Value = value;
        }

        public override string ToString()
        {
            return Name;
        }
    }
}
