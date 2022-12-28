using HtmlAgilityPack;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Web;
using System.Web.Mvc;
using uAPIDocViewer.Models;

namespace uAPIDocViewer.Controllers
{
    public class HomeController : Controller
    {
        public ActionResult Index()
        {
            return View();
        }

        public ActionResult Viewer(string targetURL = null, string uiFormat = "table")
        {
            try
            {
                if (string.IsNullOrEmpty(targetURL))
                {
                    ViewBag.ExceptionOccurred = true;
                    ViewBag.Message = "Enter the URL below and click submit";
                    return View();
                }

                HtmlWeb web = new HtmlWeb();
                HtmlDocument htmlSnippet = web.Load(targetURL);

                var tree = BuildTree(ref htmlSnippet);

                if (uiFormat == "table" || uiFormat == "default")
                {
                    var headingTds = htmlSnippet.DocumentNode
                        .Descendants("table").First()
                        .Descendants("thead").First()
                        .Descendants("tr").ToList()[0]
                        .Descendants("th").ToList();

                    ViewBag.Content = BuildTable(ref tree, ref headingTds);
                }
                else if (uiFormat == "list")
                {
                    ViewBag.Content = BuildUlLi(ref tree);
                }
                else
                {
                    ViewBag.Content = BuildUlLiForChart(ref tree);
                }

                ViewBag.TargetURL = targetURL;
                ViewBag.UiFormat = uiFormat;
                ViewBag.ExceptionOccurred = false;
            }
            catch (Exception ex)
            {
                ViewBag.ExceptionOccurred = true;
                ViewBag.Message = ex.Message;
            }

            return View();
        }

        private GeneralTree BuildTree(ref HtmlDocument htmlDocument)
        {
            var trs = htmlDocument.DocumentNode
                .Descendants("table").First()
                .Descendants("tbody").First()
                .Descendants("tr").ToList()
                .ToList();

            GeneralTree tree = new GeneralTree();

            int lastNodeKey = 0;
            int lastElementIndex = -1;

            for (int currentTrIndex = 0, key = 0; currentTrIndex < trs.Count; currentTrIndex++)
            {
                var currentTr = trs[currentTrIndex];
                string currentNodeType = currentTr.Attributes["id"].Value.ToLower();
                var tds = currentTr.Descendants("td").ToList();
                string currentElementName = tds.First().InnerText;
                //tds = tds.Skip(1).ToList();

                int currentElementIndex;
                if (currentNodeType == "complex")
                    currentElementIndex = -2;
                else if (currentNodeType == "elements" || currentNodeType == "attrs")
                    currentElementIndex = -1;
                else if (currentNodeType == "elements0" || currentNodeType == "attributes")
                    currentElementIndex = 0;
                else if (currentNodeType.Contains("elements"))
                    currentElementIndex = int.Parse(Regex.Match(currentNodeType, @"\d+").Value);
                else
                    currentElementIndex = int.Parse(Regex.Match(currentNodeType, @"\d+").Value) + 1;

                if (currentTrIndex == 0)
                {
                    key = key + 1;
                    tree.Root = new Node
                    {
                        Key = key,
                        Type = currentNodeType,
                        ElementName = currentElementName,
                        HasParent = false,
                        ParentKey = 0,
                        AdditionalData = tds.Select(x => x.InnerText).ToList()
                    };

                    lastNodeKey = key;
                    lastElementIndex = currentElementIndex;
                }
                else
                {
                    if (currentElementIndex > lastElementIndex)
                    {
                        var node = tree.GetNode(lastNodeKey);

                        key = key + 1;
                        tree.AddChild(node, new Node
                        {
                            Key = key,
                            Type = currentNodeType,
                            ElementName = currentElementName,
                            ParentKey = node.Key,
                            AdditionalData = tds.Select(x => x.InnerText).ToList()
                        });

                        lastNodeKey = key;
                        lastElementIndex = currentElementIndex;
                    }
                    else if (currentElementIndex == lastElementIndex)
                    {
                        var node = tree.GetNode(lastNodeKey);
                        node = tree.GetNode(node.ParentKey);

                        key = key + 1;
                        tree.AddChild(node, new Node
                        {
                            Key = key,
                            Type = currentNodeType,
                            ElementName = currentElementName,
                            ParentKey = node.Key,
                            AdditionalData = tds.Select(x => x.InnerText).ToList()
                        });

                        lastNodeKey = key;
                        lastElementIndex = currentElementIndex;
                    }
                    else
                    {
                        var node = FindParent(currentElementIndex, lastNodeKey, ref tree);

                        key = key + 1;
                        tree.AddChild(node, new Node
                        {
                            Key = key,
                            Type = currentNodeType,
                            ElementName = currentElementName,
                            ParentKey = node.Key,
                            AdditionalData = tds.Select(x => x.InnerText).ToList()
                        });

                        lastNodeKey = key;
                        lastElementIndex = currentElementIndex;
                    }
                }
            }

            return tree;
        }

