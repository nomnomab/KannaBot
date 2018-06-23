using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text.RegularExpressions;

namespace KannaBot.Scripts
{
    public static class Extensions
    {
        public static IEnumerable<Type> FindDerivedTypesFromAssembly<T>(this Assembly assembly, bool classOnly)
        {
            Type baseType = typeof(T);

            if (assembly == null)
                throw new ArgumentNullException("assembly", "Assembly must be defined");
            if (baseType == null)
                throw new ArgumentNullException("baseType", "Parent Type must be defined");

            // get all the types
            var types = assembly.GetTypes();

            // works out the derived types
            foreach (var type in types)
            {
                // if classOnly, it must be a class
                // useful when you want to create instance
                if (classOnly && !type.IsClass)
                    continue;

                if (baseType.IsInterface)
                {
                    var it = type.GetInterface(baseType.FullName);

                    if (it != null)
                        // add it to result list
                        yield return type;
                }
                else if (type.IsSubclassOf(baseType))
                {
                    // add it to result list
                    yield return type;
                }
            }
        }
        public static string ToArrayString(this object[] array, string seperator)
        {
            string line = string.Empty;
            foreach (object o in array) line += o.ToString() + seperator;
            line = line.Substring(0, line.Length - seperator.Length);
            return line;
        }

        public static string CleanString(this string str)
        {
            //Parse out non-ascii chinese characters
            return Regex.Replace(str, @"[^\u0000-\u007F]+", string.Empty);
        }

        public static string ExpandUnicode(this string str)
        {
            // \U00000031
            // U+31
            // \U000020e3
            string[] split = str.Split('+');
            split[0] = "\\U";
            string zeros = "";
            for (int i = 0; i < 8 - split[1].Length; i++) zeros += "0";
            split[1] = zeros + split[1];
            return split[0] + split[1];
        }
    }
}