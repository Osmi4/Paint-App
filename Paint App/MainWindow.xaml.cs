using System;
using System.Collections.Generic;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using Newtonsoft.Json;
using System.IO;
using System.Windows.Navigation;
using System.Diagnostics;
using System.Security.Cryptography;

namespace Paint_App
{
    public partial class MainWindow : Window
    {
        private List<Tuple<Point, Point, bool, SolidColorBrush>> lines = new List<Tuple<Point, Point, bool, SolidColorBrush>>();
        private List<Tuple<Point, int, SolidColorBrush>> circles = new List<Tuple<Point, int, SolidColorBrush>>();
        private List<Tuple<List<Point>, SolidColorBrush>> polygons = new List<Tuple<List<Point>, SolidColorBrush>>();
        private List<List<Point>> curves = new List<List<Point>>();

        private Point startPoint;
        private Point centerPoint;
        private Point selectedCurvePoint;
        private bool isDrawingCurve = false;
        private bool isDrawingPolygon = false;
        private bool isDrawingLine = false;
        private bool isDrawingCircle = false;
        private bool isSelectingLine = false;
        private bool isSelectingCircle = false;
        private bool isSelectingPolygon = false;
        private bool isDraggingEndpoint = false;
        private bool isEditingRadius = false;
        private bool isMovingCircle = false;
        private bool isDraggingVertex = false;
        private bool isMovingCurve = false;

        List<Point> cubicBezierPoints = new List<Point>();
        private List<Point> polygonPoints = new List<Point>();
        private Tuple<Point, Point, bool, SolidColorBrush> lineBeingDragged;
        private Tuple<Point, Point, bool, SolidColorBrush> selectedLine = null;
        private Point? selectedPoint = null;
        private Point selectedVertex = new Point();
        private Tuple<Point, int, SolidColorBrush> selectedCircle = null;
        private Tuple<List<Point>, SolidColorBrush> selectedPolygon = null;

        private SolidColorBrush currentColor = new SolidColorBrush(Colors.Black);

        public MainWindow()
        {
            InitializeComponent();
        }

        private void UseTool_Click(object sender, RoutedEventArgs e)
        {
            if (ToolPicker.SelectedItem != null)
            {
                string functionName = ((ComboBoxItem)ToolPicker.SelectedItem).Tag.ToString();
                switch (functionName)
                {
                    case "DrawLine_Click":
                        DrawLine_Click(sender, e);
                        break;
                    case "DrawCircle_Click":
                        DrawCircle_Click(sender, e);
                        break;
                    case "DrawPolygon_Click":
                        DrawPolygon_Click(sender, e);
                        break;
                    case "SaveShapesButton_Click":
                        SaveShapesButton_Click(sender, e);
                        break;
                    case "LoadShapesButton_Click":
                        LoadShapesButton_Click(sender, e);
                        break;
                    case "TurnAntialiased_Click":
                        TurnAntialiased_Click(sender, e);
                        break;
                    case "DrawCurve_Click":
                        DrawCurve_Click(sender, e);
                        break;
                    default:
                        break;
                }
            }
        }

        private void ApplyAction_Click(object sender, RoutedEventArgs e)
        {
            if (ManipulationPicker.SelectedItem != null)
            {
                string functionName = ((ComboBoxItem)ManipulationPicker.SelectedItem).Tag.ToString();
                switch (functionName)
                {
                    case "RemoveObject_Click":
                        RemoveObject_Click(sender, e);
                        break;
                    case "RemoveAllObjects_Click":
                        RemoveAllObjects_Click(sender, e);
                        break;
                    case "SelectLine_Click":
                        SelectLine_Click(sender, e);
                        break;
                    case "SelectCircle_Click":
                        SelectCircle_Click(sender, e);
                        break;
                    case "MoveCircle_Click":
                        MoveCircle_Click(sender, e);
                        break;
                    case "SelectPolygon_Click":
                        SelectPolygon_Click(sender, e);
                        break;
                    case "ChangeVertex_Click":
                        ChangeVertex_Click(sender, e);
                        break;
                    case "MoveCurve_Click":
                        MoveCurve_Click(sender, e);
                        break;
                    default:
                        break;
                }
            }
        }

