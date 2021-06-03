using System;
using System.Windows.Controls;
using System.Windows.Media;
using LiveCharts;
using LiveCharts.Wpf;
using ClosedXML.Excel;
using System.Collections.Generic;
using System.Linq;
using LiveCharts.Defaults;
using System.Globalization;

namespace Wpf.CartesianChart.PointShapeLine
{
    public partial class ReadExceleAndPrintGraphic : UserControl
    {
        List<double> Times = new List<double>();
        List<double> LeftEye = new List<double>();
        List<double> RightEye = new List<double>();
        int RowsCount => Times.Count;

        public SeriesCollection SeriesBase { get; set; }
        public SeriesCollection SeriesEyesDelta { get; set; }

        private double Min = 0;
        private double Max = 0;
        public SeriesCollection SeriesBoxPlot { get; set; }
        public string[] XLable { get; private set; }
        public Func<double, string> YFormatter { get; set; }


        public ReadExceleAndPrintGraphic()
        {
            InitializeComponent();

            ReadEyesDataFromExcele("ExampleEyesData.xlsx");

            SeriesBase = new SeriesCollection
            {
                new LineSeries
                {
                    Title = "Right Eye",
                    Values = new ChartValues<double> (RightEye)
                },
                new LineSeries
                {
                    Title = "Left Eye",
                    Values = new ChartValues<double> (LeftEye),
                    PointGeometry = null
                }
            };

            List<double> Delta = new List<double>();
            for (int i = 0; i < RowsCount; i++)
            {
                Delta.Add(LeftEye[i] - RightEye[i]);
            }

            SeriesEyesDelta = new SeriesCollection
            {
                new LineSeries
                {
                    Title = "Delta",
                    Values = new ChartValues<double> (Delta)
                }
            };

            var tmp = GetOhlcPointFrom(LeftEye);
            var tmp2 = GetOhlcPointFrom(RightEye);
            XLable = Times.ConvertAll<string>(delegate (double d) { return d.ToString(); }).ToArray();
            YFormatter = value => value.ToString();
            SeriesBoxPlot = new SeriesCollection
            {
                new CandleSeries
                {
                    Values = new ChartValues<OhlcPoint>
                    {
                        GetOhlcPointFrom(LeftEye),
                        GetOhlcPointFrom(RightEye),
                    }
                }
            };


            DataContext = this;
        }


        void ReadEyesDataFromExcele(string xlsxpath)
        {
            //TODO обработать отсутствие файла
            // Открываем книгу
            var workbook = new XLWorkbook(xlsxpath);
            // Берем в ней первый лист
            IXLWorksheet worksheet = workbook.Worksheets.First();
            int TimeColumnNum = 1;
            int LeftEyeColumnNum = 2;
            int RightEyeColumnNum = 3;

            //Получаем все непустыне строки в таблице
            var rows = worksheet.RangeUsed().RowsUsed().Skip(1); // И пропускаем первую строку-заголовок
            foreach (var row in rows)
            {
                Times.Add(row.Cell(TimeColumnNum).GetDouble());
                LeftEye.Add(row.Cell(LeftEyeColumnNum).GetDouble());
                RightEye.Add(row.Cell(RightEyeColumnNum).GetDouble());
            }
        }

        void UpdateBoxPlot(double min, double max)
        {
            var minIndex = Math.Max(Times.FindIndex(x => x >= min), 0);
            var maxIndex = Times.FindLastIndex(x => x <= max);
            maxIndex = maxIndex >= 0 ? Math.Max(maxIndex, minIndex) : Times.Count - 1;

            List<double> newLeftEye = LeftEye.GetRange(minIndex, maxIndex - minIndex + 1);
            List<double> newRightEye = RightEye.GetRange(minIndex, maxIndex - minIndex + 1);

            SeriesBoxPlot.First().Values = new ChartValues<OhlcPoint>
            {
            GetOhlcPointFrom(newLeftEye),
            GetOhlcPointFrom(newRightEye),
            };
        }

