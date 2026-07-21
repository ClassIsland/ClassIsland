using System;
using System.Collections.Generic;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media;

namespace ClassIsland.Controls.GesturePassword;

public class GesturePasswordGrid : Control
{
    private const int GridSize = 3;
    private const double NodeRadius = 12;
    private const double HitRadius = 28;
    private const double LineThickness = 3;

    private static readonly SolidColorBrush s_nodeDefaultBrush = new(Color.Parse("#B0B0B0"));
    private static readonly SolidColorBrush s_nodeSelectedBrush = new(Color.Parse("#2196F3"));
    private static readonly SolidColorBrush s_nodeHighlightBrush = new(Color.Parse("#64B5F6"));
    private static readonly SolidColorBrush s_lineBrush = new(Color.Parse("#2196F3"));
    private static readonly SolidColorBrush s_linePendingBrush = new(Color.Parse("#90CAF9"));
    private static readonly SolidColorBrush s_innerRingBrush = new(Colors.White);

    private readonly List<int> _selectedNodes = [];
    private readonly Point[] _nodePositions = new Point[GridSize * GridSize];
    private bool _isPressed;
    private Point _currentPointer;
    private int _lastNodeIndex = -1;

    public static readonly StyledProperty<int[]?> PathProperty =
        AvaloniaProperty.Register<GesturePasswordGrid, int[]?>(nameof(Path));

    public int[]? Path
    {
        get => GetValue(PathProperty);
        set => SetValue(PathProperty, value);
    }

    public event EventHandler<int[]>? GestureCompleted;

    public GesturePasswordGrid()
    {
        ClipToBounds = true;
    }

    private bool EnsureNodePositions()
    {
        var w = Bounds.Width;
        var h = Bounds.Height;
        if (w <= 0 || h <= 0) return false;

        var padding = HitRadius + 4;
        var cellW = (w - padding * 2) / (GridSize - 1);
        var cellH = (h - padding * 2) / (GridSize - 1);

        for (var row = 0; row < GridSize; row++)
        for (var col = 0; col < GridSize; col++)
        {
            _nodePositions[row * GridSize + col] = new Point(
                padding + col * cellW,
                padding + row * cellH);
        }
        return true;
    }

    private int HitTestNode(Point position)
    {
        for (var i = 0; i < _nodePositions.Length; i++)
        {
            var dx = position.X - _nodePositions[i].X;
            var dy = position.Y - _nodePositions[i].Y;
            if (dx * dx + dy * dy <= HitRadius * HitRadius)
                return i;
        }
        return -1;
    }

    private void InsertIntermediateNode(int from, int to)
    {
        if (from < 0 || to < 0 || from == to) return;

        var fromRow = from / GridSize;
        var fromCol = from % GridSize;
        var toRow = to / GridSize;
        var toCol = to % GridSize;

        if ((fromRow + toRow) % 2 == 0 && (fromCol + toCol) % 2 == 0)
        {
            var midRow = (fromRow + toRow) / 2;
            var midCol = (fromCol + toCol) / 2;
            var mid = midRow * GridSize + midCol;
            if (mid != from && mid != to && !_selectedNodes.Contains(mid))
            {
                _selectedNodes.Add(mid);
            }
        }
    }

    protected override void OnPointerPressed(PointerPressedEventArgs e)
    {
        base.OnPointerPressed(e);
        var pos = e.GetPosition(this);
        var nodeIndex = HitTestNode(pos);
        if (nodeIndex < 0) return;

        _isPressed = true;
        _selectedNodes.Clear();
        _selectedNodes.Add(nodeIndex);
        _lastNodeIndex = nodeIndex;
        _currentPointer = pos;

        e.Pointer.Capture(this);
        e.Handled = true;
        InvalidateVisual();
    }

    protected override void OnPointerMoved(PointerEventArgs e)
    {
        base.OnPointerMoved(e);
        if (!_isPressed) return;

        e.Handled = true;
        var pos = e.GetPosition(this);
        _currentPointer = pos;

        var nodeIndex = HitTestNode(pos);
        if (nodeIndex >= 0 && nodeIndex != _lastNodeIndex && !_selectedNodes.Contains(nodeIndex))
        {
            InsertIntermediateNode(_lastNodeIndex, nodeIndex);
            _selectedNodes.Add(nodeIndex);
            _lastNodeIndex = nodeIndex;
        }

        InvalidateVisual();
    }

    protected override void OnPointerReleased(PointerReleasedEventArgs e)
    {
        base.OnPointerReleased(e);
        if (!_isPressed) return;
        _isPressed = false;
        e.Pointer.Capture(null);
        e.Handled = true;

        if (_selectedNodes.Count >= 4)
        {
            var path = _selectedNodes.ToArray();
            Path = path;
            GestureCompleted?.Invoke(this, path);
        }
        else
        {
            Reset();
        }
    }

    protected override void OnPointerCaptureLost(PointerCaptureLostEventArgs e)
    {
        base.OnPointerCaptureLost(e);
        if (_isPressed)
        {
            _isPressed = false;
            Reset();
        }
    }

    public void Reset()
    {
        _selectedNodes.Clear();
        _lastNodeIndex = -1;
        _currentPointer = default;
        Path = null;
        InvalidateVisual();
    }

    public override void Render(DrawingContext context)
    {
        base.Render(context);
        if (!EnsureNodePositions()) return;

        DrawLines(context);
        DrawNodes(context);
        DrawPendingLine(context);
    }

    private void DrawLines(DrawingContext context)
    {
        if (_selectedNodes.Count < 2) return;

        var pen = new Pen(s_lineBrush, LineThickness, lineCap: PenLineCap.Round, lineJoin: PenLineJoin.Round);
        for (var i = 0; i < _selectedNodes.Count - 1; i++)
        {
            var from = _nodePositions[_selectedNodes[i]];
            var to = _nodePositions[_selectedNodes[i + 1]];
            context.DrawLine(pen, from, to);
        }
    }

    private void DrawPendingLine(DrawingContext context)
    {
        if (!_isPressed || _lastNodeIndex < 0) return;

        var pen = new Pen(s_linePendingBrush, LineThickness, lineCap: PenLineCap.Round);
        var from = _nodePositions[_lastNodeIndex];
        context.DrawLine(pen, from, _currentPointer);
    }

    private void DrawNodes(DrawingContext context)
    {
        for (var i = 0; i < _nodePositions.Length; i++)
        {
            var pos = _nodePositions[i];
            var isSelected = _selectedNodes.Contains(i);
            var isLast = i == _lastNodeIndex && _isPressed;

            var brush = isSelected
                ? (isLast ? s_nodeHighlightBrush : s_nodeSelectedBrush)
                : s_nodeDefaultBrush;

            var radius = isLast ? NodeRadius + 3 : NodeRadius;

            context.DrawEllipse(brush, null, pos, radius * 2, radius * 2);

            if (isSelected)
            {
                var innerPen = new Pen(s_innerRingBrush, 2);
                context.DrawEllipse(null, innerPen, pos, (radius - 5) * 2, (radius - 5) * 2);
            }
        }
    }
}