        private void TurnAntialiased_Click(object sender, RoutedEventArgs e)
        {
            canvas.Children.Clear();
            foreach (var line in lines)
            {
                DrawAntiAliased(line.Item1,line.Item2,line.Item4.Color);
            }
            foreach(var circle in circles)
            {
                DrawCircleOnCanvas(circle.Item1, circle.Item2, circle.Item3);
            }
            foreach(var polygon in polygons)
            {
                DrawPolygonOnCanvas(polygon.Item1, polygon.Item2);
            }
        }

        private void ApplyChange_Click(object sender, RoutedEventArgs e)
        {
            if (StylePicker.SelectedItem != null)
            {
                string functionName = ((ComboBoxItem)StylePicker.SelectedItem).Tag.ToString();
                switch (functionName)
                {
                    case "ChangeRadius_Click":
                        ChangeRadius_Click(sender, e);
                        break;
                    case "ChangeThickness_Click":
                        ChangeThickness_Click(sender, e);
                        break;
                    case "ChangeColor_Click":
                        ChangeColor_Click(sender, e);
                        break;
                    default:
                        break;
                }
            }
        }

        private void ChangeColor_Click(object sender, RoutedEventArgs e)
        {
            if(selectedCircle != null) {
                int index = circles.IndexOf(selectedCircle);
                selectedCircle = new Tuple<Point, int, SolidColorBrush>(selectedCircle.Item1, selectedCircle.Item2, currentColor);
                if (index != -1)
                {
                    circles[index] = selectedCircle;
                }
            }
            if (selectedLine != null)
            {
                int index = lines.IndexOf(selectedLine);
                selectedLine = new Tuple<Point, Point, bool, SolidColorBrush>(selectedLine.Item1, selectedLine.Item2, selectedLine.Item3, currentColor);
                if (index != -1)
                {
                    lines[index] = selectedLine;
                }
            }
            RedrawShapes();
        }

        private void colorPicker_SelectedColorChanged(object sender, RoutedPropertyChangedEventArgs<Color?> e)
        {
            Color selectedColor = colorPicker.SelectedColor ?? Colors.Black;
            currentColor = new SolidColorBrush(selectedColor);
        }

        private void SelectLine_Click(object sender, RoutedEventArgs e)
        {
            isSelectingLine = true;
        }

        private void SelectCircle_Click(object sender, RoutedEventArgs e)
        {
            isSelectingCircle = true;
        }

        private void SelectPolygon_Click(object sender, RoutedEventArgs e)
        {
            isSelectingPolygon = true;
        }

        private void DrawLine_Click(object sender, RoutedEventArgs e)
        {
            isDrawingLine = true;
        }

        private void DrawCircle_Click(object sender, RoutedEventArgs e)
        {
            isDrawingCircle = true;
        }

        private void DrawPolygon_Click(object sender, RoutedEventArgs e)
        {
            isDrawingPolygon = true;
        }

        private void DrawCurve_Click(object sender, RoutedEventArgs e)
        {
            isDrawingCurve = true;
        }

        private void RemoveObject_Click(object sender, RoutedEventArgs e)
        {
            if (selectedLine != null)
            {
                lines.Remove(selectedLine);
                selectedLine = null;
                selectedPoint = null;
                selectedLineText.Text = "Selected Line: None";
                RedrawShapes();
            }
            if(selectedCircle != null)
            {
                circles.Remove(selectedCircle);
                selectedCircle = null;
                selectedCircleText.Text = "Selected Line: None";
                RedrawShapes();
            }
            if(selectedPolygon != null)
            {
                polygons.Remove(selectedPolygon);
                selectedPolygon = null;
                selectedPolygonText.Text = "Selected Polygon: None";
                RedrawShapes();
            }
        }

        private void RemoveAllObjects_Click(object sender, RoutedEventArgs e) 
        {
            polygons.Clear();
            circles.Clear();
            lines.Clear();

            canvas.Children.Clear();
        }

        private void ChangeRadius_Click(object sender, RoutedEventArgs e)
        {
            isMovingCircle = false;
            isDraggingEndpoint = false;
            isEditingRadius = true;      
        }

        private void MoveCircle_Click(object sender, RoutedEventArgs e)
        {
            isEditingRadius = false;
            isMovingCircle = true;  
        }

        private void MoveCurve_Click(object sender, RoutedEventArgs e)
        {
            isMovingCurve = true;
        }

        private void ChangeVertex_Click(object sender, RoutedEventArgs e)
        {
            isDraggingVertex = true;
        }

