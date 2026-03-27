using Microsoft.Win32;
using System;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;

namespace GraphApp
{
    public class Graph
    {
        public int Vertices { get; private set; }
        public int[,] Matrix { get; private set; }
        public bool Directed { get; private set; }

        public Graph(int vertices, bool directed)
        {
            Vertices = vertices;
            Directed = directed;
            Matrix = new int[vertices, vertices];
            for (int i = 0; i < vertices; i++)
                for (int j = 0; j < vertices; j++)
                    Matrix[i, j] = (i == j) ? 0 : int.MaxValue / 2;
        }

        public void AddEdge(int from, int to, int weight)
        {
            if (from < 0 || from >= Vertices || to < 0 || to >= Vertices || weight < 0)
            {
                MessageBox.Show("Некоректні дані для ребра.");
                return;
            }

            Matrix[from, to] = weight;
            if (!Directed) Matrix[to, from] = weight;
        }

        // Floyd-Warshall з матрицею попередників
        public (int[,], int[,]) FloydWarshall()
        {
            int n = Vertices;
            int[,] dist = (int[,])Matrix.Clone();
            int[,] next = new int[n, n];

            for (int i = 0; i < n; i++)
                for (int j = 0; j < n; j++)
                    next[i, j] = (dist[i, j] < int.MaxValue / 2 && i != j) ? j : -1;

            for (int k = 0; k < n; k++)
                for (int i = 0; i < n; i++)
                    for (int j = 0; j < n; j++)
                        if (dist[i, k] + dist[k, j] < dist[i, j])
                        {
                            dist[i, j] = dist[i, k] + dist[k, j];
                            next[i, j] = next[i, k];
                        }

            return (dist, next);
        }

        // Данциг з матрицею попередників
        public (int[,], int[,]) Dantzig()
        {
            int n = Vertices;
            int[,] dist = (int[,])Matrix.Clone();
            int[,] next = new int[n, n];

            for (int i = 0; i < n; i++)
                for (int j = 0; j < n; j++)
                    next[i, j] = (dist[i, j] < int.MaxValue / 2 && i != j) ? j : -1;

            for (int k = 0; k < n; k++)
                for (int i = 0; i < n; i++)
                    for (int j = 0; j < n; j++)
                    {
                        int newDist = dist[i, k] + dist[k, j];
                        if (newDist < dist[i, j])
                        {
                            dist[i, j] = newDist;
                            next[i, j] = next[i, k];
                        }
                    }

            return (dist, next);
        }

        // Відновлення шляху між вершинами
        public int[] ReconstructPath(int[,] next, int u, int v)
        {
            if (next[u, v] == -1) return null;
            var path = new System.Collections.Generic.List<int> { u };
            while (u != v)
            {
                u = next[u, v];
                path.Add(u);
            }
            return path.ToArray();
        }

        // Завантаження з файлу
        public static Graph LoadFromFile(string path, bool directed)
        {
            if (!File.Exists(path))
            {
                MessageBox.Show("Файл не знайдено.");
                return null;
            }

            var lines = File.ReadAllLines(path).Where(l => !string.IsNullOrWhiteSpace(l)).ToArray();
            if (lines.Length == 0)
            {
                MessageBox.Show("Файл порожній.");
                return null;
            }

            if (!int.TryParse(lines[0], out int n))
            {
                MessageBox.Show("Помилка: перший рядок файлу має містити кількість вершин (число).");
                return null;
            }

            Graph g = new Graph(n, directed);

            for (int i = 1; i < lines.Length; i++)
            {
                var parts = lines[i].Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length < 3)
                {
                    MessageBox.Show($"Помилка у рядку {i + 1}: очікуються три числа (від, до, вага).");
                    continue;
                }

                if (!int.TryParse(parts[0], out int from) ||
                    !int.TryParse(parts[1], out int to) ||
                    !int.TryParse(parts[2], out int weight))
                {
                    MessageBox.Show($"Помилка у рядку {i + 1}: дані мають бути числами.");
                    continue;
                }

                g.AddEdge(from, to, weight);
            }

