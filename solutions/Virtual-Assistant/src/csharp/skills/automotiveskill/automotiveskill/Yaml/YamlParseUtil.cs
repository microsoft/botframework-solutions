// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using SharpYaml;
using SharpYaml.Events;

namespace AutomotiveSkill.Yaml
{
    /// <summary>
    /// Utilities for YAML parsing.
    /// </summary>
    public class YamlParseUtil
    {
        private YamlParseUtil()
        {
        }

        /// <summary>
        /// Parse an entire YAML document as an instance of the given type.
        /// Note that this method is not suitable to parse parts of a YAML document
        /// because it throws an exception if the end of the stream has not been reached after parsing an instance of the given type.
        /// </summary>
        /// <typeparam name="T">The type to parse the YAML document as.</typeparam>
        /// <param name="reader">A reader over the YAML data.</param>
        /// <returns>The parsed instance.</returns>
        public static T ParseDocument<T>(TextReader reader)
        {
            var parser = new Parser(reader);
            SkipStreamStart(parser);

            var result = ObjectFromYaml<T>(parser);

            CheckStreamEnd(parser);
            return result;
        }

        /// <summary>
        /// Check that the parser is positioned at the start of a YAML mapping and advance past it.
        /// This method would typically be called from the 'FromYaml' method of a custom class.
        /// </summary>
        /// <param name="parser">A YAML parser.</param>
        internal static void ConsumeMappingStart(IParser parser)
        {
            Consume<MappingStart>(parser, "start of mapping");
        }

        /// <summary>
        /// Check that the parser is positioned at the end of a YAML mapping and advance past it.
        /// This method would typically be called from the 'FromYaml' method of a custom class.
        /// </summary>
        /// <param name="parser">A YAML parser.</param>
        internal static void ConsumeMappingEnd(IParser parser)
        {
            Consume<MappingEnd>(parser, "end of mapping");
        }

        /// <summary>
        /// Returns an exception for encountering an unknown key while parsing an object of the given type.
        /// This method would typically be called from the 'FromYaml' method of a custom class.
        /// </summary>
        /// <typeparam name="T">The custom type currently being parsed.</typeparam>
        /// <param name="parser">A YAML parser.</param>
        /// <param name="key">The unknown key.</param>
        /// <returns>The exception to throw.</returns>
        internal static YamlParseException UnknownKeyWhileParsing<T>(IParser parser, string key)
        {
            return new YamlParseException($"Unknown key \"{key}\" while parsing {typeof(T)} at {parser.Current.Start}");
        }

        /// <summary>
        /// Parse a string value.
        /// This method would typically be called from the 'FromYaml' method of a custom class.
        /// </summary>
        /// <param name="parser">A YAML parser.</param>
        /// <returns>The parsed string value.</returns>
        internal static string StringFromYaml(IParser parser)
        {
            var scalar = Consume<Scalar>(parser, "scalar");
            return scalar.Value;
        }

        /// <summary>
        /// Parse a boolean value.
        /// This method would typically be called from the 'FromYaml' method of a custom class.
        /// </summary>
        /// <param name="parser">A YAML parser.</param>
        /// <returns>The parsed value.</returns>
        internal static bool BoolFromYaml(IParser parser)
        {
            var str = StringFromYaml(parser);

            if (!bool.TryParse(str, out bool result))
            {
                throw new YamlParseException($"Failed to parse scalar as bool: \"{str}\" at {parser.Current.Start}.");
            }

            return result;
        }

        /// <summary>
        /// Parse an integer value.
        /// This method would typically be called from the 'FromYaml' method of a custom class.
        /// </summary>
        /// <param name="parser">A YAML parser.</param>
        /// <returns>The parsed value.</returns>
        internal static int IntFromYaml(IParser parser)
        {
            var str = StringFromYaml(parser);

            if (!int.TryParse(str, out int result))
            {
                throw new YamlParseException($"Failed to parse scalar as int: \"{str}\" at {parser.Current.Start}.");
            }

            return result;
        }

        /// <summary>
        /// Parse a long value.
        /// This method would typically be called from the 'FromYaml' method of a custom class.
        /// </summary>
        /// <param name="parser">A YAML parser.</param>
        /// <returns>The parsed value.</returns>
        internal static long LongFromYaml(IParser parser)
        {
            var str = StringFromYaml(parser);

            if (!long.TryParse(str, out long result))
            {
                throw new YamlParseException($"Failed to parse scalar as long: \"{str}\" at {parser.Current.Start}.");
            }

            return result;
        }

        /// <summary>
        /// Parse a double value.
        /// This method would typically be called from the 'FromYaml' method of a custom class.
        /// </summary>
        /// <param name="parser">A YAML parser.</param>
        /// <returns>The parsed value.</returns>
        internal static double DoubleFromYaml(IParser parser)
        {
            var str = StringFromYaml(parser);

            if (!double.TryParse(str, out double result))
            {
                throw new YamlParseException($"Failed to parse scalar as double: \"{str}\" at {parser.Current.Start}.");
            }

            return result;
        }

        /// <summary>
        /// Parse a list of values.
        /// This method would typically be called from the 'FromYaml' method of a custom class.
        /// </summary>
        /// <typeparam name="TElement">The type of the list elements.</typeparam>
        /// <param name="parser">A YAML parser.</param>
        /// <returns>The parsed list.</returns>
        internal static List<TElement> ListFromYaml<TElement>(IParser parser)
        {
            return ObjectFromYaml<List<TElement>>(parser);
        }