        private void Canvas_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (isMovingCurve)
            {
                Point mousePosition = e.GetPosition(canvas);
                selectedCurvePoint = FindClosestControlPoint(mousePosition);
                //cubicBezierPoints = curves.Find(curve => curve);
            }

            if (isDrawingLine)
            {
                if (startPoint == default(Point))
                {
                    startPoint = e.GetPosition(canvas);
                }
                else
                {
                    Point endPoint = e.GetPosition(canvas);
                    DrawLine(startPoint, endPoint, currentColor);
                    startPoint = default(Point);
                    isDrawingLine = false;
                }
            }

            else if (isDrawingCircle)
            {
                if (centerPoint == default(Point))
                {
                    centerPoint = e.GetPosition(canvas);
                }
                else
                {
                    Point edgePoint = e.GetPosition(canvas);
                    DrawCircle(centerPoint,(int)Distance(centerPoint,edgePoint),currentColor);
                    centerPoint = default(Point);
                    isDrawingCircle = false;
                }
            }

            else if (isDrawingPolygon)
            {
                if (startPoint == default(Point))
                {
                    startPoint = e.GetPosition(canvas);
                    polygonPoints.Add(startPoint);
                }
                else if(Distance(e.GetPosition(canvas), startPoint) < 10)
                {
                    polygonPoints.Add(e.GetPosition(canvas));
                    DrawLineOnCanvas(polygonPoints[polygonPoints.Count - 2], startPoint, currentColor);
                    DrawPolygon(polygonPoints, currentColor);
                    polygonPoints = new List<Point>();
                    isDrawingPolygon = false;
                    startPoint = default(Point);
                }
                else
                {
                    polygonPoints.Add(e.GetPosition(canvas));
                    DrawLineOnCanvas(polygonPoints[polygonPoints.Count - 2], polygonPoints[polygonPoints.Count - 1], currentColor);
                }
            }

            else if (isDrawingCurve)
            {
                if (cubicBezierPoints.Count < 4)
                {
                    cubicBezierPoints.Add(e.GetPosition(canvas));

                    //DrawPixel((int)e.GetPosition(canvas).X, (int)e.GetPosition(canvas).Y, currentColor);
                    Ellipse ellipse = new Ellipse();
                    ellipse.Width = 5;
                    ellipse.Height = 5;
                    ellipse.Fill = currentColor;
                    Canvas.SetLeft(ellipse, e.GetPosition(canvas).X - ellipse.Width / 2);
                    Canvas.SetTop(ellipse, e.GetPosition(canvas).Y - ellipse.Height / 2);
                    canvas.Children.Add(ellipse);

                    if (cubicBezierPoints.Count == 4)
                    {
                        DrawCubicBezierCurve(cubicBezierPoints[0], cubicBezierPoints[1], cubicBezierPoints[2], cubicBezierPoints[3], currentColor);
                        cubicBezierPoints.Clear(); 
                        isDrawingCurve = false;
                    }
                }
            }

            else if (isSelectingLine)
            {
                Point clickedPoint = e.GetPosition(canvas);
                foreach (var line in lines)
                {
                    double distance = DistanceToLineSegment(clickedPoint, line.Item1, line.Item2);
                    if (distance < 10)
                    {
                        selectedLine = line;
                        isEditingRadius = false;
                        if (Distance(clickedPoint, selectedLine.Item1) < 10)
                        {
                            isDraggingEndpoint = true;
                            lineBeingDragged = line;
                            selectedLineText.Text = $"Selected Line: {lines.IndexOf(line)} (start)";
                            isSelectingLine = false;
                            selectedPoint = line.Item1;
                            return;
                        }
                        else if (Distance(clickedPoint, selectedLine.Item2) < 10)
                        {
                            isDraggingEndpoint = true;
                            lineBeingDragged = line;
                            selectedLineText.Text = $"Selected Line: {lines.IndexOf(line)} (end)";
                            isSelectingLine = false;
                            selectedPoint = line.Item2;
                            return;
                        }
                        selectedLineText.Text = $"Selected Line: {lines.IndexOf(line)}";
                        isSelectingLine = false;
                        return;
                    }
                }
                selectedPoint = null;
                selectedLine = null;
                selectedLineText.Text = "Selected Line: None";
                isSelectingLine = false;
            }