            MessageBox.Show("Граф завантажено успішно!");
            return g;
        }
    }

    public partial class MainWindow : Window
    {
        private Graph graph;

        public MainWindow()
        {
            InitializeComponent();
        }

        private void AddEdgesButton_Click(object sender, RoutedEventArgs e)
        {
            string[] lines = ManualEdgesTextBox.Text.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
            if (lines.Length == 0) { MessageBox.Show("Немає введених ребер."); return; }

            int n = lines.SelectMany(l => l.Split(' ').Take(2)).Select(int.Parse).Max() + 1;
            graph = new Graph(n, directed: true);

            foreach (var line in lines)
            {
                var parts = line.Split();
                if (parts.Length != 3) continue;
                graph.AddEdge(int.Parse(parts[0]), int.Parse(parts[1]), int.Parse(parts[2]));
            }

            MessageBox.Show("Граф створено!");
            VisualizeGraph();
        }

        private void ClearEdgesButton_Click(object sender, RoutedEventArgs e)
        {
            ManualEdgesTextBox.Clear();
        }

        private void LoadGraphButton_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog ofd = new OpenFileDialog { Filter = "Text Files|*.txt" };
            if (ofd.ShowDialog() == true)
            {
                FilePathTextBlock.Text = ofd.FileName;
                graph = Graph.LoadFromFile(ofd.FileName, directed: true);
                if (graph != null)
                {
                    MessageBox.Show("Граф завантажено!");
                    VisualizeGraph();
                }
            }
        }

        // Обчислення шляхів з Floyd-Warshall і Данциг
        private void ComputePathsButton_Click(object sender, RoutedEventArgs e)
        {
            if (graph == null) { MessageBox.Show("Граф не створено."); return; }

            var algo = (AlgorithmComboBox.SelectedItem as ComboBoxItem).Content.ToString();
            int[,] dist;
            int[,] next;

            if (algo == "Floyd-Warshall")
            {
                (dist, next) = graph.FloydWarshall();
            }
            else if (algo == "Dantzig")
            {
                (dist, next) = graph.Dantzig();
            }
            else
            {
                MessageBox.Show("Обрано невідомий алгоритм.");
                return;
            }

            ResultTextBox.Text = MatrixToString(dist);
        }

        private void VisualizeGraph()
        {
            if (graph == null) return;
            GraphCanvas.Children.Clear();
            double w = GraphCanvas.ActualWidth;
            double h = GraphCanvas.ActualHeight;
            if (w == 0) w = 800; if (h == 0) h = 400;

            double radius = 15;
            var positions = new System.Windows.Point[graph.Vertices];
            for (int i = 0; i < graph.Vertices; i++)
            {
                double angle = 2 * Math.PI * i / graph.Vertices;
                positions[i] = new System.Windows.Point(w / 2 + 150 * Math.Cos(angle), h / 2 + 150 * Math.Sin(angle));
            }

            for (int i = 0; i < graph.Vertices; i++)
            {
                for (int j = 0; j < graph.Vertices; j++)
                {
                    if (graph.Matrix[i, j] < int.MaxValue / 2 && i != j)
                    {
                        Line line = new Line
                        {
                            X1 = positions[i].X,
                            Y1 = positions[i].Y,
                            X2 = positions[j].X,
                            Y2 = positions[j].Y,
                            Stroke = Brushes.Black,
                            StrokeThickness = 2
                        };
                        GraphCanvas.Children.Add(line);
                        TextBlock weightText = new TextBlock
                        {
                            Text = graph.Matrix[i, j].ToString(),
                            Foreground = Brushes.Red
                        };
                        Canvas.SetLeft(weightText, (positions[i].X + positions[j].X) / 2);
                        Canvas.SetTop(weightText, (positions[i].Y + positions[j].Y) / 2);
                        GraphCanvas.Children.Add(weightText);
                    }
                }
            }

            for (int i = 0; i < graph.Vertices; i++)
            {
                Ellipse ellipse = new Ellipse
                {
                    Width = radius * 2,
                    Height = radius * 2,
                    Fill = Brushes.LightBlue,
                    Stroke = Brushes.Black,
                    StrokeThickness = 2
                };
                Canvas.SetLeft(ellipse, positions[i].X - radius);
                Canvas.SetTop(ellipse, positions[i].Y - radius);
                GraphCanvas.Children.Add(ellipse);

                TextBlock label = new TextBlock { Text = i.ToString(), FontWeight = FontWeights.Bold };
                Canvas.SetLeft(label, positions[i].X - 5);
                Canvas.SetTop(label, positions[i].Y - 10);
                GraphCanvas.Children.Add(label);
            }
        }

        private string MatrixToString(int[,] mat)
        {
            string s = "";
            int n = mat.GetLength(0);
            for (int i = 0; i < n; i++)
            {
                for (int j = 0; j < n; j++)
                {
                    s += (mat[i, j] >= int.MaxValue / 2 ? "∞" : mat[i, j].ToString()) + "\t";
                }
                s += "\n";
            }
            return s;
        }
    }
}