        /// <summary>
        /// Parse a dictionary of values.
        /// This method would typically be called from the 'FromYaml' method of a custom class.
        /// </summary>
        /// <typeparam name="TKey">The type of the dictionary keys.</typeparam>
        /// <typeparam name="TValue">The type of the dictionary values.</typeparam>
        /// <param name="parser">A YAML parser.</param>
        /// <returns>The parsed dictionary.</returns>
        internal static Dictionary<TKey, TValue> DictionaryFromYaml<TKey, TValue>(IParser parser)
        {
            return ObjectFromYaml<Dictionary<TKey, TValue>>(parser);
        }

        private static object ListFromYaml(IParser parser, Type elementType)
        {
            Consume<SequenceStart>(parser, "start of sequence");

            var type = typeof(List<>).MakeGenericType(new Type[] { elementType });

            var constructor = type.GetConstructor(new Type[] { });
            var list = constructor.Invoke(new object[] { });

            var addMethod = type.GetRuntimeMethod("Add", new Type[] { elementType });
            while (!(parser.Current is SequenceEnd))
            {
                var element = ObjectFromYaml(parser, elementType);
                addMethod.Invoke(list, new object[] { element });
            }

            Consume<SequenceEnd>(parser, "end of sequence");
            return list;
        }

        private static object DictionaryFromYaml(IParser parser, Type keyType, Type valueType)
        {
            ConsumeMappingStart(parser);

            var type = typeof(Dictionary<,>).MakeGenericType(new Type[] { keyType, valueType });

            var constructor = type.GetConstructor(new Type[] { });
            var dictionary = constructor.Invoke(new object[] { });

            var itemProperty = type.GetRuntimeProperty("Item");
            while (!(parser.Current is MappingEnd))
            {
                var key = ObjectFromYaml(parser, keyType);
                var value = ObjectFromYaml(parser, valueType);
                itemProperty.SetValue(dictionary, value, new object[] { key });
            }

            ConsumeMappingEnd(parser);
            return dictionary;
        }

        private static T ObjectFromYaml<T>(IParser parser)
        {
            return (T)ObjectFromYaml(parser, typeof(T));
        }

        private static object ObjectFromYaml(IParser parser, Type type)
        {
            if (typeof(string) == type)
            {
                return StringFromYaml(parser);
            }
            else if (typeof(bool) == type)
            {
                return BoolFromYaml(parser);
            }
            else if (typeof(int) == type)
            {
                return IntFromYaml(parser);
            }
            else if (typeof(long) == type)
            {
                return LongFromYaml(parser);
            }
            else if (typeof(double) == type)
            {
                return DoubleFromYaml(parser);
            }
            else if (ImplementsGenericInterface(type, typeof(IList<object>)))
            {
                var typeArgs = type.GetGenericArguments();
                return ListFromYaml(parser, typeArgs[0]);
            }
            else if (ImplementsGenericInterface(type, typeof(IDictionary<object, object>)))
            {
                var typeArgs = type.GetGenericArguments();
                return DictionaryFromYaml(parser, typeArgs[0], typeArgs[1]);
            }

            var method = type.GetRuntimeMethod("FromYaml", new Type[] { typeof(IParser) });
            if (method == null)
            {
                throw new YamlParseException($"Don't know how to parse an object of type {type} because it does not have the method: public static {type.Name} FromYaml(IParser)");
            }

            return method.Invoke(null, new object[] { parser });
        }

        private static bool ImplementsGenericInterface(Type type, Type genericInterfaceOfObject)
        {
            if (!type.IsGenericType)
            {
                return false;
            }

            var interfaceArgs = genericInterfaceOfObject.GetGenericArguments();
            if (type.GetGenericArguments().Length != interfaceArgs.Length)
            {
                return false;
            }

            var typeArgs = new Type[interfaceArgs.Length];
            for (int i = 0; i < interfaceArgs.Length; ++i)
            {
                typeArgs[i] = typeof(object);
            }

            var objectType = type.GetGenericTypeDefinition().MakeGenericType(typeArgs);

            if (Enumerable.Any(interfaceArgs, t => t != typeof(object)))
            {
                genericInterfaceOfObject = genericInterfaceOfObject.GetGenericTypeDefinition().MakeGenericType(typeArgs);
            }

            return genericInterfaceOfObject.IsAssignableFrom(objectType);
        }

        private static void SkipStreamStart(IParser parser)
        {
            // This is necessary to make the parser start looking at the input.
            parser.MoveNext();

            while (parser.Current is StreamStart || parser.Current is DocumentStart)
            {
                parser.MoveNext();
            }
        }

        private static void CheckStreamEnd(IParser parser)
        {
            while (parser.MoveNext())
            {
                if (!IsStreamEnd(parser))
                {
                    throw new YamlParseException($"Expected end of document or stream at {parser.Current.Start}.");
                }
            }
        }

        private static void CheckNotStreamEnd(IParser parser, string expectation)
        {
            if (IsStreamEnd(parser))
            {
                string endOfWhat = "stream";
                if (parser.Current is DocumentEnd)
                {
                    endOfWhat = "document";
                }

                string expectationSentence = string.Empty;
                if (!string.IsNullOrEmpty(expectation))
                {
                    expectationSentence = $" Expected {expectation}.";
                }

                throw new YamlParseException($"Unexpected end of {endOfWhat} at {parser.Current.Start}.{expectationSentence}");
            }
        }

        private static bool IsStreamEnd(IParser parser)
        {
            return parser.Current == null || parser.Current is StreamEnd || parser.Current is DocumentEnd;
        }

        private static T Consume<T>(IParser parser, string expectation)
            where T : ParsingEvent
        {
            CheckNotStreamEnd(parser, expectation);
            if (!(parser.Current is T result))
            {
                throw new YamlParseException($"Expected {expectation} at {parser.Current.Start}.");
            }

            parser.MoveNext();
            return result;
        }
    }
}