            else if (isSelectingCircle)
            {
                Point clickedPoint = e.GetPosition(canvas);
                foreach (var circle in circles)
                {
                    double distanceToCircle = DistanceToCircleSegment(clickedPoint, circle.Item1, circle.Item2, 0, 2 * Math.PI);

                        if (distanceToCircle < 10)
                        {
                            selectedCircle = circle;
                            selectedCircleText.Text = $"Selected Circle: {circles.IndexOf(circle)}";
                            isSelectingCircle = false;
                            return;
                        }      
                }
                selectedCircle = null;
                selectedCircleText.Text = $"Selected Circle: None";
                isSelectingCircle = false;
            }

            else if (isSelectingPolygon)
            {
                Point clickedPoint = e.GetPosition(canvas);
                foreach (var polygon in polygons)
                {
                    if (IsPointInsidePolygon(clickedPoint, polygon))
                    {
                        selectedPolygon = polygon;
                        selectedPolygonText.Text = $"Selected Polygon: {polygons.IndexOf(polygon)}";
                        isSelectingPolygon = false;
                        return;
                    }
                }
                selectedPolygon = null;
                selectedPolygonText.Text = "Selected Polygon: None";
                isSelectingPolygon = false;
            }

            else if (isDraggingVertex)
            {
                Point clickedPoint = e.GetPosition(canvas);
                foreach (var polygon in polygons)
                {
                    foreach(var vertex in polygon.Item1)
                    {
                        if(Distance(clickedPoint, vertex) < 10)
                        {
                            selectedVertex = vertex;
                            return;
                        }
                    }
                }             
            }
        }

        void MoveControlPoint(Point point, Point newPosition)
        {
            int index = cubicBezierPoints.IndexOf(point);
            if (index != -1)
            {
                cubicBezierPoints[index] = newPosition;
            }
        }

        Point FindClosestControlPoint(Point position)
        {
            foreach (var point in cubicBezierPoints)
            {
                double distance = Math.Sqrt(Math.Pow(position.X - point.X, 2) + Math.Pow(position.Y - point.Y, 2));
                if (distance < 50)
                {
                    return point;
                }
            }
            return new Point();
        }

        void DrawCubicBezierCurveOnCanvas(Point p0, Point p1, Point p2, Point p3, SolidColorBrush color)
        {
            for (double t = 0; t <= 1; t += 0.0005)
            {
                double x = Math.Pow(1 - t, 3) * p0.X + 3 * t * Math.Pow(1 - t, 2) * p1.X + 3 * Math.Pow(t, 2) * (1 - t) * p2.X + Math.Pow(t, 3) * p3.X;
                double y = Math.Pow(1 - t, 3) * p0.Y + 3 * t * Math.Pow(1 - t, 2) * p1.Y + 3 * Math.Pow(t, 2) * (1 - t) * p2.Y + Math.Pow(t, 3) * p3.Y;
                DrawPixel((int)x, (int)y, color);
            }
        }
        void DrawCubicBezierCurve(Point p0, Point p1, Point p2, Point p3, SolidColorBrush color)
        {
            DrawCubicBezierCurveOnCanvas(p0,p1,p2,p3,color);    
        }

        void DrawControlPoint(Point point)
        {
            Ellipse ellipse = new Ellipse();
            ellipse.Width = 5;
            ellipse.Height = 5; 
            ellipse.Fill = currentColor;
            Canvas.SetLeft(ellipse, point.X - ellipse.Width / 2);
            Canvas.SetTop(ellipse, point.Y - ellipse.Height / 2);
            canvas.Children.Add(ellipse);
        }

