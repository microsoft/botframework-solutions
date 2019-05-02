using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using Microsoft.Bot.Protocol;

namespace Microsoft.Bot.Builder.Skills.Protocol
{
    public class Router
    {
        private readonly IEnumerable<RouteTemplate> _routes;
        private readonly TrieNode _root;

        public Router(IEnumerable<RouteTemplate> routes)
        {
            _routes = routes;
            _root = new TrieNode("root");
            Compile();
        }

        public RouteContext Route(ReceiveRequest request)
        {
            var found = true;
            string path = request.Path;
            if (Uri.IsWellFormedUriString(path, UriKind.Absolute))
            {
                var uri = new Uri(path);
                path = uri.AbsolutePath;
            }

            if (path.StartsWith("/"))
            {
                path = path.Substring(1);
            }

            var parts = path.Split('/');

            if (_root.TryGetNext(request.Verb, out TrieNode current))
            {
                var routeData = new ExpandoObject() as IDictionary<string, object>;
                foreach (var part in parts)
                {
                    if (current.TryGetNext(part, out TrieNode next))
                    {
                        // found an exact match, keep going
                        current = next;
                    }
                    else
                    {
                        // check for variables and continue
                        var variables = current.GetVariables();
                        if (variables.Any())
                        {
                            // TODO: we are only going to allow 1 variable for now
                            current = variables.First();
                            routeData[current.VariableName] = part;
                        }
                        else
                        {
                            found = false;
                        }
                    }
                }

                if (found && current.Action != null)
                {
                    return new RouteContext()
                    {
                        Request = request,
                        RouteData = routeData,
                        Action = current.Action,
                    };
                }
            }

            return null;
        }

        private void Compile()
        {
            foreach (var route in _routes)
            {
                var path = route.Path;
                if (path.StartsWith("/"))
                {
                    path = path.Substring(1);
                }

                var parts = path.Split('/');

                var methodNode = _root.Add(route.Method.ToUpperInvariant());
                TrieNode current = methodNode;
                foreach (var part in parts)
                {
                    current = current.Add(part);
                }

                if (current.Action != null)
                {
                    throw new InvalidOperationException("Route already exists");
                }

                current.Action = route.Action;
            }
        }

        private class TrieNode
        {
            public TrieNode(string value)
            {
                Value = value;
                IsVariable = value[0] == '{' && value[value.Length - 1] == '}';
                if (IsVariable)
                {
                    VariableName = value.Substring(1, value.Length - 2);
                }

                Next = new Dictionary<string, TrieNode>();
            }

            public string Value { get; private set; }

            public bool IsVariable { get; private set; }

            public string VariableName { get; private set; }

            public RouteAction Action { get; set; }

            public IDictionary<string, TrieNode> Next { get; set; }

            public TrieNode Add(string value)
            {
                if (!Next.TryGetValue(value, out TrieNode next))
                {
                    next = new TrieNode(value);
                    Next.Add(value, next);
                }

                return next;
            }

            public bool TryGetNext(string value, out TrieNode node)
            {
                return Next.TryGetValue(value, out node);
            }

            public IEnumerable<TrieNode> GetVariables()
            {
                return Next.Values.Where(n => n.IsVariable);
            }
        }
    }
}