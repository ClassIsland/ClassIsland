using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;

using Color = System.Drawing.Color;

namespace ClassIsland;

public class ColorOctTreeNode
{
    public int LeafNum = 0;
    public List<ColorOctTreeNode>[] ToReduce = Enumerable.Repeat(new List<ColorOctTreeNode>(), 8).ToArray();

    public ColorOctTreeNode?[] Children = new ColorOctTreeNode?[8] {null, null , null , null , null , null , null , null };
    public bool IsLeaf = false;
    public int r = 0;
    public int g = 0;
    public int b = 0;
    public int ChildrenCount = 0;
    public ColorOctTreeNode Root;

    public ColorOctTreeNode(ColorOctTreeNode? root, int index, int level)
    {
        Root = root ?? this;
        if (root == null)
        {
            return;
        }
        if (level == 7)
        {
            IsLeaf = true;
            Root.LeafNum++;
        }
        else
        {
            Root.ToReduce[level].Add(this);
            Root.ToReduce[level] = Root.ToReduce[level].OrderBy(i => i.ChildrenCount).ToList();
        }
    }

    public void AddColor(Color color, int level)
    {
        if (IsLeaf)
        {
            ChildrenCount++;
            this.r += color.R;
            this.g += color.G;
            this.b += color.B;
        }
        else
        {
            var str = "";
            
            var r1 = Convert.ToString(color.R, 2).PadLeft(8, '0');
            var g1 = Convert.ToString(color.G, 2).PadLeft(8, '0');
            var b1 = Convert.ToString(color.B, 2).PadLeft(8, '0');

            str += r1[level];
            str += g1[level];
            str += b1[level];

            var index = Convert.ToInt32(str, 2);
            Children[index] ??= new ColorOctTreeNode(Root, index, level + 1);
            Children[index]!.AddColor(color, level + 1);
        }
    }

    public void ReduceTree()
    {
        var lv = 6;
        while (lv >= 0 && Root.ToReduce[lv].Count == 0)
        {
            lv--;
        }

        if (lv < 0)
        {
            return;
        }

        var node = Root.ToReduce[lv].Last();
        Root.ToReduce[lv].Remove(node);

        // Merge children
        node.IsLeaf = true;
        node.r = 0;
        node.g = 0;
        node.b = 0;
        node.ChildrenCount = 0;
        for (var i = 0; i < 8; i++)
        {
            if (node.Children[i] == null)
            {
                continue;
            }
            var child = node.Children[i]!;
            node.r += child.r;
            node.g += child.g;
            node.b += child.b;
            node.ChildrenCount += child.ChildrenCount;
            Root.LeafNum--;
        }

        Root.LeafNum++;
    }

    public static void ColorStats(ColorOctTreeNode node, Dictionary<string, int> record)
    {
        if (node.IsLeaf)
        {
            var r = Convert.ToString(~~(node.r / node.ChildrenCount), 16).PadLeft(2, '0')!;
            var g = Convert.ToString(~~(node.g / node.ChildrenCount), 16).PadLeft(2, '0')!;
            var b = Convert.ToString(~~(node.b / node.ChildrenCount), 16).PadLeft(2, '0')!;

            var color = $"#{r}{g}{b}";
            if (record.Keys.Contains(color))
            {
                record[color] += node.ChildrenCount;
            }
            else
            {
                record[color] = node.ChildrenCount;
            }

            return;
        }

        for (var i = 0; i < 8; i++)
        {
            if (node.Children[i] != null)
            {
                ColorStats(node.Children[i]!, record);
            }
        }
    }

    public static Dictionary<string, int> ProcessImage(Bitmap img)
    {
        var root = new ColorOctTreeNode(null, 0, 0);
        for (var x = 0; x < img.Width; x++)
        {
            for (var y = 0; y < img.Height; y++)
            {
                root.AddColor(img.GetPixel(x, y), 0);
                while (root.LeafNum > 16) 
                {
                    root.ReduceTree();
                }
            }
        }

        var r = new Dictionary<string, int>();
        ColorStats(root, r);
        return r;
    }
}