        private void Canvas_MouseMove(object sender, MouseEventArgs e)
        {
            if(selectedCurvePoint != null && e.LeftButton == MouseButtonState.Pressed)
            {
                Point newPosition = e.GetPosition(canvas);
                MoveControlPoint(selectedCurvePoint, newPosition);

                canvas.Children.Clear();

                if (cubicBezierPoints.Count == 4)
                {
                    DrawCubicBezierCurve(cubicBezierPoints[0], cubicBezierPoints[1], cubicBezierPoints[2], cubicBezierPoints[3], currentColor);
                }

                foreach (var point in cubicBezierPoints)
                {
                    DrawControlPoint(point);
                }

                RedrawShapes();
            }

            if (isDraggingEndpoint && e.LeftButton == MouseButtonState.Pressed)
            {
                Point endPointOfDrag = e.GetPosition(canvas);
                int index = lines.IndexOf(lineBeingDragged);

                if (selectedPoint == selectedLine.Item1)
                {
                    lineBeingDragged = new Tuple<Point, Point, bool, SolidColorBrush>(endPointOfDrag, lineBeingDragged.Item2, true, lineBeingDragged.Item4);
                }
                else if (selectedPoint == selectedLine.Item2)
                {
                    lineBeingDragged = new Tuple<Point, Point, bool, SolidColorBrush>(lineBeingDragged.Item1, endPointOfDrag, true, lineBeingDragged.Item4);
                }


                if (index != -1)
                {
                    lines[index] = lineBeingDragged;
                }

                RedrawShapes();
            }

            if(isEditingRadius && e.LeftButton == MouseButtonState.Pressed && selectedCircle != null)
            {
                Point endPointOfDrag = e.GetPosition(canvas);
                int index = circles.IndexOf(selectedCircle);

                selectedCircle = new Tuple<Point, int, SolidColorBrush>(selectedCircle.Item1, (int)Distance(selectedCircle.Item1, endPointOfDrag), selectedCircle.Item3);

                if (index != -1)
                {
                    circles[index] = new Tuple<Point, int, SolidColorBrush>(selectedCircle.Item1, (int)Distance(selectedCircle.Item1, endPointOfDrag), selectedCircle.Item3);
                }

                RedrawShapes();
            }

            if (isMovingCircle && e.LeftButton == MouseButtonState.Pressed && selectedCircle != null)
            {
                Point endPointOfDrag = e.GetPosition(canvas);
                int index = circles.IndexOf(selectedCircle);

                selectedCircle = new Tuple<Point, int, SolidColorBrush>(endPointOfDrag, selectedCircle.Item2, selectedCircle.Item3);
                if (index != -1)
                {
                    circles[index] = selectedCircle;
                }

                RedrawShapes();
            }

            if (isDraggingVertex && e.LeftButton == MouseButtonState.Pressed && selectedPolygon != null)
            {
                Point endPointOfDrag = e.GetPosition(canvas);
                int index = polygons.IndexOf(selectedPolygon);
                int indexOfVertex = polygons[index].Item1.IndexOf(selectedVertex);
                selectedVertex = endPointOfDrag;
                List<Point> newPoints = polygons[index].Item1;
                newPoints[indexOfVertex] = selectedVertex;
                selectedPolygon = new Tuple<List<Point>, SolidColorBrush>(newPoints, selectedPolygon.Item2);
                if (index != -1)
                {
                    polygons[index] = selectedPolygon;
                }
                RedrawShapes();
            }
        }

