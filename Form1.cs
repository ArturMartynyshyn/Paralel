using System;
using System.Diagnostics;
using System.Drawing;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace lab6
{
    public partial class Form1 : Form
    {
        private readonly Button btnStart = new Button();
        private readonly DataGridView table = new DataGridView();
        private readonly Label lblStatus = new Label();

        private const int RepeatCount = 5;

        public Form1()
        {
            InitializeComponent();
            SetupInterface();
        }

        private void SetupInterface()
        {
            Text = "Лабораторна №6 — Варіант 3";
            ClientSize = new Size(900, 450);
            MinimumSize = new Size(800, 400);
            StartPosition = FormStartPosition.CenterScreen;

            btnStart.Text = "Запустити дослідження";
            btnStart.SetBounds(20, 20, 240, 40);
            btnStart.Click += BtnStart_Click;

            lblStatus.Text = "Піднесення до квадрату всіх елементів матриці";
            lblStatus.SetBounds(20, 75, 850, 45);
            lblStatus.Anchor =
                AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;

            table.SetBounds(20, 130, 860, 300);
            table.Anchor = AnchorStyles.Top | AnchorStyles.Bottom
                         | AnchorStyles.Left | AnchorStyles.Right;

            table.ReadOnly = true;
            table.AllowUserToAddRows = false;
            table.AllowUserToDeleteRows = false;
            table.RowHeadersVisible = false;
            table.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
            table.SelectionMode = DataGridViewSelectionMode.FullRowSelect;

            table.Columns.Add("Size", "Розмір матриці");
            table.Columns.Add("Sequential", "Послідовно, мс");
            table.Columns.Add("Parallel", "Паралельно, мс");
            table.Columns.Add("Speedup", "Прискорення");
            table.Columns.Add("Equal", "Результати збігаються");

            Controls.Add(btnStart);
            Controls.Add(lblStatus);
            Controls.Add(table);
        }

        private async void BtnStart_Click(object sender, EventArgs e)
        {
            btnStart.Enabled = false;
            table.Rows.Clear();

            int[] sizes = { 100, 500, 1000, 2000, 3000, 10000 };

            try
            {
                foreach (int size in sizes)
                {
                    lblStatus.Text =
                        $"Обчислення матриці {size} × {size}...";

                    ExperimentResult result =
                        await Task.Run(() => RunExperiment(size));

                    table.Rows.Add(
                        $"{size} × {size}",
                        result.SequentialTime.ToString("F4"),
                        result.ParallelTime.ToString("F4"),
                        result.Speedup.ToString("F2") + "×",
                        result.Equal ? "Так" : "Ні"
                    );
                }

                lblStatus.Text =
                    "Готово. Прискорення = Tпосл / Tпарал. " +
                    "Значення > 1 означає, що паралельний метод швидший.";
            }
            catch (Exception ex)
            {
                lblStatus.Text = "Під час обчислень виникла помилка.";
                MessageBox.Show(
                    ex.Message,
                    "Помилка",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error
                );
            }
            finally
            {
                btnStart.Enabled = true;
            }
        }

        private ExperimentResult RunExperiment(int size)
        {
            double[,] matrix = CreateMatrix(size);
            double[,] sequentialResult = new double[size, size];
            double[,] parallelResult = new double[size, size];

            SquareSequential(matrix, sequentialResult);
            SquareParallel(matrix, parallelResult);

            double[] sequentialTimes = new double[RepeatCount];
            double[] parallelTimes = new double[RepeatCount];

            Action sequentialAction =
                () => SquareSequential(matrix, sequentialResult);

            Action parallelAction =
                () => SquareParallel(matrix, parallelResult);

            for (int k = 0; k < RepeatCount; k++)
            {
                if (k % 2 == 0)
                {
                    sequentialTimes[k] = Measure(sequentialAction);
                    parallelTimes[k] = Measure(parallelAction);
                }
                else
                {
                    parallelTimes[k] = Measure(parallelAction);
                    sequentialTimes[k] = Measure(sequentialAction);
                }
            }

            Array.Sort(sequentialTimes);
            Array.Sort(parallelTimes);

            double sequentialTime = sequentialTimes[RepeatCount / 2];
            double parallelTime = parallelTimes[RepeatCount / 2];

            return new ExperimentResult
            {
                SequentialTime = sequentialTime,
                ParallelTime = parallelTime,
                Speedup = parallelTime > 0
                    ? sequentialTime / parallelTime
                    : double.NaN,
                Equal = CheckResults(
                    matrix, sequentialResult, parallelResult)
            };
        }

        private double[,] CreateMatrix(int size)
        {
            double[,] matrix = new double[size, size];
            Random random = new Random(42);

            for (int i = 0; i < size; i++)
            {
                for (int j = 0; j < size; j++)
                {
                    matrix[i, j] = random.Next(-100, 101);
                }
            }

            return matrix;
        }

        private void SquareSequential(double[,] matrix, double[,] result)
        {
            int rows = matrix.GetLength(0);
            int columns = matrix.GetLength(1);

            for (int i = 0; i < rows; i++)
            {
                for (int j = 0; j < columns; j++)
                {
                    result[i, j] = matrix[i, j] * matrix[i, j];
                }
            }
        }

        private void SquareParallel(double[,] matrix, double[,] result)
        {
            int rows = matrix.GetLength(0);
            int columns = matrix.GetLength(1);

            Parallel.For(0, rows, i =>
            {
                for (int j = 0; j < columns; j++)
                {
                    result[i, j] = matrix[i, j] * matrix[i, j];
                }
            });
        }

        private double Measure(Action action)
        {
            Stopwatch stopwatch = Stopwatch.StartNew();

            action();

            stopwatch.Stop();
            return stopwatch.Elapsed.TotalMilliseconds;
        }

        private bool CheckResults(
            double[,] matrix,
            double[,] sequential,
            double[,] parallel)
        {
            for (int i = 0; i < matrix.GetLength(0); i++)
            {
                for (int j = 0; j < matrix.GetLength(1); j++)
                {
                    double expected = matrix[i, j] * matrix[i, j];

                    if (sequential[i, j] != expected ||
                        parallel[i, j] != expected)
                    {
                        return false;
                    }
                }
            }

            return true;
        }

        private class ExperimentResult
        {
            public double SequentialTime { get; set; }
            public double ParallelTime { get; set; }
            public double Speedup { get; set; }
            public bool Equal { get; set; }
        }
    }
}