        static OhlcPoint GetOhlcPointFrom(List<double> list)
        {
            return new OhlcPoint(Quartile(list.ToArray(), 1), list.Max(), list.Min(), Quartile(list.ToArray(), 3));
        }

        internal static double Quartile(double[] array, int nth_quartile)
        {
            if (array.Length == 0) return 0;
            if (array.Length == 1) return 1;
            Array.Sort(array);
            double dblPercentage = 0;

            switch (nth_quartile)
            {
                case 0:
                    dblPercentage = 0; //Smallest value in the data set
                    break;
                case 1:
                    dblPercentage = 25; //First quartile (25th percentile)
                    break;
                case 2:
                    dblPercentage = 50; //Second quartile (50th percentile)
                    break;

                case 3:
                    dblPercentage = 75; //Third quartile (75th percentile)
                    break;

                case 4:
                    dblPercentage = 100; //Largest value in the data set
                    break;
                default:
                    dblPercentage = 0;
                    break;
            }


            if (dblPercentage >= 100.0d) return array[array.Length - 1];

            double position = (double)(array.Length + 1) * dblPercentage / 100.0;
            double leftNumber = 0.0d, rightNumber = 0.0d;

            double n = dblPercentage / 100.0d * (array.Length - 1) + 1.0d;

            if (position >= 1)
            {
                leftNumber = array[(int)System.Math.Floor(n) - 1];
                rightNumber = array[(int)System.Math.Floor(n)];
            }
            else
            {
                leftNumber = array[0]; // first data
                rightNumber = array[1]; // first data
            }

            if (leftNumber == rightNumber)
                return leftNumber;
            else
            {
                double part = n - System.Math.Floor(n);
                return leftNumber + part * (rightNumber - leftNumber);
            }
        }
        private void Min_TextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            //TODO улучшить обработчик исключений приведения типа
            try
            {
                Min = double.Parse(Min_TextBox.Text, CultureInfo.InvariantCulture);
            }
            catch
            {
                Min_TextBox.Clear();
                Min = 0;
            }

            UpdateBoxPlot(Min, Max);
        }

        private void Max_TextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            try
            {
                Max = Convert.ToDouble(Max_TextBox.Text);
            }
            catch
            {
                Max_TextBox.Clear();
                Max = Times.Last();
            }
            UpdateBoxPlot(Min, Max);
        }

      


        //private void Button_Click(object sender, System.Windows.RoutedEventArgs e)
        //{
        //    var metrics = ReadEyesDataFromExcele("ExampleEyesData.xlsx");
        //    MetricsDataGrid.ItemsSource = metrics;
        //}
    }

}
/*
 * public class Metric
    {
        public double Time { get; set; }
        public double LeftEye { get; set; }
        public double RigthEye { get; set; }
    }
    static IEnumerable<Metric> EnumerateMetrics(string xlsxpath)
    {
        //TODO обработать отсутствие файла
        // Открываем книгу
        var workbook = new XLWorkbook(xlsxpath);

        // Берем в ней первый лист
        IXLWorksheet worksheet = workbook.Worksheets.First();
        var totalRows = worksheet.RowsUsed().Count();
        int LeftEyeColumnNum = 2;
        int RightEyeColumnNum = 3;
        // Перебираем диапазон нужных строк
        for (int row = 2; row <= totalRows; ++row)
        {
            // По каждой строке формируем объект
            var metric = new Metric
            {
                //TODO добавить обработку исключений конвертирования типов
                Time = worksheet.Cell(row, 1).GetDouble(),
                LeftEye = worksheet.Cell(row, LeftEyeColumnNum).GetDouble(),
                RigthEye = worksheet.Cell(row, RightEyeColumnNum).GetDouble(),
            };
            // И возвращаем его
            yield return metric;
        }
        yield break;
    }
    */
