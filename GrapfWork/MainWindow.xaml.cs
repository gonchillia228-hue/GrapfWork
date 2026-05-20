using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace GraphApp
{
    public class Graph
    {
        public int Vertices { get; }
        public int[,] Matrix { get; }
        public bool Directed { get; }
        public const int Infinity = int.MaxValue / 2;

        public Graph(int vertices, bool directed)
        {
            Vertices = vertices;
            Directed = directed;
            Matrix = new int[vertices, vertices];
            for (int i = 0; i < vertices; i++)
                for (int j = 0; j < vertices; j++)
                    Matrix[i, j] = (i == j) ? 0 : Infinity;
        }

        public void AddEdge(int from, int to, int weight)
        {
            if (from < 0 || from >= Vertices || to < 0 || to >= Vertices || weight < 0)
                throw new ArgumentException("Некоректні дані для ребра.");

            Matrix[from, to] = weight;
            if (!Directed) Matrix[to, from] = weight;
        }
    }

    public static class PathFinder
    {
        public static (int[,] dist, int[,] next, List<int[,]> steps, long operations) FloydWarshall(Graph graph)
        {
            int n = graph.Vertices;
            int[,] dist = (int[,])graph.Matrix.Clone();
            int[,] next = new int[n, n];
            var steps = new List<int[,]>();
            long operations = 0;

            for (int i = 0; i < n; i++)
                for (int j = 0; j < n; j++)
                    next[i, j] = (dist[i, j] < Graph.Infinity && i != j) ? j : -1;

            for (int k = 0; k < n; k++)
            {
                for (int i = 0; i < n; i++)
                {
                    if (dist[i, k] >= Graph.Infinity) continue;

                    for (int j = 0; j < n; j++)
                    {
                        operations++;
                        int newDist = dist[i, k] + dist[k, j];
                        if (newDist < dist[i, j])
                        {
                            dist[i, j] = newDist;
                            next[i, j] = next[i, k];
                        }
                    }
                }
                steps.Add((int[,])dist.Clone());
            }

            return (dist, next, steps, operations);
        }

        public static (int[,] dist, int[,] next, long operations) Dantzig(Graph graph)
        {
            int n = graph.Vertices;
            int[,] dist = new int[n, n];
            int[,] next = new int[n, n];
            long operations = 0;

            for (int i = 0; i < n; i++)
            {
                for (int j = 0; j < n; j++)
                {
                    dist[i, j] = Graph.Infinity;
                    next[i, j] = -1;
                }
            }

            if (n > 0) dist[0, 0] = 0;

            for (int k = 1; k < n; k++)
            {
                for (int i = 0; i < k; i++)
                {
                    dist[i, k] = graph.Matrix[i, k];
                    if (dist[i, k] < Graph.Infinity) next[i, k] = k;

                    dist[k, i] = graph.Matrix[k, i];
                    if (dist[k, i] < Graph.Infinity) next[k, i] = i;

                    for (int j = 0; j < k; j++)
                    {
                        operations++;
                        if (dist[i, j] < Graph.Infinity && graph.Matrix[j, k] < Graph.Infinity)
                        {
                            if (dist[i, j] + graph.Matrix[j, k] < dist[i, k])
                            {
                                dist[i, k] = dist[i, j] + graph.Matrix[j, k];
                                next[i, k] = next[i, j];
                            }
                        }

                        if (graph.Matrix[k, j] < Graph.Infinity && dist[j, i] < Graph.Infinity)
                        {
                            if (dist[k, i] > graph.Matrix[k, j] + dist[j, i])
                            {
                                dist[k, i] = graph.Matrix[k, j] + dist[j, i];
                                next[k, i] = next[k, j] != -1 ? next[k, j] : j;
                            }
                        }
                    }
                }

                for (int i = 0; i < k; i++)
                {
                    for (int j = 0; j < k; j++)
                    {
                        operations++;
                        if (dist[i, k] < Graph.Infinity && dist[k, j] < Graph.Infinity)
                        {
                            if (dist[i, k] + dist[k, j] < dist[i, j])
                            {
                                dist[i, j] = dist[i, k] + dist[k, j];
                                next[i, j] = next[i, k];
                            }
                        }
                    }
                }
                dist[k, k] = 0;
            }

            return (dist, next, operations);
        }

        public static List<int> GetPath(int u, int v, int[,] next)
        {
            if (next[u, v] == -1) return null;

            var path = new List<int> { u };
            while (u != v)
            {
                u = next[u, v];
                path.Add(u);
            }
            return path;
        }
    }

    public static class GraphIO
    {
        public static Graph LoadFromFile(string path, bool directed)
        {
            if (!File.Exists(path)) throw new FileNotFoundException("Файл не знайдено.");
            var lines = File.ReadLines(path).Where(l => !string.IsNullOrWhiteSpace(l)).ToArray();

            if (lines.Length == 0) throw new FormatException("Файл порожній.");
            if (!int.TryParse(lines[0], out int n)) throw new FormatException("Помилка: перший рядок файлу має містити кількість вершин (ціле число).");

            Graph g = new Graph(n, directed);
            for (int i = 1; i < lines.Length; i++)
            {
                var parts = lines[i].Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);

                if (parts.Length != 3)
                    throw new FormatException($"Помилка у рядку {i + 1} ('{lines[i]}'): очікуються рівно три значення.");

                if (!int.TryParse(parts[0], out int u) || !int.TryParse(parts[1], out int v) || !int.TryParse(parts[2], out int w))
                    throw new FormatException($"Помилка у рядку {i + 1} ('{lines[i]}'): виявлено літери або символи. Мають бути лише цілі числа.");

                if (u < 0 || v < 0 || w < 0)
                    throw new FormatException($"Помилка у рядку {i + 1} ('{lines[i]}'): значення не можуть бути від'ємними.");

                g.AddEdge(u, v, w);
            }
            return g;
        }

        public static Graph ParseManualInput(string text, bool directed)
        {
            var lines = text.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
            if (lines.Length == 0) throw new FormatException("Немає введених ребер.");

            var edges = new List<(int u, int v, int w)>();
            int maxVertex = 0;

            for (int i = 0; i < lines.Length; i++)
            {
                var parts = lines[i].Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);

                if (parts.Length != 3)
                    throw new FormatException($"Помилка у рядку {i + 1} ('{lines[i]}'): має бути рівно три значення (від, до, вага).");

                if (!int.TryParse(parts[0], out int u) || !int.TryParse(parts[1], out int v) || !int.TryParse(parts[2], out int w))
                    throw new FormatException($"Помилка у рядку {i + 1} ('{lines[i]}'): виявлено літери або недопустимі символи.");

                if (u < 0 || v < 0 || w < 0)
                    throw new FormatException($"Помилка у рядку {i + 1} ('{lines[i]}'): значення вершин та ваги не можуть бути від'ємними.");

                maxVertex = Math.Max(maxVertex, Math.Max(u, v));
                edges.Add((u, v, w));
            }

            Graph g = new Graph(maxVertex + 1, directed);
            foreach (var edge in edges) g.AddEdge(edge.u, edge.v, edge.w);
            return g;
        }
    }

    public static class GraphVisualizer
    {
        public static void Draw(Graph graph, Canvas canvas, List<int> path = null)
        {
            canvas.Children.Clear();
            double w = canvas.ActualWidth == 0 ? 800 : canvas.ActualWidth;
            double h = canvas.ActualHeight == 0 ? 400 : canvas.ActualHeight;
            double radius = 15;

            var positions = new Point[graph.Vertices];
            double graphRadius = Math.Min(w, h) / 2.5;

            for (int i = 0; i < graph.Vertices; i++)
            {
                double angle = 2 * Math.PI * i / graph.Vertices;
                positions[i] = new Point(w / 2 + graphRadius * Math.Cos(angle), h / 2 + graphRadius * Math.Sin(angle));
            }

            for (int i = 0; i < graph.Vertices; i++)
            {
                for (int j = 0; j < graph.Vertices; j++)
                {
                    if (graph.Matrix[i, j] < Graph.Infinity && i != j)
                    {
                        bool isPathEdge = false;
                        if (path != null)
                        {
                            for (int p = 0; p < path.Count - 1; p++)
                            {
                                if (path[p] == i && path[p + 1] == j)
                                {
                                    isPathEdge = true;
                                    break;
                                }
                            }
                        }

                        var line = new Line
                        {
                            X1 = positions[i].X,
                            Y1 = positions[i].Y,
                            X2 = positions[j].X,
                            Y2 = positions[j].Y,
                            Stroke = isPathEdge ? Brushes.Red : Brushes.Black,
                            StrokeThickness = isPathEdge ? 4 : 1.5,
                            Opacity = isPathEdge ? 1.0 : 0.4
                        };
                        canvas.Children.Add(line);

                        var weightText = new TextBlock
                        {
                            Text = graph.Matrix[i, j].ToString(),
                            Foreground = isPathEdge ? Brushes.DarkRed : Brushes.Gray,
                            FontWeight = isPathEdge ? FontWeights.Bold : FontWeights.Normal,
                            Background = Brushes.White,
                            Padding = new Thickness(2)
                        };

                        double textX = positions[i].X + (positions[j].X - positions[i].X) * 0.3;
                        double textY = positions[i].Y + (positions[j].Y - positions[i].Y) * 0.3;

                        Canvas.SetLeft(weightText, textX - 5);
                        Canvas.SetTop(weightText, textY - 5);
                        canvas.Children.Add(weightText);
                    }
                }
            }

            for (int i = 0; i < graph.Vertices; i++)
            {
                bool isNodeInPath = path != null && path.Contains(i);

                var ellipse = new Ellipse
                {
                    Width = radius * 2,
                    Height = radius * 2,
                    Fill = isNodeInPath ? Brushes.Yellow : Brushes.LightBlue,
                    Stroke = isNodeInPath ? Brushes.Red : Brushes.Black,
                    StrokeThickness = isNodeInPath ? 3 : 1
                };
                Canvas.SetLeft(ellipse, positions[i].X - radius);
                Canvas.SetTop(ellipse, positions[i].Y - radius);
                canvas.Children.Add(ellipse);

                var label = new TextBlock { Text = i.ToString(), FontWeight = FontWeights.Bold };
                label.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
                Canvas.SetLeft(label, positions[i].X - label.DesiredSize.Width / 2);
                Canvas.SetTop(label, positions[i].Y - label.DesiredSize.Height / 2);
                canvas.Children.Add(label);
            }
        }
    }

    public partial class MainWindow : Window
    {
        private Graph graph;

        public MainWindow()
        {
            InitializeComponent();
        }

        private void HelpButton_Click(object sender, RoutedEventArgs e)
        {
            string helpText =
                "ІНСТРУКЦІЯ КОРИСТУВАЧА\n\n" +
                "1. Введення графа:\n" +
                "   • З файлу (.txt): Першим рядком вкажіть загальну кількість вершин. Далі пропишіть ребра (від, до, вага).\n" +
                "   • Вручну: Вводьте кожне ребро з нового рядка у форматі 'від до вага'.\n" +
                "   • Орієнтованість: Якщо зняти галочку, програма автоматично створить двосторонні дороги для кожного введеного ребра.\n\n" +
                "2. Обчислення:\n" +
                "   • Оберіть метод (Floyd-Warshall або Dantzig).\n" +
                "   • Вкажіть початкову ('Від') та кінцеву ('До') вершини, щоб прокласти конкретний маршрут.\n" +
                "   • Окрім часу, програма також виведе практичну складність (реальну кількість виконаних ітерацій).\n\n" +
                "3. Візуалізація:\n" +
                "   • Граф малюється автоматично. Знайдений найкоротший шлях буде підсвічено червоним кольором та жовтими вершинами.\n\n" +
                "4. Експорт результатів:\n" +
                "   • «Зберегти у файл» – вивантажує текстовий звіт (матриці та кроки) у .txt документ.\n" +
                "   • «Зберегти картинку графа» – робить знімок поточного стану графа і зберігає його у форматі .png.";

            MessageBox.Show(helpText, "Довідка: Пошук найкоротших шляхів", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void AddEdgesButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                graph = GraphIO.ParseManualInput(ManualEdgesTextBox.Text, directed: true);
                MessageBox.Show("Граф створено!");
                GraphVisualizer.Draw(graph, GraphCanvas);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Помилка", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private void ClearEdgesButton_Click(object sender, RoutedEventArgs e)
        {
            ManualEdgesTextBox.Clear();
        }

        private void LoadGraphButton_Click(object sender, RoutedEventArgs e)
        {
            var ofd = new OpenFileDialog { Filter = "Text Files|*.txt" };
            if (ofd.ShowDialog() == true)
            {
                try
                {
                    graph = GraphIO.LoadFromFile(ofd.FileName, directed: true);
                    FilePathTextBlock.Text = ofd.FileName;
                    MessageBox.Show("Граф завантажено!");
                    GraphVisualizer.Draw(graph, GraphCanvas);
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message, "Помилка файлу", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void ComputePathsButton_Click(object sender, RoutedEventArgs e)
        {
            if (graph == null) { MessageBox.Show("Граф не створено."); return; }

            if (!int.TryParse(StartNodeTextBox.Text, out int startNode) ||
                !int.TryParse(EndNodeTextBox.Text, out int endNode))
            {
                MessageBox.Show("Будь ласка, введіть коректні числа для вершин.", "Увага", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (startNode < 0 || startNode >= graph.Vertices || endNode < 0 || endNode >= graph.Vertices)
            {
                MessageBox.Show($"Номери вершин мають бути в межах від 0 до {graph.Vertices - 1}.", "Помилка", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            var algo = (AlgorithmComboBox.SelectedItem as ComboBoxItem)?.Content.ToString();
            int[,] dist;
            int[,] next = null;
            List<int[,]> steps = null;
            long operations = 0;

            var sw = Stopwatch.StartNew();

            if (algo == "Floyd-Warshall")
            {
                var result = PathFinder.FloydWarshall(graph);
                dist = result.dist;
                next = result.next;
                steps = result.steps;
                operations = result.operations;
            }
            else if (algo == "Dantzig")
            {
                var result = PathFinder.Dantzig(graph);
                dist = result.dist;
                next = result.next;
                operations = result.operations;
            }
            else return;

            sw.Stop();

            var sb = new StringBuilder();

            sb.AppendLine("=== ФІНАЛЬНА МАТРИЦЯ ===");
            sb.AppendLine(FormatMatrix(dist));

            sb.AppendLine($"Час виконання: {sw.Elapsed.TotalMilliseconds:F4} ms");
            sb.AppendLine($"Практична складність (ітерацій): {operations}");

            if (steps != null)
            {
                sb.AppendLine("\n=== ПРОМІЖНІ КРОКИ ===");
                for (int i = 0; i < steps.Count; i++)
                {
                    sb.AppendLine($"\nКрок k = {i}");
                    sb.AppendLine(FormatMatrix(steps[i]));
                }
            }

            if (next != null)
            {
                var path = PathFinder.GetPath(startNode, endNode, next);
                if (path != null)
                {
                    sb.AppendLine($"\nШлях {startNode} → {endNode}:");
                    sb.AppendLine(string.Join(" -> ", path));
                    GraphVisualizer.Draw(graph, GraphCanvas, path);
                }
                else
                {
                    sb.AppendLine($"\nШляху {startNode} → {endNode} не існує");
                    GraphVisualizer.Draw(graph, GraphCanvas);

                    // НОВЕ: Спливаюче вікно з повідомленням про відсутність шляху
                    MessageBox.Show(
                        $"Неможливо прокласти маршрут.\nШляху між вершинами {startNode} та {endNode} не існує у заданому напрямку.",
                        "Маршрут не знайдено",
                        MessageBoxButton.OK,
                        MessageBoxImage.Information);
                }
            }
            else
            {
                GraphVisualizer.Draw(graph, GraphCanvas);
            }

            ResultTextBox.Text = sb.ToString();
        }

        private void SaveImageButton_Click(object sender, RoutedEventArgs e)
        {
            // Перевіряємо, чи є взагалі що зберігати (чи не порожнє полотно)
            if (GraphCanvas.Children.Count == 0)
            {
                MessageBox.Show("Немає графа для збереження. Спочатку побудуйте його.", "Увага", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var sfd = new SaveFileDialog
            {
                Filter = "PNG Image|*.png", // Дозволяємо зберігати лише у форматі картинки
                Title = "Зберегти зображення графа",
                FileName = "MyGraph.png"
            };

            if (sfd.ShowDialog() == true)
            {
                try
                {
                    // 1. Створюємо "віртуальний фотоапарат" розміром з наше полотно
                    var rtb = new RenderTargetBitmap(
                        (int)GraphCanvas.RenderSize.Width,
                        (int)GraphCanvas.RenderSize.Height,
                        96d, 96d, PixelFormats.Default);

                    // 2. "Фотографуємо" полотно
                    rtb.Render(GraphCanvas);

                    // 3. Створюємо кодувальник, який перетворить "фотографію" у формат PNG
                    var encoder = new PngBitmapEncoder();
                    encoder.Frames.Add(BitmapFrame.Create(rtb));

                    // 4. Записуємо картинку у файл на диску
                    using (var fs = File.OpenWrite(sfd.FileName))
                    {
                        encoder.Save(fs);
                    }

                    MessageBox.Show("Зображення графа успішно збережено!", "Успіх", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Помилка при збереженні зображення: {ex.Message}", "Помилка", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }
        private string FormatMatrix(int[,] mat)
        {
            var sb = new StringBuilder();
            int n = mat.GetLength(0);
            for (int i = 0; i < n; i++)
            {
                for (int j = 0; j < n; j++)
                {
                    sb.Append(mat[i, j] >= Graph.Infinity ? "∞\t" : $"{mat[i, j]}\t");
                }
                sb.AppendLine();
            }
            return sb.ToString();
        }

        private void SaveResultButton_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(ResultTextBox.Text))
            {
                MessageBox.Show("Немає результатів для збереження. Спочатку обчисліть шляхи.", "Увага", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            var sfd = new SaveFileDialog
            {
                Filter = "Text Files|*.txt",
                Title = "Зберегти результати обчислень",
                FileName = "GraphResults.txt"
            };

            if (sfd.ShowDialog() == true)
            {
                try
                {
                    File.WriteAllText(sfd.FileName, ResultTextBox.Text);
                    MessageBox.Show("Результати успішно збережено!", "Успіх", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Помилка при збереженні файлу: {ex.Message}", "Помилка", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }
    }
}