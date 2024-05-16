using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using Newtonsoft.Json;
using System.IO;
using System.Linq;
using System.Collections;
using System.Net;
using System.Windows.Media.Imaging;
using Microsoft.Win32;
using System.Diagnostics;

namespace Paint_App
{
    public partial class MainWindow : Window
    {
        private List<Tuple<Point, Point, bool, SolidColorBrush>> lines = new List<Tuple<Point, Point, bool, SolidColorBrush>>();
        private List<Tuple<Point, int, SolidColorBrush>> circles = new List<Tuple<Point, int, SolidColorBrush>>();
        private List<Tuple<List<Point>, SolidColorBrush, SolidColorBrush, string>> polygons = new List<Tuple<List<Point>, SolidColorBrush, SolidColorBrush, string>>();
        private List<List<Point>> curves = new List<List<Point>>();
        private List<Tuple<Point, Point, SolidColorBrush>> rectangles = new List<Tuple<Point, Point, SolidColorBrush>>();
        private WriteableBitmap writeableBitmap;


        private Point startPoint;
        private Point centerPoint;
        private Point selectedCurvePoint;
        private Image polygonImage;

        private bool isDrawingCurve = false;
        private bool isDrawingPolygon = false;
        private bool isDrawingLine = false;
        private bool isDrawingCircle = false;
        private bool isDrawingRect = false;

        private bool isSelectingLine = false;
        private bool isSelectingCircle = false;
        private bool isSelectingPolygon = false;
        private bool isSelectingRect = false;

        private bool isDraggingEndpoint = false;
        private bool isEditingRadius = false;
        private bool isMovingCircle = false;
        private bool isMovingRectangle = false;
        private bool isDraggingVertex = false;
        private bool isFloodFilling = false;

        List<Point> cubicBezierPoints = new List<Point>();
        private List<Point> polygonPoints = new List<Point>();
        private Tuple<Point, Point, bool, SolidColorBrush> lineBeingDragged;

        private Tuple<Point, Point, bool, SolidColorBrush> selectedLine = null;
        private Tuple<Point, int, SolidColorBrush> selectedCircle = null;
        private Tuple<List<Point>, SolidColorBrush, SolidColorBrush,string> selectedPolygon = null;
        private Tuple<Point, Point, SolidColorBrush> selectedRect = null;

        private Point? selectedPoint = null;
        private Point selectedVertex = new Point();


        private SolidColorBrush currentColor = new SolidColorBrush(Colors.Black);

        const byte LEFT = 1;   // 0001
        const byte RIGHT = 2;  // 0010
        const byte BOTTOM = 4; // 0100
        const byte TOP = 8;    // 1000

