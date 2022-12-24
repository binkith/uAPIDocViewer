using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace uAPIDocViewer.Models
{
    public class Node
    {
        public Node(bool hasParent = true)
        {
            Children = new List<Node>();
            HasParent = hasParent;
        }

        public int Key { get; set; }
        public int ParentKey { get; set; }
        public string Type { get; set; }
        public string ElementName { get; set; }
        public bool HasParent { get; set; }
        public List<string> AdditionalData { get; set; }
        public List<Node> Children { get; set; }
    }

    public class GeneralTree
    {
        public Node Root { get; set; }

        #region "Finding the node by node type"
        public Node GetNode(string nodeType)
        {
            if (Root == null)
                return null;
            else if (Root.Type == nodeType)
                return Root;
            else
                return Find(nodeType, Root.Children);
        }

        private Node Find(string nodeType, List<Node> children)
        {
            Node resultNode = null;

            foreach (var child in children)
            {
                if (child.Type == nodeType)
                {
                    return children.OrderBy(x => x.Key).Last();
                    //return child;
                }
                else if (child.Children.Count > 0)
                    return Find(nodeType, child.Children);
            }

            return resultNode;
        }
        #endregion

        #region "Finding the node by node key"
        public Node GetNode(int key)
        {
            if (Root == null)
                return null;
            else if (Root.Key == key)
                return Root;
            else
                return Find(key, Root.Children);
        }

        private Node Find(int key, List<Node> children)
        {
            Node resultNode = null;

            foreach (var child in children)
            {
                if (child.Key == key)
                    return child;
                else if (child.Children.Count > 0)
                {
                    var node = Find(key, child.Children);
                    if (node != null)
                        return node;
                }
            }

            return resultNode;
        }
        #endregion

        public void AddChild(Node parentNode, Node child)
        {
            parentNode.Children.Add(new Node
            {
                Key = child.Key,
                Type = child.Type,
                ElementName = child.ElementName,
                ParentKey = child.ParentKey,
                HasParent = child.HasParent,
                AdditionalData = child.AdditionalData
            });
        }
    }
}