        private Node FindParent(int targetElementIndex, int recentNodeKey, ref GeneralTree tree)
        {
            Node parentNode;

            while (true)
            {
                var node = tree.GetNode(recentNodeKey);
                string currentNodeType = node.Type.ToLower();

                int currentElementIndex;
                if (currentNodeType == "complex")
                    currentElementIndex = -2;
                else if (currentNodeType == "elements" || currentNodeType == "attrs")
                    currentElementIndex = -1;
                else if (currentNodeType == "elements0" || currentNodeType == "attributes")
                    currentElementIndex = 0;
                else if (currentNodeType.Contains("elements"))
                    currentElementIndex = int.Parse(Regex.Match(currentNodeType, @"\d+").Value);
                else
                    currentElementIndex = int.Parse(Regex.Match(currentNodeType, @"\d+").Value) + 1;

                if (currentElementIndex == targetElementIndex)
                {
                    parentNode = tree.GetNode(node.ParentKey);
                    break;
                }
                else
                {
                    recentNodeKey = node.ParentKey;
                }
            }

            return parentNode;
        }

        private string BuildUlLi(ref GeneralTree tree)
        {
            StringBuilder ulLi = new StringBuilder();

            ulLi.AppendLine("<ul class='tree'>");
            ulLi.AppendLine("<li>");
            ulLi.AppendLine("<details open>");
            ulLi.AppendLine($"<summary>{tree.Root.ElementName}</summary>");

            if (tree.Root.Children.Count > 0)
            {
                ulLi.AppendLine("<ul>");
                string result = WrapUlLIChildren(tree.Root.Children);
                ulLi.AppendLine(result);
                ulLi.AppendLine("</ul>");
            }

            ulLi.AppendLine("</details>");
            ulLi.AppendLine("</li>");
            ulLi.AppendLine("</ul>");

            return ulLi.ToString();
        }

        private string WrapUlLIChildren(List<Node> children)
        {
            StringBuilder liList = new StringBuilder();

            foreach (var child in children)
            {
                liList.AppendLine("<li>");
                if (child.Children.Count > 0)
                {
                    liList.AppendLine("<details open>");
                    liList.AppendLine($"<summary>{child.ElementName}</summary>");
                    liList.Append("<ul>");
                    liList.Append(WrapUlLIChildren(child.Children));
                    liList.Append("</ul>");
                    liList.AppendLine($"</summary");
                    liList.AppendLine("</details>");
                }
                else
                {
                    liList.AppendLine(child.ElementName);
                }


                liList.AppendLine("</li>");
            }

            return liList.ToString();
        }

        private string BuildUlLiForChart(ref GeneralTree tree)
        {
            StringBuilder ulLi = new StringBuilder();

            ulLi.AppendLine("<ul id='chartUlLiDataSource'>");
            ulLi.AppendLine("<li>");
            ulLi.AppendLine(tree.Root.ElementName);

            if (tree.Root.Children.Count > 0)
            {
                ulLi.AppendLine("<ul>");
                string result = WrapUlLIChildrenForChart(tree.Root.Children);
                ulLi.AppendLine(result);
                ulLi.AppendLine("</ul>");
            }

            ulLi.AppendLine("</li>");
            ulLi.AppendLine("</ul>");

            return ulLi.ToString();
        }