        public MainWindow()
        {
            InitializeComponent();
            int width = 800;
            int height = 600;
            writeableBitmap = new WriteableBitmap(width, height, 96, 96, PixelFormats.Bgra32, null);

            Image image = new Image();
            image.Source = writeableBitmap;
            canvas.Children.Add(image); 
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
                    case "DrawRectangle_Click":
                        DrawRectangle_Click(sender, e);
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
                    case "MoveRectangle_Click":
                        MoveRectangle_Click(sender, e);
                        break;
                    case "SelectPolygon_Click":
                        SelectPolygon_Click(sender, e);
                        break;
                    case "SelectRectangle_Click":
                        SelectRectangle_Click(sender, e);
                        break;
                    case "ChangeVertex_Click":
                        ChangeVertex_Click(sender, e);
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
                DrawAntiAliased(line.Item1, line.Item2, line.Item4.Color);
            }
            foreach (var circle in circles)
            {
                DrawCircleOnCanvas(circle.Item1, circle.Item2, circle.Item3);
            }
            foreach (var rect in rectangles)
            {
                DrawLineOnCanvas(rect.Item1, new Point(rect.Item2.X, rect.Item1.Y), rect.Item3);
                DrawLineOnCanvas(rect.Item1, new Point(rect.Item1.X, rect.Item2.Y), rect.Item3);
                DrawLineOnCanvas(rect.Item2, new Point(rect.Item2.X, rect.Item1.Y), rect.Item3);
                DrawLineOnCanvas(rect.Item2, new Point(rect.Item1.X, rect.Item2.Y), rect.Item3);
            }
            foreach (var polygon in polygons)
            {
                DrawPolygonOnCanvas(polygon);
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
                    case "FillPolygon_Click":
                        FillPolygon_Click(sender, e);
                        break;
                    case "FillImage_Click":
                        FillImage_Click(sender, e);
                        break;
                    case "FloodFill_Click":
                        FloodFill_Click(sender, e);
                        break;
                    case "ClipPolygon_Click":
                        ClipPolygon_Click(sender, e);
                        break;
                    default:
                        break;
                }
            }
        }

        private void ChangeColor_Click(object sender, RoutedEventArgs e)
        {
            if (selectedCircle != null)
            {
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

        private void SelectRectangle_Click(object sender, RoutedEventArgs e)
        {
            isSelectingRect = true;
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
            if (selectedCircle != null)
            {
                circles.Remove(selectedCircle);
                selectedCircle = null;
                selectedCircleText.Text = "Selected Line: None";
                RedrawShapes();
            }
            if (selectedPolygon != null)
            {
                polygons.Remove(selectedPolygon);
                selectedPolygon = null;
                selectedPolygonText.Text = "Selected Polygon: None";
                RedrawShapes();
            }
            if (selectedRect != null)
            {
                rectangles.Remove(selectedRect);
                selectedRect = null;
                selectedRectangleText.Text = "Selected Rectangle: None";
                RedrawShapes();
            }
        }

        private void RemoveAllObjects_Click(object sender, RoutedEventArgs e)
        {
            polygons.Clear();
            circles.Clear();
            lines.Clear();
            rectangles.Clear();

            selectedCircle = null;
            selectedPolygon = null;
            selectedRect = null;
            selectedLine = null;

            RedrawShapes();
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

        private void MoveRectangle_Click(object sender, RoutedEventArgs e)
        {
            isMovingRectangle = true;
        }

        private void ChangeVertex_Click(object sender, RoutedEventArgs e)
        {
            isDraggingVertex = true;
        }

        private void DrawRectangle_Click(object sender, RoutedEventArgs e)
        {
            isDrawingRect = true;
        }

        private void FloodFill_Click(object sender, RoutedEventArgs e)
        {
            isFloodFilling = true;
        }

        private void FillImage_Click(object sender, EventArgs e)
        {
            List<Point> points = selectedPolygon.Item1;
            SolidColorBrush brush = selectedPolygon.Item2;
            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.Filter = "Image Files (*.png;*.jpg;*.jpeg;*.gif;*.bmp)|*.png;*.jpg;*.jpeg;*.gif;*.bmp|All files (*.*)|*.*";
            openFileDialog.FilterIndex = 1;
            openFileDialog.Multiselect = false;

            if (openFileDialog.ShowDialog() == true)
            {
                string imagePath = openFileDialog.FileName;
                int index = polygons.IndexOf(selectedPolygon);
                selectedPolygon = new Tuple<List<Point>, SolidColorBrush, SolidColorBrush, string>(selectedPolygon.Item1, selectedPolygon.Item2, null, imagePath);
                if (index != -1) polygons[index] = selectedPolygon;
                FillPolygonWithImage(selectedPolygon.Item1, selectedPolygon.Item4);
            }
        }

        private void FillPolygonWithImage(List<Point> points, string imagePath)
        {
            BitmapImage image = new BitmapImage(new Uri(imagePath, UriKind.RelativeOrAbsolute));

            DrawingBrush imageBrush = new DrawingBrush();
            imageBrush.Stretch = Stretch.None;
            imageBrush.TileMode = TileMode.Tile;
            imageBrush.Viewport = new Rect(0, 0, image.PixelWidth, image.PixelHeight);
            imageBrush.ViewportUnits = BrushMappingMode.Absolute;
            imageBrush.Drawing = new ImageDrawing(image, new Rect(0, 0, image.PixelWidth, image.PixelHeight));

            var visual = new DrawingVisual();
            using (DrawingContext drawingContext = visual.RenderOpen())
            {
                Point[] pointArray = points.ToArray();
                StreamGeometry streamGeometry = new StreamGeometry();
                using (StreamGeometryContext geometryContext = streamGeometry.Open())
                {
                    geometryContext.BeginFigure(pointArray[0], true, true);
                    geometryContext.PolyLineTo(pointArray.Skip(1).ToList(), true, true);
                }
                drawingContext.DrawGeometry(imageBrush, null, streamGeometry);
            }

            RenderTargetBitmap renderBitmap = new RenderTargetBitmap((int)canvas.ActualWidth, (int)canvas.ActualHeight, 96, 96, PixelFormats.Pbgra32);
            renderBitmap.Render(visual);

            polygonImage = new Image();
            polygonImage.Source = renderBitmap;

            canvas.Children.Add(polygonImage);
        }

        private void ClipPolygon_Click(object sender, EventArgs e)
        {
            if (selectedPolygon == null | selectedRect == null) return;

            int index = polygons.IndexOf(selectedPolygon);
            polygons[index] = Tuple.Create(selectedPolygon.Item1, new SolidColorBrush(Colors.White),selectedPolygon.Item3,selectedPolygon.Item4);
            RedrawShapes();

            List<Point> clippedPolygon = new List<Point>();

            for (int i = 0; i < selectedPolygon.Item1.Count; i++)
            {
                Point p1 = selectedPolygon.Item1[i];
                Point p2 = selectedPolygon.Item1[(i + 1) % selectedPolygon.Item1.Count];

                Tuple<Point, Point> clippedPoints = CohenSutherlandClipLine(p1, p2, selectedRect);
                if (clippedPoints != null)
                {
                    clippedPolygon.Add(clippedPoints.Item1);
                    if (!clippedPolygon.Contains(clippedPoints.Item2))
                        clippedPolygon.Add(clippedPoints.Item2);
                }
            }

            polygons[index] = Tuple.Create(clippedPolygon, new SolidColorBrush(Colors.Red), selectedPolygon.Item3, selectedPolygon.Item4);
            RedrawShapes();
        }

        byte ComputeOutcode(Point p, Tuple<Point, Point, SolidColorBrush>  clipRect)
        {
            Point rectPoint1 = clipRect.Item1;
            Point rectPoint2 = clipRect.Item2;

            byte code = 0;
            if (p.X > Math.Max(rectPoint1.X, rectPoint2.X))
                code |= RIGHT;
            else if (p.X < Math.Min(rectPoint1.X, rectPoint2.X))
                code |= LEFT;
            if (p.Y < Math.Min(rectPoint1.Y, rectPoint2.Y))
                code |= TOP;
            else if (p.Y > Math.Max(rectPoint1.Y, rectPoint2.Y))
                code |= BOTTOM;
           
            return code;
        }

        private Tuple<Point, Point> CohenSutherlandClipLine(Point p1, Point p2, Tuple<Point, Point, SolidColorBrush> clipRect)
        {
            byte outcode1 = ComputeOutcode(p1, clipRect);
            byte outcode2 = ComputeOutcode(p2, clipRect);

            Point clippedP1 = p1;
            Point clippedP2 = p2;

            while (true)
            {
                if ((outcode1 | outcode2) == 0)
                {
                    return Tuple.Create(clippedP1, clippedP2);
                }
                else if ((outcode1 & outcode2) != 0)
                {
                    return null;
                }
                else
                {
                    if (outcode1 != 0)
                    {
                        clippedP1 = Subdivide(clippedP1, clippedP2, clipRect, outcode1);
                        outcode1 = ComputeOutcode(clippedP1, clipRect);
                    }
                    else
                    {
                        clippedP2 = Subdivide(clippedP1, clippedP2, clipRect, outcode2);
                        outcode2 = ComputeOutcode(clippedP2, clipRect);
                    }
                }
            }
        }

        Point Subdivide(Point p1, Point p2, Tuple<Point, Point, SolidColorBrush> clipRect, byte outcode)
        {
            Point rectPoint1 = clipRect.Item1;
            Point rectPoint2 = clipRect.Item2;

            Point p = new Point();
            if ((outcode & TOP) != 0)
            {
                p.X = p1.X + (p2.X - p1.X) * (Math.Min(rectPoint1.Y, rectPoint2.Y) - p1.Y)/ (p2.Y - p1.Y);
                p.Y = Math.Min(rectPoint1.Y, rectPoint2.Y);
            }
            else if ((outcode & BOTTOM) != 0)
            {
                p.X = p1.X + (p2.X - p1.X) * (Math.Max(rectPoint1.Y, rectPoint2.Y) - p1.Y)/ (p2.Y - p1.Y);
                p.Y = Math.Max(rectPoint1.Y, rectPoint2.Y);
            }
            else if ((outcode & LEFT) != 0)
            {
                p.Y = p1.Y + (p2.Y - p1.Y) * (Math.Min(rectPoint1.X, rectPoint2.X) - p1.X) / (p2.X - p1.X);
                p.X = Math.Min(rectPoint1.X, rectPoint2.X);
            }
            else if ((outcode & RIGHT) != 0)
            {
                p.Y = p1.Y + (p2.Y - p1.Y) * (Math.Max(rectPoint1.X, rectPoint2.X) - p1.X) / (p2.X - p1.X);
                p.X = Math.Max(rectPoint1.X, rectPoint2.X);
            }

            return p;
        }

        class EdgeComparer : IComparer<Edge>
        {
            public int Compare(Edge a, Edge b)
            {
                return a.x.CompareTo(b.x);
            }
        }

        class Edge
        {
            public double x;
            public double m;
            public int ymax;

            public Edge(Point p1, Point p2, int scanlineY)
            {
                Point bottomPoint = p1.Y < p2.Y ? p1 : p2;
                Point topPoint = p1.Y < p2.Y ? p2 : p1;

                ymax = (int)topPoint.Y;

                if (bottomPoint.Y == topPoint.Y)
                    m = 0;
                else
                    m = (p1.X - p2.X) / (p1.Y - p2.Y);

                x = bottomPoint.X + m * (scanlineY - bottomPoint.Y);
            }
        }

        private void FillPolygon_Click(object sender, RoutedEventArgs e)
        {
            if (selectedPolygon == null)
                return;
            int index = polygons.IndexOf(selectedPolygon);
            selectedPolygon = new Tuple<List<Point>, SolidColorBrush, SolidColorBrush, string>(selectedPolygon.Item1, selectedPolygon.Item2, currentColor, null);
            if (index != -1) polygons[index] = selectedPolygon;

            FillPolygon(selectedPolygon);
            
        }

        private void FillPolygon(Tuple<List<Point>, SolidColorBrush, SolidColorBrush, string> polygon)
        {
            List<Point> points = polygon.Item1;
            SolidColorBrush color = polygon.Item3;

            if (points.Count < 3)
                return;

            int N = points.Count;
            int[] indices = Enumerable.Range(0, N).OrderBy(j => points[j].Y).ToArray();

            int k = 0;
            int y = (int)points[indices[0]].Y;
            int ymin = y;
            int ymax = (int)points[indices[N - 1]].Y;
            List<Edge> AET = new List<Edge>();

            while (y < ymax)
            {
                while (k < N && points[indices[k]].Y == y)
                {
                    int i = indices[k];
                    int prevIndex = (i == 0) ? N - 1 : i - 1;
                    int nextIndex = (i == N - 1) ? 0 : i + 1;

                    if (points[prevIndex].Y > points[i].Y)
                        AET.Add(new Edge(points[i], points[prevIndex], y));

                    if (points[nextIndex].Y > points[i].Y)
                        AET.Add(new Edge(points[i], points[nextIndex], y));

                    k++;
                }

                AET.RemoveAll(edge => edge.ymax == y);

                AET.Sort(new EdgeComparer());

                for (int j = 0; j < AET.Count; j += 2)
                {
                    int xStart = (int)Math.Ceiling(AET[j].x);
                    int xEnd = (int)Math.Ceiling(AET[j + 1].x);
                    for (int x = xStart; x < xEnd; x++)
                    {
                        DrawPixel(x, y, color);
                    }
                }

                y++;

                for (int j = 0; j < AET.Count; j++)
                {
                    AET[j].x += AET[j].m;
                }
            }
        }

        private void Canvas_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
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
                    DrawCircle(centerPoint, (int)Distance(centerPoint, edgePoint), currentColor);
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
                else if (Distance(e.GetPosition(canvas), startPoint) < 10)
                {
                    polygonPoints.Add(startPoint);
                    DrawLineOnCanvas(polygonPoints[polygonPoints.Count - 2], startPoint, currentColor);
                    DrawPolygon(polygonPoints, currentColor, null, null);
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

            else if (isDrawingRect)
            {
                if (startPoint == default(Point))
                {
                    startPoint = e.GetPosition(canvas);
                }
                else
                {
                    Point endPoint = e.GetPosition(canvas);
                    DrawLineOnCanvas(startPoint, new Point(endPoint.X, startPoint.Y), currentColor);
                    DrawLineOnCanvas(startPoint, new Point(startPoint.X, endPoint.Y), currentColor);
                    DrawLineOnCanvas(endPoint, new Point(endPoint.X, startPoint.Y), currentColor);
                    DrawLineOnCanvas(endPoint, new Point(startPoint.X, endPoint.Y), currentColor);

                    rectangles.Add(new Tuple<Point, Point, SolidColorBrush>(startPoint, endPoint, currentColor));
                    isDrawingRect = false;
                    startPoint = default(Point);
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

            else if (isSelectingRect)
            {
                Point clickedPoint = e.GetPosition(canvas);
                string imageFill = null;
                foreach (var rectangle in rectangles)
                {
                    List<Point> rectanglePoints = new List<Point>
                    {
                        new Point(rectangle.Item1.X, rectangle.Item1.Y),
                        new Point(rectangle.Item2.X, rectangle.Item1.Y),
                        new Point(rectangle.Item2.X, rectangle.Item2.Y),
                        new Point(rectangle.Item1.X, rectangle.Item2.Y)
                    };

                    if (IsPointInsidePolygon(clickedPoint, Tuple.Create(rectanglePoints, currentColor, new SolidColorBrush(), imageFill)))
                    {
                        selectedRect = rectangle;
                        selectedRectangleText.Text = $"Selected Rectangle: {rectangles.IndexOf(rectangle)}";
                        isSelectingRect = false;
                        return;
                    }
                }
                selectedRect = null;
                selectedRectangleText.Text = "Selected Rectangle: None";
                isSelectingRect = false;
            }

            else if (isDraggingVertex)
            {
                Point clickedPoint = e.GetPosition(canvas);
                if (selectedPolygon != null)
                {
                    foreach (var polygon in polygons)
                    {
                        foreach (var vertex in polygon.Item1)
                        {
                            if (Distance(clickedPoint, vertex) < 10)
                            {
                                selectedVertex = vertex;
                                return;
                            }
                        }
                    }
                }

                if (selectedRect != null)
                {
                    foreach (var rectangle in rectangles)
                    {
                        List<Point> rectanglePoints = new List<Point>
                        {
                            new Point(rectangle.Item1.X, rectangle.Item1.Y),
                            new Point(rectangle.Item2.X, rectangle.Item1.Y),
                            new Point(rectangle.Item2.X, rectangle.Item2.Y),
                            new Point(rectangle.Item1.X, rectangle.Item2.Y)
                        };
                        foreach (var vertex in rectanglePoints)
                        {
                            if (Distance(clickedPoint, vertex) < 10)
                            {
                                selectedVertex = vertex;
                                return;
                            }
                        }
                    }
                }
            }

            else if (isFloodFilling)
            {
                Point clickPosition = e.GetPosition(canvas);

                int x = (int)clickPosition.X;
                int y = (int)clickPosition.Y;

                Color targetColor = GetPixelColor(writeableBitmap, x, y);

                FloodFill(writeableBitmap, x, y, targetColor, currentColor.Color);
                isFloodFilling = false; 
            }
        }

        private void FloodFill(WriteableBitmap wb, int x, int y, Color targetColor, Color replacementColor)
        {
            if (targetColor == replacementColor)
                return;

            int width = wb.PixelWidth;
            int height = wb.PixelHeight;
            int stride = width * (wb.Format.BitsPerPixel / 8);
            byte[] pixels = new byte[height * stride];
            wb.CopyPixels(pixels, stride, 0);

            Queue<Point> queue = new Queue<Point>();
            queue.Enqueue(new Point(x, y));

            while (queue.Count > 0)
            {
                Point p = queue.Dequeue();
                int px = (int)p.X;
                int py = (int)p.Y;

                if (px < 0 || px >= width || py < 0 || py >= height)
                    continue;

                int index = (py * stride) + (px * 4);

                byte b = pixels[index];
                byte g = pixels[index + 1];
                byte r = pixels[index + 2];
                byte a = pixels[index + 3];

                if (r == targetColor.R && g == targetColor.G && b == targetColor.B && a == targetColor.A)
                {
                    pixels[index] = replacementColor.B;
                    pixels[index + 1] = replacementColor.G;
                    pixels[index + 2] = replacementColor.R;
                    pixels[index + 3] = replacementColor.A;

                    queue.Enqueue(new Point(px + 1, py));
                    queue.Enqueue(new Point(px - 1, py));
                    queue.Enqueue(new Point(px, py + 1));
                    queue.Enqueue(new Point(px, py - 1));
                }
            }

            wb.WritePixels(new Int32Rect(0, 0, width, height), pixels, stride, 0);
        }

        private Color GetPixelColor(WriteableBitmap wb, int x, int y)
        {
            int stride = (int)wb.PixelWidth * (wb.Format.BitsPerPixel / 8);
            byte[] pixelData = new byte[stride];
            wb.CopyPixels(new Int32Rect(x, y, 1, 1), pixelData, stride, 0);

            return Color.FromArgb(pixelData[3], pixelData[2], pixelData[1], pixelData[0]);
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
            DrawCubicBezierCurveOnCanvas(p0, p1, p2, p3, color);
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
            if (e.LeftButton != MouseButtonState.Pressed)
                return;

            Point endPointOfDrag = e.GetPosition(canvas);

            if (isDraggingEndpoint)
                HandleDraggingEndpoint(endPointOfDrag);
            else if (isEditingRadius && selectedCircle != null)
                HandleEditingRadius(endPointOfDrag);
            else if (isMovingCircle && selectedCircle != null)
                HandleMovingCircle(endPointOfDrag);
            else if (isMovingRectangle && selectedRect != null)
                HandleMovingRectangle(endPointOfDrag);
            else if (isDraggingVertex && (selectedPolygon != null || selectedRect != null))
                HandleDraggingVertex(endPointOfDrag);
        }

        private void HandleDraggingEndpoint(Point endPointOfDrag)
        {
            if (selectedLine != null)
            {
                int index = lines.IndexOf(lineBeingDragged);
                if (index != -1)
                {
                    if (selectedPoint == selectedLine.Item1)
                        lineBeingDragged = new Tuple<Point, Point, bool, SolidColorBrush>(endPointOfDrag, lineBeingDragged.Item2, true, lineBeingDragged.Item4);
                    else if (selectedPoint == selectedLine.Item2)
                        lineBeingDragged = new Tuple<Point, Point, bool, SolidColorBrush>(lineBeingDragged.Item1, endPointOfDrag, true, lineBeingDragged.Item4);

                    lines[index] = lineBeingDragged;
                    RedrawShapes();
                }
            }
        }

        private void HandleEditingRadius(Point endPointOfDrag)
        {
            int index = circles.IndexOf(selectedCircle);
            if (index != -1)
            {
                selectedCircle = new Tuple<Point, int, SolidColorBrush>(selectedCircle.Item1, (int)Distance(selectedCircle.Item1, endPointOfDrag), selectedCircle.Item3);
                circles[index] = selectedCircle;
                RedrawShapes();
            }
        }

        private void HandleMovingCircle(Point endPointOfDrag)
        {
            int index = circles.IndexOf(selectedCircle);
            if (index != -1)
            {
                selectedCircle = new Tuple<Point, int, SolidColorBrush>(endPointOfDrag, selectedCircle.Item2, selectedCircle.Item3);
                circles[index] = selectedCircle;
                RedrawShapes();
            }
        }

        private void HandleMovingRectangle(Point endPointOfDrag)
        {
            int index = rectangles.IndexOf(selectedRect);
            if (index != -1)
            {
                Point currentCenter = new Point(
                    (selectedRect.Item1.X + selectedRect.Item2.X) / 2,
                    (selectedRect.Item1.Y + selectedRect.Item2.Y) / 2);

                double deltaX = endPointOfDrag.X - currentCenter.X;
                double deltaY = endPointOfDrag.Y - currentCenter.Y;

                Point newEndPoint1 = new Point(selectedRect.Item1.X + deltaX, selectedRect.Item1.Y + deltaY);
                Point newEndPoint2 = new Point(selectedRect.Item2.X + deltaX, selectedRect.Item2.Y + deltaY);

                selectedRect = new Tuple<Point, Point, SolidColorBrush>(newEndPoint1, newEndPoint2, selectedRect.Item3);

                rectangles[index] = selectedRect;

                RedrawShapes();
            }
        }

        private void HandleDraggingVertex(Point endPointOfDrag)
        {
            if (selectedPolygon != null)
            {
                int index = polygons.IndexOf(selectedPolygon);
                if (index != -1 && selectedVertex != null)
                {
                    int indexOfVertex = polygons[index].Item1.IndexOf(selectedVertex);
                    if (indexOfVertex != -1)
                    {
                        List<Point> newPoints = polygons[index].Item1;
                        newPoints[indexOfVertex] = endPointOfDrag;

                        selectedVertex = endPointOfDrag;
                        selectedPolygon = new Tuple<List<Point>, SolidColorBrush, SolidColorBrush, string>(newPoints, selectedPolygon.Item2, selectedPolygon.Item3, selectedPolygon.Item4);
                        polygons[index] = selectedPolygon;
                        RedrawShapes();
                    }
                }
            }

            if (selectedRect != null)
            {
                int index = rectangles.IndexOf(selectedRect);
                if (index != -1 && selectedVertex != null)
                {
                    List<Point> rectanglePoints = new List<Point>
                    {
                         new Point(selectedRect.Item1.X, selectedRect.Item1.Y),
                         new Point(selectedRect.Item2.X, selectedRect.Item1.Y),
                         new Point(selectedRect.Item2.X, selectedRect.Item2.Y),
                         new Point(selectedRect.Item1.X, selectedRect.Item2.Y)
                    };

                    int indexOfVertex = rectanglePoints.IndexOf(selectedVertex);
                    if (indexOfVertex != -1)
                    {
                        int oppositeIndex = (indexOfVertex + 2) % 4;

                        Point end = endPointOfDrag;
                        Point start = new Point(rectanglePoints[oppositeIndex].X, rectanglePoints[oppositeIndex].Y);

                        selectedVertex = endPointOfDrag;
                        selectedRect = new Tuple<Point, Point, SolidColorBrush>(start, end, selectedRect.Item3);
                        rectangles[index] = selectedRect;
                        RedrawShapes();
                    }
                }
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
                selectedRect = null;
                selectedVertex = new Point();
                selectedPolygonText.Text = $"Selected Polygon: None";
                selectedRectangleText.Text = $"Selected Rectangle: None";
                isDraggingVertex = false;
                isSelectingPolygon = false;
                isSelectingRect = false;
            }
        }

        private void ClearWriteableBitmap(WriteableBitmap writeableBitmap)
        {
            // Get a reference to the back buffer
            writeableBitmap.Lock();

            // Calculate the number of bytes per pixel
            int bytesPerPixel = (writeableBitmap.Format.BitsPerPixel + 7) / 8;

            // Get the width and height of the bitmap
            int width = writeableBitmap.PixelWidth;
            int height = writeableBitmap.PixelHeight;

            // Create a byte array with the size of the bitmap
            int stride = width * bytesPerPixel;
            byte[] clearArray = new byte[height * stride];

            // Set the pixels to transparent (assuming 32 bits per pixel with BGRA format)
            for (int i = 0; i < clearArray.Length; i += 4)
            {
                clearArray[i] = 0;     // Blue
                clearArray[i + 1] = 0; // Green
                clearArray[i + 2] = 0; // Red
                clearArray[i + 3] = 0; // Alpha (transparent)
            }

            // Copy the clearArray to the back buffer
            Int32Rect rect = new Int32Rect(0, 0, width, height);
            writeableBitmap.WritePixels(rect, clearArray, stride, 0);

            // Unlock the back buffer
            writeableBitmap.Unlock();
        }
          
        private void RedrawShapes()
        {
            canvas.Children.Clear();

            if (writeableBitmap != null)
            {
                Image image = new Image();
                image.Source = writeableBitmap;
                canvas.Children.Add(image);
                ClearWriteableBitmap(writeableBitmap);
            }
            foreach (var line in lines)
            {
                DrawLineOnCanvas(line.Item1, line.Item2, line.Item4);
            }
            foreach (var circle in circles)
            {
                DrawCircleOnCanvas(circle.Item1, circle.Item2, circle.Item3);
            }
            foreach (var rect in rectangles)
            {
                DrawLineOnCanvas(rect.Item1, new Point(rect.Item2.X, rect.Item1.Y), rect.Item3);
                DrawLineOnCanvas(rect.Item1, new Point(rect.Item1.X, rect.Item2.Y), rect.Item3);
                DrawLineOnCanvas(rect.Item2, new Point(rect.Item2.X, rect.Item1.Y), rect.Item3);
                DrawLineOnCanvas(rect.Item2, new Point(rect.Item1.X, rect.Item2.Y), rect.Item3);
            }
            foreach (var polygon in polygons)
            {
                DrawPolygonOnCanvas(polygon);
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

        private void DrawPolygonOnCanvas(Tuple<List<Point>, SolidColorBrush, SolidColorBrush, string> polygon)
        {
            if (polygon.Item1.Count < 2)
                return;

            for (int i = 0; i < polygon.Item1.Count - 1; i++)
            {
                DrawLineOnCanvas(polygon.Item1[i], polygon.Item1[i + 1], polygon.Item2);
            }

            DrawLineOnCanvas(polygon.Item1[polygon.Item1.Count - 1], polygon.Item1[0], polygon.Item2);
            if(polygon.Item4 != null)
            {
                FillPolygonWithImage(polygon.Item1, polygon.Item4);
            }
            else if(polygon.Item3 != null)
            {
                FillPolygon(polygon);
            }
        }

        private void DrawPolygon(List<Point> points, SolidColorBrush color, SolidColorBrush fillColor, string fillImage)
        {
            polygons.Add(new Tuple<List<Point>, SolidColorBrush, SolidColorBrush, string>(points, color, fillColor, fillImage));
        }

        private Color ConvertBrushToColor(Brush brush)
        {
            if (brush is SolidColorBrush solidColorBrush)
            {
                return solidColorBrush.Color;
            }

            return Colors.Transparent;
        }

        private void DrawPixel(int x, int y, Brush brush)
        {
            if (writeableBitmap == null || x < 0 || x >= writeableBitmap.PixelWidth || y < 0 || y >= writeableBitmap.PixelHeight)
                return;

            Color color = ConvertBrushToColor(brush);

            byte[] colorData = new byte[4] { color.B, color.G, color.R, color.A };
            Int32Rect rect = new Int32Rect(x, y, 1, 1);

            writeableBitmap.WritePixels(rect, colorData, 4, 0);
        }

        private (List<Point>, bool) SymmetricMidpointLine(int x0, int y0, int x1, int y1)
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

            return (points, horizontal);
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

        private bool IsPointInsidePolygon(Point point, Tuple<List<Point>, SolidColorBrush, SolidColorBrush, string> polygon)
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
            if (selectedLine != null)
            {
                if (selectedLine.Item3)
                {
                    RedrawShapes();
                    for (int i = 1; i < ThicknessSlider.Value / 2; i++)
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
                Polygons = polygons,
                Rectangles = rectangles
            };

            string json = JsonConvert.SerializeObject(shapes, Formatting.Indented);

            File.WriteAllText(filePath, json);
        }

        private void LoadShapesFromFile(string filePath)
        {
            string json = File.ReadAllText(filePath);

            var shapes = JsonConvert.DeserializeObject<dynamic>(json);

            lines = JsonConvert.DeserializeObject<List<Tuple<Point, Point, bool, SolidColorBrush>>>(shapes.Lines.ToString());
            rectangles = JsonConvert.DeserializeObject<List<Tuple<Point, Point, SolidColorBrush>>>(shapes.Lines.ToString());
            circles = JsonConvert.DeserializeObject<List<Tuple<Point, int, SolidColorBrush>>>(shapes.Circles.ToString());
            polygons = JsonConvert.DeserializeObject<List<Tuple<List<Point>, SolidColorBrush, SolidColorBrush, string>>>(shapes.Polygons.ToString());
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

        public void DrawAntiAliased(Point p1, Point p2, Color color)
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