        private void Canvas_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (isDraggingEndpoint)
            {
                selectedLine = null;
                selectedPoint = null;
                selectedLineText.Text = $"Selected Line: None";
                isDraggingEndpoint = false;
                lineBeingDragged = null;
                isSelectingLine = false;
            }
            if (isEditingRadius)
            {
                selectedCircle = null;
                selectedCircleText.Text = $"Selected Circle: None";
                isEditingRadius = false;
                isSelectingCircle = false;
            }
            if (isMovingCircle)
            {
                selectedCircle = null;
                selectedCircleText.Text = $"Selected Circle: None";
                isMovingCircle = false;
                isSelectingCircle = false;
            }
            if (isDraggingVertex)
            {
                selectedPolygon = null;
                selectedVertex = new Point();
                selectedPolygonText.Text = $"Selected Polygon: None";
                isDraggingVertex = false;
                isSelectingPolygon = false;
            }
            if(selectedCurvePoint != null)
            {
                selectedCurvePoint = new Point();
            }
        }

        private void RedrawShapes()
        {
            canvas.Children.Clear();
            foreach (var line in lines)
            {
                DrawLineOnCanvas(line.Item1, line.Item2, line.Item4);
            }
            foreach(var circle in circles)
            {
                DrawCircleOnCanvas(circle.Item1, circle.Item2, circle.Item3);
            }
            foreach(var polygon in polygons)
            {
                DrawPolygonOnCanvas(polygon.Item1, currentColor);
            }
            foreach(var curve in curves)
            {
                DrawCubicBezierCurveOnCanvas(curve.Item1, curve.Item2, curve.Item3,curve.Item4,currentColor);
            }
        }

        private bool DrawLineOnCanvas(Point p1, Point p2, SolidColorBrush color)
        {
            (List<Point> points, bool horizontal) = SymmetricMidpointLine((int)p1.X, (int)p1.Y, (int)p2.X, (int)p2.Y);

            foreach (var point in points)
            {
                DrawPixel((int)point.X, (int)point.Y, color);
            }

            return horizontal;
        }

        private void DrawLine(Point p1, Point p2, SolidColorBrush color)
        {
            bool horizontal = DrawLineOnCanvas(p1, p2, color);
            lines.Add(new Tuple<Point, Point, bool, SolidColorBrush>(p1, p2, horizontal, color));
        }

        private void DrawCircleOnCanvas(Point center, int radius, SolidColorBrush color) 
        {
            int dE = 3;
            int dSE = 5 - 2 * radius;
            int d = 1 - radius;
            int x = 0;
            int y = radius;
            DrawPixel((int)center.X + x, (int)center.Y + y, color);
            while (y > x)
            {
                if (d < 0)
                {
                    d += dE;
                    dE += 2;
                    dSE += 2;
                }
                else
                {
                    d += dSE;
                    dE += 2;
                    dSE += 4;
                    --y;
                }
                ++x;
                DrawPixel((int)center.X + x, (int)center.Y + y, color);
                DrawPixel((int)center.X - x, (int)center.Y + y, color);
                DrawPixel((int)center.X + x, (int)center.Y - y, color);
                DrawPixel((int)center.X - x, (int)center.Y - y, color);

                DrawPixel((int)center.X + y, (int)center.Y + x, color);
                DrawPixel((int)center.X - y, (int)center.Y + x, color);
                DrawPixel((int)center.X + y, (int)center.Y - x, color);
                DrawPixel((int)center.X - y, (int)center.Y - x, color);
            }
        }

        private void DrawCircle(Point center, int radius, SolidColorBrush color)
        {
            DrawCircleOnCanvas(center, radius, color);
            circles.Add(new Tuple<Point, int, SolidColorBrush>(center, radius, color));
        }

        private void DrawPolygonOnCanvas(List<Point> points, SolidColorBrush color)
        {
            if (points.Count < 2)
                return; 

            for (int i = 0; i < points.Count - 1; i++)
            {
                DrawLineOnCanvas(points[i], points[i + 1], color);
            }

            DrawLineOnCanvas(points[points.Count - 1], points[0], color);
        }

        private void DrawPolygon(List<Point> points,  SolidColorBrush color)
        {
            polygons.Add(new Tuple<List<Point>, SolidColorBrush>(points, color));
        }

        private void DrawPixel(int x, int y, Brush color)
        {
            Rectangle pixel = new Rectangle
            {
                Width = 1,
                Height = 1,
                Fill = color
            };
            Canvas.SetLeft(pixel, x);
            Canvas.SetTop(pixel, y);
            canvas.Children.Add(pixel);
        }

        private (List<Point>,bool) SymmetricMidpointLine(int x0, int y0, int x1, int y1)
        {
            List<Point> points = new List<Point>();

            int dx = Math.Abs(x1 - x0);
            int dy = Math.Abs(y1 - y0);
            int sx = x0 < x1 ? 1 : -1;
            int sy = y0 < y1 ? 1 : -1;
            int err = dx - dy;
            int e2;
            bool horizontal = Math.Abs(dx) >= Math.Abs(dy);

            while (true)
            {
                points.Add(new Point(x0, y0));
                if (x0 == x1 && y0 == y1)
                    break;
                e2 = 2 * err;
                if (e2 > -dy)
                {
                    err -= dy;
                    x0 += sx;
                }
                if (e2 < dx)
                {
                    err += dx;
                    y0 += sy;
                }
            }

            return (points,horizontal);
        }

        private double Distance(Point p1, Point p2)
        {
            double dx = p2.X - p1.X;
            double dy = p2.Y - p1.Y;
            return Math.Sqrt(dx * dx + dy * dy);
        }

        private double DistanceToLineSegment(Point point, Point start, Point end)
        {
            double length = Distance(start, end);
            if (length == 0)
                return Distance(point, start);

            double t = Math.Max(0, Math.Min(1, ((point.X - start.X) * (end.X - start.X) + (point.Y - start.Y) * (end.Y - start.Y)) / (length * length)));
            Point projection = new Point(start.X + t * (end.X - start.X), start.Y + t * (end.Y - start.Y));
            return Distance(point, projection);
        }

        private double DistanceToCircleSegment(Point point, Point circleCenter, double radius, double startAngle, double endAngle)
        {
            double distanceToCenter = Distance(point, circleCenter);

            if (distanceToCenter < radius)
            {
                return Math.Abs(distanceToCenter - radius);
            }
            else
            {
                double angle = Math.Atan2(point.Y - circleCenter.Y, point.X - circleCenter.X);
                angle = (angle < 0) ? angle + 2 * Math.PI : angle;
                double closestAngle = Math.Max(startAngle, Math.Min(endAngle, angle));
                Point closestPointOnCircle = new Point(circleCenter.X + radius * Math.Cos(closestAngle), circleCenter.Y + radius * Math.Sin(closestAngle));

                if (closestAngle >= startAngle && closestAngle <= endAngle)
                {
                    return Distance(point, closestPointOnCircle);
                }
                else
                {
                    double distanceToStartPoint = Distance(point, new Point(circleCenter.X + radius * Math.Cos(startAngle), circleCenter.Y + radius * Math.Sin(startAngle)));
                    double distanceToEndPoint = Distance(point, new Point(circleCenter.X + radius * Math.Cos(endAngle), circleCenter.Y + radius * Math.Sin(endAngle)));
                    return Math.Min(distanceToStartPoint, distanceToEndPoint);
                }
            }
        }

        private bool IsPointInsidePolygon(Point point, Tuple<List<Point>, SolidColorBrush> polygon)
        {
            List<Point> polygonPoints = polygon.Item1;
            int polygonSides = polygonPoints.Count;
            bool isInside = false;

            for (int i = 0, j = polygonSides - 1; i < polygonSides; j = i++)
            {
                if (((polygonPoints[i].Y > point.Y) != (polygonPoints[j].Y > point.Y)) &&
                    (point.X < (polygonPoints[j].X - polygonPoints[i].X) * (point.Y - polygonPoints[i].Y) / (polygonPoints[j].Y - polygonPoints[i].Y) + polygonPoints[i].X))
                {
                    isInside = !isInside;
                }
            }

            return isInside;
        }

        private void ChangeThickness_Click(object sender, RoutedEventArgs e)
        {
            if(selectedLine != null)
            {
                if (selectedLine.Item3)
                {
                    RedrawShapes();
                    for(int i = 1;i < ThicknessSlider.Value / 2; i++)
                    {
                        DrawLineOnCanvas(new Point(selectedLine.Item1.X, selectedLine.Item1.Y + i), new Point(selectedLine.Item2.X, selectedLine.Item2.Y + i), selectedLine.Item4);
                        DrawLineOnCanvas(new Point(selectedLine.Item1.X, selectedLine.Item1.Y - i), new Point(selectedLine.Item2.X, selectedLine.Item2.Y - i), selectedLine.Item4);
                    }
                }
                else
                {
                    RedrawShapes();
                    for (int i = 1; i < ThicknessSlider.Value / 2; i++)
                    {
                        DrawLineOnCanvas(new Point(selectedLine.Item1.X + i, selectedLine.Item1.Y), new Point(selectedLine.Item2.X + i, selectedLine.Item2.Y), selectedLine.Item4);
                        DrawLineOnCanvas(new Point(selectedLine.Item1.X - i, selectedLine.Item1.Y), new Point(selectedLine.Item2.X - i, selectedLine.Item2.Y), selectedLine.Item4);
                    }
                }
            }

            if (selectedPolygon != null)
            {
                RedrawShapes();
                int thickness = (int)ThicknessSlider.Value / 2;

                for (int i = 0; i < selectedPolygon.Item1.Count; i++)
                {
                    Point currentPoint = selectedPolygon.Item1[i];
                    Point nextPoint = selectedPolygon.Item1[(i + 1) % selectedPolygon.Item1.Count];

                    double edgeSlope = (nextPoint.Y - currentPoint.Y) / (nextPoint.X - currentPoint.X);

                    if (Math.Abs(edgeSlope) > 1) 
                    {
                        for (int j = 1; j <= thickness; j++)
                        {
                            DrawLineOnCanvas(new Point(currentPoint.X + j, currentPoint.Y),
                                             new Point(nextPoint.X + j, nextPoint.Y),
                                             selectedPolygon.Item2);
                            DrawLineOnCanvas(new Point(currentPoint.X - j, currentPoint.Y),
                                             new Point(nextPoint.X - j, nextPoint.Y),
                                             selectedPolygon.Item2);
                        }
                    }
                    else
                    {
                        for (int j = 1; j <= thickness; j++)
                        {
                            DrawLineOnCanvas(new Point(currentPoint.X, currentPoint.Y + j),
                                             new Point(nextPoint.X, nextPoint.Y + j),
                                             selectedPolygon.Item2);
                            DrawLineOnCanvas(new Point(currentPoint.X, currentPoint.Y - j),
                                             new Point(nextPoint.X, nextPoint.Y - j),
                                             selectedPolygon.Item2);
                        }
                    }
                }
            }
        }

        private void SaveShapesToFile(string filePath)
        {
            var shapes = new
            {
                Lines = lines,
                Circles = circles,
                Polygons = polygons
            };

            string json = JsonConvert.SerializeObject(shapes, Formatting.Indented);

            File.WriteAllText(filePath, json);
        }

        private void LoadShapesFromFile(string filePath)
        {
            string json = File.ReadAllText(filePath);

            var shapes = JsonConvert.DeserializeObject<dynamic>(json);

            lines = JsonConvert.DeserializeObject<List<Tuple<Point, Point, bool, SolidColorBrush>>>(shapes.Lines.ToString());
            circles = JsonConvert.DeserializeObject<List<Tuple<Point, int, SolidColorBrush>>>(shapes.Circles.ToString());
            polygons = JsonConvert.DeserializeObject<List<Tuple<List<Point>, SolidColorBrush>>>(shapes.Polygons.ToString());
        }

        private void SaveShapesButton_Click(object sender, RoutedEventArgs e)
        {
            Microsoft.Win32.SaveFileDialog saveFileDialog = new Microsoft.Win32.SaveFileDialog();
            saveFileDialog.Filter = "JSON Files (*.json)|*.json|All files (*.*)|*.*";
            if (saveFileDialog.ShowDialog() == true)
            {
                SaveShapesToFile(saveFileDialog.FileName);
            }
        }

        private void LoadShapesButton_Click(object sender, RoutedEventArgs e)
        {
            Microsoft.Win32.OpenFileDialog openFileDialog = new Microsoft.Win32.OpenFileDialog();
            openFileDialog.Filter = "JSON Files (*.json)|*.json|All files (*.*)|*.*";
            if (openFileDialog.ShowDialog() == true)
            {
                LoadShapesFromFile(openFileDialog.FileName);
                RedrawShapes();
            }
        }

        public void DrawAntiAliased(Point p1, Point p2,Color color)
        {
            double dx = p2.X - p1.X;
            double dy = p2.Y - p1.Y;
            double length = Math.Sqrt(dx * dx + dy * dy);

            for (double i = 0; i <= length; i += 0.5)
            {
                double t = i / length;

                double x = p1.X + t * dx;
                double y = p1.Y + t * dy;

                double alpha = CalculateAlpha(x, y, p1.X, p1.Y, p2.X, p2.Y);

                DrawCanvasPixelAlpha(canvas, new Point(x, y), alpha, color);
            }
        }

        private void DrawCanvasPixelAlpha(Canvas canvas, Point point, double alpha, Color color)
        {
            byte alphaByte = (byte)(alpha * 255);
            Color blendedColor = Color.FromArgb(alphaByte, color.R, color.G, color.B);

            SolidColorBrush brush = new SolidColorBrush(blendedColor);

            Ellipse pixel = new Ellipse
            {
                Width = 1,
                Height = 1,
                Fill = brush,
                Margin = new Thickness(point.X - 0.5, point.Y - 0.5, 0, 0)
            };

            canvas.Children.Add(pixel);
        }


        private double CalculateAlpha(double x, double y, double x1, double y1, double x2, double y2)
        {
            double dx = x2 - x1;
            double dy = y2 - y1;
            double t = ((x - x1) * dx + (y - y1) * dy) / (dx * dx + dy * dy);

            double px = x1 + t * dx;
            double py = y1 + t * dy;

            double distance = Math.Sqrt((x - px) * (x - px) + (y - py) * (y - py));
            return 1.0 - (distance / 0.5);
        }

    }
}