        private string WrapUlLIChildrenForChart(List<Node> children)
        {
            StringBuilder liList = new StringBuilder();

            foreach (var child in children)
            {
                liList.AppendLine("<li>");
                if (child.Children.Count > 0)
                {
                    liList.AppendLine(child.ElementName);
                    liList.Append("<ul>");
                    liList.Append(WrapUlLIChildrenForChart(child.Children));
                    liList.Append("</ul>");
                }
                else
                {
                    liList.AppendLine(child.ElementName);
                }

                liList.AppendLine("</li>");
            }

            return liList.ToString();
        }

        private string BuildTable(ref GeneralTree tree, ref List<HtmlNode> headingTds)
        {
            StringBuilder ulLi = new StringBuilder();

            ulLi.AppendLine("<table class='table table-bordered'>");

            ulLi.AppendLine("<tr class='bg-dark text-white'>");
            ulLi.AppendLine("<th>");
            ulLi.AppendLine($"#");
            ulLi.AppendLine("</th>");
            foreach (var item in headingTds)
            {
                ulLi.AppendLine("<th>");
                ulLi.AppendLine($"<span>{item.InnerText}</span>");
                ulLi.AppendLine("</th>");
            }
            ulLi.AppendLine("</tr>");


            ulLi.AppendLine("<tr class='root'>");
            ulLi.AppendLine("<td>");
            ulLi.AppendLine($"<button class='btn-toggle btn btn-sm btn-dark'>-</button>");
            ulLi.AppendLine("</td>");
            foreach (var item in tree.Root.AdditionalData)
            {
                ulLi.AppendLine("<td>");
                ulLi.AppendLine($"<span>{item}</span>");
                ulLi.AppendLine("</td>");
            }
            ulLi.AppendLine("</tr>");

            ulLi.AppendLine("<tr class='root'>");
            ulLi.AppendLine("<td colspan='7'>");
            if (tree.Root.Children.Count > 0)
            {
                ulLi.Append("<table class='table table-bordered'>");
                string result = WrapTableChildren(tree.Root.Children);
                ulLi.AppendLine(result);
                ulLi.Append("</table>");
            }

            ulLi.AppendLine("</td>");
            ulLi.AppendLine("</tr>");
            ulLi.AppendLine("</table>");

            return ulLi.ToString();
        }

        private string WrapTableChildren(List<Node> children)
        {
            StringBuilder liList = new StringBuilder();

            foreach (var child in children)
            {
                string nodeType = child.Type.Contains("element") ? "element" : "attribute";

                liList.AppendLine($"<tr class='{nodeType}'>");
                liList.AppendLine("<td>");
                if (nodeType == "element") liList.AppendLine($"<button class='btn-toggle btn btn-sm btn-dark'>-</button>");
                else liList.AppendLine($"");
                liList.AppendLine("</td>");

                foreach (var item in child.AdditionalData)
                {
                    liList.AppendLine("<td>");
                    liList.AppendLine($"<span>{item}</span>");
                    liList.AppendLine("</td>");
                }

                liList.AppendLine("</tr>");

                if (child.Children.Count > 0)
                {
                    //liList.AppendLine($"<tr class='{nodeType}'>");
                    ////liList.AppendLine("<td>");
                    ////liList.AppendLine($"<span>{child.ElementName}</span>");
                    ////liList.AppendLine("</td>");
                    //foreach (var item in child.AdditionalData)
                    //{
                    //    liList.AppendLine("<td>");
                    //    liList.AppendLine($"<span>{item}</span>");
                    //    liList.AppendLine("</td>");
                    //}
                    //liList.AppendLine("</tr>");

                    liList.AppendLine($"<tr class='{nodeType}'>");
                    liList.AppendLine("<td colspan='7'>");
                    liList.Append("<table class='table table-bordered'>");
                    liList.Append(WrapTableChildren(child.Children));
                    liList.Append("</table>");
                    liList.AppendLine("</td>");
                    liList.AppendLine("</tr>");
                }
            }

            return liList.ToString();
        }
    }
}